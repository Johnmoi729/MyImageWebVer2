using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo gallery display.
/// Contains all information needed to render photo thumbnails
/// and allow users to select photos for printing.
/// </summary>
public class PhotoGalleryDto
{
    /// <summary>
    /// Unique photo identifier for API operations
    /// Used when adding photos to shopping cart
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original filename for user recognition
    /// Displayed as photo title in gallery view
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes for user information
    /// Can be displayed as "2.3 MB" for user reference
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When the photo was uploaded to the system
    /// Used for sorting and organization in gallery
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// API endpoint for thumbnail image
    /// Used by frontend img tags for gallery display
    /// </summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint for full-size image download
    /// Used for photo preview and download functionality
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Photo dimensions for print size recommendations
    /// Helps users understand quality expectations for different sizes
    /// </summary>
    public PhotoDimensionsDto Dimensions { get; set; } = new();

    /// <summary>
    /// Whether this photo is part of any existing order
    /// Photos in orders cannot be deleted until order completion
    /// Used to disable delete button in gallery interface
    /// </summary>
    public bool IsOrdered { get; set; }

    /// <summary>
    /// Original folder path where photo was selected
    /// Useful for user reference and photo organization
    /// </summary>
    public string SourceFolder { get; set; } = string.Empty;
}