using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for Photo entity operations.
/// Handles photo lifecycle from upload through deletion after order completion.
/// Integrates with MongoDB GridFS for binary file storage.
/// </summary>
public interface IPhotoRepository
{
    /// <summary>
    /// Create new photo record with GridFS file reference.
    /// Called after successful file upload to GridFS.
    /// </summary>
    /// <param name="photo">Photo entity with metadata</param>
    /// <returns>Created photo with assigned ID</returns>
    Task<Photo> CreateAsync(Photo photo);

    /// <summary>
    /// Get paginated photos for a specific user.
    /// Supports photo gallery display with performance optimization.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated photo results</returns>
    Task<PagedResult<Photo>> GetUserPhotosAsync(ObjectId userId, int page, int pageSize);

    /// <summary>
    /// Get specific photo by ID with ownership verification.
    /// Ensures users can only access their own photos.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <param name="userId">User's ObjectId for ownership check</param>
    /// <returns>Photo entity if found and owned by user</returns>
    Task<Photo?> GetByIdAsync(ObjectId photoId, ObjectId userId);

    /// <summary>
    /// Get multiple photos by IDs for cart/order operations.
    /// Batch operation for efficiency during order processing.
    /// </summary>
    /// <param name="photoIds">Collection of photo ObjectIds</param>
    /// <param name="userId">User's ObjectId for ownership verification</param>
    /// <returns>Collection of photos owned by user</returns>
    Task<List<Photo>> GetByIdsAsync(List<ObjectId> photoIds, ObjectId userId);

    /// <summary>
    /// Update photo entity.
    /// Used for order status tracking and cleanup flags.
    /// </summary>
    /// <param name="photo">Photo entity with updates</param>
    /// <returns>Updated photo entity</returns>
    Task<Photo> UpdateAsync(Photo photo);

    /// <summary>
    /// Mark photo for deletion after order completion.
    /// Implements soft delete with cleanup scheduling.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <param name="deletionDate">When photo should be deleted</param>
    /// <returns>Task completion</returns>
    Task MarkForDeletionAsync(ObjectId photoId, DateTime deletionDate);

    /// <summary>
    /// Permanently delete photo and associated GridFS files.
    /// Called during cleanup process after order completion.
    /// </summary>
    /// <param name="photoId">Photo's ObjectId</param>
    /// <returns>Storage freed in bytes</returns>
    Task<long> DeleteAsync(ObjectId photoId);

    /// <summary>
    /// Find photos pending deletion for cleanup job.
    /// Returns photos scheduled for deletion where date has passed.
    /// </summary>
    /// <param name="beforeDate">Find photos scheduled before this date</param>
    /// <returns>Photos ready for deletion</returns>
    Task<List<Photo>> GetPhotosForCleanupAsync(DateTime beforeDate);

    /// <summary>
    /// Mark photos as ordered to prevent deletion.
    /// Called when photos are added to confirmed orders.
    /// </summary>
    /// <param name="photoIds">Photo ObjectIds to mark</param>
    /// <param name="orderId">Order ObjectId containing these photos</param>
    /// <returns>Task completion</returns>
    Task MarkPhotosAsOrderedAsync(List<ObjectId> photoIds, ObjectId orderId);
}