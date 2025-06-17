using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Admin;

/// <summary>
/// Data Transfer Object for completing orders.
/// Contains shipping information and triggers photo cleanup.
/// </summary>
public class CompleteOrderDto
{
    /// <summary>
    /// Date when order was shipped
    /// </summary>
    [Required(ErrorMessage = "Shipping date is required")]
    public DateTime ShippingDate { get; set; }

    /// <summary>
    /// Optional tracking number for customer reference
    /// </summary>
    [MaxLength(100, ErrorMessage = "Tracking number cannot exceed 100 characters")]
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Optional notes about shipping or completion
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
}