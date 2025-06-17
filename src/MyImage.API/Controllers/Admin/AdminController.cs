using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Security.Claims;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Admin;
using MyImage.Core.DTOs.PrintSizes;
using MyImage.Core.DTOs.Common;
using MyImage.Core.Entities;

namespace MyImage.API.Controllers.Admin;

/// <summary>
/// Admin controller handling administrative functions.
/// This controller implements Requirements 9 and 11 for admin order processing and price management.
/// 
/// Key features:
/// - Order management and status updates
/// - Payment verification workflow
/// - Print size and pricing management
/// - Dashboard statistics
/// - Order completion and photo cleanup
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")] // Only admin users can access these endpoints
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPrintSizeRepository _printSizeRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initialize admin controller with required repositories and services.
    /// </summary>
    public AdminController(
        IOrderRepository orderRepository,
        IPrintSizeRepository printSizeRepository,
        IPhotoRepository photoRepository,
        IUserRepository userRepository,
        ILogger<AdminController> logger)
    {
        _orderRepository = orderRepository;
        _printSizeRepository = printSizeRepository;
        _photoRepository = photoRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get admin dashboard statistics.
    /// Provides overview of system status and pending work for daily admin workflow.
    /// Shows key metrics like pending orders, processing orders, and revenue.
    /// </summary>
    /// <returns>Dashboard statistics</returns>
    /// <response code="200">Dashboard statistics retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
    {
        try
        {
            _logger.LogDebug("Admin dashboard request from user {UserId}", GetAdminUserId());

            // Get orders by status for workflow metrics
            var pendingOrders = await _orderRepository.GetOrdersByStatusAsync("pending", 1, 1000);
            var processingOrders = await _orderRepository.GetOrdersByStatusAsync("processing", 1, 1000);

            // Get completed orders for today
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var completedToday = await _orderRepository.GetCompletedOrdersAsync(today, tomorrow);

            // Calculate total revenue from completed orders (simplified for Week 1)
            var totalRevenue = completedToday.Sum(order => order.Pricing.Total);

            // Get active user count (simplified)
            // Note: In a full implementation, this would be more sophisticated
            var activeUsers = 50; // Placeholder for Week 1

            // Storage metrics (placeholder for Week 1)
            var storageUsed = 1024L * 1024L * 1024L * 5; // 5GB placeholder

            var dashboard = new AdminDashboardDto
            {
                PendingOrders = pendingOrders.TotalCount,
                ProcessingOrders = processingOrders.TotalCount,
                CompletedToday = completedToday.Count,
                TotalRevenue = totalRevenue,
                ActiveUsers = activeUsers,
                StorageUsed = storageUsed
            };

            return Ok(ApiResponse<AdminDashboardDto>.SuccessResponse(
                dashboard,
                "Dashboard statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin dashboard");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve dashboard",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get orders for admin management with filtering by status.
    /// Returns paginated list of orders for admin workflow processing.
    /// Includes customer information and payment status for verification.
    /// </summary>
    /// <param name="status">Filter by order status (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated orders for admin management</returns>
    /// <response code="200">Orders retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AdminOrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminOrderDto>>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogDebug("Admin orders request: status={Status}, page={Page}", status, page);

            PagedResult<Order> pagedOrders;

            if (!string.IsNullOrEmpty(status))
            {
                pagedOrders = await _orderRepository.GetOrdersByStatusAsync(status, page, pageSize);
            }
            else
            {
                // Get all orders if no status filter (implementation would need this method)
                pagedOrders = await _orderRepository.GetOrdersByStatusAsync("pending", page, pageSize);
                // Note: In full implementation, would have GetAllOrdersAsync method
            }

            var adminOrders = pagedOrders.Items.Select(order => new AdminOrderDto
            {
                OrderId = order.Id.ToString(),
                OrderNumber = order.OrderNumber,
                CustomerName = order.UserInfo.Name,
                CustomerEmail = order.UserInfo.Email,
                Status = order.Status,
                PaymentMethod = order.Payment.Method,
                PaymentStatus = order.Payment.Status,
                TotalAmount = order.Pricing.Total,
                OrderDate = order.Metadata.CreatedAt,
                PaymentVerifiedDate = order.Payment.VerifiedAt,
                PhotoCount = order.Items.Count,
                PrintCount = order.Items.Sum(item => item.PrintSelections.Sum(ps => ps.Quantity))
            }).ToList();

            var result = new PagedResult<AdminOrderDto>
            {
                Items = adminOrders,
                TotalCount = pagedOrders.TotalCount,
                Page = pagedOrders.Page,
                PageSize = pagedOrders.PageSize
            };

            return Ok(ApiResponse<PagedResult<AdminOrderDto>>.SuccessResponse(
                result,
                $"Retrieved {adminOrders.Count} orders"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin orders");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve orders",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Update order status in admin workflow.
    /// This endpoint implements Requirement 9 for admin order processing.
    /// Allows progression through order statuses: pending → payment_verified → processing → printed → shipped → completed.
    /// </summary>
    /// <param name="orderId">Order ID to update</param>
    /// <param name="updateDto">New status and optional notes</param>
    /// <returns>Updated order confirmation</returns>
    /// <response code="200">Order status updated successfully</response>
    /// <response code="400">Invalid status transition or data</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">Order not found</response>
    [HttpPut("orders/{orderId}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOrderStatus(
        string orderId,
        [FromBody] UpdateOrderStatusDto updateDto)
    {
        try
        {
            var adminUserId = GetAdminUserId();
            _logger.LogInformation("Admin {AdminId} updating order {OrderId} status to {Status}",
                adminUserId, orderId, updateDto.Status);

            if (!ObjectId.TryParse(orderId, out var orderObjectId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid order ID",
                    "Order ID format is invalid"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid status update data", errors));
            }

            var order = await _orderRepository.GetByIdAsync(orderObjectId);
            if (order == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Order not found",
                    "Order not found"));
            }

            // Validate status transition is allowed
            if (!IsValidStatusTransition(order.Status, updateDto.Status))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid status transition",
                    $"Cannot change status from {order.Status} to {updateDto.Status}"));
            }

            // Update order status and related fields
            order.Status = updateDto.Status;

            // Handle payment verification
            if (updateDto.Status == "payment_verified" && order.Payment.Status == "pending")
            {
                order.Payment.Status = "verified";
                order.Payment.VerifiedAt = DateTime.UtcNow;
                order.Payment.VerifiedBy = adminUserId;
            }

            // Handle fulfillment status updates
            switch (updateDto.Status)
            {
                case "processing":
                    // Order moved to print queue
                    break;
                case "printed":
                    order.Fulfillment.PrintedAt = DateTime.UtcNow;
                    break;
                case "shipped":
                    order.Fulfillment.ShippedAt = DateTime.UtcNow;
                    break;
            }

            // Add admin notes if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Notes))
            {
                var noteEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {adminUserId}: {updateDto.Notes}";
                order.Fulfillment.Notes.Add(noteEntry);
            }

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderNumber} status updated from {OldStatus} to {NewStatus} by admin {AdminId}",
                order.OrderNumber, order.Status, updateDto.Status, adminUserId);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                $"Order status updated to {updateDto.Status}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to update order status",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Complete order and trigger photo cleanup.
    /// This endpoint implements Requirement 10 for photo deletion after order completion.
    /// Marks order as completed and schedules photos for deletion.
    /// </summary>
    /// <param name="orderId">Order ID to complete</param>
    /// <param name="completeDto">Completion details including shipping info</param>
    /// <returns>Completion confirmation with cleanup statistics</returns>
    /// <response code="200">Order completed successfully</response>
    /// <response code="400">Invalid completion data or order not ready</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">Order not found</response>
    [HttpPost("orders/{orderId}/complete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> CompleteOrder(
        string orderId,
        [FromBody] CompleteOrderDto completeDto)
    {
        try
        {
            var adminUserId = GetAdminUserId();
            _logger.LogInformation("Admin {AdminId} completing order {OrderId}", adminUserId, orderId);

            if (!ObjectId.TryParse(orderId, out var orderObjectId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid order ID",
                    "Order ID format is invalid"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid completion data", errors));
            }

            var order = await _orderRepository.GetByIdAsync(orderObjectId);
            if (order == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Order not found",
                    "Order not found"));
            }

            // Verify order is in a state that can be completed
            if (order.Status == "completed")
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Order already completed",
                    "This order has already been completed"));
            }

            if (order.Status != "printed" && order.Status != "shipped")
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Order not ready for completion",
                    "Order must be printed or shipped before completion"));
            }

            // Update order to completed status
            order.Status = "completed";
            order.Fulfillment.CompletedAt = DateTime.UtcNow;
            order.Fulfillment.ShippedAt = completeDto.ShippingDate;

            if (!string.IsNullOrWhiteSpace(completeDto.TrackingNumber))
            {
                order.Fulfillment.TrackingNumber = completeDto.TrackingNumber;
            }

            if (!string.IsNullOrWhiteSpace(completeDto.Notes))
            {
                var noteEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {adminUserId}: COMPLETED - {completeDto.Notes}";
                order.Fulfillment.Notes.Add(noteEntry);
            }

            // Schedule photos for deletion (7 days buffer for customer service)
            var deletionDate = DateTime.UtcNow.AddDays(7);
            var photoIds = order.Items.Select(item => item.PhotoId).ToList();
            var storageFreed = order.Items.Sum(item => item.PhotoFileSize);

            foreach (var photoId in photoIds)
            {
                await _photoRepository.MarkForDeletionAsync(photoId, deletionDate);
            }

            // Update cleanup tracking
            order.PhotoCleanup = new PhotoCleanup
            {
                IsCompleted = false, // Will be completed by cleanup job
                PhotosDeleted = 0,
                StorageFreed = 0,
                CleanupDate = null
            };

            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderNumber} completed by admin {AdminId}, {PhotoCount} photos scheduled for deletion",
                order.OrderNumber, adminUserId, photoIds.Count);

            var response = new
            {
                OrderId = order.Id.ToString(),
                Status = order.Status,
                PhotosScheduledForDeletion = photoIds.Count,
                DeletionScheduledFor = deletionDate,
                EstimatedStorageToFree = storageFreed
            };

            return Ok(ApiResponse<object>.SuccessResponse(
                response,
                $"Order {order.OrderNumber} completed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing order");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to complete order",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get all print sizes for admin management.
    /// Returns both active and inactive print sizes for administrative control.
    /// </summary>
    /// <returns>All print sizes with admin metadata</returns>
    /// <response code="200">Print sizes retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    [HttpGet("print-sizes")]
    [ProducesResponseType(typeof(ApiResponse<List<AdminPrintSizeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<AdminPrintSizeDto>>>> GetPrintSizes()
    {
        try
        {
            var printSizes = await _printSizeRepository.GetAllAsync();

            var adminPrintSizes = printSizes.Select(ps => new AdminPrintSizeDto
            {
                Id = ps.Id.ToString(),
                SizeCode = ps.SizeCode,
                DisplayName = ps.DisplayName,
                Width = ps.Dimensions.Width,
                Height = ps.Dimensions.Height,
                Unit = ps.Dimensions.Unit,
                Price = ps.Pricing.BasePrice,
                Currency = ps.Pricing.Currency,
                IsActive = ps.Metadata.IsActive,
                MinWidth = ps.Dimensions.PixelRequirements.MinWidth,
                MinHeight = ps.Dimensions.PixelRequirements.MinHeight,
                RecommendedWidth = ps.Dimensions.PixelRequirements.RecommendedWidth,
                RecommendedHeight = ps.Dimensions.PixelRequirements.RecommendedHeight,
                SortOrder = ps.Metadata.SortOrder,
                LastUpdated = ps.Pricing.LastUpdated,
                UpdatedBy = ps.Pricing.UpdatedBy,
                CreatedAt = ps.Metadata.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<AdminPrintSizeDto>>.SuccessResponse(
                adminPrintSizes,
                $"Retrieved {adminPrintSizes.Count} print sizes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin print sizes");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve print sizes",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Update print size pricing and settings.
    /// This endpoint implements Requirement 11 where "admin will decide the price and other things".
    /// </summary>
    /// <param name="printSizeId">Print size ID to update</param>
    /// <param name="updateDto">Updated pricing and settings</param>
    /// <returns>Update confirmation</returns>
    /// <response code="200">Print size updated successfully</response>
    /// <response code="400">Invalid update data</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    /// <response code="404">Print size not found</response>
    [HttpPut("print-sizes/{printSizeId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> UpdatePrintSize(
        string printSizeId,
        [FromBody] UpdatePrintSizeDto updateDto)
    {
        try
        {
            var adminUserId = GetAdminUserId();
            _logger.LogInformation("Admin {AdminId} updating print size {PrintSizeId}", adminUserId, printSizeId);

            if (!ObjectId.TryParse(printSizeId, out var printSizeObjectId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid print size ID",
                    "Print size ID format is invalid"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid print size data", errors));
            }

            var printSize = await _printSizeRepository.GetByIdAsync(printSizeObjectId);
            if (printSize == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Print size not found",
                    "Print size not found"));
            }

            // Update pricing information
            printSize.Pricing.BasePrice = updateDto.Price;
            printSize.Pricing.LastUpdated = DateTime.UtcNow;
            printSize.Pricing.UpdatedBy = adminUserId;

            // Update metadata
            printSize.Metadata.IsActive = updateDto.IsActive;
            printSize.Metadata.SortOrder = updateDto.SortOrder;
            printSize.Metadata.UpdatedAt = DateTime.UtcNow;

            await _printSizeRepository.UpdateAsync(printSize);

            _logger.LogInformation("Print size {SizeCode} updated by admin {AdminId}: price=${Price}, active={IsActive}",
                printSize.SizeCode, adminUserId, updateDto.Price, updateDto.IsActive);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                $"Print size {printSize.SizeCode} updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating print size");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to update print size",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Create new print size option.
    /// Allows admin to add new print sizes and pricing options for customers.
    /// </summary>
    /// <param name="createDto">New print size information</param>
    /// <returns>Created print size information</returns>
    /// <response code="201">Print size created successfully</response>
    /// <response code="400">Invalid creation data or size code already exists</response>
    /// <response code="401">Authentication required</response>
    /// <response code="403">Admin role required</response>
    [HttpPost("print-sizes")]
    [ProducesResponseType(typeof(ApiResponse<AdminPrintSizeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AdminPrintSizeDto>>> CreatePrintSize([FromBody] CreatePrintSizeDto createDto)
    {
        try
        {
            var adminUserId = GetAdminUserId();
            _logger.LogInformation("Admin {AdminId} creating new print size {SizeCode}", adminUserId, createDto.SizeCode);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid print size data", errors));
            }

            // Check if size code already exists
            if (await _printSizeRepository.SizeCodeExistsAsync(createDto.SizeCode))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Size code already exists",
                    $"A print size with code '{createDto.SizeCode}' already exists"));
            }

            // Create new print size entity
            var printSize = new PrintSize
            {
                SizeCode = createDto.SizeCode,
                DisplayName = createDto.DisplayName,
                Dimensions = new PrintDimensions
                {
                    Width = createDto.Width,
                    Height = createDto.Height,
                    Unit = "inches",
                    PixelRequirements = new PixelRequirements
                    {
                        MinWidth = createDto.MinWidth,
                        MinHeight = createDto.MinHeight,
                        RecommendedWidth = createDto.RecommendedWidth,
                        RecommendedHeight = createDto.RecommendedHeight
                    }
                },
                Pricing = new PrintPricing
                {
                    BasePrice = createDto.Price,
                    Currency = "USD",
                    LastUpdated = DateTime.UtcNow,
                    UpdatedBy = adminUserId
                },
                Metadata = new PrintSizeMetadata
                {
                    IsActive = true,
                    SortOrder = 99, // Place new sizes at end by default
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var createdPrintSize = await _printSizeRepository.CreateAsync(printSize);

            var responseDto = new AdminPrintSizeDto
            {
                Id = createdPrintSize.Id.ToString(),
                SizeCode = createdPrintSize.SizeCode,
                DisplayName = createdPrintSize.DisplayName,
                Width = createdPrintSize.Dimensions.Width,
                Height = createdPrintSize.Dimensions.Height,
                Unit = createdPrintSize.Dimensions.Unit,
                Price = createdPrintSize.Pricing.BasePrice,
                Currency = createdPrintSize.Pricing.Currency,
                IsActive = createdPrintSize.Metadata.IsActive,
                MinWidth = createdPrintSize.Dimensions.PixelRequirements.MinWidth,
                MinHeight = createdPrintSize.Dimensions.PixelRequirements.MinHeight,
                RecommendedWidth = createdPrintSize.Dimensions.PixelRequirements.RecommendedWidth,
                RecommendedHeight = createdPrintSize.Dimensions.PixelRequirements.RecommendedHeight,
                SortOrder = createdPrintSize.Metadata.SortOrder,
                LastUpdated = createdPrintSize.Pricing.LastUpdated,
                UpdatedBy = createdPrintSize.Pricing.UpdatedBy,
                CreatedAt = createdPrintSize.Metadata.CreatedAt
            };

            _logger.LogInformation("Print size {SizeCode} created successfully by admin {AdminId}",
                createDto.SizeCode, adminUserId);

            return CreatedAtAction(
                nameof(GetPrintSizes),
                ApiResponse<AdminPrintSizeDto>.SuccessResponse(
                    responseDto,
                    $"Print size {createDto.SizeCode} created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating print size");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to create print size",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Helper method to get admin user ID from JWT token claims.
    /// </summary>
    private string GetAdminUserId()
    {
        return User.FindFirst("user_id")?.Value ?? "unknown_admin";
    }

    /// <summary>
    /// Validate that status transition is allowed in the order workflow.
    /// Prevents invalid status changes that would break the order process.
    /// </summary>
    /// <param name="currentStatus">Current order status</param>
    /// <param name="newStatus">Requested new status</param>
    /// <returns>True if transition is valid</returns>
    private static bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        // Define allowed status transitions
        var allowedTransitions = new Dictionary<string, string[]>
        {
            ["pending"] = new[] { "payment_verified", "cancelled" },
            ["payment_verified"] = new[] { "processing", "cancelled" },
            ["processing"] = new[] { "printed", "cancelled" },
            ["printed"] = new[] { "shipped", "completed" },
            ["shipped"] = new[] { "completed" },
            ["completed"] = new string[0], // No transitions from completed
            ["cancelled"] = new string[0]  // No transitions from cancelled
        };

        return allowedTransitions.ContainsKey(currentStatus) &&
               allowedTransitions[currentStatus].Contains(newStatus);
    }
}