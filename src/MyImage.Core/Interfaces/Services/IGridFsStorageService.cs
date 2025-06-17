using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Services;

/// <summary>
/// Service interface for GridFS file storage operations.
/// Handles binary file storage and retrieval within MongoDB.
/// </summary>
public interface IGridFsStorageService
{
    /// <summary>
    /// Upload file to GridFS with metadata.
    /// Returns GridFS file ID for database reference.
    /// </summary>
    /// <param name="fileStream">File content stream</param>
    /// <param name="filename">Original filename</param>
    /// <param name="metadata">Additional file metadata</param>
    /// <returns>GridFS file ObjectId</returns>
    Task<ObjectId> UploadFileAsync(Stream fileStream, string filename, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Download file from GridFS by ID.
    /// Returns file stream for API response.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId</param>
    /// <returns>File stream if found</returns>
    Task<Stream?> DownloadFileAsync(ObjectId fileId);

    /// <summary>
    /// Delete file from GridFS by ID.
    /// Called during photo cleanup process.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId</param>
    /// <returns>Task completion</returns>
    Task DeleteFileAsync(ObjectId fileId);

    /// <summary>
    /// Get file information without downloading content.
    /// Used for file metadata and size information.
    /// </summary>
    /// <param name="fileId">GridFS file ObjectId</param>
    /// <returns>File metadata if found</returns>
    Task<GridFsFileInfo?> GetFileInfoAsync(ObjectId fileId);
}

/// <summary>
/// File information returned from GridFS queries.
/// Contains metadata without file content.
/// </summary>
public class GridFsFileInfo
{
    public ObjectId Id { get; set; }
    public string Filename { get; set; } = string.Empty;
    public long Length { get; set; }
    public DateTime UploadDateTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}