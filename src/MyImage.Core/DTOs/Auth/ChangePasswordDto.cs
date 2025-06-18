using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Auth;

/// <summary>
/// Data Transfer Object for password change requests.
/// Used when authenticated users want to update their password.
/// Requires current password for security verification.
/// </summary>
public class ChangePasswordDto
{
    /// <summary>
    /// User's current password for verification
    /// Prevents unauthorized password changes if account is compromised
    /// Validated against stored BCrypt hash
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password with same complexity requirements as registration
    /// Must meet security standards and be different from current password
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number, and special character")] //Fixed malformed regex pattern
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of new password to prevent typos
    /// Must match NewPassword exactly
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}