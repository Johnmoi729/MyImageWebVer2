using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for individual print size selections within a cart item.
/// Represents one size choice with its quantity for a specific photo.
/// </summary>
public class PrintSelectionDto
{
    /// <summary>
    /// Print size code matching available PrintSize entities
    /// Example: "4x6", "5x7", "8x10", "wallet"
    /// Validated against active print sizes in the system
    /// </summary>
    [Required(ErrorMessage = "Size code is required")]
    [MaxLength(20, ErrorMessage = "Size code cannot exceed 20 characters")]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of copies requested for this print size
    /// Must be positive integer, typically 1-100 for reasonable orders
    /// </summary>
    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; }
}