using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.PrintSizes;

/// <summary>
/// Data Transfer Object for creating new print sizes.
/// Used by admin to add new size options.
/// </summary>
public class CreatePrintSizeDto
{
    /// <summary>
    /// Unique size code for this print size
    /// </summary>
    [Required(ErrorMessage = "Size code is required")]
    [MaxLength(20, ErrorMessage = "Size code cannot exceed 20 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Size code can only contain letters, numbers, and underscores")]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for customers
    /// </summary>
    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Physical dimensions
    /// </summary>
    [Required(ErrorMessage = "Width is required")]
    [Range(0.1, 100, ErrorMessage = "Width must be between 0.1 and 100")]
    public decimal Width { get; set; }

    [Required(ErrorMessage = "Height is required")]
    [Range(0.1, 100, ErrorMessage = "Height must be between 0.1 and 100")]
    public decimal Height { get; set; }

    /// <summary>
    /// Initial price for this size
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999.99, ErrorMessage = "Price must be between $0.01 and $999.99")]
    public decimal Price { get; set; }

    /// <summary>
    /// Pixel requirements for quality validation
    /// </summary>
    [Range(100, 10000, ErrorMessage = "Minimum width must be between 100 and 10000 pixels")]
    public int MinWidth { get; set; }

    [Range(100, 10000, ErrorMessage = "Minimum height must be between 100 and 10000 pixels")]
    public int MinHeight { get; set; }

    [Range(100, 10000, ErrorMessage = "Recommended width must be between 100 and 10000 pixels")]
    public int RecommendedWidth { get; set; }

    [Range(100, 10000, ErrorMessage = "Recommended height must be between 100 and 10000 pixels")]
    public int RecommendedHeight { get; set; }
}