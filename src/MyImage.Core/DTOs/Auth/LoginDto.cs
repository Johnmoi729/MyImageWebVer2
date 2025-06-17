using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Auth;

/// <summary>
/// Data Transfer Object for user login requests.
/// Supports dual authentication modes: email or User ID with password.
/// This flexibility addresses the requirement that users receive a User ID for login
/// while also allowing modern email-based authentication.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Login identifier - can be either email address or User ID
    /// Examples: "user@example.com" or "USR-2024-001234"
    /// Business logic determines which type and validates accordingly
    /// </summary>
    [Required(ErrorMessage = "Email or User ID is required")]
    [MaxLength(255, ErrorMessage = "Identifier cannot exceed 255 characters")]
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// User's password for authentication
    /// Validated against the BCrypt hash stored in the database
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Optional flag to extend session duration for "remember me" functionality
    /// When true, JWT token expires in 30 days instead of 24 hours
    /// Future enhancement - currently not implemented in MVP
    /// </summary>
    public bool RememberMe { get; set; } = false;
}