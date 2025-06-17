using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Data Transfer Object for branch location information.
/// Used in branch payment option and admin management.
/// </summary>
public class BranchLocationDto
{
    /// <summary>
    /// Branch name for selection
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Branch address for customer reference
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Branch phone number for customer contact
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Branch operating hours (future enhancement)
    /// </summary>
    public string Hours { get; set; } = string.Empty;
}