using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for order history display.
/// Contains summary information for customer order list.
/// </summary>
public class OrderSummaryDto
{
    /// <summary>
    /// Order identifier for detailed view navigation
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable order number for customer reference
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Current order status for customer tracking
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total order amount for reference
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of unique photos in this order
    /// </summary>
    public int PhotoCount { get; set; }

    /// <summary>
    /// Total number of prints across all photos
    /// </summary>
    public int PrintCount { get; set; }

    /// <summary>
    /// Payment method used for this order
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
}