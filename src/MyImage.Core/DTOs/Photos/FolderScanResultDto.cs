using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for folder scan results.
/// Contains all JPEG files found in the selected folder,
/// allowing users to choose which photos to upload.
/// </summary>
public class FolderScanResultDto
{
    /// <summary>
    /// Original folder path that was scanned
    /// Echoed back for user confirmation and frontend reference
    /// </summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>
    /// Collection of JPEG files found in the folder
    /// Only includes .jpg and .jpeg files per requirements
    /// Other image formats are filtered out automatically
    /// </summary>
    public List<JpegFileDto> JpegFiles { get; set; } = new();

    /// <summary>
    /// Total number of JPEG files found
    /// Provides quick summary for user interface display
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Combined size of all JPEG files in bytes
    /// Helps users understand total upload size and time requirements
    /// </summary>
    public long TotalSize { get; set; }
}