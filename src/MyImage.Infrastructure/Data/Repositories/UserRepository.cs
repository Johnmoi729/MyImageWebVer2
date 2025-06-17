using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for User entity operations.
/// This class implements the IUserRepository interface and handles all user data access,
/// including the critical requirement for generating unique User IDs upon registration.
/// 
/// Key features:
/// - Generates human-readable User IDs (USR-YYYY-NNNNNN format)
/// - Ensures email uniqueness across registrations
/// - Supports dual authentication (email OR User ID login)
/// - Tracks user statistics and login history
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// Initialize repository with MongoDB context and logging.
    /// Sets up collection reference and configures logging for user operations.
    /// </summary>
    /// <param name="context">MongoDB context providing database access</param>
    /// <param name="logger">Logger for user repository operations</param>
    public UserRepository(MongoDbContext context, ILogger<UserRepository> logger)
    {
        _users = context.Users;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user with auto-generated User ID.
    /// This method implements the requirement that "user id and password will be provided to the user"
    /// by generating a unique, human-readable identifier during registration.
    /// 
    /// The User ID format follows: USR-YYYY-NNNNNN (e.g., USR-2024-001234)
    /// where YYYY is the current year and NNNNNN is a sequential number.
    /// </summary>
    /// <param name="user">User entity to create (UserId will be generated)</param>
    /// <returns>Created user with generated User ID</returns>
    /// <exception cref="InvalidOperationException">Thrown if email already exists</exception>
    public async Task<User> CreateAsync(User user)
    {
        try
        {
            _logger.LogInformation("Creating new user with email: {Email}", user.Email);

            // Verify email uniqueness before attempting creation
            if (await EmailExistsAsync(user.Email))
            {
                _logger.LogWarning("Attempted to create user with existing email: {Email}", user.Email);
                throw new InvalidOperationException($"Email {user.Email} is already registered");
            }

            // Generate unique User ID for this registration
            user.UserId = await GenerateNextUserIdAsync();

            // Set metadata for new user
            user.Metadata.CreatedAt = DateTime.UtcNow;
            user.Metadata.IsActive = true;

            // Initialize user statistics
            user.Stats = new UserStats();

            // Create the user in database
            await _users.InsertOneAsync(user);

            _logger.LogInformation("Successfully created user {UserId} with email {Email}",
                user.UserId, user.Email);

            return user;
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Handle race condition where email uniqueness check passed but insertion failed
            _logger.LogError(ex, "Duplicate key error creating user with email: {Email}", user.Email);
            throw new InvalidOperationException($"Email {user.Email} is already registered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user with email: {Email}", user.Email);
            throw;
        }
    }

    /// <summary>
    /// Find user by email address for login authentication.
    /// Supports email-based login as an alternative to User ID authentication.
    /// Email lookups are case-insensitive for better user experience.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User entity if found, null otherwise</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            // Case-insensitive email lookup for better user experience
            var filter = Builders<User>.Filter.Regex(u => u.Email,
                new BsonRegularExpression($"^{Regex.Escape(email)}$", "i"));

            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("Found user by email: {Email} -> {UserId}", email, user.UserId);
            }
            else
            {
                _logger.LogDebug("No user found with email: {Email}", email);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by email: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Find user by generated User ID for login authentication.
    /// This method supports the requirement for User ID-based login using the 
    /// human-readable IDs generated during registration (USR-YYYY-NNNNNN format).
    /// </summary>
    /// <param name="userId">Generated user identifier</param>
    /// <returns>User entity if found, null otherwise</returns>
    public async Task<User?> GetByUserIdAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("Found user by User ID: {UserId}", userId);
            }
            else
            {
                _logger.LogDebug("No user found with User ID: {UserId}", userId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by User ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Find user by MongoDB ObjectId for internal operations.
    /// Used for authorization checks and entity relationships throughout the application.
    /// </summary>
    /// <param name="id">MongoDB ObjectId</param>
    /// <returns>User entity if found, null otherwise</returns>
    public async Task<User?> GetByIdAsync(ObjectId id)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, id);
            var user = await _users.Find(filter).FirstOrDefaultAsync();

            if (user != null)
            {
                _logger.LogDebug("Found user by ObjectId: {ObjectId} -> {UserId}", id, user.UserId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding user by ObjectId: {ObjectId}", id);
            throw;
        }
    }

    /// <summary>
    /// Update existing user entity.
    /// Used for profile updates, statistics tracking, and metadata changes.
    /// Updates the 'updatedAt' timestamp automatically.
    /// </summary>
    /// <param name="user">User entity with updated information</param>
    /// <returns>Updated user entity</returns>
    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            // Update the modification timestamp
            user.Metadata.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _users.ReplaceOneAsync(filter, user);

            _logger.LogDebug("Successfully updated user: {UserId}", user.UserId);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user: {UserId}", user.UserId);
            throw;
        }
    }

    /// <summary>
    /// Check if email address is already registered.
    /// Prevents duplicate email registrations and provides immediate feedback
    /// during registration validation. Uses case-insensitive comparison.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if email exists, false otherwise</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            // Case-insensitive email check for uniqueness validation
            var filter = Builders<User>.Filter.Regex(u => u.Email,
                new BsonRegularExpression($"^{Regex.Escape(email)}$", "i"));

            var count = await _users.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
            var exists = count > 0;

            _logger.LogDebug("Email existence check for {Email}: {Exists}", email, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email existence: {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Generate next unique User ID in sequence.
    /// Creates human-readable IDs in the format USR-YYYY-NNNNNN where:
    /// - USR is the prefix identifying this as a user ID
    /// - YYYY is the current year (4 digits)
    /// - NNNNNN is a sequential number starting from 000001 each year
    /// 
    /// This format provides approximately 1 million unique IDs per year,
    /// which should be sufficient for the projected user base.
    /// </summary>
    /// <returns>Next available User ID</returns>
    public async Task<string> GenerateNextUserIdAsync()
    {
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearPrefix = $"USR-{currentYear}-";

            // Find the highest sequence number for the current year
            var filter = Builders<User>.Filter.Regex(u => u.UserId,
                new BsonRegularExpression($"^{Regex.Escape(yearPrefix)}"));

            var projection = Builders<User>.Projection.Include(u => u.UserId);

            var existingUserIds = await _users
                .Find(filter)
                .Project(projection)
                .ToListAsync();

            // Extract sequence numbers and find the maximum
            var maxSequence = 0;
            foreach (var doc in existingUserIds)
            {
                if (doc.TryGetValue("userId", out var userIdValue) &&
                    userIdValue.IsString)
                {
                    var userId = userIdValue.AsString;
                    if (userId.StartsWith(yearPrefix) && userId.Length == yearPrefix.Length + 6)
                    {
                        var sequencePart = userId.Substring(yearPrefix.Length);
                        if (int.TryParse(sequencePart, out var sequence))
                        {
                            maxSequence = Math.Max(maxSequence, sequence);
                        }
                    }
                }
            }

            // Generate next sequence number
            var nextSequence = maxSequence + 1;
            var newUserId = $"{yearPrefix}{nextSequence:D6}";

            _logger.LogDebug("Generated new User ID: {UserId} (sequence: {Sequence})",
                newUserId, nextSequence);

            return newUserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate next User ID");
            throw;
        }
    }

    /// <summary>
    /// Update user's last login timestamp.
    /// Called during successful authentication to track user activity.
    /// This information is useful for user engagement analytics and security monitoring.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <returns>Task completion</returns>
    public async Task UpdateLastLoginAsync(ObjectId userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set("metadata.lastLoginAt", DateTime.UtcNow);

            await _users.UpdateOneAsync(filter, update);

            _logger.LogDebug("Updated last login time for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login time for user: {UserId}", userId);
            // Don't throw - login timestamp update shouldn't fail the authentication process
        }
    }
}