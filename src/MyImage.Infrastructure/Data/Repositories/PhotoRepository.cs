using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Common;
using Microsoft.Extensions.Logging;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Photo entity operations.
/// This class manages the complete photo lifecycle from upload through deletion,
/// implementing Requirements 1 (folder browsing) and 10 (photo cleanup after order completion).
/// 
/// Key responsibilities:
/// - Photo metadata persistence with GridFS file references
/// - User ownership enforcement for security
/// - Order tracking to prevent premature deletion
/// - Cleanup scheduling and execution after order completion
/// - Pagination for efficient gallery display
/// </summary>
public class PhotoRepository : IPhotoRepository
{
    private readonly IMongoCollection<Photo> _photos;
    private readonly MongoDbContext _context;
    private readonly ILogger<PhotoRepository> _logger;

    /// <summary>
    /// Initialize repository with MongoDB context and logging.
    /// Provides access to photos collection and GridFS bucket for file operations.
    /// </summary>
    /// <param name="context">MongoDB context providing database and GridFS access</param>
    /// <param name="logger">Logger for photo repository operations</param>
    public PhotoRepository(MongoDbContext context, ILogger<PhotoRepository> logger)
    {
        _photos = context.Photos;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create new photo record with GridFS file reference.
    /// Called after successful file upload to GridFS during bulk photo upload process.
    /// Links photo metadata with stored binary files for complete photo management.
    /// </summary>
    /// <param name="photo">Photo entity with file metadata and GridFS references</param>
    /// <returns>Created photo with assigned MongoDB ObjectId</returns>
    public async Task<Photo> CreateAsync(Photo photo)
    {
        try
        {
            _logger.LogDebug("Creating photo record for user {UserId}: {Filename}",
                photo.UserId, photo.FileInfo.OriginalFilename);

            // Set creation metadata
            photo.Metadata.CreatedAt = DateTime.UtcNow;
            photo.Metadata.UpdatedAt = DateTime.UtcNow;

            // Initialize order tracking as not ordered
            photo.OrderInfo = new PhotoOrderInfo
            {
                IsOrdered = false,
                OrderedIn = new List<ObjectId>()
            };

            // Initialize deletion flags as not scheduled for deletion
            photo.Flags = new PhotoFlags
            {
                IsDeleted = false,
                IsPendingDeletion = false
            };

            await _photos.InsertOneAsync(photo);

            _logger.LogInformation("Successfully created photo {PhotoId} for user {UserId}: {Filename}",
                photo.Id, photo.UserId, photo.FileInfo.OriginalFilename);

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create photo for user {UserId}: {Filename}",
                photo.UserId, photo.FileInfo.OriginalFilename);
            throw;
        }
    }

