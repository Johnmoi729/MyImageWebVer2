using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Common;

/// <summary>
/// Generic pagination wrapper for list results.
/// Provides consistent pagination across all endpoints that return lists.
/// </summary>
/// <typeparam name="T">Type of items in the list</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Items for the current page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there are more pages after current
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are pages before current
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}