using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for paginated photo gallery results.
/// Implements pagination to handle large photo collections efficiently.
/// </summary>
public class PhotoGalleryPageDto
{
    /// <summary>
    /// Collection of photos for the current page
    /// Limited by page size to ensure reasonable response times
    /// </summary>
    public List<PhotoGalleryDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of photos across all pages
    /// Used for pagination controls in frontend
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// Echoed back for frontend pagination state
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// Configurable but typically 20-50 for good performance
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages available
    /// Calculated as ceiling(TotalCount / PageSize)
    /// Used for pagination navigation
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there are more pages after the current page
    /// Convenience property for "Next" button state
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are pages before the current page
    /// Convenience property for "Previous" button state
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}