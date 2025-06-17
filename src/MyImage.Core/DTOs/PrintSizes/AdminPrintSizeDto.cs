using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.PrintSizes;

/// <summary>
/// Data Transfer Object for admin print size management.
/// Includes additional metadata not shown to customers.
/// </summary>
public class AdminPrintSizeDto : PrintSizeDto
{
    /// <summary>
    /// Display sort order for admin interface
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// When price was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Who updated the price
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this size was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}