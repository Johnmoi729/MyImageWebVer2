using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Auth;

/// <summary>
/// Data Transfer Object for successful authentication responses.
/// Contains user information and JWT token for session management.
/// This response fulfills the requirement to provide User ID to users.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// User's generated ID provided during registration
    /// Format: USR-YYYY-NNNNNN (e.g., USR-2024-001234)
    /// This is what users can use for future logins
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for verification and display
    /// Useful for confirming correct account during login
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for UI personalization
    /// Used in welcome messages and interface customization
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for complete identification
    /// Combined with FirstName for full name display
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's role for authorization purposes
    /// Values: "customer" or "admin"
    /// Determines access to administrative features
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// JWT token for API authentication
    /// Must be included in Authorization header for protected endpoints
    /// Contains encrypted user identity and permissions
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds from issue
    /// Typically 86400 (24 hours) for standard sessions
    /// Frontend can use this to refresh tokens before expiration
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type for proper Authorization header formatting
    /// Always "Bearer" for JWT tokens
    /// Frontend uses this to construct: "Authorization: Bearer {token}"
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
}