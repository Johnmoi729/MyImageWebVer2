using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// Photo entity representing uploaded JPEG files throughout their lifecycle.
/// This entity implements Requirement 1 (folder browsing and JPEG selection) and
/// Requirement 10 (photo deletion after order completion).
/// 
/// The lifecycle flow is: Upload → Order → Print → Ship → Delete
/// </summary>
public class Photo
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Reference to the User who owns this photo
    /// Enforces photo access control - users can only see their own photos
    /// </summary>
    [BsonElement("userId")]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// File information embedded document containing original upload details
    /// Preserves the source folder path for audit and user reference
    /// </summary>
    [BsonElement("fileInfo")]
    public PhotoFileInfo FileInfo { get; set; } = new();

    /// <summary>
    /// Storage information for MongoDB GridFS integration
    /// GridFS stores both original images and generated thumbnails
    /// </summary>
    [BsonElement("storage")]
    public PhotoStorage Storage { get; set; } = new();

    /// <summary>
    /// Image dimensions and metadata for print size compatibility checking
    /// Helps validate if photo resolution is sufficient for requested print sizes
    /// </summary>
    [BsonElement("dimensions")]
    public PhotoDimensions Dimensions { get; set; } = new();

    /// <summary>
    /// Order tracking information to manage photo lifecycle
    /// Prevents deletion of photos that are part of active orders
    /// </summary>
    [BsonElement("orderInfo")]
    public PhotoOrderInfo OrderInfo { get; set; } = new();

    /// <summary>
    /// Deletion flags for cleanup process after order completion
    /// Implements the requirement to delete photos after shipping
    /// </summary>
    [BsonElement("flags")]
    public PhotoFlags Flags { get; set; } = new();

    /// <summary>
    /// Metadata for audit trail and photo management
    /// </summary>
    [BsonElement("metadata")]
    public PhotoMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Embedded document for file information from upload process
/// Preserves original file details for user reference and audit
/// </summary>
public class PhotoFileInfo
{
    /// <summary>
    /// Original filename as provided by user during folder browsing
    /// Example: "IMG_001.jpg" from the source folder
    /// </summary>
    [BsonElement("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;

    /// <summary>
    /// Sanitized filename for safe storage in GridFS
    /// Format: {userId}_{timestamp}_{sequence}.jpg
    /// Example: "usr123_2024_01_20_001.jpg"
    /// </summary>
    [BsonElement("sanitizedFilename")]
    public string SanitizedFilename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes for storage tracking and cleanup statistics
    /// Used to calculate total storage freed when photos are deleted
    /// </summary>
    [BsonElement("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type validation - should always be "image/jpeg" per requirements
    /// Enforces the JPEG-only constraint from folder browsing
    /// </summary>
    [BsonElement("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// When the file was uploaded to the system
    /// Used for photo organization and cleanup scheduling
    /// </summary>
    [BsonElement("uploadedAt")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Original folder path where user selected this photo
    /// Preserved for user reference and audit trail
    /// Example: "C:\Users\John\Pictures\Vacation2024"
    /// </summary>
    [BsonElement("sourceFolderPath")]
    public string SourceFolderPath { get; set; } = string.Empty;
}

/// <summary>
/// Embedded document for MongoDB GridFS storage references
/// GridFS allows storing large binary files within MongoDB
/// </summary>
public class PhotoStorage
{
    /// <summary>
    /// Reference to the original image file in GridFS
    /// Used for download and print-quality image retrieval
    /// </summary>
    [BsonElement("gridFsFileId")]
    public ObjectId GridFsFileId { get; set; }

    /// <summary>
    /// Reference to the thumbnail image in GridFS
    /// Thumbnails are generated during upload for gallery display
    /// Max size: 300x300 pixels as configured in appsettings
    /// </summary>
    [BsonElement("thumbnailGridFsId")]
    public ObjectId ThumbnailGridFsId { get; set; }

    /// <summary>
    /// Total storage size including original and thumbnail
    /// Used for storage statistics and cleanup metrics
    /// </summary>
    [BsonElement("storageSize")]
    public long StorageSize { get; set; }
}

/// <summary>
/// Embedded document for image dimensions and quality information
/// Essential for validating print size compatibility
/// </summary>
public class PhotoDimensions
{
    /// <summary>
    /// Image width in pixels
    /// Used to validate sufficient resolution for print sizes
    /// </summary>
    [BsonElement("width")]
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels
    /// Used to validate sufficient resolution for print sizes
    /// </summary>
    [BsonElement("height")]
    public int Height { get; set; }

    /// <summary>
    /// Calculated aspect ratio for print size recommendations
    /// Example: "4:3", "16:9", "1:1"
    /// </summary>
    [BsonElement("aspectRatio")]
    public string AspectRatio { get; set; } = string.Empty;
}

/// <summary>
/// Embedded document for tracking photo usage in orders
/// Implements business rule: photos in orders cannot be deleted until order completion
/// </summary>
public class PhotoOrderInfo
{
    /// <summary>
    /// Flag indicating if this photo is part of any order
    /// Set to true when photo is added to cart and order is placed
    /// Prevents accidental deletion of ordered photos
    /// </summary>
    [BsonElement("isOrdered")]
    public bool IsOrdered { get; set; } = false;

    /// <summary>
    /// Array of Order ObjectIds that contain this photo
    /// One photo can be in multiple orders with different print selections
    /// Used for cleanup verification - all orders must be completed
    /// </summary>
    [BsonElement("orderedIn")]
    public List<ObjectId> OrderedIn { get; set; } = new();

    /// <summary>
    /// When this photo was last included in an order
    /// Used for audit trail and cleanup scheduling
    /// </summary>
    [BsonElement("lastOrderedDate")]
    public DateTime? LastOrderedDate { get; set; }
}

/// <summary>
/// Embedded document for photo deletion flags and cleanup management
/// Implements Requirement 10: delete photos after order completion
/// </summary>
public class PhotoFlags
{
    /// <summary>
    /// Soft delete flag - photo is marked as deleted but not physically removed yet
    /// Allows for recovery in case of errors during cleanup process
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Flag indicating photo is scheduled for deletion after order completion
    /// Set when all orders containing this photo reach "completed" status
    /// </summary>
    [BsonElement("isPendingDeletion")]
    public bool IsPendingDeletion { get; set; } = false;

    /// <summary>
    /// Scheduled deletion date - typically set for a few days after order completion
    /// Provides buffer period for customer service issues or order problems
    /// </summary>
    [BsonElement("deletionScheduledFor")]
    public DateTime? DeletionScheduledFor { get; set; }
}

/// <summary>
/// Embedded document for photo metadata and audit information
/// </summary>
public class PhotoMetadata
{
    /// <summary>
    /// When the photo record was created in the system
    /// Set during upload process
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the photo record was last modified
    /// Updated when order status changes or cleanup flags are set
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}