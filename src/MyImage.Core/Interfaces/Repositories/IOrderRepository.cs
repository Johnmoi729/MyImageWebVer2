using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for Order entity operations.
/// Handles complete order lifecycle from creation to completion.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Create new order from shopping cart.
    /// Generates human-readable order number and locks pricing.
    /// </summary>
    /// <param name="order">Order entity to create</param>
    /// <returns>Created order with assigned number</returns>
    Task<Order> CreateAsync(Order order);

    /// <summary>
    /// Get order by ObjectId for admin operations.
    /// Used for order management and status updates.
    /// </summary>
    /// <param name="orderId">Order's ObjectId</param>
    /// <returns>Order entity if found</returns>
    Task<Order?> GetByIdAsync(ObjectId orderId);

    /// <summary>
    /// Get order by human-readable order number.
    /// Used for customer service and order lookup.
    /// </summary>
    /// <param name="orderNumber">Order number like ORD-2024-001234</param>
    /// <returns>Order entity if found</returns>
    Task<Order?> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Get paginated orders for specific user.
    /// Used for customer order history display.
    /// </summary>
    /// <param name="userId">User's ObjectId</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated order results</returns>
    Task<PagedResult<Order>> GetUserOrdersAsync(ObjectId userId, int page, int pageSize);

    /// <summary>
    /// Get orders by status for admin workflow.
    /// Used to filter orders needing specific admin actions.
    /// </summary>
    /// <param name="status">Order status to filter by</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated orders with specified status</returns>
    Task<PagedResult<Order>> GetOrdersByStatusAsync(string status, int page, int pageSize);

    /// <summary>
    /// Update order status and related information.
    /// Called during admin workflow progression.
    /// </summary>
    /// <param name="order">Order with status updates</param>
    /// <returns>Updated order entity</returns>
    Task<Order> UpdateAsync(Order order);

    /// <summary>
    /// Generate next unique order number in sequence.
    /// Creates human-readable order numbers like ORD-2024-001234.
    /// </summary>
    /// <returns>Next available order number</returns>
    Task<string> GenerateNextOrderNumberAsync();

    /// <summary>
    /// Get orders completed within date range for reporting.
    /// Used for admin dashboard statistics.
    /// </summary>
    /// <param name="fromDate">Start date for range</param>
    /// <param name="toDate">End date for range</param>
    /// <returns>Orders completed in date range</returns>
    Task<List<Order>> GetCompletedOrdersAsync(DateTime fromDate, DateTime toDate);
}