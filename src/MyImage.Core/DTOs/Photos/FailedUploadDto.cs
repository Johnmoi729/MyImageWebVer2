using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photos that failed to upload.
/// Provides specific error information to help users resolve issues.
/// </summary>
public class FailedUploadDto
{
    /// <summary>
    /// Original filename of the photo that failed
    /// Helps users identify which specific photo had issues
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// Specific error message explaining why the upload failed
    /// Examples: "File too large", "Invalid file format", "Server error"
    /// Used to guide user action for retry attempts
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling by frontend
    /// Examples: "FILE_TOO_LARGE", "INVALID_FORMAT", "SERVER_ERROR"
    /// Allows frontend to implement specific retry logic
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;
}