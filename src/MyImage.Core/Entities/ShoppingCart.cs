using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// ShoppingCart entity representing a user's current photo printing selections.
/// This entity implements Requirement 2: "user should be able to mark the photographs 
/// he wants to order and specify the size of the print and the number of copies"
/// 
/// Key feature: Supports multiple print sizes per photo (e.g., user can order both 
/// 4×6 and 5×7 prints of the same photo in a single cart item).
/// </summary>
public class ShoppingCart
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Reference to the User who owns this shopping cart
    /// Each user has exactly one active shopping cart
    /// </summary>
    [BsonElement("userId")]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// Collection of cart items - each item represents one photo with its print selections
    /// A photo can have multiple print sizes and quantities within a single cart item
    /// </summary>
    [BsonElement("items")]
    public List<CartItem> Items { get; set; } = new();

    /// <summary>
    /// Cart summary calculations for quick display without recalculation
    /// Updated whenever items are added, removed, or modified
    /// </summary>
    [BsonElement("summary")]
    public CartSummary Summary { get; set; } = new();

    /// <summary>
    /// Cart metadata including expiration for automatic cleanup
    /// Carts expire after 2 weeks of inactivity to prevent database bloat
    /// </summary>
    [BsonElement("metadata")]
    public CartMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Represents one photo in the shopping cart with all its print size selections
/// This structure allows ordering multiple sizes of the same photo efficiently
/// </summary>
public class CartItem
{
    /// <summary>
    /// Unique identifier for this cart item (not the photo ID)
    /// Allows multiple cart items for the same photo if needed
    /// </summary>
    [BsonElement("_id")]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Reference to the Photo entity this cart item represents
    /// Used to verify photo still exists and belongs to user
    /// </summary>
    [BsonElement("photoId")]
    public ObjectId PhotoId { get; set; }

    /// <summary>
    /// Denormalized photo details for cart display performance
    /// Avoids needing to join with Photos collection for cart viewing
    /// Updated when photo metadata changes (rare after upload)
    /// </summary>
    [BsonElement("photoDetails")]
    public CartPhotoDetails PhotoDetails { get; set; } = new();

    /// <summary>
    /// Collection of print selections for this photo
    /// Each selection represents one print size with its quantity and pricing
    /// Example: Same photo could have 10×4×6 prints + 2×5×7 prints
    /// </summary>
    [BsonElement("printSelections")]
    public List<PrintSelection> PrintSelections { get; set; } = new();

    /// <summary>
    /// Total cost for this photo across all print selections
    /// Sum of all printSelections[].lineTotal values
    /// Cached for performance during cart calculations
    /// </summary>
    [BsonElement("photoTotal")]
    public decimal PhotoTotal { get; set; }

    /// <summary>
    /// When this photo was added to the cart
    /// Used for cart expiration and user experience
    /// </summary>
    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this cart item was last modified (quantity changes, etc.)
    /// Updated whenever print selections are modified
    /// </summary>
    [BsonElement("lastModified")]
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Denormalized photo information stored in cart for performance
/// Reduces database queries when displaying cart contents
/// </summary>
public class CartPhotoDetails
{
    /// <summary>
    /// Original filename for user recognition
    /// Example: "IMG_001.jpg" from their vacation folder
    /// </summary>
    [BsonElement("filename")]
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint for retrieving photo thumbnail
    /// Format: "/api/photos/{photoId}/thumbnail"
    /// Used by frontend for cart display
    /// </summary>
    [BsonElement("thumbnailUrl")]
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// Photo dimensions for print size compatibility display
    /// Helps user understand if photo resolution is sufficient
    /// </summary>
    [BsonElement("dimensions")]
    public PhotoDimensions Dimensions { get; set; } = new();
}

/// <summary>
/// Represents one print size selection within a cart item
/// This is where users specify "size of the print and number of copies"
/// </summary>
public class PrintSelection
{
    /// <summary>
    /// Print size identifier matching PrintSize.SizeCode
    /// Example: "4x6", "5x7", "8x10"
    /// Used to link with current pricing information
    /// </summary>
    [BsonElement("sizeCode")]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable size name for display
    /// Example: "Standard 4×6", "Classic 5×7"
    /// Denormalized from PrintSize for cart display performance
    /// </summary>
    [BsonElement("sizeName")]
    public string SizeName { get; set; } = string.Empty;

    /// <summary>
    /// Number of copies requested for this size
    /// Must be positive integer - validated in business logic
    /// </summary>
    [BsonElement("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit for this print size at time of cart addition
    /// Locked-in price protects customer from price changes during shopping
    /// </summary>
    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total cost for this print selection (quantity × unitPrice)
    /// Calculated and cached for performance
    /// </summary>
    [BsonElement("lineTotal")]
    public decimal LineTotal { get; set; }
}

/// <summary>
/// Cart totals and statistics for quick display and checkout
/// Pre-calculated to avoid real-time computation during cart viewing
/// </summary>
public class CartSummary
{
    /// <summary>
    /// Total number of unique photos in the cart
    /// Each photo counts once regardless of how many sizes are selected
    /// </summary>
    [BsonElement("totalPhotos")]
    public int TotalPhotos { get; set; }

    /// <summary>
    /// Total number of individual prints across all photos and sizes
    /// Sum of all quantity values across all print selections
    /// </summary>
    [BsonElement("totalPrints")]
    public int TotalPrints { get; set; }

    /// <summary>
    /// Subtotal before tax calculation
    /// Sum of all photoTotal values across all cart items
    /// </summary>
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Estimated tax amount based on default tax rate
    /// Actual tax calculated during checkout based on shipping address
    /// </summary>
    [BsonElement("estimatedTax")]
    public decimal EstimatedTax { get; set; }

    /// <summary>
    /// Estimated total including tax
    /// Final total calculated during checkout with actual shipping address
    /// </summary>
    [BsonElement("estimatedTotal")]
    public decimal EstimatedTotal { get; set; }
}

/// <summary>
/// Cart metadata for automatic cleanup and audit trail
/// </summary>
public class CartMetadata
{
    /// <summary>
    /// When the cart was initially created
    /// Set when user adds their first item
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the cart was last modified (items added/removed/updated)
    /// Used to determine cart activity for expiration
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this cart should be automatically deleted
    /// Set to 2 weeks from last update to keep database clean
    /// MongoDB TTL index will automatically remove expired carts
    /// </summary>
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(14);
}