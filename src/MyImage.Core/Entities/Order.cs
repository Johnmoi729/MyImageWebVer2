using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// Order entity representing the complete order lifecycle from placement to completion.
/// This entity implements multiple requirements:
/// - Requirement 6: Payment method selection and shipping address
/// - Requirement 7: Credit card encryption and verification
/// - Requirement 8: Purchase request creation and order number generation
/// - Requirement 9: Admin order processing workflow
/// - Requirement 10: Photo cleanup after order completion
/// </summary>
public class Order
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Human-readable order number for customer reference and support
    /// Format: ORD-YYYY-NNNNNNN (e.g., ORD-2024-0001234)
    /// Used in customer communications and admin interface
    /// </summary>
    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the User who placed this order
    /// Used for order history queries and access control
    /// </summary>
    [BsonElement("userId")]
    public ObjectId UserId { get; set; }

    /// <summary>
    /// Snapshot of user information at time of order placement
    /// Preserves customer details even if user account changes later
    /// Essential for order fulfillment and customer service
    /// </summary>
    [BsonElement("userInfo")]
    public OrderUserInfo UserInfo { get; set; } = new();

    /// <summary>
    /// Current order status for workflow management
    /// Flow: pending → payment_verified → processing → printed → shipped → completed
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Collection of ordered items with their print selections
    /// Snapshot from shopping cart at time of order placement
    /// Preserves exact selections even if cart changes afterward
    /// </summary>
    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    /// <summary>
    /// Pricing breakdown for this order including tax calculations
    /// Locked at time of order placement to maintain price integrity
    /// </summary>
    [BsonElement("pricing")]
    public OrderPricing Pricing { get; set; } = new();

    /// <summary>
    /// Payment information and processing status
    /// Handles both credit card and branch payment methods
    /// Implements Requirement 3: credit card encryption
    /// </summary>
    [BsonElement("payment")]
    public OrderPayment Payment { get; set; } = new();

    /// <summary>
    /// Customer shipping address for order fulfillment
    /// Required for both payment methods (credit card and branch payment)
    /// </summary>
    [BsonElement("shippingAddress")]
    public ShippingAddress ShippingAddress { get; set; } = new();

    /// <summary>
    /// Order fulfillment tracking information managed by admin
    /// Implements Requirement 9: admin order processing
    /// </summary>
    [BsonElement("fulfillment")]
    public OrderFulfillment Fulfillment { get; set; } = new();

    /// <summary>
    /// Photo cleanup tracking for post-completion deletion
    /// Implements Requirement 10: delete photos after shipping
    /// </summary>
    [BsonElement("photoCleanup")]
    public PhotoCleanup PhotoCleanup { get; set; } = new();

    /// <summary>
    /// Order metadata for audit trail and management
    /// </summary>
    [BsonElement("metadata")]
    public OrderMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Snapshot of user information at time of order placement
/// Preserves customer contact details for order fulfillment
/// </summary>
public class OrderUserInfo
{
    /// <summary>
    /// User's readable ID for customer service reference
    /// Example: "USR-2024-001234"
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address for order notifications and support
    /// Preserved even if user changes email later
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name for shipping labels and customer service
    /// Derived from user profile at time of order placement
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Individual order item representing one photo with its print selections
/// Captures the exact state of cart item at time of order placement
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Reference to the original Photo entity
    /// Used for photo retrieval during printing process
    /// </summary>
    [BsonElement("photoId")]
    public ObjectId PhotoId { get; set; }

    /// <summary>
    /// Original filename for print shop reference and customer recognition
    /// Example: "IMG_001.jpg"
    /// </summary>
    [BsonElement("photoFilename")]
    public string PhotoFilename { get; set; } = string.Empty;

    /// <summary>
    /// Photo file size for cleanup tracking
    /// Used to calculate total storage freed when photos are deleted
    /// </summary>
    [BsonElement("photoFileSize")]
    public long PhotoFileSize { get; set; }

    /// <summary>
    /// Collection of print selections for this photo
    /// Locked snapshot from cart - prices cannot change after order placement
    /// </summary>
    [BsonElement("printSelections")]
    public List<OrderPrintSelection> PrintSelections { get; set; } = new();

