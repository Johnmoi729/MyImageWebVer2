using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyImage.Core.Entities;

/// <summary>
/// User entity representing both customers and administrators in the system.
/// This entity implements the user registration requirement where users receive
/// a human-readable User ID upon registration (e.g., USR-2024-001234).
/// </summary>
public class User
{
    /// <summary>
    /// MongoDB ObjectId - primary key for database operations
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Human-readable unique identifier generated upon registration.
    /// Format: USR-YYYY-NNNNNN (e.g., USR-2024-001234)
    /// This fulfills the requirement that "user id and password will be provided to the user"
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address - must be unique across the system.
    /// Can be used alternatively to UserId for login authentication.
    /// </summary>
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password - never store passwords in plain text.
    /// The hash includes salt for security against rainbow table attacks.
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User profile information embedded document
    /// </summary>
    [BsonElement("profile")]
    public UserProfile Profile { get; set; } = new();

    /// <summary>
    /// User role for authorization - "customer" or "admin"
    /// Determines access to administrative functions
    /// </summary>
    [BsonElement("role")]
    public string Role { get; set; } = "customer";

    /// <summary>
    /// User statistics embedded document for quick dashboard access
    /// Updated when orders are placed and completed
    /// </summary>
    [BsonElement("stats")]
    public UserStats Stats { get; set; } = new();

    /// <summary>
    /// Metadata for user account management and audit trail
    /// </summary>
    [BsonElement("metadata")]
    public UserMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Embedded document for user profile information
/// Kept minimal for the 3-week project scope
/// </summary>
public class UserProfile
{
    /// <summary>
    /// User's first name for personalization and order processing
    /// </summary>
    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name for personalization and order processing
    /// </summary>
    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Computed full name property for display purposes
    /// Not stored in database - calculated on demand
    /// </summary>
    [BsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>
/// Embedded document for user statistics tracking
/// Provides quick access to user metrics without complex aggregations
/// </summary>
public class UserStats
{
    /// <summary>
    /// Total number of orders placed by this user
    /// Incremented when order status changes to "payment_verified"
    /// </summary>
    [BsonElement("totalOrders")]
    public int TotalOrders { get; set; } = 0;

    /// <summary>
    /// Total number of photos uploaded by this user
    /// Incremented during bulk upload operations
    /// </summary>
    [BsonElement("totalPhotosUploaded")]
    public int TotalPhotosUploaded { get; set; } = 0;

    /// <summary>
    /// Total amount spent across all completed orders
    /// Updated when orders reach "completed" status
    /// </summary>
    [BsonElement("totalSpent")]
    public decimal TotalSpent { get; set; } = 0.00m;
}

/// <summary>
/// Embedded document for user metadata and audit information
/// </summary>
public class UserMetadata
{
    /// <summary>
    /// When the user account was created
    /// Set during registration process
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login timestamp
    /// Updated during JWT token generation in authentication
    /// </summary>
    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    // ADD this missing property:
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Account status flag for enabling/disabling users
    /// Can be used for account suspension without deletion
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}