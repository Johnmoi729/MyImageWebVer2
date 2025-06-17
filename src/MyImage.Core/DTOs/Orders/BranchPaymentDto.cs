using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Orders;

/// <summary>
/// Data Transfer Object for branch payment option.
/// Alternative payment method for customers preferring in-person payment.
/// </summary>
public class BranchPaymentDto
{
    /// <summary>
    /// Selected branch location for in-person payment
    /// Must match one of the configured branch locations in SystemSettings
    /// </summary>
    [Required(ErrorMessage = "Preferred branch is required")]
    [MaxLength(100, ErrorMessage = "Branch name cannot exceed 100 characters")]
    public string PreferredBranch { get; set; } = string.Empty;
}