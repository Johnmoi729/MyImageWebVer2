using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// SystemSettings entity for storing application-wide configuration settings.
/// This entity provides a flexible key-value store for system configuration
/// that can be managed by administrators without code deployment.
/// 
/// Common settings include encryption keys, tax rates, branch locations,
/// and other configurable business parameters.
/// </summary>
public class SystemSettings
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Unique identifier for this configuration setting
    /// Examples: "payment_encryption", "tax_rates", "branch_locations"
    /// Used as the lookup key for retrieving specific settings
    /// </summary>
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Configuration value stored as BsonDocument for maximum flexibility
    /// Can contain simple values, complex objects, or arrays
    /// Allows different setting types without schema changes
    /// </summary>
    [BsonElement("value")]
    public BsonDocument Value { get; set; } = new();

    /// <summary>
    /// Setting metadata for management and audit purposes
    /// Tracks when settings were changed and by whom
    /// </summary>
    [BsonElement("metadata")]
    public SettingMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Metadata for system settings management and audit trail
/// Provides accountability for configuration changes
/// </summary>
public class SettingMetadata
{
    /// <summary>
    /// Human-readable description of what this setting controls
    /// Helps administrators understand the purpose and impact
    /// </summary>
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When this setting was last updated
    /// Important for tracking configuration changes over time
    /// </summary>
    [BsonElement("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated this setting
    /// Critical for accountability in configuration management
    /// Typically stores admin user ID or "system" for automated updates
    /// </summary>
    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = "system";
}

/// <summary>
/// Static class containing standard setting keys used throughout the application
/// Provides compile-time checking and centralized key management
/// </summary>
public static class SystemSettingKeys
{
    /// <summary>
    /// RSA encryption keys for credit card data protection
    /// Value contains: { publicKey: "-----BEGIN PUBLIC KEY-----...", algorithm: "RSA-OAEP", keySize: 2048 }
    /// Public key is shared with frontend for client-side encryption
    /// Private key is stored securely in environment variables, not in database
    /// </summary>
    public const string PaymentEncryption = "payment_encryption";

    /// <summary>
    /// Tax rate configuration by state and locality
    /// Value contains: { default: 0.0625, byState: { "MA": 0.0625, "NH": 0.0000, "NY": 0.0800 } }
    /// Used for calculating order totals based on shipping address
    /// </summary>
    public const string TaxRates = "tax_rates";

    /// <summary>
    /// Available branch locations for in-person payment option
    /// Value contains: [{ name: "Boston Downtown", address: "123 Main St", phone: "+1-617-555-0100" }]
    /// Used in checkout process when customer selects branch payment
    /// </summary>
    public const string BranchLocations = "branch_locations";

    /// <summary>
    /// File upload limits and validation rules
    /// Value contains: { maxFileSizeBytes: 52428800, allowedExtensions: [".jpg", ".jpeg"] }
    /// Configures photo upload restrictions and validation
    /// </summary>
    public const string FileUploadSettings = "file_upload_settings";

    /// <summary>
    /// Email notification templates and settings
    /// Value contains: { orderConfirmation: {...}, paymentReceived: {...}, orderShipped: {...} }
    /// Future expansion for automated customer communications
    /// </summary>
    public const string EmailTemplates = "email_templates";

    /// <summary>
    /// Photo cleanup schedule and retention policies
    /// Value contains: { retentionDays: 7, cleanupSchedule: "daily", bufferDays: 3 }
    /// Controls when photos are deleted after order completion
    /// </summary>
    public const string PhotoCleanupSettings = "photo_cleanup_settings";
}