    /// <summary>
    /// Get paginated photos for a specific user.
    /// Implements efficient photo gallery display with user ownership enforcement.
    /// Only returns photos that are not soft-deleted, ordered by upload date (newest first).
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated photo results with metadata</returns>
    public async Task<PagedResult<Photo>> GetUserPhotosAsync(ObjectId userId, int page, int pageSize)
    {
        try
        {
            _logger.LogDebug("Getting photos for user {UserId}, page {Page}, size {PageSize}",
                userId, page, pageSize);

            // Build filter for user's non-deleted photos
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.Eq(p => p.UserId, userId),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            // Calculate skip amount for pagination
            var skip = (page - 1) * pageSize;

            // Get total count for pagination metadata
            var totalCount = await _photos.CountDocumentsAsync(filter);

            // Get photos ordered by creation date (newest first)
            var photos = await _photos
                .Find(filter)
                .Sort(Builders<Photo>.Sort.Descending("metadata.createdAt"))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            var result = new PagedResult<Photo>
            {
                Items = photos,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogDebug("Retrieved {Count} photos for user {UserId} (page {Page}/{TotalPages})",
                photos.Count, userId, page, result.TotalPages);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photos for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get specific photo by ID with ownership verification.
    /// Ensures users can only access their own photos for security.
    /// Returns null if photo doesn't exist or doesn't belong to the user.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <param name="userId">User's ObjectId for ownership verification</param>
    /// <returns>Photo entity if found and owned by user, null otherwise</returns>
    public async Task<Photo?> GetByIdAsync(ObjectId photoId, ObjectId userId)
    {
        try
        {
            // Filter includes ownership check for security
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.Eq(p => p.Id, photoId),
                Builders<Photo>.Filter.Eq(p => p.UserId, userId),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            var photo = await _photos.Find(filter).FirstOrDefaultAsync();

            if (photo != null)
            {
                _logger.LogDebug("Found photo {PhotoId} for user {UserId}: {Filename}",
                    photoId, userId, photo.FileInfo.OriginalFilename);
            }
            else
            {
                _logger.LogDebug("Photo {PhotoId} not found or not owned by user {UserId}",
                    photoId, userId);
            }

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photo {PhotoId} for user {UserId}", photoId, userId);
            throw;
        }
    }

    /// <summary>
    /// Get multiple photos by IDs for cart/order operations.
    /// Batch operation for efficiency during order processing.
    /// Only returns photos owned by the specified user for security.
    /// </summary>
    /// <param name="photoIds">Collection of photo ObjectIds</param>
    /// <param name="userId">User's ObjectId for ownership verification</param>
    /// <returns>Collection of photos owned by user</returns>
    public async Task<List<Photo>> GetByIdsAsync(List<ObjectId> photoIds, ObjectId userId)
    {
        try
        {
            _logger.LogDebug("Getting {Count} photos for user {UserId}", photoIds.Count, userId);

            // Filter includes ownership check and non-deleted status
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.In(p => p.Id, photoIds),
                Builders<Photo>.Filter.Eq(p => p.UserId, userId),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            var photos = await _photos.Find(filter).ToListAsync();

            _logger.LogDebug("Retrieved {Retrieved} of {Requested} photos for user {UserId}",
                photos.Count, photoIds.Count, userId);

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting photos by IDs for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Update photo entity.
    /// Used for order status tracking, cleanup flag management, and metadata updates.
    /// Automatically updates the modification timestamp.
    /// </summary>
    /// <param name="photo">Photo entity with updates</param>
    /// <returns>Updated photo entity</returns>
    public async Task<Photo> UpdateAsync(Photo photo)
    {
        try
        {
            // Update modification timestamp
            photo.Metadata.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, photo.Id);
            await _photos.ReplaceOneAsync(filter, photo);

            _logger.LogDebug("Successfully updated photo {PhotoId}: {Filename}",
                photo.Id, photo.FileInfo.OriginalFilename);

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update photo {PhotoId}", photo.Id);
            throw;
        }
    }

    /// <summary>
    /// Mark photo for deletion after order completion.
    /// Implements soft delete with cleanup scheduling as required by the system.
    /// Photos are not immediately deleted to allow for order verification and customer service.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <param name="deletionDate">When photo should be deleted</param>
    /// <returns>Task completion</returns>
    public async Task MarkForDeletionAsync(ObjectId photoId, DateTime deletionDate)
    {
        try
        {
            _logger.LogDebug("Marking photo {PhotoId} for deletion on {DeletionDate}",
                photoId, deletionDate);

            var filter = Builders<Photo>.Filter.Eq(p => p.Id, photoId);
            var update = Builders<Photo>.Update
                .Set("flags.isPendingDeletion", true)
                .Set("flags.deletionScheduledFor", deletionDate)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _photos.UpdateOneAsync(filter, update);

            if (result.ModifiedCount > 0)
            {
                _logger.LogInformation("Photo {PhotoId} marked for deletion on {DeletionDate}",
                    photoId, deletionDate);
            }
            else
            {
                _logger.LogWarning("Photo {PhotoId} not found for deletion marking", photoId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark photo {PhotoId} for deletion", photoId);
            throw;
        }
    }

    /// <summary>
    /// Permanently delete photo and associated GridFS files.
    /// Called during cleanup process after order completion.
    /// Removes both photo metadata and binary files from GridFS to free storage space.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <returns>Storage freed in bytes</returns>
    public async Task<long> DeleteAsync(ObjectId photoId)
    {
        try
        {
            _logger.LogDebug("Permanently deleting photo {PhotoId}", photoId);

            // Get photo to retrieve GridFS file IDs and calculate storage freed
            var photo = await _photos.Find(p => p.Id == photoId).FirstOrDefaultAsync();
            if (photo == null)
            {
                _logger.LogWarning("Photo {PhotoId} not found for deletion", photoId);
                return 0;
            }

            var storageFreed = photo.Storage.StorageSize;

            // Delete original image from GridFS
            if (photo.Storage.GridFsFileId != ObjectId.Empty)
            {
                try
                {
                    await _context.GridFsBucket.DeleteAsync(photo.Storage.GridFsFileId);
                    _logger.LogDebug("Deleted original file {FileId} for photo {PhotoId}",
                        photo.Storage.GridFsFileId, photoId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete original file {FileId} for photo {PhotoId}",
                        photo.Storage.GridFsFileId, photoId);
                }
            }

            // Delete thumbnail from GridFS
            if (photo.Storage.ThumbnailGridFsId != ObjectId.Empty)
            {
                try
                {
                    await _context.GridFsBucket.DeleteAsync(photo.Storage.ThumbnailGridFsId);
                    _logger.LogDebug("Deleted thumbnail {FileId} for photo {PhotoId}",
                        photo.Storage.ThumbnailGridFsId, photoId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete thumbnail {FileId} for photo {PhotoId}",
                        photo.Storage.ThumbnailGridFsId, photoId);
                }
            }

            // Delete photo metadata from collection
            var filter = Builders<Photo>.Filter.Eq(p => p.Id, photoId);
            var result = await _photos.DeleteOneAsync(filter);

            if (result.DeletedCount > 0)
            {
                _logger.LogInformation("Successfully deleted photo {PhotoId}, freed {StorageFreed} bytes",
                    photoId, storageFreed);
            }
            else
            {
                _logger.LogWarning("Photo {PhotoId} metadata not found for deletion", photoId);
            }

            return storageFreed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete photo {PhotoId}", photoId);
            throw;
        }
    }

    /// <summary>
    /// Find photos pending deletion for cleanup job.
    /// Returns photos scheduled for deletion where the scheduled date has passed.
    /// Used by background cleanup process to maintain storage efficiency.
    /// </summary>
    /// <param name="beforeDate">Find photos scheduled for deletion before this date</param>
    /// <returns>Photos ready for deletion</returns>
    public async Task<List<Photo>> GetPhotosForCleanupAsync(DateTime beforeDate)
    {
        try
        {
            _logger.LogDebug("Finding photos for cleanup before {BeforeDate}", beforeDate);

            // Find photos marked for deletion with scheduled date in the past
            var filter = Builders<Photo>.Filter.And(
                Builders<Photo>.Filter.Eq("flags.isPendingDeletion", true),
                Builders<Photo>.Filter.Lte("flags.deletionScheduledFor", beforeDate),
                Builders<Photo>.Filter.Eq("flags.isDeleted", false)
            );

            var photos = await _photos.Find(filter).ToListAsync();

            _logger.LogInformation("Found {Count} photos ready for cleanup", photos.Count);

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get photos for cleanup");
            throw;
        }
    }

    /// <summary>
    /// Mark photos as ordered to prevent deletion.
    /// Called when photos are added to confirmed orders.
    /// Prevents accidental deletion of photos that are part of pending or processing orders.
    /// </summary>
    /// <param name="photoIds">Photo ObjectIds to mark as ordered</param>
    /// <param name="orderId">Order ObjectId containing these photos</param>
    /// <returns>Task completion</returns>
    public async Task MarkPhotosAsOrderedAsync(List<ObjectId> photoIds, ObjectId orderId)
    {
        try
        {
            _logger.LogDebug("Marking {Count} photos as ordered for order {OrderId}",
                photoIds.Count, orderId);

            var filter = Builders<Photo>.Filter.In(p => p.Id, photoIds);
            var update = Builders<Photo>.Update
                .Set("orderInfo.isOrdered", true)
                .Set("orderInfo.lastOrderedDate", DateTime.UtcNow)
                .AddToSet("orderInfo.orderedIn", orderId)
                .Set("metadata.updatedAt", DateTime.UtcNow);

            var result = await _photos.UpdateManyAsync(filter, update);

            _logger.LogInformation("Marked {ModifiedCount} photos as ordered for order {OrderId}",
                result.ModifiedCount, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark photos as ordered for order {OrderId}", orderId);
            throw;
        }
    }
}