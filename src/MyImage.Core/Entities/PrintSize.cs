using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// PrintSize entity representing available print sizes and their pricing.
/// This entity implements Requirement 11 where "admin will decide the price and other things"
/// and supports Requirement 2 for users to "specify the size of the print".
/// 
/// Admins can add new sizes, update pricing, and activate/deactivate options without
/// affecting existing orders (price integrity is maintained).
/// </summary>
public class PrintSize
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Unique identifier for this print size used in API and cart operations
    /// Examples: "4x6", "5x7", "8x10", "wallet"
    /// Must be URL-safe and human-readable for REST API endpoints
    /// </summary>
    [BsonElement("sizeCode")]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-friendly display name shown to customers in the UI
    /// Examples: "Standard 4×6", "Classic 5×7", "Large 8×10"
    /// Uses × symbol for professional appearance
    /// </summary>
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Physical dimensions embedded document for print specifications
    /// Used to validate photo resolution compatibility
    /// </summary>
    [BsonElement("dimensions")]
    public PrintDimensions Dimensions { get; set; } = new();

    /// <summary>
    /// Pricing information embedded document managed by administrators
    /// Includes price history tracking for audit purposes
    /// </summary>
    [BsonElement("pricing")]
    public PrintPricing Pricing { get; set; } = new();

    /// <summary>
    /// Metadata for print size management and display ordering
    /// </summary>
    [BsonElement("metadata")]
    public PrintSizeMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Embedded document for physical print dimensions and resolution requirements
/// Essential for validating photo quality against print size requirements
/// </summary>
public class PrintDimensions
{
    /// <summary>
    /// Width of the print in specified units (typically inches)
    /// Used for display to customers and print shop specifications
    /// </summary>
    [BsonElement("width")]
    public decimal Width { get; set; }

    /// <summary>
    /// Height of the print in specified units (typically inches)
    /// Used for display to customers and print shop specifications
    /// </summary>
    [BsonElement("height")]
    public decimal Height { get; set; }

    /// <summary>
    /// Unit of measurement for width and height
    /// Standard value: "inches" for US market
    /// Could be extended to support "cm" for international markets
    /// </summary>
    [BsonElement("unit")]
    public string Unit { get; set; } = "inches";

    /// <summary>
    /// Pixel requirements embedded document for quality validation
    /// Ensures uploaded photos have sufficient resolution for good print quality
    /// </summary>
    [BsonElement("pixelRequirements")]
    public PixelRequirements PixelRequirements { get; set; } = new();
}

/// <summary>
/// Embedded document defining minimum and recommended pixel dimensions
/// Helps customers understand if their photos are suitable for specific print sizes
/// </summary>
public class PixelRequirements
{
    /// <summary>
    /// Minimum width in pixels for acceptable print quality
    /// Photos below this resolution should show a warning to customers
    /// </summary>
    [BsonElement("minWidth")]
    public int MinWidth { get; set; }

    /// <summary>
    /// Minimum height in pixels for acceptable print quality
    /// Photos below this resolution should show a warning to customers
    /// </summary>
    [BsonElement("minHeight")]
    public int MinHeight { get; set; }

    /// <summary>
    /// Recommended width in pixels for optimal print quality
    /// Photos at or above this resolution should show as "excellent quality"
    /// </summary>
    [BsonElement("recommendedWidth")]
    public int RecommendedWidth { get; set; }

    /// <summary>
    /// Recommended height in pixels for optimal print quality
    /// Photos at or above this resolution should show as "excellent quality"
    /// </summary>
    [BsonElement("recommendedHeight")]
    public int RecommendedHeight { get; set; }
}

/// <summary>
/// Embedded document for pricing information and administrative controls
/// Supports the requirement for admin price management with audit trail
/// </summary>
public class PrintPricing
{
    /// <summary>
    /// Current base price for this print size in USD
    /// Updated by administrators through the admin interface
    /// </summary>
    [BsonElement("basePrice")]
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Currency code for pricing (typically "USD")
    /// Prepared for future international expansion
    /// </summary>
    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// When the price was last updated
    /// Used for audit trail and change tracking
    /// </summary>
    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID of the administrator who last updated the price
    /// Important for accountability in price changes
    /// </summary>
    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Embedded document for print size metadata and display management
/// Controls how print sizes appear to customers and administrators
/// </summary>
public class PrintSizeMetadata
{
    /// <summary>
    /// Whether this print size is currently available to customers
    /// Allows admins to temporarily disable sizes without deleting them
    /// Maintains order history even for inactive sizes
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for sorting print sizes in customer interface
    /// Lower numbers appear first (1, 2, 3, etc.)
    /// Allows admins to control the presentation order
    /// </summary>
    [BsonElement("sortOrder")]
    public int SortOrder { get; set; } = 1;

    /// <summary>
    /// When this print size was first created in the system
    /// Used for audit trail and historical tracking
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this print size record was last modified
    /// Updated whenever pricing or metadata changes
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}