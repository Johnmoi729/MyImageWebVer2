using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for order creation response.
/// Provides immediate confirmation and reference information to customer.
/// </summary>
public class OrderCreatedDto
{
    /// <summary>
    /// Unique order identifier for API operations
    /// Used for order tracking and updates
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable order number for customer reference
    /// Format: ORD-YYYY-NNNNNNN
    /// Used in customer communications and support
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current order status after creation
    /// Typically "pending" awaiting payment verification
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total amount charged or to be paid
    /// Locked at time of order creation for price integrity
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Payment processing status
    /// "processing" for credit cards, "pending" for branch payments
    /// </summary>
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// When the order was created
    /// Used for customer reference and order tracking
    /// </summary>
    public DateTime CreatedAt { get; set; }
}