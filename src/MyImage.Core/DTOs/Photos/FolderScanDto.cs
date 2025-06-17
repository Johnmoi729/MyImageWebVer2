using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for folder scanning requests.
/// This DTO implements Requirement 1: "system should be able to display the contents of desktop folder"
/// 
/// The folder scanning process happens client-side for security, but the server validates
/// the request and processes the file list to identify JPEG files only.
/// </summary>
public class FolderScanDto
{
    /// <summary>
    /// Path to the folder selected by the user during folder browsing
    /// Example: "C:\Users\John\Pictures\Vacation2024"
    /// Used for audit trail and user reference in photo metadata
    /// </summary>
    [Required(ErrorMessage = "Folder path is required")]
    [MaxLength(500, ErrorMessage = "Folder path cannot exceed 500 characters")]
    public string FolderPath { get; set; } = string.Empty;
}