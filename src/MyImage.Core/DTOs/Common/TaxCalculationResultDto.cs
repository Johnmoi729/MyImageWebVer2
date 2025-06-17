using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Data Transfer Object for tax calculation results.
/// Returns calculated tax information for order totals.
/// </summary>
public class TaxCalculationResultDto
{
    /// <summary>
    /// Original subtotal amount
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Applied tax rate as decimal (e.g., 0.0625 for 6.25%)
    /// </summary>
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Calculated tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Total amount including tax
    /// </summary>
    public decimal Total { get; set; }
}