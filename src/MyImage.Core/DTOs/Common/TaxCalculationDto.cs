using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Data Transfer Object for tax calculation requests.
/// Used to calculate order totals based on shipping address.
/// </summary>
public class TaxCalculationDto
{
    /// <summary>
    /// Subtotal amount before tax
    /// </summary>
    [Required(ErrorMessage = "Subtotal is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Subtotal must be greater than 0")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// State code for tax rate lookup
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [MaxLength(10, ErrorMessage = "State cannot exceed 10 characters")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal code for detailed tax calculation (future use)
    /// </summary>
    [MaxLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string PostalCode { get; set; } = string.Empty;
}