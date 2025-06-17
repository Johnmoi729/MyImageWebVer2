using MongoDB.Driver; // Important for cursor extensions
using MongoDB.Driver.GridFS;
using MongoDB.Bson;
using System.Linq;
using MyImage.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using MyImage.Infrastructure.Data;

namespace MyImage.Infrastructure.Services;

/// <summary>
/// Service implementation for GridFS file storage operations.
/// Handles binary file storage and retrieval within MongoDB using GridFS.
/// GridFS automatically handles file chunking for efficient storage of large photos.
/// 
/// This service implements the photo storage requirements by providing:
/// - Secure file upload with metadata
/// - Efficient file retrieval for API responses
/// - Cleanup operations for order completion workflow
/// - File information queries without downloading content
/// </summary>
public class GridFsStorageService : IGridFsStorageService
{
    private readonly IGridFSBucket _gridFsBucket;
    private readonly ILogger<GridFsStorageService> _logger;

    /// <summary>
    /// Initialize GridFS storage service with MongoDB context.
    /// Uses the configured GridFS bucket from the database context.
    /// </summary>
    /// <param name="context">MongoDB context providing GridFS bucket access</param>
    /// <param name="logger">Logger for storage operations</param>
    public GridFsStorageService(MongoDbContext context, ILogger<GridFsStorageService> logger)
    {
        _gridFsBucket = context.GridFsBucket;
        _logger = logger;
    }

    /// <summary>
    /// Upload file to GridFS with metadata.
    /// Stores binary file content in GridFS with optional metadata for categorization.
    /// GridFS automatically handles chunking for large files and provides efficient storage.
    /// 
    /// Metadata can include:
    /// - User ID for ownership tracking
    /// - File type (original, thumbnail)
    /// - Original filename for reference
    /// - Upload timestamp and other audit information
    /// </summary>
    /// <param name="fileStream">File content stream to upload</param>
    /// <param name="filename">Filename for GridFS storage</param>
    /// <param name="metadata">Optional metadata dictionary</param>
    /// <returns>GridFS file ObjectId for database reference</returns>
    public async Task<ObjectId> UploadFileAsync(Stream fileStream, string filename, Dictionary<string, object>? metadata = null)
    {
        try
        {
            _logger.LogDebug("Uploading file to GridFS: {Filename}, Size: {Size} bytes",
                filename, fileStream.Length);

            // Create GridFS upload options with metadata
            var options = new GridFSUploadOptions
            {
                Metadata = metadata != null ? new BsonDocument(metadata) : null
            };

            // Reset stream position to beginning for upload
            fileStream.Position = 0;

            // Upload file and get GridFS file ID
            var fileId = await _gridFsBucket.UploadFromStreamAsync(filename, fileStream, options);

            _logger.LogInformation("Successfully uploaded file {Filename} to GridFS with ID {FileId}",
                filename, fileId);

            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {Filename} to GridFS", filename);
            throw new InvalidOperationException($"Failed to upload file {filename}", ex);
        }
    }

