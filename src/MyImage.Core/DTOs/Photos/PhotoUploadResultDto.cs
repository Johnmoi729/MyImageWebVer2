using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for bulk photo upload results.
/// Provides detailed feedback about which photos uploaded successfully
/// and any failures that occurred during the process.
/// </summary>
public class PhotoUploadResultDto
{
    /// <summary>
    /// Collection of successfully uploaded photos with their new IDs
    /// Each photo gets a unique identifier for future reference
    /// </summary>
    public List<UploadedPhotoDto> UploadedPhotos { get; set; } = new();

    /// <summary>
    /// Collection of photos that failed to upload with error details
    /// Allows users to understand and retry failed uploads
    /// </summary>
    public List<FailedUploadDto> FailedUploads { get; set; } = new();

    /// <summary>
    /// Total number of photos successfully uploaded
    /// Quick summary for user feedback and statistics
    /// </summary>
    public int TotalUploaded { get; set; }

    /// <summary>
    /// Original folder path for reference
    /// Useful for audit trail and user confirmation
    /// </summary>
    public string SourceFolder { get; set; } = string.Empty;
}