using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Security.Claims;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.Interfaces.Services;
using MyImage.Core.DTOs.Photos;
using MyImage.Core.DTOs.Common;
using MyImage.Core.Entities;

namespace MyImage.API.Controllers;

/// <summary>
/// Photos controller handling photo upload, management, and retrieval operations.
/// This controller implements Requirements 1 (folder browsing and JPEG selection) and
/// provides the foundation for the photo printing workflow.
/// 
/// Key endpoints:
/// - POST /api/photos/bulk-upload - Upload multiple JPEG files
/// - GET /api/photos - Get user's photo gallery with pagination
/// - GET /api/photos/{id} - Get specific photo details
/// - GET /api/photos/{id}/download - Download original photo
/// - GET /api/photos/{id}/thumbnail - Get photo thumbnail
/// - DELETE /api/photos/{id} - Delete photo (if not ordered)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All photo operations require authentication
[Produces("application/json")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoRepository _photoRepository;
    private readonly IGridFsStorageService _storageService;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly ILogger<PhotosController> _logger;

    // Configuration for file uploads from appsettings
    private readonly long _maxFileSize;
    private readonly string[] _allowedExtensions;
    private readonly int _thumbnailMaxWidth;
    private readonly int _thumbnailMaxHeight;

    /// <summary>
    /// Initialize photos controller with required services and configuration.
    /// </summary>
    /// <param name="photoRepository">Photo data access repository</param>
    /// <param name="storageService">GridFS file storage service</param>
    /// <param name="imageProcessingService">Image processing and validation service</param>
    /// <param name="configuration">Application configuration for upload settings</param>
    /// <param name="logger">Logger for photo operations</param>
    public PhotosController(
        IPhotoRepository photoRepository,
        IGridFsStorageService storageService,
        IImageProcessingService imageProcessingService,
        IConfiguration configuration,
        ILogger<PhotosController> logger)
    {
        _photoRepository = photoRepository;
        _storageService = storageService;
        _imageProcessingService = imageProcessingService;
        _logger = logger;

        // Load upload configuration from appsettings
        _maxFileSize = configuration.GetValue<long>("FileUploadSettings:MaxFileSizeBytes", 52428800); // 50MB default
        _allowedExtensions = configuration.GetSection("FileUploadSettings:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg" };
        _thumbnailMaxWidth = configuration.GetValue<int>("FileUploadSettings:ThumbnailMaxWidth", 300);
        _thumbnailMaxHeight = configuration.GetValue<int>("FileUploadSettings:ThumbnailMaxHeight", 300);
    }

    /// <summary>
    /// Upload multiple JPEG files in bulk operation.
    /// This endpoint implements the photo upload workflow after users have selected
    /// JPEG files from their desktop folder through the frontend folder browser.
    /// 
    /// The upload process:
    /// 1. Validates each file format and size
    /// 2. Stores original images in GridFS
    /// 3. Generates thumbnails for gallery display
    /// 4. Creates photo metadata records
    /// 5. Returns success/failure results for each file
    /// </summary>
    /// <param name="files">Collection of JPEG files to upload</param>
    /// <param name="folderPath">Original folder path for audit trail</param>
    /// <returns>Upload results with photo IDs and any failures</returns>
    /// <response code="201">Files uploaded successfully (may include partial failures)</response>
    /// <response code="400">Invalid request or no files provided</response>
    /// <response code="401">Authentication required</response>
    /// <response code="413">Files too large</response>
    [HttpPost("bulk-upload")]
    [ProducesResponseType(typeof(ApiResponse<PhotoUploadResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<ApiResponse<PhotoUploadResultDto>>> BulkUpload(
        [FromForm] IFormFileCollection files,
        [FromForm] string? folderPath = null)
    {
        try
        {
            // Get authenticated user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            _logger.LogInformation("Bulk upload request from user {UserId}: {FileCount} files",
                userId, files.Count);

            // Validate that files were provided
            if (files == null || files.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Upload failed",
                    "No files provided for upload"));
            }

            // Check total upload size to prevent memory issues
            var totalSize = files.Sum(f => f.Length);
            var maxTotalSize = _maxFileSize * 10; // Allow up to 10 times single file limit for bulk
            if (totalSize > maxTotalSize)
            {
                return StatusCode(StatusCodes.Status413PayloadTooLarge,
                    ApiResponse<object>.ErrorResponse(
                        "Upload failed",
                        $"Total upload size {totalSize:N0} bytes exceeds limit of {maxTotalSize:N0} bytes"));
            }

            var uploadResults = new PhotoUploadResultDto
            {
                UploadedPhotos = new List<UploadedPhotoDto>(),
                FailedUploads = new List<FailedUploadDto>(),
                SourceFolder = folderPath ?? "Unknown"
            };

            // Process each file individually
            foreach (var file in files)
            {
                try
                {
                    var uploadResult = await ProcessSingleFileUpload(file, userId.Value, folderPath);
                    if (uploadResult.Success)
                    {
                        uploadResults.UploadedPhotos.Add(uploadResult.Photo!);
                    }
                    else
                    {
                        uploadResults.FailedUploads.Add(new FailedUploadDto
                        {
                            Filename = file.FileName,
                            ErrorMessage = uploadResult.ErrorMessage,
                            ErrorCode = uploadResult.ErrorCode
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing file {Filename}", file.FileName);
                    uploadResults.FailedUploads.Add(new FailedUploadDto
                    {
                        Filename = file.FileName,
                        ErrorMessage = "Unexpected error during upload",
                        ErrorCode = "PROCESSING_ERROR"
                    });
                }
            }

            uploadResults.TotalUploaded = uploadResults.UploadedPhotos.Count;

            var message = uploadResults.TotalUploaded == files.Count
                ? "All files uploaded successfully"
                : $"{uploadResults.TotalUploaded} of {files.Count} files uploaded successfully";

            _logger.LogInformation("Bulk upload completed for user {UserId}: {Uploaded}/{Total} files successful",
                userId, uploadResults.TotalUploaded, files.Count);

            return CreatedAtAction(
                nameof(BulkUpload),
                ApiResponse<PhotoUploadResultDto>.SuccessResponse(uploadResults, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during bulk upload");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Upload failed",
                    "An unexpected error occurred during upload"));
        }
    }

    /// <summary>
    /// Get user's photo gallery with pagination.
    /// Returns photos uploaded by the authenticated user, ordered by upload date (newest first).
    /// Only returns photos that haven't been soft-deleted.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1-100)</param>
    /// <returns>Paginated photo gallery</returns>
    /// <response code="200">Photo gallery retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">Authentication required</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PhotoGalleryPageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PhotoGalleryPageDto>>> GetPhotos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            // Validate pagination parameters
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid parameters",
                    "Page must be >= 1 and pageSize must be between 1 and 100"));
            }

            _logger.LogDebug("Getting photos for user {UserId}, page {Page}, size {PageSize}",
                userId, page, pageSize);

            // Get paginated photos from repository
            var pagedPhotos = await _photoRepository.GetUserPhotosAsync(userId.Value, page, pageSize);

            // Convert to DTOs for API response
            var galleryItems = pagedPhotos.Items.Select(photo => new PhotoGalleryDto
            {
                Id = photo.Id.ToString(),
                Filename = photo.FileInfo.OriginalFilename,
                FileSize = photo.FileInfo.FileSize,
                UploadedAt = photo.Metadata.CreatedAt,
                ThumbnailUrl = $"/api/photos/{photo.Id}/thumbnail",
                DownloadUrl = $"/api/photos/{photo.Id}/download",
                Dimensions = new PhotoDimensionsDto
                {
                    Width = photo.Dimensions.Width,
                    Height = photo.Dimensions.Height,
                    AspectRatio = photo.Dimensions.AspectRatio
                },
                IsOrdered = photo.OrderInfo.IsOrdered,
                SourceFolder = photo.FileInfo.SourceFolderPath
            }).ToList();

            var result = new PhotoGalleryPageDto
            {
                Items = galleryItems,
                TotalCount = pagedPhotos.TotalCount,
                Page = pagedPhotos.Page,
                PageSize = pagedPhotos.PageSize,
                TotalPages = pagedPhotos.TotalPages
            };

            return Ok(ApiResponse<PhotoGalleryPageDto>.SuccessResponse(
                result,
                $"Retrieved {galleryItems.Count} photos"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve photos",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Download original photo file.
    /// Returns the full-resolution image file for viewing or printing.
    /// Enforces user ownership to prevent unauthorized access.
    /// </summary>
    /// <param name="id">Photo ID</param>
    /// <returns>Photo file stream</returns>
    /// <response code="200">Photo file</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Photo not found or not owned by user</response>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPhoto(string id)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            if (!ObjectId.TryParse(id, out var photoId))
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo not found",
                    "Invalid photo ID format"));
            }

            // Get photo with ownership verification
            var photo = await _photoRepository.GetByIdAsync(photoId, userId.Value);
            if (photo == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo not found",
                    "Photo not found or access denied"));
            }

            // Download file from GridFS
            var fileStream = await _storageService.DownloadFileAsync(photo.Storage.GridFsFileId);
            if (fileStream == null)
            {
                _logger.LogError("Photo file not found in GridFS: {PhotoId} -> {GridFsId}",
                    photoId, photo.Storage.GridFsFileId);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo file not found",
                    "The photo file is missing from storage"));
            }

            // Return file with appropriate content type
            return File(fileStream, "image/jpeg", photo.FileInfo.OriginalFilename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading photo {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Download failed",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get photo thumbnail for gallery display.
    /// Returns scaled-down version of the photo for efficient gallery loading.
    /// Thumbnails are generated during upload and cached in GridFS.
    /// </summary>
    /// <param name="id">Photo ID</param>
    /// <returns>Thumbnail image file</returns>
    /// <response code="200">Thumbnail image file</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Photo or thumbnail not found</response>
    [HttpGet("{id}/thumbnail")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(string id)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ObjectId.TryParse(id, out var photoId))
            {
                return NotFound();
            }

            // Get photo with ownership verification
            var photo = await _photoRepository.GetByIdAsync(photoId, userId.Value);
            if (photo == null)
            {
                return NotFound();
            }

            // Download thumbnail from GridFS
            var thumbnailStream = await _storageService.DownloadFileAsync(photo.Storage.ThumbnailGridFsId);
            if (thumbnailStream == null)
            {
                // If thumbnail is missing, generate it on-demand from original
                var originalStream = await _storageService.DownloadFileAsync(photo.Storage.GridFsFileId);
                if (originalStream == null)
                {
                    return NotFound();
                }

                thumbnailStream = await _imageProcessingService.GenerateThumbnailAsync(
                    originalStream, _thumbnailMaxWidth, _thumbnailMaxHeight);

                originalStream.Dispose();
            }

            // Return thumbnail with caching headers for performance
            Response.Headers.Add("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
            return File(thumbnailStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thumbnail for photo {PhotoId}", id);
            return NotFound();
        }
    }

    /// <summary>
    /// Delete photo if not part of any order.
    /// Implements soft delete initially, with physical deletion during cleanup.
    /// Photos that are part of orders cannot be deleted until order completion.
    /// </summary>
    /// <param name="id">Photo ID</param>
    /// <returns>Deletion confirmation</returns>
    /// <response code="200">Photo deleted successfully</response>
    /// <response code="400">Photo cannot be deleted (part of order)</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Photo not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePhoto(string id)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            if (!ObjectId.TryParse(id, out var photoId))
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo not found",
                    "Invalid photo ID format"));
            }

            // Get photo with ownership verification
            var photo = await _photoRepository.GetByIdAsync(photoId, userId.Value);
            if (photo == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo not found",
                    "Photo not found or access denied"));
            }

            // Check if photo is part of any order
            if (photo.OrderInfo.IsOrdered)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Cannot delete photo",
                    "Photo is part of an order and cannot be deleted"));
            }

            // Perform immediate deletion for photos not in orders
            await _photoRepository.DeleteAsync(photoId);

            _logger.LogInformation("Photo {PhotoId} deleted by user {UserId}", photoId, userId);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Photo deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo {PhotoId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Delete failed",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Process individual file upload with validation and storage.
    /// Helper method for bulk upload operation.
    /// </summary>
    private async Task<SingleUploadResult> ProcessSingleFileUpload(IFormFile file, ObjectId userId, string? folderPath)
    {
        // Validate file extension (JPEG only requirement)
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return SingleUploadResult.CreateFailure("Invalid file format. Only JPEG files are allowed", "INVALID_FORMAT");
        }

        // Validate file size
        if (file.Length > _maxFileSize)
        {
            return SingleUploadResult.CreateFailure(
                $"File size {file.Length:N0} bytes exceeds maximum of {_maxFileSize:N0} bytes",
                "FILE_TOO_LARGE");
        }

        if (file.Length == 0)
        {
            return SingleUploadResult.CreateFailure("File is empty", "EMPTY_FILE");
        }

        using var stream = file.OpenReadStream();

        // Validate image format and get dimensions
        var validation = await _imageProcessingService.ValidateImageAsync(stream);
        if (!validation.IsValid)
        {
            return SingleUploadResult.CreateFailure(
                string.Join("; ", validation.Errors),
                "VALIDATION_FAILED");
        }

        var dimensions = await _imageProcessingService.GetImageDimensionsAsync(stream);

        // Generate sanitized filename
        var sanitizedFilename = GenerateSanitizedFilename(userId, file.FileName);

        // Upload original image to GridFS
        stream.Position = 0;
        var originalFileId = await _storageService.UploadFileAsync(stream, sanitizedFilename, new Dictionary<string, object>
        {
            ["userId"] = userId.ToString(),
            ["type"] = "original",
            ["originalFilename"] = file.FileName
        });

        // Generate and upload thumbnail
        stream.Position = 0;
        using var thumbnailStream = await _imageProcessingService.GenerateThumbnailAsync(
            stream, _thumbnailMaxWidth, _thumbnailMaxHeight);

        var thumbnailFilename = $"thumb_{sanitizedFilename}";
        var thumbnailFileId = await _storageService.UploadFileAsync(thumbnailStream, thumbnailFilename, new Dictionary<string, object>
        {
            ["userId"] = userId.ToString(),
            ["type"] = "thumbnail",
            ["originalFilename"] = file.FileName
        });

        // Create photo entity
        var photo = new Photo
        {
            UserId = userId,
            FileInfo = new PhotoFileInfo
            {
                OriginalFilename = file.FileName,
                SanitizedFilename = sanitizedFilename,
                FileSize = file.Length,
                MimeType = file.ContentType,
                SourceFolderPath = folderPath ?? ""
            },
            Storage = new PhotoStorage
            {
                GridFsFileId = originalFileId,
                ThumbnailGridFsId = thumbnailFileId,
                StorageSize = file.Length + thumbnailStream.Length
            },
            Dimensions = new PhotoDimensions
            {
                Width = dimensions.Width,
                Height = dimensions.Height,
                AspectRatio = dimensions.AspectRatio
            }
        };

        // Save photo metadata
        var createdPhoto = await _photoRepository.CreateAsync(photo);

        var photoDto = new UploadedPhotoDto
        {
            Id = createdPhoto.Id.ToString(),
            Filename = file.FileName,
            Status = "uploaded",
            ThumbnailUrl = $"/api/photos/{createdPhoto.Id}/thumbnail",
            Dimensions = new PhotoDimensionsDto
            {
                Width = dimensions.Width,
                Height = dimensions.Height,
                AspectRatio = dimensions.AspectRatio
            }
        };

        return SingleUploadResult.CreateSuccess(photoDto);
    }

    /// <summary>
    /// Generate sanitized filename for safe storage.
    /// Format: {userId}_{timestamp}_{sequence}.jpg
    /// </summary>
    private static string GenerateSanitizedFilename(ObjectId userId, string originalFilename)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
        var extension = Path.GetExtension(originalFilename).ToLowerInvariant();
        var sequence = Guid.NewGuid().ToString("N")[..8]; // 8 character unique suffix

        return $"{userId}_{timestamp}_{sequence}{extension}";
    }

    /// <summary>
    /// Extract authenticated user ID from JWT token claims.
    /// </summary>
    private ObjectId? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && ObjectId.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Helper class for individual upload results.
    /// </summary>
    private class SingleUploadResult
    {
        public bool Success { get; private set; }
        public UploadedPhotoDto? Photo { get; private set; }
        public string ErrorMessage { get; private set; } = "";
        public string ErrorCode { get; private set; } = "";

        // Make constructors private and use static factory methods
        private SingleUploadResult() { }

        public static SingleUploadResult CreateSuccess(UploadedPhotoDto photo)
        {
            return new SingleUploadResult { Success = true, Photo = photo };
        }

        public static SingleUploadResult CreateFailure(string errorMessage, string errorCode)
        {
            return new SingleUploadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }
    }
}