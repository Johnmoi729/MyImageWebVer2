using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Common;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for ShoppingCart entity operations.
/// Handles cart persistence and multi-size selection per photo.
/// Implements Requirement 2 for print size and quantity specification.
/// </summary>
public class ShoppingCartRepository : IShoppingCartRepository
{
    private readonly IMongoCollection<ShoppingCart> _carts;
    private readonly ILogger<ShoppingCartRepository> _logger;

    public ShoppingCartRepository(MongoDbContext context, ILogger<ShoppingCartRepository> logger)
    {
        _carts = context.ShoppingCarts;
        _logger = logger;
    }

    /// <summary>
    /// Get user's current shopping cart.
    /// Creates new cart if none exists for the user.
    /// </summary>
    public async Task<ShoppingCart> GetByUserIdAsync(ObjectId userId)
    {
        try
        {
            var filter = Builders<ShoppingCart>.Filter.Eq(sc => sc.UserId, userId);
            var cart = await _carts.Find(filter).FirstOrDefaultAsync();

            if (cart == null)
            {
                // Create new cart if none exists
                cart = new ShoppingCart
                {
                    UserId = userId,
                    Items = new List<CartItem>(),
                    Summary = new CartSummary(),
                    Metadata = new CartMetadata
                    {
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(14) // 2 weeks expiration
                    }
                };

                await _carts.InsertOneAsync(cart);
                _logger.LogDebug("Created new shopping cart for user {UserId}", userId);
            }
            else
            {
                _logger.LogDebug("Retrieved existing cart for user {UserId} with {ItemCount} items",
                    userId, cart.Items.Count);
            }

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cart for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Update shopping cart with new items or modifications.
    /// Recalculates totals and updates timestamps.
    /// </summary>
    public async Task<ShoppingCart> UpdateAsync(ShoppingCart cart)
    {
        try
        {
            // Update timestamps and expiration
            cart.Metadata.UpdatedAt = DateTime.UtcNow;
            cart.Metadata.ExpiresAt = DateTime.UtcNow.AddDays(14); // Reset expiration

            var filter = Builders<ShoppingCart>.Filter.Eq(sc => sc.Id, cart.Id);
            await _carts.ReplaceOneAsync(filter, cart);

            _logger.LogDebug("Updated shopping cart for user {UserId}", cart.UserId);
            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cart for user {UserId}", cart.UserId);
            throw;
        }
    }

    /// <summary>
    /// Clear all items from user's cart.
    /// Called after successful order placement.
    /// </summary>
    public async Task ClearCartAsync(ObjectId userId)
    {
        try
        {
            var filter = Builders<ShoppingCart>.Filter.Eq(sc => sc.UserId, userId);
            var update = Builders<ShoppingCart>.Update
                .Set(sc => sc.Items, new List<CartItem>())
                .Set(sc => sc.Summary, new CartSummary())
                .Set("metadata.updatedAt", DateTime.UtcNow);

            await _carts.UpdateOneAsync(filter, update);

            _logger.LogInformation("Cleared shopping cart for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cart for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Remove expired carts for cleanup.
    /// Called by background job to maintain database performance.
    /// </summary>
    public async Task<int> RemoveExpiredCartsAsync(DateTime expiredBefore)
    {
        try
        {
            var filter = Builders<ShoppingCart>.Filter.Lt("metadata.expiresAt", expiredBefore);
            var result = await _carts.DeleteManyAsync(filter);

            _logger.LogInformation("Removed {Count} expired carts", result.DeletedCount);
            return (int)result.DeletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove expired carts");
            throw;
        }
    }
}