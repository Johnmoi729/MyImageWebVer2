using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Data Transfer Object for pagination parameters.
/// Used in query parameters for list endpoints.
/// </summary>
public class PaginationDto
{
    /// <summary>
    /// Page number to retrieve (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}