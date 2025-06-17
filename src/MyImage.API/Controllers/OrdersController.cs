using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Security.Claims;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Cart;
using MyImage.Core.DTOs.Orders;
using MyImage.Core.DTOs.Common;
using MyImage.Core.Entities;

namespace MyImage.API.Controllers;

/// <summary>
/// Orders controller handling order creation and management.
/// This controller implements Requirements 6-8 for order processing and payment handling.
/// 
/// Key features:
/// - Order creation from shopping cart
/// - Payment method selection (credit card or branch payment)
/// - Order history for customers
/// - Order status tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All order operations require authentication
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShoppingCartRepository _cartRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderRepository orderRepository,
        IShoppingCartRepository cartRepository,
        IPhotoRepository photoRepository,
        ISystemSettingsRepository settingsRepository,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _photoRepository = photoRepository;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create order from shopping cart.
    /// This endpoint implements Requirements 6-8 for order creation and payment processing.
    /// Supports both credit card and branch payment methods.
    /// </summary>
    /// <param name="createOrderDto">Order information including shipping and payment details</param>
    /// <returns>Created order information with order number</returns>
    /// <response code="201">Order created successfully</response>
    /// <response code="400">Invalid order data or empty cart</response>
    /// <response code="401">Authentication required</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderCreatedDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<OrderCreatedDto>>> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid order data", errors));
            }

            // Get user's cart and validate it has items
            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);
            if (cart.Items.Count == 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Cannot create order",
                    "Shopping cart is empty"));
            }

            // Calculate final totals with tax
            var taxRate = await GetTaxRateForState(createOrderDto.ShippingAddress.State);
            var subtotal = cart.Summary.Subtotal;
            var taxAmount = subtotal * taxRate;
            var total = subtotal + taxAmount;

            // Get user information for order
            var userClaim = User.FindFirst("user_id")?.Value ?? "";
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
            var nameClaim = User.FindFirst(ClaimTypes.Name)?.Value ?? "";

            // Create order entity
            var order = new Order
            {
                UserId = userId.Value,
                UserInfo = new OrderUserInfo
                {
                    UserId = userClaim,
                    Email = emailClaim,
                    Name = nameClaim
                },
                Status = "pending",
                Items = ConvertCartToOrderItems(cart),
                Pricing = new OrderPricing
                {
                    Subtotal = subtotal,
                    TaxRate = taxRate,
                    TaxAmount = taxAmount,
                    Total = total,
                    Currency = "USD"
                },
                Payment = CreateOrderPayment(createOrderDto),
                ShippingAddress = ConvertToShippingAddress(createOrderDto.ShippingAddress),
                Fulfillment = new OrderFulfillment(),
                PhotoCleanup = new PhotoCleanup()
            };

            // Create order in database
            var createdOrder = await _orderRepository.CreateAsync(order);

            // Mark photos as ordered to prevent deletion
            var photoIds = cart.Items.Select(item => item.PhotoId).ToList();
            await _photoRepository.MarkPhotosAsOrderedAsync(photoIds, createdOrder.Id);

            // Clear the cart after successful order creation
            await _cartRepository.ClearCartAsync(userId.Value);

            var response = new OrderCreatedDto
            {
                OrderId = createdOrder.Id.ToString(),
                OrderNumber = createdOrder.OrderNumber,
                Status = createdOrder.Status,
                TotalAmount = createdOrder.Pricing.Total,
                PaymentStatus = createdOrder.Payment.Status,
                CreatedAt = createdOrder.Metadata.CreatedAt
            };

            _logger.LogInformation("Order {OrderNumber} created successfully for user {UserId} with total {Total:C}",
                createdOrder.OrderNumber, userId, total);

            return CreatedAtAction(
                nameof(GetOrder),
                new { id = createdOrder.Id.ToString() },
                ApiResponse<OrderCreatedDto>.SuccessResponse(
                    response,
                    $"Order {createdOrder.OrderNumber} created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to create order",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get user's order history with pagination.
    /// Returns orders sorted by creation date (newest first).
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated order history</returns>
    /// <response code="200">Order history retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderSummaryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderSummaryDto>>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            var pagedOrders = await _orderRepository.GetUserOrdersAsync(userId.Value, page, pageSize);

            var orderSummaries = pagedOrders.Items.Select(order => new OrderSummaryDto
            {
                OrderId = order.Id.ToString(),
                OrderNumber = order.OrderNumber,
                OrderDate = order.Metadata.CreatedAt,
                Status = order.Status,
                TotalAmount = order.Pricing.Total,
                PhotoCount = order.Items.Count,
                PrintCount = order.Items.Sum(item => item.PrintSelections.Sum(ps => ps.Quantity)),
                PaymentMethod = order.Payment.Method
            }).ToList();

            var result = new PagedResult<OrderSummaryDto>
            {
                Items = orderSummaries,
                TotalCount = pagedOrders.TotalCount,
                Page = pagedOrders.Page,
                PageSize = pagedOrders.PageSize
            };

            return Ok(ApiResponse<PagedResult<OrderSummaryDto>>.SuccessResponse(
                result,
                $"Retrieved {orderSummaries.Count} orders"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve orders",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Get specific order details by ID.
    /// Returns complete order information including items and status.
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    /// <response code="200">Order details retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetOrder(string id)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse(
                    "Authentication required",
                    "Invalid user identification"));
            }

            if (!ObjectId.TryParse(id, out var orderId))
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Order not found",
                    "Invalid order ID format"));
            }

            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId.Value)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Order not found",
                    "Order not found or access denied"));
            }

            // Convert to detailed DTO (implementation would include full order details)
            var orderDetails = new
            {
                order.OrderNumber,
                order.Status,
                order.Metadata.CreatedAt,
                order.Pricing,
                order.ShippingAddress,
                Items = order.Items.Select(item => new
                {
                    item.PhotoFilename,
                    item.PrintSelections,
                    item.PhotoTotal
                }),
                order.Payment.Method,
                PaymentStatus = order.Payment.Status
            };

            return Ok(ApiResponse<object>.SuccessResponse(
                orderDetails,
                "Order details retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order details");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve order",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Helper methods for order creation and processing.
    /// </summary>
    private ObjectId? GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && ObjectId.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    private static List<OrderItem> ConvertCartToOrderItems(ShoppingCart cart)
    {
        return cart.Items.Select(cartItem => new OrderItem
        {
            PhotoId = cartItem.PhotoId,
            PhotoFilename = cartItem.PhotoDetails.Filename,
            PhotoFileSize = 0, // Would be populated from actual photo data
            PrintSelections = cartItem.PrintSelections.Select(ps => new OrderPrintSelection
            {
                SizeCode = ps.SizeCode,
                SizeName = ps.SizeName,
                Quantity = ps.Quantity,
                UnitPrice = ps.UnitPrice,
                Subtotal = ps.LineTotal
            }).ToList(),
            PhotoTotal = cartItem.PhotoTotal
        }).ToList();
    }

    private static OrderPayment CreateOrderPayment(CreateOrderDto createOrderDto)
    {
        var payment = new OrderPayment
        {
            Method = createOrderDto.PaymentMethod,
            Status = "pending"
        };

        if (createOrderDto.PaymentMethod == "credit_card" && createOrderDto.CreditCard != null)
        {
            // Extract last 4 digits from encrypted card number (implementation detail)
            payment.CreditCard = new CreditCardInfo
            {
                LastFour = "****", // Would extract from encrypted data
                CardholderName = createOrderDto.CreditCard.CardholderName
            };
        }
        else if (createOrderDto.PaymentMethod == "branch_payment" && createOrderDto.BranchPayment != null)
        {
            payment.BranchPayment = new BranchPaymentInfo
            {
                PreferredBranch = createOrderDto.BranchPayment.PreferredBranch,
                ReferenceNumber = $"BP-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000000, 9999999)}"
            };
        }

        return payment;
    }

    private static ShippingAddress ConvertToShippingAddress(ShippingAddressDto dto)
    {
        return new ShippingAddress
        {
            FullName = dto.FullName,
            StreetLine1 = dto.StreetLine1,
            StreetLine2 = dto.StreetLine2,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            Phone = dto.Phone
        };
    }

    private async Task<decimal> GetTaxRateForState(string state)
    {
        try
        {
            var taxSettings = await _settingsRepository.GetByKeyAsync("tax_rates");
            if (taxSettings?.Value != null)
            {
                if (taxSettings.Value.TryGetValue("byState", out var byStateValue) &&
                    byStateValue.IsBsonDocument)
                {
                    var byStateDoc = byStateValue.AsBsonDocument;
                    if (byStateDoc.TryGetValue(state, out var stateRateValue) &&
                        stateRateValue.IsNumeric)
                    {
                        return (decimal)stateRateValue.AsDouble;
                    }
                }

                if (taxSettings.Value.TryGetValue("default", out var defaultValue) &&
                    defaultValue.IsNumeric)
                {
                    return (decimal)defaultValue.AsDouble;
                }
            }

            return 0.0625m; // 6.25% default
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting tax rate for state {State}", state);
            return 0.0625m;
        }
    }
}