    /// <summary>
    /// Total cost for this photo across all its print selections
    /// Sum of all printSelections[].subtotal values
    /// </summary>
    [BsonElement("photoTotal")]
    public decimal PhotoTotal { get; set; }
}

/// <summary>
/// Print selection within an order item with locked pricing
/// Preserves exact print specifications and prices from time of order
/// </summary>
public class OrderPrintSelection
{
    /// <summary>
    /// Print size code for print shop specifications
    /// Example: "4x6", "5x7", "8x10"
    /// </summary>
    [BsonElement("sizeCode")]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable size name for order documentation
    /// Example: "Standard 4×6", "Classic 5×7"
    /// </summary>
    [BsonElement("sizeName")]
    public string SizeName { get; set; } = string.Empty;

    /// <summary>
    /// Number of copies to be printed
    /// Final quantity locked at time of order placement
    /// </summary>
    [BsonElement("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Price per unit at time of order placement
    /// Locked to protect customer from price changes during processing
    /// </summary>
    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total cost for this print selection (quantity × unitPrice)
    /// Pre-calculated for order processing efficiency
    /// </summary>
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }
}

/// <summary>
/// Order pricing breakdown with tax calculations
/// All values locked at time of order placement for price integrity
/// </summary>
public class OrderPricing
{
    /// <summary>
    /// Total before tax across all order items
    /// Sum of all orderItems[].photoTotal values
    /// </summary>
    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tax rate applied based on shipping address
    /// Determined by state/locality at time of order placement
    /// </summary>
    [BsonElement("taxRate")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Calculated tax amount (subtotal × taxRate)
    /// Applied based on shipping address state
    /// </summary>
    [BsonElement("taxAmount")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Final total amount charged to customer (subtotal + taxAmount)
    /// This is the amount processed for payment
    /// </summary>
    [BsonElement("total")]
    public decimal Total { get; set; }

    /// <summary>
    /// Currency code for international expansion support
    /// Currently "USD" for US market
    /// </summary>
    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";
}

/// <summary>
/// Payment information and processing status
/// Implements Requirement 3: encrypted credit card handling
/// Supports both credit card and branch payment methods
/// </summary>
public class OrderPayment
{
    /// <summary>
    /// Payment method chosen by customer
    /// Values: "credit_card" or "branch_payment"
    /// Determines processing workflow
    /// </summary>
    [BsonElement("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Payment processing status
    /// Values: "pending", "verified", "failed", "refunded"
    /// Managed by admin during payment verification
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Credit card information (encrypted data only)
    /// NEVER stores unencrypted card numbers or CVV codes
    /// Implements secure payment data handling
    /// </summary>
    [BsonElement("creditCard")]
    public CreditCardInfo? CreditCard { get; set; }

    /// <summary>
    /// Branch payment information for in-person payment option
    /// Alternative to credit card for customers preferring cash/check
    /// </summary>
    [BsonElement("branchPayment")]
    public BranchPaymentInfo? BranchPayment { get; set; }

    /// <summary>
    /// When payment was verified by admin
    /// Set during admin payment verification process
    /// </summary>
    [BsonElement("verifiedAt")]
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Admin user who verified the payment
    /// Important for accountability and audit trail
    /// </summary>
    [BsonElement("verifiedBy")]
    public string? VerifiedBy { get; set; }
}

/// <summary>
/// Credit card information with security compliance
/// ONLY stores last four digits - all sensitive data is encrypted
/// </summary>
public class CreditCardInfo
{
    /// <summary>
    /// Last four digits of card number for customer reference
    /// Only non-sensitive data stored in database
    /// Used for customer service and order confirmation
    /// </summary>
    [BsonElement("lastFour")]
    public string LastFour { get; set; } = string.Empty;

    /// <summary>
    /// Cardholder name as entered by customer
    /// Used for payment processing and verification
    /// </summary>
    [BsonElement("cardholderName")]
    public string CardholderName { get; set; } = string.Empty;

    // NOTE: Full card number and CVV are NEVER stored in database
    // They are encrypted client-side and processed immediately
    // Only encrypted payment tokens are handled server-side
}

/// <summary>
/// Branch payment information for in-person payment option
/// Provides alternative to credit card processing
/// </summary>
public class BranchPaymentInfo
{
    /// <summary>
    /// Selected branch location for payment
    /// Must match configured branch locations in SystemSettings
    /// </summary>
    [BsonElement("preferredBranch")]
    public string PreferredBranch { get; set; } = string.Empty;

    /// <summary>
    /// Reference number for branch payment tracking
    /// Format: BP-YYYY-NNNNNNN for easy identification
    /// Provided to customer for branch payment reference
    /// </summary>
    [BsonElement("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;
}

/// <summary>
/// Customer shipping address for order fulfillment
/// Required regardless of payment method
/// </summary>
public class ShippingAddress
{
    /// <summary>
    /// Recipient's full name for shipping label
    /// May differ from account holder name
    /// </summary>
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Primary street address line
    /// Required for all shipments
    /// </summary>
    [BsonElement("streetLine1")]
    public string StreetLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary address line (apartment, suite, etc.)
    /// Optional field for additional address details
    /// </summary>
    [BsonElement("streetLine2")]
    public string StreetLine2 { get; set; } = string.Empty;

    /// <summary>
    /// City name for shipping address
    /// Required for tax calculation and shipping
    /// </summary>
    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State/province code for tax calculation
    /// Used to determine applicable tax rates
    /// </summary>
    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code for shipping and tax calculation
    /// Required for accurate shipping cost and tax determination
    /// </summary>
    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country code for international expansion
    /// Currently "USA" for domestic market
    /// </summary>
    [BsonElement("country")]
    public string Country { get; set; } = "USA";

    /// <summary>
    /// Contact phone number for delivery coordination
    /// Optional but recommended for shipping carrier contact
    /// </summary>
    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Order fulfillment tracking managed by admin users
/// Implements Requirement 9: admin order processing workflow
/// </summary>
public class OrderFulfillment
{
    /// <summary>
    /// When order was sent to print queue
    /// Set by admin when order moves to "processing" status
    /// </summary>
    [BsonElement("printedAt")]
    public DateTime? PrintedAt { get; set; }

    /// <summary>
    /// When order was shipped to customer
    /// Set by admin when order moves to "shipped" status
    /// </summary>
    [BsonElement("shippedAt")]
    public DateTime? ShippedAt { get; set; }

    /// <summary>
    /// Shipping carrier tracking number for customer reference
    /// Optional but recommended for customer service
    /// </summary>
    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// When order was marked as completed
    /// Triggers photo cleanup process
    /// </summary>
    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Administrative notes for order processing
    /// Used for special instructions or issue tracking
    /// </summary>
    [BsonElement("notes")]
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Photo cleanup tracking for post-completion deletion
/// Implements Requirement 10: delete photos after order completion
/// </summary>
public class PhotoCleanup
{
    /// <summary>
    /// Whether photo cleanup has been completed for this order
    /// Set to true after all photos are successfully deleted
    /// </summary>
    [BsonElement("isCompleted")]
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Number of photos successfully deleted
    /// Used for verification and statistics
    /// </summary>
    [BsonElement("photosDeleted")]
    public int PhotosDeleted { get; set; } = 0;

    /// <summary>
    /// Total storage space freed in bytes
    /// Calculated from sum of photoFileSize values
    /// </summary>
    [BsonElement("storageFreed")]
    public long StorageFreed { get; set; } = 0;

    /// <summary>
    /// When photo cleanup was completed
    /// Used for audit trail and cleanup verification
    /// </summary>
    [BsonElement("cleanupDate")]
    public DateTime? CleanupDate { get; set; }
}

/// <summary>
/// Order metadata for audit trail and management
/// </summary>
public class OrderMetadata
{
    /// <summary>
    /// When the order was initially created
    /// Set when customer completes checkout process
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the order was last modified
    /// Updated during status changes and admin actions
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}