using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using MyImage.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using MyImage.Infrastructure.Data;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// Service implementation for image processing operations.
/// Handles thumbnail generation and image validation using ImageSharp library.
/// 
/// This service supports the photo upload workflow by:
/// - Generating thumbnails for gallery display (300x300 max)
/// - Extracting image dimensions for print compatibility
/// - Validating image format and quality
/// - Ensuring JPEG-only compliance per requirements
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    /// <summary>
    /// Initialize image processing service with logging.
    /// ImageSharp library is configured with default JPEG codec settings.
    /// </summary>
    /// <param name="logger">Logger for image processing operations</param>
    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate thumbnail from image stream.
    /// Creates scaled-down version for gallery display while maintaining aspect ratio.
    /// 
    /// Thumbnail specifications:
    /// - Maximum dimensions: configurable (typically 300x300)
    /// - Maintains original aspect ratio
    /// - JPEG format with 85% quality for optimal size/quality balance
    /// - Efficient memory usage with stream processing
    /// </summary>
    /// <param name="imageStream">Original image stream</param>
    /// <param name="maxWidth">Maximum thumbnail width</param>
    /// <param name="maxHeight">Maximum thumbnail height</param>
    /// <returns>Thumbnail image stream</returns>
    public async Task<Stream> GenerateThumbnailAsync(Stream imageStream, int maxWidth, int maxHeight)
    {
        try
        {
            _logger.LogDebug("Generating thumbnail with max dimensions: {MaxWidth}x{MaxHeight}",
                maxWidth, maxHeight);

            // Reset stream position for reading
            imageStream.Position = 0;

            // Load image using ImageSharp
            using var image = await Image.LoadAsync(imageStream);

            _logger.LogDebug("Original image dimensions: {Width}x{Height}",
                image.Width, image.Height);

            // Calculate thumbnail dimensions while maintaining aspect ratio
            var (thumbnailWidth, thumbnailHeight) = CalculateThumbnailDimensions(
                image.Width, image.Height, maxWidth, maxHeight);

            _logger.LogDebug("Calculated thumbnail dimensions: {Width}x{Height}",
                thumbnailWidth, thumbnailHeight);

            // Resize image maintaining aspect ratio and quality
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(thumbnailWidth, thumbnailHeight),
                Mode = ResizeMode.Max, // Maintain aspect ratio
                Sampler = KnownResamplers.Lanczos3 // High-quality resampling
            }));

            // Create output stream for thumbnail
            var thumbnailStream = new MemoryStream();

            // Save as JPEG with optimized quality settings
            var encoder = new JpegEncoder
            {
                Quality = 85, // Good balance of quality and file size
                // Removed Progressive property - not available in newer versions
            };

            await image.SaveAsync(thumbnailStream, encoder);

            // Reset position for reading
            thumbnailStream.Position = 0;

            _logger.LogInformation("Generated thumbnail: {OriginalSize} bytes -> {ThumbnailSize} bytes",
                imageStream.Length, thumbnailStream.Length);

            return thumbnailStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail");
            throw new InvalidOperationException("Thumbnail generation failed", ex);
        }
    }

    /// <summary>
    /// Get image dimensions from stream.
    /// Used for print size compatibility checking and metadata storage.
    /// Extracts dimensions without loading the full image into memory.
    /// </summary>
    /// <param name="imageStream">Image stream to analyze</param>
    /// <returns>Image dimensions and metadata</returns>
    public async Task<ImageDimensions> GetImageDimensionsAsync(Stream imageStream)
    {
        try
        {
            _logger.LogDebug("Extracting image dimensions");

            // Reset stream position for reading
            imageStream.Position = 0;

            // Use ImageSharp to get image info without fully loading
            var imageInfo = await Image.IdentifyAsync(imageStream);

            if (imageInfo == null)
            {
                throw new InvalidOperationException("Unable to identify image format or dimensions");
            }

            // Calculate aspect ratio
            var aspectRatio = CalculateAspectRatio(imageInfo.Width, imageInfo.Height);

            var dimensions = new ImageDimensions
            {
                Width = imageInfo.Width,
                Height = imageInfo.Height,
                AspectRatio = aspectRatio,
                Format = imageInfo.Metadata.DecodedImageFormat?.Name ?? "Unknown"
            };

            _logger.LogDebug("Image dimensions: {Width}x{Height}, Aspect: {AspectRatio}, Format: {Format}",
                dimensions.Width, dimensions.Height, dimensions.AspectRatio, dimensions.Format);

            return dimensions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image dimensions");
            throw new InvalidOperationException("Image dimension extraction failed", ex);
        }
    }

    /// <summary>
    /// Validate image format and quality.
    /// Ensures uploaded files meet JPEG-only requirement and quality standards.
    /// 
    /// Validation checks:
    /// - File format is JPEG (.jpg or .jpeg)
    /// - Image is not corrupted
    /// - Dimensions are reasonable (not too small or extremely large)
    /// - File size is within limits
    /// </summary>
    /// <param name="imageStream">Image stream to validate</param>
    /// <returns>Validation result with any issues found</returns>
    public async Task<ImageValidationResult> ValidateImageAsync(Stream imageStream)
    {
        var result = new ImageValidationResult
        {
            IsValid = true,
            Errors = new List<string>(),
            FileSize = imageStream.Length
        };

        try
        {
            _logger.LogDebug("Validating image, Size: {Size} bytes", imageStream.Length);

            // Reset stream position for reading
            imageStream.Position = 0;

            // Try to identify the image format
            var imageInfo = await Image.IdentifyAsync(imageStream);

            if (imageInfo == null)
            {
                result.IsValid = false;
                result.Errors.Add("Unable to identify image format - file may be corrupted");
                return result;
            }

            result.Format = imageInfo.Metadata.DecodedImageFormat?.Name ?? "Unknown";

            // Validate JPEG format requirement
            if (!IsJpegFormat(result.Format))
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid format '{result.Format}'. Only JPEG images are allowed");
            }

            // Validate dimensions are reasonable
            if (imageInfo.Width < 100 || imageInfo.Height < 100)
            {
                result.IsValid = false;
                result.Errors.Add($"Image too small: {imageInfo.Width}x{imageInfo.Height}. Minimum size is 100x100 pixels");
            }

            if (imageInfo.Width > 50000 || imageInfo.Height > 50000)
            {
                result.IsValid = false;
                result.Errors.Add($"Image too large: {imageInfo.Width}x{imageInfo.Height}. Maximum size is 50000x50000 pixels");
            }

            // Validate file size (50MB limit from configuration)
            const long maxFileSize = 52428800; // 50MB in bytes
            if (imageStream.Length > maxFileSize)
            {
                result.IsValid = false;
                result.Errors.Add($"File size {imageStream.Length:N0} bytes exceeds maximum of {maxFileSize:N0} bytes (50MB)");
            }

            if (result.IsValid)
            {
                _logger.LogDebug("Image validation passed: {Width}x{Height}, {Format}, {Size} bytes",
                    imageInfo.Width, imageInfo.Height, result.Format, result.FileSize);
            }
            else
            {
                _logger.LogWarning("Image validation failed: {Errors}", string.Join("; ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image validation");
            result.IsValid = false;
            result.Errors.Add("Image validation failed due to processing error");
            return result;
        }
    }

    /// <summary>
    /// Calculate thumbnail dimensions while maintaining aspect ratio.
    /// Ensures thumbnail fits within specified maximum dimensions.
    /// </summary>
    /// <param name="originalWidth">Original image width</param>
    /// <param name="originalHeight">Original image height</param>
    /// <param name="maxWidth">Maximum thumbnail width</param>
    /// <param name="maxHeight">Maximum thumbnail height</param>
    /// <returns>Calculated thumbnail dimensions</returns>
    private static (int width, int height) CalculateThumbnailDimensions(
        int originalWidth, int originalHeight, int maxWidth, int maxHeight)
    {
        // Calculate scaling factors for both dimensions
        var scaleX = (double)maxWidth / originalWidth;
        var scaleY = (double)maxHeight / originalHeight;

        // Use the smaller scale to ensure thumbnail fits within bounds
        var scale = Math.Min(scaleX, scaleY);

        // Calculate new dimensions
        var newWidth = (int)Math.Round(originalWidth * scale);
        var newHeight = (int)Math.Round(originalHeight * scale);

        // Ensure minimum size of 1 pixel
        return (Math.Max(1, newWidth), Math.Max(1, newHeight));
    }

    /// <summary>
    /// Calculate aspect ratio string from dimensions.
    /// Returns common ratios like "4:3", "16:9" or numeric ratio.
    /// </summary>
    /// <param name="width">Image width</param>
    /// <param name="height">Image height</param>
    /// <returns>Aspect ratio string</returns>
    private static string CalculateAspectRatio(int width, int height)
    {
        // Calculate GCD to reduce ratio to simplest form
        var gcd = CalculateGcd(width, height);
        var ratioWidth = width / gcd;
        var ratioHeight = height / gcd;

        // Return common aspect ratios as recognizable strings
        return (ratioWidth, ratioHeight) switch
        {
            (4, 3) => "4:3",
            (3, 4) => "3:4",
            (16, 9) => "16:9",
            (9, 16) => "9:16",
            (3, 2) => "3:2",
            (2, 3) => "2:3",
            (1, 1) => "1:1",
            _ => $"{ratioWidth}:{ratioHeight}"
        };
    }

    /// <summary>
    /// Calculate Greatest Common Divisor using Euclidean algorithm.
    /// Used for aspect ratio calculation.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Greatest common divisor</returns>
    private static int CalculateGcd(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    /// <summary>
    /// Check if image format is JPEG.
    /// Validates against JPEG format requirement.
    /// </summary>
    /// <param name="format">Image format string from ImageSharp</param>
    /// <returns>True if format is JPEG</returns>
    private static bool IsJpegFormat(string format)
    {
        return format.Equals("JPEG", StringComparison.OrdinalIgnoreCase) ||
               format.Equals("JPG", StringComparison.OrdinalIgnoreCase);
    }
}