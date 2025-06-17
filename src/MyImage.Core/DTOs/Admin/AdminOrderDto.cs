using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Admin;

/// <summary>
/// Data Transfer Object for admin order management display.
/// Extended order information for administrative operations.
/// </summary>
public class AdminOrderDto
{
    /// <summary>
    /// Order identifier for operations
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer information for order processing
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer email for communication
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Current order status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Payment method and status
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;

    /// <summary>
    /// Order totals for reference
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Order timing information
    /// </summary>
    public DateTime OrderDate { get; set; }
    public DateTime? PaymentVerifiedDate { get; set; }

    /// <summary>
    /// Order contents summary
    /// </summary>
    public int PhotoCount { get; set; }
    public int PrintCount { get; set; }
}