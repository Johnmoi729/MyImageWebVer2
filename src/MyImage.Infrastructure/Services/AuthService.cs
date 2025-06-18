using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// Service implementation for authentication operations.
/// Handles user registration, login, and JWT token management.
/// Implements the requirement for user registration with generated User IDs
/// and supports dual authentication (email OR User ID with password).
/// 
/// Key security features:
/// - BCrypt password hashing with salt
/// - JWT tokens with configurable expiration
/// - Secure password change workflow
/// - User ID generation with collision handling
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    // JWT configuration cached for performance
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationHours;

    /// <summary>
    /// Initialize authentication service with dependencies and JWT configuration.
    /// Caches JWT settings for efficient token generation and validation.
    /// </summary>
    /// <param name="userRepository">User data access repository</param>
    /// <param name="configuration">Application configuration for JWT settings</param>
    /// <param name="logger">Logger for authentication operations</param>
    /// <exception cref="InvalidOperationException">Thrown if JWT configuration is missing</exception>
    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;

        // Load and validate JWT configuration at startup
        _jwtSecret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        _jwtIssuer = configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer not configured");
        _jwtAudience = configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException("JWT Audience not configured");
        _jwtExpirationHours = int.TryParse(configuration["JwtSettings:ExpirationHours"], out var expHours) ? expHours : 24;

        // Validate JWT secret length for security
        if (_jwtSecret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret must be at least 32 characters long");
        }

        _logger.LogInformation("AuthService initialized with JWT expiration: {Hours} hours", _jwtExpirationHours);
    }

    /// <summary>
    /// Register new user with generated User ID.
    /// This method implements the requirement that "user id and password will be provided to the user"
    /// by creating a unique, human-readable User ID during registration.
    /// 
    /// The registration process:
    /// 1. Validates email uniqueness
    /// 2. Hashes password using BCrypt with salt
    /// 3. Generates unique User ID (USR-YYYY-NNNNNN format)
    /// 4. Creates user record in database
    /// 5. Returns user with generated credentials
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">Plain text password</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <returns>Created user with generated User ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if email already exists</exception>
    public async Task<User> RegisterAsync(string email, string password, string firstName, string lastName)
    {
        try
        {
            _logger.LogInformation("Starting user registration for email: {Email}", email);

            // Validate email format (additional validation beyond DTO)
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }

            // Validate password strength (additional validation beyond DTO)
            if (!IsValidPassword(password))
            {
                throw new ArgumentException("Password does not meet security requirements", nameof(password));
            }

            // Check if email already exists (repository will also check, but early validation improves UX)
            if (await _userRepository.EmailExistsAsync(email))
            {
                _logger.LogWarning("Registration attempted with existing email: {Email}", email);
                throw new InvalidOperationException($"An account with email {email} already exists");
            }

            // Hash password using BCrypt with automatic salt generation
            // BCrypt automatically handles salt generation and combines it with the hash
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

            // Create user entity with provided information
            var user = new User
            {
                Email = email.ToLowerInvariant(), // Normalize email for consistency
                PasswordHash = passwordHash,
                Profile = new UserProfile
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim()
                },
                Role = "customer", // Default role for new registrations
                Stats = new UserStats(), // Initialize with zero values
                Metadata = new UserMetadata
                {
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            // Create user in repository (User ID will be generated automatically)
            var createdUser = await _userRepository.CreateAsync(user);

            _logger.LogInformation("Successfully registered user {UserId} with email {Email}",
                createdUser.UserId, email);

            // Return user without password hash for security
            createdUser.PasswordHash = string.Empty;
            return createdUser;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException || ex is ArgumentException))
        {
            _logger.LogError(ex, "Failed to register user with email: {Email}", email);
            throw new InvalidOperationException("Registration failed due to an internal error", ex);
        }
    }

    /// <summary>
    /// Authenticate user and generate JWT token.
    /// Supports both email and User ID authentication for maximum flexibility.
    /// 
    /// The login process:
    /// 1. Determines if identifier is email or User ID
    /// 2. Retrieves user from appropriate lookup method
    /// 3. Verifies password against BCrypt hash
    /// 4. Updates last login timestamp
    /// 5. Returns authenticated user for token generation
    /// </summary>
    /// <param name="identifier">Email address or User ID</param>
    /// <param name="password">Plain text password</param>
    /// <returns>User entity if authentication successful, null if failed</returns>
    public async Task<User?> LoginAsync(string identifier, string password)
    {
        try
        {
            _logger.LogDebug("Login attempt for identifier: {Identifier}", identifier);

            // Determine if identifier is email or User ID based on format
            User? user;
            if (IsEmailFormat(identifier))
            {
                user = await _userRepository.GetByEmailAsync(identifier);
                _logger.LogDebug("Login attempt using email lookup");
            }
            else
            {
                user = await _userRepository.GetByUserIdAsync(identifier);
                _logger.LogDebug("Login attempt using User ID lookup");
            }

            // Check if user exists and is active
            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found for identifier {Identifier}", identifier);
                return null;
            }

            if (!user.Metadata.IsActive)
            {
                _logger.LogWarning("Login failed: user account is inactive for {UserId}", user.UserId);
                return null;
            }

            // Verify password against BCrypt hash
            // BCrypt.Verify handles the salt extraction and comparison automatically
            var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: invalid password for user {UserId}", user.UserId);
                return null;
            }

            // Update last login timestamp for activity tracking
            await _userRepository.UpdateLastLoginAsync(user.Id);

            _logger.LogInformation("Successful login for user {UserId}", user.UserId);

            // Clear password hash before returning for security
            user.PasswordHash = string.Empty;
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for identifier: {Identifier}", identifier);
            return null; // Don't expose internal errors to prevent information disclosure
        }
    }

    /// <summary>
    /// Generate JWT token for authenticated user.
    /// Creates token with user claims and configurable expiration.
    /// 
    /// Token includes claims for:
    /// - User ID (ObjectId for internal operations)
    /// - User readable ID (for display and reference)
    /// - Email address
    /// - User role (for authorization)
    /// - Full name (for personalization)
    /// </summary>
    /// <param name="user">Authenticated user entity</param>
    /// <returns>JWT token string</returns>
    public string GenerateJwtToken(User user)
    {
        try
        {
            _logger.LogDebug("Generating JWT token for user {UserId}", user.UserId);

            // Create claims for the user
            var claims = new List<Claim>
            {
                // User ObjectId for internal operations and authorization
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                
                // Human-readable User ID for display purposes
                new Claim("user_id", user.UserId),
                
                // Email for communication and verification
                new Claim(ClaimTypes.Email, user.Email),
                
                // Role for authorization decisions
                new Claim(ClaimTypes.Role, user.Role),
                
                // Full name for UI personalization
                new Claim(ClaimTypes.Name, user.Profile.FullName),
                
                // Individual name components for flexibility
                new Claim(ClaimTypes.GivenName, user.Profile.FirstName),
                new Claim(ClaimTypes.Surname, user.Profile.LastName),
                
                // Token issued time for validation
                new Claim(JwtRegisteredClaimNames.Iat,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                
                // Unique token identifier for revocation (future enhancement)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Create symmetric security key from configuration
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Set token expiration
            var expiration = DateTime.UtcNow.AddHours(_jwtExpirationHours);

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                Issuer = _jwtIssuer,
                Audience = _jwtAudience,
                SigningCredentials = credentials
            };

            // Generate and write token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogDebug("Generated JWT token for user {UserId}, expires at {Expiration}",
                user.UserId, expiration);

            return tokenString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT token for user {UserId}", user.UserId);
            throw new InvalidOperationException("Token generation failed", ex);
        }
    }

    /// <summary>
    /// Validate and decode JWT token.
    /// Used by middleware for request authentication.
    /// Extracts user ObjectId from valid tokens for authorization.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ObjectId if token valid, null if invalid</returns>
    public async Task<ObjectId?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            // Set validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            // Validate token and extract claims
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Extract user ObjectId from claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && ObjectId.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogDebug("Token validation successful for user {UserId}", userId);
                return userId;
            }

            _logger.LogWarning("Token validation failed: invalid user ID claim");
            return null;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogDebug("Token validation failed: token expired");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// Change user's password with current password verification.
    /// Implements secure password change workflow requiring current password.
    /// 
    /// Security measures:
    /// - Requires current password verification
    /// - Validates new password strength
    /// - Uses BCrypt for secure hashing
    /// - Prevents password reuse (same as current)
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <param name="currentPassword">Current password for verification</param>
    /// <param name="newPassword">New password to set</param>
    /// <returns>True if password changed successfully, false if current password invalid</returns>
    public async Task<bool> ChangePasswordAsync(ObjectId userId, string currentPassword, string newPassword)
    {
        try
        {
            _logger.LogDebug("Password change request for user {UserId}", userId);

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Password change failed: user {UserId} not found", userId);
                return false;
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Password change failed: incorrect current password for user {UserId}", userId);
                return false;
            }

            // Validate new password strength
            if (!IsValidPassword(newPassword))
            {
                throw new ArgumentException("New password does not meet security requirements");
            }

            // Prevent password reuse (don't allow same password)
            if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
            {
                throw new ArgumentException("New password must be different from current password");
            }

            // Hash new password
            var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);

            // Update password in database
            user.PasswordHash = newPasswordHash;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password successfully changed for user {UserId}", userId);
            return true;
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            _logger.LogError(ex, "Failed to change password for user {UserId}", userId);
            throw new InvalidOperationException("Password change failed due to an internal error", ex);
        }
    }

    /// <summary>
    /// Validate email format using basic regex pattern.
    /// Additional validation beyond standard EmailAddress attribute.
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if email format is valid</returns>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Use .NET's built-in email validation
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determine if identifier is in email format.
    /// Used to choose appropriate lookup method during login.
    /// </summary>
    /// <param name="identifier">Login identifier</param>
    /// <returns>True if identifier appears to be an email</returns>
    private static bool IsEmailFormat(string identifier)
    {
        return !string.IsNullOrWhiteSpace(identifier) && identifier.Contains('@');
    }

    /// <summary>
    /// Validate password strength requirements.
    /// Ensures passwords meet minimum security standards.
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if password meets requirements</returns>
    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // Check for required character types
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => "@$!%*?&".Contains(c)); // Match DTO validation

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}