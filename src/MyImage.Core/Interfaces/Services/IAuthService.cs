using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Services;

/// <summary>
/// Service interface for authentication operations.
/// Handles user registration, login, and JWT token management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register new user with generated User ID.
    /// Implements the requirement for user registration with ID provision.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">Plain text password</param>
    /// <param name="firstName">User's first name</param>
    /// <param name="lastName">User's last name</param>
    /// <returns>Created user with generated User ID</returns>
    Task<User> RegisterAsync(string email, string password, string firstName, string lastName);

    /// <summary>
    /// Authenticate user and generate JWT token.
    /// Supports both email and User ID authentication.
    /// </summary>
    /// <param name="identifier">Email or User ID</param>
    /// <param name="password">Plain text password</param>
    /// <returns>User entity if authentication successful</returns>
    Task<User?> LoginAsync(string identifier, string password);

    /// <summary>
    /// Generate JWT token for authenticated user.
    /// Creates token with user claims and expiration.
    /// </summary>
    /// <param name="user">Authenticated user</param>
    /// <returns>JWT token string</returns>
    string GenerateJwtToken(User user);

    /// <summary>
    /// Validate and decode JWT token.
    /// Used by middleware for request authentication.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ObjectId if token valid</returns>
    Task<ObjectId?> ValidateTokenAsync(string token);

    /// <summary>
    /// Change user's password with current password verification.
    /// Implements secure password change workflow.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <param name="currentPassword">Current password for verification</param>
    /// <param name="newPassword">New password to set</param>
    /// <returns>True if password changed successfully</returns>
    Task<bool> ChangePasswordAsync(ObjectId userId, string currentPassword, string newPassword);
}