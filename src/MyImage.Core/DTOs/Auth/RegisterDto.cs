using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Auth;

/// <summary>
/// Data Transfer Object for user registration requests.
/// This DTO implements the registration requirement where users provide basic information
/// and receive a generated User ID upon successful registration.
/// 
/// The validation attributes ensure data quality before reaching business logic,
/// providing immediate feedback to the frontend about data requirements.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// User's email address - must be unique and valid format
    /// Used for login authentication and order notifications
    /// Email uniqueness is enforced at the business logic layer
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's chosen password with security requirements
    /// Must meet minimum complexity standards for account security
    /// Will be hashed using BCrypt before storage
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number, and special character")] //Fixed malformed regex pattern
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation to prevent typos during registration
    /// Must match the Password field exactly
    /// Validation handled by custom validator in business logic
    /// </summary>
    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// User's first name for personalization and order processing
    /// Required for complete user profile and shipping purposes
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for personalization and order processing
    /// Required for complete user profile and shipping purposes
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public string LastName { get; set; } = string.Empty;
}