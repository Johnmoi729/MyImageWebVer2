using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Admin;

/// <summary>
/// Data Transfer Object for updating order status.
/// Used by admin to progress orders through workflow.
/// </summary>
public class UpdateOrderStatusDto
{
    /// <summary>
    /// New status for the order
    /// Values: "payment_verified", "processing", "printed", "shipped", "completed"
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(payment_verified|processing|printed|shipped|completed)$",
        ErrorMessage = "Invalid status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Optional notes about the status change
    /// Used for audit trail and special instructions
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string Notes { get; set; } = string.Empty;
}