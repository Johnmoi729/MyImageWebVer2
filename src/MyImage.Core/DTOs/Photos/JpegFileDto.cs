using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object representing a JPEG file found during folder scanning.
/// This represents individual files that users can select for upload,
/// implementing the JPEG-only filtering requirement.
/// </summary>
public class JpegFileDto
{
    /// <summary>
    /// Original filename as it appears in the user's folder
    /// Example: "IMG_001.jpg", "vacation-sunset.jpeg"
    /// Preserved for user recognition during selection process
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes for upload planning and validation
    /// Used to check against maximum file size limits (50MB per requirements)
    /// Also helps users understand storage requirements
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When the file was last modified in the user's folder
    /// Helps users identify recent photos and organize their selections
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Complete file path for upload reference
    /// Used by the frontend to locate the file for actual upload
    /// Not stored permanently - only used during upload process
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
}