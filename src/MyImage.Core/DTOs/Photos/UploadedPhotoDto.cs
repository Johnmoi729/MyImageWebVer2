using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for individual successfully uploaded photos.
/// Contains the information needed for immediate display in photo gallery.
/// </summary>
public class UploadedPhotoDto
{
    /// <summary>
    /// Unique identifier assigned to the uploaded photo
    /// Used for all future API operations on this photo
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original filename for user recognition
    /// Displayed in gallery to help users identify their photos
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Upload status for confirmation
    /// Typically "uploaded" for successful uploads
    /// May include additional states like "processing" for thumbnail generation
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint for retrieving the photo thumbnail
    /// Format: "/api/photos/{photoId}/thumbnail"
    /// Used immediately by frontend for gallery display
    /// </summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// Photo dimensions for display and print compatibility checking
    /// Helps users understand which print sizes are recommended
    /// </summary>
    public PhotoDimensionsDto Dimensions { get; set; } = new();
}