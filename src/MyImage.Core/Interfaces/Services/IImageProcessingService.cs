using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Services;

/// <summary>
/// Service interface for image processing operations.
/// Handles thumbnail generation and image validation.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Generate thumbnail from image stream.
    /// Creates scaled-down version for gallery display.
    /// </summary>
    /// <param name="imageStream">Original image stream</param>
    /// <param name="maxWidth">Maximum thumbnail width</param>
    /// <param name="maxHeight">Maximum thumbnail height</param>
    /// <returns>Thumbnail image stream</returns>
    Task<Stream> GenerateThumbnailAsync(Stream imageStream, int maxWidth, int maxHeight);

    /// <summary>
    /// Get image dimensions from stream.
    /// Used for print size compatibility checking.
    /// </summary>
    /// <param name="imageStream">Image stream to analyze</param>
    /// <returns>Image dimensions and metadata</returns>
    Task<ImageDimensions> GetImageDimensionsAsync(Stream imageStream);

    /// <summary>
    /// Validate image format and quality.
    /// Ensures uploaded files meet requirements.
    /// </summary>
    /// <param name="imageStream">Image stream to validate</param>
    /// <returns>Validation result with any issues</returns>
    Task<ImageValidationResult> ValidateImageAsync(Stream imageStream);
}

/// <summary>
/// Image dimensions result from processing service.
/// </summary>
public class ImageDimensions
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string AspectRatio { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}

/// <summary>
/// Image validation result from processing service.
/// </summary>
public class ImageValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Format { get; set; } = string.Empty;
    public long FileSize { get; set; }
}