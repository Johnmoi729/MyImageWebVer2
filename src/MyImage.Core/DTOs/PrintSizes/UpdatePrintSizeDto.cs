using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.PrintSizes;

/// <summary>
/// Data Transfer Object for updating print size pricing.
/// Used by admin to modify existing print sizes.
/// </summary>
public class UpdatePrintSizeDto
{
    /// <summary>
    /// New price for this print size
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999.99, ErrorMessage = "Price must be between $0.01 and $999.99")]
    public decimal Price { get; set; }

    /// <summary>
    /// Whether this size should be active for customers
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display sort order for customer interface
    /// </summary>
    [Range(1, 100, ErrorMessage = "Sort order must be between 1 and 100")]
    public int SortOrder { get; set; } = 1;
}

