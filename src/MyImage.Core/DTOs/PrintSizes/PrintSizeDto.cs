using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.PrintSizes;

/// <summary>
/// Data Transfer Object for print size display to customers.
/// Contains current pricing and specifications.
/// </summary>
public class PrintSizeDto
{
    /// <summary>
    /// Unique identifier for API operations
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Size code for cart operations
    /// Example: "4x6", "5x7", "8x10"
    /// </summary>
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Display name for customer interface
    /// Example: "Standard 4×6", "Classic 5×7"
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Physical dimensions for reference
    /// </summary>
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Unit { get; set; } = "inches";

    /// <summary>
    /// Current price for this size
    /// </summary>
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether this size is currently available
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Pixel requirements for quality guidance
    /// </summary>
    public int MinWidth { get; set; }
    public int MinHeight { get; set; }
    public int RecommendedWidth { get; set; }
    public int RecommendedHeight { get; set; }
}