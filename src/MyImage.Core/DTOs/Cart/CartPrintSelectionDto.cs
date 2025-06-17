using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for print selections within cart items.
/// Includes pricing information locked at time of cart addition.
/// </summary>
public class CartPrintSelectionDto
{
    /// <summary>
    /// Print size code for reference and updates
    /// Example: "4x6", "5x7", "8x10"
    /// </summary>
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable size name for display
    /// Example: "Standard 4×6", "Classic 5×7"
    /// </summary>
    public string SizeName { get; set; } = string.Empty;

    /// <summary>
    /// Number of copies for this size
    /// Can be modified through cart update operations
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit locked when added to cart
    /// Protects customer from price changes during shopping session
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total cost for this selection (quantity × unitPrice)
    /// Calculated and displayed for user clarity
    /// </summary>
    public decimal LineTotal { get; set; }
}