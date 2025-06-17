using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for ShoppingCart entity operations.
/// Handles cart persistence and multi-size selection per photo.
/// </summary>
public interface IShoppingCartRepository
{
    /// <summary>
    /// Get user's current shopping cart.
    /// Creates new cart if none exists for the user.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <returns>User's shopping cart</returns>
    Task<ShoppingCart> GetByUserIdAsync(ObjectId userId);

    /// <summary>
    /// Update shopping cart with new items or modifications.
    /// Recalculates totals and updates timestamps.
    /// </summary>
    /// <param name="cart">Shopping cart with updates</param>
    /// <returns>Updated shopping cart</returns>
    Task<ShoppingCart> UpdateAsync(ShoppingCart cart);

    /// <summary>
    /// Clear all items from user's cart.
    /// Called after successful order placement.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <returns>Task completion</returns>
    Task ClearCartAsync(ObjectId userId);

    /// <summary>
    /// Remove expired carts for cleanup.
    /// Called by background job to maintain database performance.
    /// </summary>
    /// <param name="expiredBefore">Remove carts expired before this date</param>
    /// <returns>Number of carts removed</returns>
    Task<int> RemoveExpiredCartsAsync(DateTime expiredBefore);
}
