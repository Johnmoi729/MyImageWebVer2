using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity operations.
/// Defines contract for user data access including unique ID generation.
/// This interface supports the requirement for user registration with generated User IDs.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Create a new user with auto-generated User ID.
    /// Generates unique human-readable ID (USR-YYYY-NNNNNN format)
    /// and ensures email uniqueness before creation.
    /// </summary>
    /// <param name="user">User entity to create</param>
    /// <returns>Created user with generated User ID</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Find user by email address for login authentication.
    /// Supports email-based login as alternative to User ID.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>User entity if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Find user by generated User ID for login authentication.
    /// Supports User ID-based login (USR-YYYY-NNNNNN format).
    /// </summary>
    /// <param name="userId">Generated user identifier</param>
    /// <returns>User entity if found, null otherwise</returns>
    Task<User?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Find user by MongoDB ObjectId for internal operations.
    /// Used for authorization and entity relationships.
    /// </summary>
    /// <param name="id">MongoDB ObjectId</param>
    /// <returns>User entity if found, null otherwise</returns>
    Task<User?> GetByIdAsync(ObjectId id);

    /// <summary>
    /// Update existing user entity.
    /// Used for profile updates and statistics tracking.
    /// </summary>
    /// <param name="user">User entity with updated information</param>
    /// <returns>Updated user entity</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Check if email address is already registered.
    /// Prevents duplicate email registrations.
    /// </summary>
    /// <param name="email">Email address to check</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Generate next unique User ID in sequence.
    /// Creates human-readable IDs like USR-2024-001234.
    /// </summary>
    /// <returns>Next available User ID</returns>
    Task<string> GenerateNextUserIdAsync();

    /// <summary>
    /// Update user's last login timestamp.
    /// Called during successful authentication for tracking.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <returns>Task completion</returns>
    Task UpdateLastLoginAsync(ObjectId userId);
}