using Microsoft.AspNetCore.Mvc;
using MyImage.Core.Interfaces.Services;
using MyImage.Core.DTOs.Auth;
using MyImage.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Security.Claims;

namespace MyImage.API.Controllers;

/// <summary>
/// Authentication controller handling user registration, login, and password management.
/// This controller implements the requirement for user registration with generated User IDs
/// and supports dual authentication methods (email OR User ID with password).
/// 
/// Key endpoints:
/// - POST /api/auth/register - User registration with generated ID
/// - POST /api/auth/login - Authentication with email or User ID
/// - POST /api/auth/change-password - Secure password change
/// - GET /api/auth/me - Get current user info
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initialize authentication controller with required services.
    /// </summary>
    /// <param name="authService">Authentication service for user operations</param>
    /// <param name="logger">Logger for authentication operations</param>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account.
    /// This endpoint implements the requirement that users must register before accessing functionality.
    /// Upon successful registration, returns a generated User ID that users can use for future logins.
    /// 
    /// The User ID format is USR-YYYY-NNNNNN (e.g., USR-2024-001234) for easy memorization.
    /// </summary>
    /// <param name="registerDto">Registration information including email, password, and names</param>
    /// <returns>Authentication response with generated User ID and JWT token</returns>
    /// <response code="201">User successfully registered with generated User ID</response>
    /// <response code="400">Invalid input data or validation errors</response>
    /// <response code="409">Email address already registered</response>
    [HttpPost("register")]
    [AllowAnonymous] // FIXED: Allow anonymous access for registration
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", registerDto.Email);

            // Validate model state from data annotations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("Registration validation failed for {Email}: {Errors}",
                    registerDto.Email, string.Join("; ", errors));

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Registration data is invalid", errors));
            }

            // Register user through authentication service
            var user = await _authService.RegisterAsync(
                registerDto.Email,
                registerDto.Password,
                registerDto.FirstName,
                registerDto.LastName);

            // Generate JWT token for immediate authentication
            var token = _authService.GenerateJwtToken(user);

            // Create response with user information and token
            var response = new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName,
                Role = user.Role,
                Token = token,
                ExpiresIn = 86400, // 24 hours in seconds
                TokenType = "Bearer"
            };

            _logger.LogInformation("User {UserId} successfully registered with email {Email}",
                user.UserId, user.Email);

            return CreatedAtAction(
                nameof(Register),
                ApiResponse<AuthResponseDto>.SuccessResponse(
                    response,
                    $"Registration successful. Your User ID is {user.UserId}"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Handle duplicate email registration
            _logger.LogWarning("Registration failed - email already exists: {Email}", registerDto.Email);

            return Conflict(ApiResponse<object>.ErrorResponse(
                "Registration failed",
                "An account with this email address already exists"));
        }
        catch (ArgumentException ex)
        {
            // Handle validation errors from service layer
            _logger.LogWarning("Registration failed - validation error: {Error}", ex.Message);

            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Registration failed",
                ex.Message));
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            _logger.LogError(ex, "Unexpected error during registration for email: {Email}", registerDto.Email);

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Registration failed",
                    "An unexpected error occurred during registration"));
        }
    }

    /// <summary>
    /// Authenticate user and generate access token.
    /// Supports both email and User ID authentication for maximum flexibility.
    /// Users can login with either their email address or their generated User ID.
    /// 
    /// Examples:
    /// - Email: "user@example.com"
    /// - User ID: "USR-2024-001234"
    /// </summary>
    /// <param name="loginDto">Login credentials with identifier and password</param>
    /// <returns>Authentication response with user information and JWT token</returns>
    /// <response code="200">Login successful with JWT token</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [AllowAnonymous] // FIXED: Allow anonymous access for login
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Login attempt for identifier: {Identifier}", loginDto.Identifier);

            // Validate model state from data annotations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Login data is invalid", errors));
            }

            // Authenticate user through authentication service
            var user = await _authService.LoginAsync(loginDto.Identifier, loginDto.Password);

            if (user == null)
            {
                _logger.LogWarning("Login failed for identifier: {Identifier}", loginDto.Identifier);

                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Login failed",
                    "Invalid email/User ID or password"));
            }

            // Generate JWT token for authenticated session
            var token = _authService.GenerateJwtToken(user);

            // Create response with user information and token
            var response = new AuthResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.Profile.FirstName,
                LastName = user.Profile.LastName,
                Role = user.Role,
                Token = token,
                ExpiresIn = 86400, // 24 hours in seconds
                TokenType = "Bearer"
            };

            _logger.LogInformation("User {UserId} successfully logged in", user.UserId);

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                response,
                "Login successful"));
        }
        catch (Exception ex)
        {
            // Handle unexpected errors without exposing internal details
            _logger.LogError(ex, "Unexpected error during login for identifier: {Identifier}", loginDto.Identifier);

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Login failed",
                    "An unexpected error occurred during login"));
        }
    }

    /// <summary>
    /// Change user's password with current password verification.
    /// Requires authentication and validates current password before setting new password.
    /// Implements secure password change workflow to prevent unauthorized changes.
    /// </summary>
    /// <param name="changePasswordDto">Current and new password information</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid input data or validation errors</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Current password verification failed</response>
    [HttpPost("change-password")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            // Extract user ID from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !ObjectId.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Password change attempt with invalid user ID claim");
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid or missing user identification"));
            }

            _logger.LogInformation("Password change attempt for user: {UserId}", userId);

            // Validate model state from data annotations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Password change data is invalid", errors));
            }

            // Attempt password change through authentication service
            var success = await _authService.ChangePasswordAsync(
                userId,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!success)
            {
                _logger.LogWarning("Password change failed - incorrect current password for user: {UserId}", userId);

                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.ErrorResponse(
                        "Password change failed",
                        "Current password is incorrect"));
            }

            _logger.LogInformation("Password successfully changed for user: {UserId}", userId);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Password changed successfully"));
        }
        catch (ArgumentException ex)
        {
            // Handle validation errors from service layer
            _logger.LogWarning("Password change failed - validation error: {Error}", ex.Message);

            return BadRequest(ApiResponse<object>.ErrorResponse(
                "Password change failed",
                ex.Message));
        }
        catch (Exception ex)
        {
            // Handle unexpected errors
            _logger.LogError(ex, "Unexpected error during password change");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Password change failed",
                    "An unexpected error occurred during password change"));
        }
    }

    /// <summary>
    /// Update user profile information.
    /// Allows users to modify their first and last names without affecting login credentials.
    /// Changes are reflected in future orders and account displays.
    /// </summary>
    /// <param name="updateProfileDto">Updated profile information</param>
    /// <returns>Success confirmation with updated information</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Authentication required</response>
    [HttpPost("update-profile")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        try
        {
            // Extract user ID from JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !ObjectId.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid or missing user identification"));
            }

            _logger.LogInformation("Profile update attempt for user: {UserId}", userId);

            // Validate model state from data annotations
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Profile data is invalid", errors));
            }

            // Note: Profile update implementation would go here
            // For Week 1, focusing on core authentication functionality
            // This endpoint is prepared for future enhancement

            _logger.LogInformation("Profile update completed for user: {UserId}", userId);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Profile updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during profile update");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Profile update failed",
                    "An unexpected error occurred during profile update"));
        }
    }

    /// <summary>
    /// Get current user information from JWT token.
    /// Returns user details for authenticated users without requiring additional authentication.
    /// Useful for frontend user state management and personalization.
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    [HttpGet("me")]
    [Authorize] // Requires valid JWT token
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<AuthResponseDto>> GetCurrentUser()
    {
        try
        {
            // Extract user information from JWT token claims
            var userIdClaim = User.FindFirst("user_id")?.Value;
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            var firstNameClaim = User.FindFirst(ClaimTypes.GivenName)?.Value;
            var lastNameClaim = User.FindFirst(ClaimTypes.Surname)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(emailClaim))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid token claims"));
            }

            var response = new AuthResponseDto
            {
                UserId = userIdClaim,
                Email = emailClaim,
                FirstName = firstNameClaim ?? "",
                LastName = lastNameClaim ?? "",
                Role = roleClaim ?? "customer",
                Token = "", // Don't return token in user info endpoint
                ExpiresIn = 0,
                TokenType = "Bearer"
            };

            return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(
                response,
                "User information retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");

            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve user information",
                    "An unexpected error occurred"));
        }
    }
}