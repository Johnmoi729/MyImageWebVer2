using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Auth;

/// <summary>
/// Data Transfer Object for user profile updates.
/// Allows users to modify their personal information without affecting login credentials.
/// Changes are reflected in future orders and account displays.
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// Updated first name with same validation as registration
    /// Changes appear in future orders and account information
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First name can only contain letters, spaces, hyphens, and apostrophes")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Updated last name with same validation as registration
    /// Changes appear in future orders and account information
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last name can only contain letters, spaces, hyphens, and apostrophes")]
    public string LastName { get; set; } = string.Empty;
}