using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MyImage.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyImage.Infrastructure.Data;

/// <summary>
/// MongoDB database context providing access to collections and GridFS.
/// This class centralizes database configuration and connection management,
/// implementing the MongoDB integration specified in the project architecture.
/// 
/// The context provides strongly-typed collection access and GridFS bucket
/// for photo storage, following the database design in project knowledge.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbContext> _logger;

    /// <summary>
    /// GridFS bucket for photo and thumbnail storage.
    /// Configured with chunking for efficient large file handling.
    /// Bucket name and chunk size are configurable in appsettings.
    /// </summary>
    public IGridFSBucket GridFsBucket { get; }

    /// <summary>
    /// Users collection with proper indexing for authentication.
    /// Supports both email and User ID lookups with unique constraints.
    /// </summary>
    public IMongoCollection<User> Users { get; }

    /// <summary>
    /// Photos collection with user ownership and lifecycle tracking.
    /// Indexed for efficient user photo queries and cleanup operations.
    /// </summary>
    public IMongoCollection<Photo> Photos { get; }

    /// <summary>
    /// Print sizes collection for pricing and size management.
    /// Indexed by size code for quick cart operations.
    /// </summary>
    public IMongoCollection<PrintSize> PrintSizes { get; }

    /// <summary>
    /// Shopping carts collection with automatic expiration.
    /// TTL index automatically removes expired carts.
    /// </summary>
    public IMongoCollection<ShoppingCart> ShoppingCarts { get; }

    /// <summary>
    /// Orders collection with comprehensive indexing for admin and customer queries.
    /// Supports efficient filtering by status, user, and date ranges.
    /// </summary>
    public IMongoCollection<Order> Orders { get; }

    /// <summary>
    /// System settings collection for application configuration.
    /// Indexed by key for fast configuration lookup.
    /// </summary>
    public IMongoCollection<SystemSettings> SystemSettings { get; }

    /// <summary>
    /// Initialize MongoDB context with connection and collection setup.
    /// Creates database connection, configures GridFS, and ensures indexes exist.
    /// </summary>
    /// <param name="configuration">Application configuration for connection string</param>
    /// <param name="logger">Logger for database operations</param>
    public MongoDbContext(IConfiguration configuration, ILogger<MongoDbContext> logger)
    {
        _logger = logger;

        // Get MongoDB connection string from configuration
        var connectionString = configuration.GetConnectionString("MongoDB")
            ?? throw new InvalidOperationException("MongoDB connection string not found");

        // Parse database name from connection string or use default
        var mongoUrl = MongoUrl.Create(connectionString);
        var databaseName = mongoUrl.DatabaseName ?? "myimage";

        _logger.LogInformation("Connecting to MongoDB database: {DatabaseName}", databaseName);

        // Create MongoDB client with optimal settings for photo upload performance
        var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
        clientSettings.MaxConnectionPoolSize = 100; // Support concurrent photo uploads
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(30);
        clientSettings.SocketTimeout = TimeSpan.FromMinutes(5); // Long timeout for large file operations

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(databaseName);

        // Configure GridFS bucket for photo storage
        var gridFsBucketOptions = new GridFSBucketOptions
        {
            BucketName = configuration["GridFsSettings:BucketName"] ?? "photos",
            ChunkSizeBytes = int.TryParse(configuration["GridFsSettings:ChunkSizeBytes"], out var chunkSize) ? chunkSize : 1048576 // 1MB chunks
        };
        GridFsBucket = new GridFSBucket(_database, gridFsBucketOptions);

        // Initialize collections with proper typing
        Users = _database.GetCollection<User>("users");
        Photos = _database.GetCollection<Photo>("photos");
        PrintSizes = _database.GetCollection<PrintSize>("printSizes");
        ShoppingCarts = _database.GetCollection<ShoppingCart>("shoppingCarts");
        Orders = _database.GetCollection<Order>("orders");
        SystemSettings = _database.GetCollection<SystemSettings>("systemSettings");

        // Ensure indexes exist for optimal performance
        Task.Run(async () => await CreateIndexesAsync());

        _logger.LogInformation("MongoDB context initialized successfully");
    }

    /// <summary>
    /// Create database indexes for optimal query performance.
    /// This method ensures all required indexes exist for efficient operations.
    /// Called automatically during context initialization.
    /// </summary>
    private async Task CreateIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Creating MongoDB indexes...");

            // Users collection indexes for authentication and uniqueness
            await Users.Indexes.CreateManyAsync(new[]
            {
                // Unique index on email for registration validation and login
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions { Unique = true, Name = "email_unique" }
                ),
                // Unique index on User ID for login and lookup
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.UserId),
                    new CreateIndexOptions { Unique = true, Name = "userId_unique" }
                ),
                // Index on role for admin operations
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.Role),
                    new CreateIndexOptions { Name = "role_index" }
                ),
                // Index on active status for user management
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending("metadata.isActive"),
                    new CreateIndexOptions { Name = "active_status_index" }
                )
            });

            // Photos collection indexes for user queries and cleanup operations
            await Photos.Indexes.CreateManyAsync(new[]
            {
                // Compound index for user photo queries with date sorting
                new CreateIndexModel<Photo>(
                    Builders<Photo>.IndexKeys
                        .Ascending(p => p.UserId)
                        .Descending("metadata.createdAt"),
                    new CreateIndexOptions { Name = "user_photos_by_date" }
                ),
                // Index for order status tracking
                new CreateIndexModel<Photo>(
                    Builders<Photo>.IndexKeys.Ascending("orderInfo.isOrdered"),
                    new CreateIndexOptions { Name = "ordered_status_index" }
                ),
                // Index for cleanup operations
                new CreateIndexModel<Photo>(
                    Builders<Photo>.IndexKeys.Ascending("flags.isPendingDeletion"),
                    new CreateIndexOptions { Name = "pending_deletion_index" }
                ),
                // Index for scheduled deletion date
                new CreateIndexModel<Photo>(
                    Builders<Photo>.IndexKeys.Ascending("flags.deletionScheduledFor"),
                    new CreateIndexOptions { Name = "deletion_schedule_index" }
                ),
                // Index for soft delete status
                new CreateIndexModel<Photo>(
                    Builders<Photo>.IndexKeys.Ascending("flags.isDeleted"),
                    new CreateIndexOptions { Name = "deleted_status_index" }
                )
            });

            // Print sizes collection indexes for cart operations
            await PrintSizes.Indexes.CreateManyAsync(new[]
            {
                // Unique index on size code for cart operations
                new CreateIndexModel<PrintSize>(
                    Builders<PrintSize>.IndexKeys.Ascending(ps => ps.SizeCode),
                    new CreateIndexOptions { Unique = true, Name = "size_code_unique" }
                ),
                // Index for active sizes display
                new CreateIndexModel<PrintSize>(
                    Builders<PrintSize>.IndexKeys.Ascending("metadata.isActive"),
                    new CreateIndexOptions { Name = "active_sizes_index" }
                ),
                // Compound index for active sizes sorted by display order
                new CreateIndexModel<PrintSize>(
                    Builders<PrintSize>.IndexKeys
                        .Ascending("metadata.isActive")
                        .Ascending("metadata.sortOrder"),
                    new CreateIndexOptions { Name = "active_sizes_sorted" }
                )
            });

            // Shopping carts collection indexes with TTL for automatic cleanup
            await ShoppingCarts.Indexes.CreateManyAsync(new[]
            {
                // Unique index on user ID (one cart per user)
                new CreateIndexModel<ShoppingCart>(
                    Builders<ShoppingCart>.IndexKeys.Ascending(sc => sc.UserId),
                    new CreateIndexOptions { Unique = true, Name = "user_cart_unique" }
                ),
                // TTL index for automatic cart expiration (2 weeks)
                new CreateIndexModel<ShoppingCart>(
                    Builders<ShoppingCart>.IndexKeys.Ascending("metadata.expiresAt"),
                    new CreateIndexOptions
                    {
                        ExpireAfter = TimeSpan.Zero, // Expire immediately when expiresAt is reached
                        Name = "cart_expiration_ttl"
                    }
                ),
                // Index on last updated for cleanup queries
                new CreateIndexModel<ShoppingCart>(
                    Builders<ShoppingCart>.IndexKeys.Descending("metadata.updatedAt"),
                    new CreateIndexOptions { Name = "cart_last_updated" }
                )
            });

            // Orders collection indexes for admin and customer queries
            await Orders.Indexes.CreateManyAsync(new[]
            {
                // Unique index on order number for customer lookup
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(o => o.OrderNumber),
                    new CreateIndexOptions { Unique = true, Name = "order_number_unique" }
                ),
                // Compound index for user orders by date
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys
                        .Ascending(o => o.UserId)
                        .Descending("metadata.createdAt"),
                    new CreateIndexOptions { Name = "user_orders_by_date" }
                ),
                // Index on order status for admin filtering
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending(o => o.Status),
                    new CreateIndexOptions { Name = "order_status_index" }
                ),
                // Index on payment status for admin workflow
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending("payment.status"),
                    new CreateIndexOptions { Name = "payment_status_index" }
                ),
                // Index for photo cleanup tracking
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys.Ascending("photoCleanup.isCompleted"),
                    new CreateIndexOptions { Name = "photo_cleanup_index" }
                ),
                // Compound index for completed orders by date range (reporting)
                new CreateIndexModel<Order>(
                    Builders<Order>.IndexKeys
                        .Ascending(o => o.Status)
                        .Ascending("fulfillment.completedAt"),
                    new CreateIndexOptions { Name = "completed_orders_by_date" }
                )
            });

            // System settings collection indexes for configuration lookup
            await SystemSettings.Indexes.CreateManyAsync(new[]
            {
                // Unique index on setting key for fast configuration lookup
                new CreateIndexModel<SystemSettings>(
                    Builders<SystemSettings>.IndexKeys.Ascending(ss => ss.Key),
                    new CreateIndexOptions { Unique = true, Name = "setting_key_unique" }
                )
            });

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MongoDB indexes");
            // Don't throw - indexes are performance optimization, not critical for functionality
        }
    }

    /// <summary>
    /// Get collection with explicit typing for custom operations.
    /// Allows access to collections not explicitly defined in context.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="collectionName">Collection name</param>
    /// <returns>Typed MongoDB collection</returns>
    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }

    /// <summary>
    /// Get the underlying MongoDB database for advanced operations.
    /// Used for database-level operations like transactions (future enhancement).
    /// </summary>
    /// <returns>MongoDB database instance</returns>
    public IMongoDatabase GetDatabase()
    {
        return _database;
    }

    /// <summary>
    /// Test database connectivity.
    /// Used for health checks and startup validation.
    /// </summary>
    /// <returns>True if database is accessible</returns>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Simple ping operation to test connectivity
            await _database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB connection test failed");
            return false;
        }
    }
}