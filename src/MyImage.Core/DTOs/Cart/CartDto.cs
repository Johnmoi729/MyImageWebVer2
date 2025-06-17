using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for complete shopping cart display.
/// Contains all information needed to render cart contents and calculate totals.
/// </summary>
public class CartDto
{
    /// <summary>
    /// Collection of cart items - each represents one photo with its print selections
    /// Ordered by most recently added for better user experience
    /// </summary>
    public List<CartItemDto> Items { get; set; } = new();

    /// <summary>
    /// Cart summary with totals and statistics
    /// Pre-calculated for immediate display without frontend computation
    /// </summary>
    public CartSummaryDto Summary { get; set; } = new();
}