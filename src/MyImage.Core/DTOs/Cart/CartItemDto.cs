using System.ComponentModel.DataAnnotations;
using MyImage.Core.DTOs.Photos;

namespace MyImage.Core.DTOs.Cart;

/// <summary>
/// Data Transfer Object for individual cart items in the shopping cart.
/// Contains photo details and all print selections for that photo.
/// </summary>
public class CartItemDto
{
    /// <summary>
    /// Unique identifier for this cart item
    /// Used for updating or removing specific items
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Photo identifier for reference and validation
    /// Links to the original photo in user's gallery
    /// </summary>
    public string PhotoId { get; set; } = string.Empty;

    /// <summary>
    /// Original photo filename for user recognition
    /// Helps users identify which photo they're ordering
    /// </summary>
    public string PhotoFilename { get; set; } = string.Empty;

    /// <summary>
    /// Thumbnail URL for cart display
    /// Allows users to see the photo while reviewing their cart
    /// </summary>
    public string PhotoThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// Photo dimensions for print quality reference
    /// Helps users understand if their photo is suitable for selected sizes
    /// </summary>
    public PhotoDimensionsDto PhotoDimensions { get; set; } = new();

    /// <summary>
    /// Collection of print selections for this photo
    /// Each selection represents one print size with quantity and pricing
    /// </summary>
    public List<CartPrintSelectionDto> PrintSelections { get; set; } = new();

    /// <summary>
    /// Total cost for this photo across all print selections
    /// Sum of all lineTotal values in PrintSelections
    /// </summary>
    public decimal PhotoTotal { get; set; }

    /// <summary>
    /// When this photo was added to the cart
    /// Used for cart organization and expiration tracking
    /// </summary>
    public DateTime AddedAt { get; set; }
}