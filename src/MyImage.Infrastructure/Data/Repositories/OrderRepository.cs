using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Common;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Order entity operations.
/// Handles complete order lifecycle from creation to completion.
/// Implements Requirements 6-10 for order processing and management.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly IMongoCollection<Order> _orders;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(MongoDbContext context, ILogger<OrderRepository> logger)
    {
        _orders = context.Orders;
        _logger = logger;
    }

    /// <summary>
    /// Create new order from shopping cart.
    /// Generates human-readable order number and locks pricing.
    /// </summary>
    public async Task<Order> CreateAsync(Order order)
    {
        try
        {
            // Generate unique order number
            order.OrderNumber = await GenerateNextOrderNumberAsync();

            // Set creation metadata
            order.Metadata.CreatedAt = DateTime.UtcNow;
            order.Metadata.UpdatedAt = DateTime.UtcNow;

            await _orders.InsertOneAsync(order);

            _logger.LogInformation("Created order {OrderNumber} for user {UserId} with total {Total:C}",
                order.OrderNumber, order.UserId, order.Pricing.Total);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for user {UserId}", order.UserId);
            throw;
        }
    }

    /// <summary>
    /// Generate next unique order number in sequence.
    /// Creates human-readable order numbers like ORD-2024-001234.
    /// </summary>
    public async Task<string> GenerateNextOrderNumberAsync()
    {
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearPrefix = $"ORD-{currentYear}-";

            // Find highest sequence number for current year
            var filter = Builders<Order>.Filter.Regex(o => o.OrderNumber,
                new BsonRegularExpression($"^{Regex.Escape(yearPrefix)}"));

            var projection = Builders<Order>.Projection.Include(o => o.OrderNumber);

            var existingOrderNumbers = await _orders
                .Find(filter)
                .Project(projection)
                .ToListAsync();

            var maxSequence = 0;
            foreach (var doc in existingOrderNumbers)
            {
                if (doc.TryGetValue("orderNumber", out var orderNumberValue) &&
                    orderNumberValue.IsString)
                {
                    var orderNumber = orderNumberValue.AsString;
                    if (orderNumber.StartsWith(yearPrefix) && orderNumber.Length == yearPrefix.Length + 7)
                    {
                        var sequencePart = orderNumber.Substring(yearPrefix.Length);
                        if (int.TryParse(sequencePart, out var sequence))
                        {
                            maxSequence = Math.Max(maxSequence, sequence);
                        }
                    }
                }
            }

            var nextSequence = maxSequence + 1;
            var newOrderNumber = $"{yearPrefix}{nextSequence:D7}";

            _logger.LogDebug("Generated new order number: {OrderNumber}", newOrderNumber);
            return newOrderNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate order number");
            throw;
        }
    }

    /// <summary>
    /// Get order by ObjectId for admin operations.
    /// </summary>
    public async Task<Order?> GetByIdAsync(ObjectId orderId)
    {
        try
        {
            var order = await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by ID: {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Get order by human-readable order number.
    /// </summary>
    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderNumber, orderNumber);
            var order = await _orders.Find(filter).FirstOrDefaultAsync();
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by number: {OrderNumber}", orderNumber);
            throw;
        }
    }

    /// <summary>
    /// Get paginated orders for specific user.
    /// </summary>
    public async Task<PagedResult<Order>> GetUserOrdersAsync(ObjectId userId, int page, int pageSize)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.UserId, userId);
            var skip = (page - 1) * pageSize;

            var totalCount = await _orders.CountDocumentsAsync(filter);

            var orders = await _orders
                .Find(filter)
                .Sort(Builders<Order>.Sort.Descending("metadata.createdAt"))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = orders,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get orders by status for admin workflow.
    /// </summary>
    public async Task<PagedResult<Order>> GetOrdersByStatusAsync(string status, int page, int pageSize)
    {
        try
        {
            var filter = Builders<Order>.Filter.Eq(o => o.Status, status);
            var skip = (page - 1) * pageSize;

            var totalCount = await _orders.CountDocumentsAsync(filter);

            var orders = await _orders
                .Find(filter)
                .Sort(Builders<Order>.Sort.Ascending("metadata.createdAt"))
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResult<Order>
            {
                Items = orders,
                TotalCount = (int)totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get orders by status: {Status}", status);
            throw;
        }
    }

    /// <summary>
    /// Update order status and related information.
    /// </summary>
    public async Task<Order> UpdateAsync(Order order)
    {
        try
        {
            order.Metadata.UpdatedAt = DateTime.UtcNow;

            var filter = Builders<Order>.Filter.Eq(o => o.Id, order.Id);
            await _orders.ReplaceOneAsync(filter, order);

            _logger.LogDebug("Updated order {OrderNumber}", order.OrderNumber);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update order {OrderNumber}", order.OrderNumber);
            throw;
        }
    }

    /// <summary>
    /// Get orders completed within date range for reporting.
    /// </summary>
    public async Task<List<Order>> GetCompletedOrdersAsync(DateTime fromDate, DateTime toDate)
    {
        try
        {
            var filter = Builders<Order>.Filter.And(
                Builders<Order>.Filter.Eq(o => o.Status, "completed"),
                Builders<Order>.Filter.Gte("fulfillment.completedAt", fromDate),
                Builders<Order>.Filter.Lte("fulfillment.completedAt", toDate)
            );

            var orders = await _orders.Find(filter).ToListAsync();

            _logger.LogDebug("Retrieved {Count} completed orders from {FromDate} to {ToDate}",
                orders.Count, fromDate, toDate);

            return orders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get completed orders in date range");
            throw;
        }
    }
}