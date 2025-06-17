using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for cart summary and totals.
/// Provides quick overview of cart contents for checkout decisions.
/// </summary>
public class CartSummaryDto
{
    /// <summary>
    /// Total number of unique photos in cart
    /// Each photo counts once regardless of print sizes selected
    /// </summary>
    public int TotalPhotos { get; set; }

    /// <summary>
    /// Total number of individual prints across all photos and sizes
    /// Sum of all quantities across all selections
    /// </summary>
    public int TotalPrints { get; set; }

    /// <summary>
    /// Subtotal before tax
    /// Sum of all photo totals in the cart
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Estimated tax based on default rate
    /// Actual tax calculated during checkout with shipping address
    /// </summary>
    public decimal Tax { get; set; }

    /// <summary>
    /// Estimated total including tax
    /// Final amount calculated during checkout process
    /// </summary>
    public decimal Total { get; set; }
}