using System.ComponentModel.DataAnnotations;

namespace MyImage.Core.DTOs.Photos;

/// <summary>
/// Data Transfer Object for photo dimensions and metadata.
/// Used throughout the system for display and print compatibility.
/// </summary>
public class PhotoDimensionsDto
{
    /// <summary>
    /// Photo width in pixels
    /// Used for print size compatibility calculations
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Photo height in pixels
    /// Used for print size compatibility calculations
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Calculated aspect ratio for display purposes
    /// Examples: "4:3", "16:9", "1:1"
    /// Helps users understand photo orientation
    /// </summary>
    public string AspectRatio { get; set; } = string.Empty;
}