    /// <summary>
    /// Download file from GridFS by ID.
    /// Returns file stream for API response or further processing.
    /// The returned stream should be disposed by the caller.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId</param>
    /// <returns>File stream if found, null if file doesn't exist</returns>
    public async Task<Stream?> DownloadFileAsync(ObjectId fileId)
    {
        try
        {
            _logger.LogDebug("Downloading file from GridFS: {FileId}", fileId);

            // Check if file exists before attempting download
            var fileInfo = await GetFileInfoAsync(fileId);
            if (fileInfo == null)
            {
                _logger.LogWarning("File not found in GridFS: {FileId}", fileId);
                return null;
            }

            // Download file to memory stream
            var memoryStream = new MemoryStream();
            await _gridFsBucket.DownloadToStreamAsync(fileId, memoryStream);

            // Reset position for reading
            memoryStream.Position = 0;

            _logger.LogDebug("Successfully downloaded file {FileId}, Size: {Size} bytes",
                fileId, memoryStream.Length);

            return memoryStream;
        }
        catch (GridFSFileNotFoundException)
        {
            _logger.LogWarning("GridFS file not found: {FileId}", fileId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from GridFS: {FileId}", fileId);
            throw new InvalidOperationException($"Failed to download file {fileId}", ex);
        }
    }

    /// <summary>
    /// Delete file from GridFS by ID.
    /// Called during photo cleanup process after order completion.
    /// Permanently removes file and all its chunks from GridFS.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId to delete</param>
    /// <returns>Task completion</returns>
    public async Task DeleteFileAsync(ObjectId fileId)
    {
        try
        {
            _logger.LogDebug("Deleting file from GridFS: {FileId}", fileId);

            await _gridFsBucket.DeleteAsync(fileId);

            _logger.LogInformation("Successfully deleted file from GridFS: {FileId}", fileId);
        }
        catch (GridFSFileNotFoundException)
        {
            _logger.LogWarning("Attempted to delete non-existent GridFS file: {FileId}", fileId);
            // Don't throw - file already doesn't exist, which is the desired state
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from GridFS: {FileId}", fileId);
            throw new InvalidOperationException($"Failed to delete file {fileId}", ex);
        }
    }

    /// <summary>
    /// Get file information without downloading content.
    /// Used for file metadata queries and validation without transferring large files.
    /// Returns information like file size, upload date, and custom metadata.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId</param>
    /// <returns>File metadata if found, null if file doesn't exist</returns>
    public async Task<GridFsFileInfo?> GetFileInfoAsync(ObjectId fileId)
    {
        try
        {
            _logger.LogDebug("Getting GridFS file info: {FileId}", fileId);

            // MongoDB 3.x: Use manual cursor iteration (most compatible approach)
            var filter = new MongoDB.Bson.BsonDocument("_id", fileId);
            var cursor = await _gridFsBucket.FindAsync(filter);

            GridFSFileInfo? fileInfo = null;

            // Manual iteration - works with all MongoDB.Driver versions
            while (await cursor.MoveNextAsync())
            {
                var batch = cursor.Current;
                fileInfo = batch.FirstOrDefault();
                if (fileInfo != null) break;
            }

            if (fileInfo == null)
            {
                _logger.LogDebug("GridFS file info not found: {FileId}", fileId);
                return null;
            }

            // Convert Metadata safely for MongoDB 3.x
            var metadata = new Dictionary<string, object>();
            if (fileInfo.Metadata != null)
            {
                foreach (var element in fileInfo.Metadata.Elements)
                {
                    // Handle different BSON value types safely
                    var value = element.Value switch
                    {
                        MongoDB.Bson.BsonString bsonString => bsonString.Value,
                        MongoDB.Bson.BsonInt32 bsonInt => bsonInt.Value.ToString(),
                        MongoDB.Bson.BsonInt64 bsonLong => bsonLong.Value.ToString(),
                        MongoDB.Bson.BsonDouble bsonDouble => bsonDouble.Value.ToString(),
                        MongoDB.Bson.BsonBoolean bsonBool => bsonBool.Value.ToString(),
                        MongoDB.Bson.BsonDateTime bsonDate => bsonDate.ToUniversalTime().ToString(),
                        _ => element.Value.ToString() ?? ""
                    };
                    metadata[element.Name] = value;
                }
            }

            var result = new GridFsFileInfo
            {
                Id = fileInfo.Id,
                Filename = fileInfo.Filename,
                Length = fileInfo.Length,
                UploadDateTime = fileInfo.UploadDateTime,
                Metadata = metadata
            };

            _logger.LogDebug("Retrieved GridFS file info: {FileId}, Size: {Size} bytes",
                fileId, fileInfo.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GridFS file info: {FileId}", fileId);
            throw new InvalidOperationException($"Failed to get file info for {fileId}", ex);
        }
    }
}