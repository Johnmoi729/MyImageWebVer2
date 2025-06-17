using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using System.Security.Claims;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Cart;
using MyImage.Core.DTOs.Orders;
using MyImage.Core.DTOs.Common;
using MyImage.Core.DTOs.Photos;
using MyImage.Core.Entities;

namespace MyImage.API.Controllers;

/// <summary>
/// Shopping cart controller handling cart operations.
/// This controller implements Requirement 2: "user should be able to mark the photographs 
/// he wants to order and specify the size of the print and the number of copies"
/// 
/// Key features:
/// - Multiple print sizes per photo in a single cart item
/// - Real-time price calculations
/// - Cart persistence between sessions
/// - Automatic cart expiration (2 weeks)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // All cart operations require authentication
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly IShoppingCartRepository _cartRepository;
    private readonly IPhotoRepository _photoRepository;
    private readonly IPrintSizeRepository _printSizeRepository;
    private readonly ISystemSettingsRepository _settingsRepository;
    private readonly ILogger<CartController> _logger;

    public CartController(
        IShoppingCartRepository cartRepository,
        IPhotoRepository photoRepository,
        IPrintSizeRepository printSizeRepository,
        ISystemSettingsRepository settingsRepository,
        ILogger<CartController> logger)
    {
        _cartRepository = cartRepository;
        _photoRepository = photoRepository;
        _printSizeRepository = printSizeRepository;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's shopping cart with calculated totals.
    /// Returns empty cart if user hasn't added any items yet.
    /// </summary>
    /// <returns>Shopping cart with items and totals</returns>
    /// <response code="200">Cart retrieved successfully</response>
    /// <response code="401">Authentication required</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
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

            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);
            var cartDto = await ConvertToCartDto(cart);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cartDto,
                $"Cart retrieved with {cartDto.Items.Count} items"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve cart",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Add photo to cart with multiple print size selections.
    /// This endpoint allows users to select multiple sizes and quantities for the same photo,
    /// implementing the requirement for size and quantity specification.
    /// 
    /// Example: User can order 10×4×6 prints + 2×5×7 prints + 1×8×10 print of the same photo.
    /// </summary>
    /// <param name="addToCartDto">Photo ID and print selections</param>
    /// <returns>Updated cart information</returns>
    /// <response code="200">Photo added to cart successfully</response>
    /// <response code="400">Invalid request or validation errors</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Photo or print sizes not found</response>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart([FromBody] AddToCartDto addToCartDto)
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
                    "Invalid cart data", errors));
            }

            // Validate photo ID and ownership
            if (!ObjectId.TryParse(addToCartDto.PhotoId, out var photoId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid photo ID",
                    "Photo ID format is invalid"));
            }

            var photo = await _photoRepository.GetByIdAsync(photoId, userId.Value);
            if (photo == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Photo not found",
                    "Photo not found or access denied"));
            }

            // Validate and get print sizes with current pricing
            var sizeCodes = addToCartDto.PrintSelections.Select(ps => ps.SizeCode).ToList();
            var printSizes = await _printSizeRepository.GetBySizeCodesAsync(sizeCodes);

            if (printSizes.Count != sizeCodes.Count)
            {
                var foundCodes = printSizes.Select(ps => ps.SizeCode).ToList();
                var missingCodes = sizeCodes.Except(foundCodes).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid print sizes",
                    $"Print sizes not found: {string.Join(", ", missingCodes)}"));
            }

            // Get or create user's cart
            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);

            // Remove existing cart item for this photo (if any)
            cart.Items.RemoveAll(item => item.PhotoId == photoId);

            // Create new cart item with print selections
            var cartItem = new CartItem
            {
                Id = ObjectId.GenerateNewId(),
                PhotoId = photoId,
                PhotoDetails = new CartPhotoDetails
                {
                    Filename = photo.FileInfo.OriginalFilename,
                    ThumbnailUrl = $"/api/photos/{photoId}/thumbnail",
                    Dimensions = photo.Dimensions
                },
                PrintSelections = new List<PrintSelection>(),
                AddedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };

            // Convert print selections with current pricing
            foreach (var selection in addToCartDto.PrintSelections)
            {
                var printSize = printSizes.First(ps => ps.SizeCode == selection.SizeCode);
                var lineTotal = selection.Quantity * printSize.Pricing.BasePrice;

                cartItem.PrintSelections.Add(new PrintSelection
                {
                    SizeCode = printSize.SizeCode,
                    SizeName = printSize.DisplayName,
                    Quantity = selection.Quantity,
                    UnitPrice = printSize.Pricing.BasePrice,
                    LineTotal = lineTotal
                });
            }

            cartItem.PhotoTotal = cartItem.PrintSelections.Sum(ps => ps.LineTotal);
            cart.Items.Add(cartItem);

            // Recalculate cart totals
            await RecalculateCartTotals(cart);

            // Save updated cart
            await _cartRepository.UpdateAsync(cart);

            var cartDto = await ConvertToCartDto(cart);

            _logger.LogInformation("Photo {PhotoId} added to cart for user {UserId} with {SelectionCount} print selections",
                photoId, userId, addToCartDto.PrintSelections.Count);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cartDto,
                "Photo added to cart successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding photo to cart");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to add to cart",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Update print selections for existing cart item.
    /// Allows customers to modify quantities or add/remove print sizes.
    /// </summary>
    /// <param name="itemId">Cart item ID</param>
    /// <param name="updateDto">Updated print selections</param>
    /// <returns>Updated cart information</returns>
    /// <response code="200">Cart item updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Cart item not found</response>
    [HttpPut("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(
        string itemId,
        [FromBody] UpdateCartItemDto updateDto)
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

            if (!ObjectId.TryParse(itemId, out var cartItemId))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid item ID",
                    "Cart item ID format is invalid"));
            }

            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);
            var cartItem = cart.Items.FirstOrDefault(item => item.Id == cartItemId);

            if (cartItem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Cart item not found",
                    "Cart item not found"));
            }

            // If no print selections provided, remove item from cart
            if (updateDto.PrintSelections == null || updateDto.PrintSelections.Count == 0)
            {
                cart.Items.Remove(cartItem);
            }
            else
            {
                // Validate and update print selections
                var sizeCodes = updateDto.PrintSelections.Select(ps => ps.SizeCode).ToList();
                var printSizes = await _printSizeRepository.GetBySizeCodesAsync(sizeCodes);

                if (printSizes.Count != sizeCodes.Count)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        "Invalid print sizes",
                        "One or more print sizes are invalid"));
                }

                // Update print selections with current pricing
                cartItem.PrintSelections.Clear();
                foreach (var selection in updateDto.PrintSelections)
                {
                    var printSize = printSizes.First(ps => ps.SizeCode == selection.SizeCode);
                    var lineTotal = selection.Quantity * printSize.Pricing.BasePrice;

                    cartItem.PrintSelections.Add(new PrintSelection
                    {
                        SizeCode = printSize.SizeCode,
                        SizeName = printSize.DisplayName,
                        Quantity = selection.Quantity,
                        UnitPrice = printSize.Pricing.BasePrice,
                        LineTotal = lineTotal
                    });
                }

                cartItem.PhotoTotal = cartItem.PrintSelections.Sum(ps => ps.LineTotal);
                cartItem.LastModified = DateTime.UtcNow;
            }

            // Recalculate cart totals
            await RecalculateCartTotals(cart);

            // Save updated cart
            await _cartRepository.UpdateAsync(cart);

            var cartDto = await ConvertToCartDto(cart);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cartDto,
                "Cart updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to update cart",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Remove specific item from cart.
    /// Completely removes the photo and all its print selections from the cart.
    /// </summary>
    /// <param name="itemId">Cart item ID</param>
    /// <returns>Updated cart information</returns>
    /// <response code="200">Item removed successfully</response>
    /// <response code="401">Authentication required</response>
    /// <response code="404">Cart item not found</response>
    [HttpDelete("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveCartItem(string itemId)
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

            if (!ObjectId.TryParse(itemId, out var cartItemId))
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Item not found",
                    "Invalid cart item ID"));
            }

            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);
            var cartItem = cart.Items.FirstOrDefault(item => item.Id == cartItemId);

            if (cartItem == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Item not found",
                    "Cart item not found"));
            }

            cart.Items.Remove(cartItem);

            // Recalculate cart totals
            await RecalculateCartTotals(cart);

            // Save updated cart
            await _cartRepository.UpdateAsync(cart);

            var cartDto = await ConvertToCartDto(cart);

            return Ok(ApiResponse<CartDto>.SuccessResponse(
                cartDto,
                "Item removed from cart"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to remove item",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Clear entire cart.
    /// Removes all items from the user's shopping cart.
    /// </summary>
    /// <returns>Confirmation of cart clearing</returns>
    /// <response code="200">Cart cleared successfully</response>
    /// <response code="401">Authentication required</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> ClearCart()
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

            await _cartRepository.ClearCartAsync(userId.Value);

            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Cart cleared successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to clear cart",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Calculate order total with tax for checkout preview.
    /// Provides accurate total calculation based on shipping address before order placement.
    /// </summary>
    /// <param name="taxCalcDto">Shipping address for tax calculation</param>
    /// <returns>Calculated totals with tax</returns>
    /// <response code="200">Total calculated successfully</response>
    /// <response code="400">Invalid address data</response>
    /// <response code="401">Authentication required</response>
    [HttpPost("calculate-total")]
    [ProducesResponseType(typeof(ApiResponse<TaxCalculationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TaxCalculationResultDto>>> CalculateTotal([FromBody] TaxCalculationDto taxCalcDto)
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

            var cart = await _cartRepository.GetByUserIdAsync(userId.Value);
            var subtotal = cart.Summary.Subtotal;

            // Get tax rate for state
            var taxRate = await GetTaxRateForState(taxCalcDto.State);
            var taxAmount = subtotal * taxRate;
            var total = subtotal + taxAmount;

            var result = new TaxCalculationResultDto
            {
                Subtotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                Total = total
            };

            return Ok(ApiResponse<TaxCalculationResultDto>.SuccessResponse(
                result,
                "Total calculated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating total");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to calculate total",
                    "An unexpected error occurred"));
        }
    }

    /// <summary>
    /// Helper method to extract authenticated user ID from JWT token.
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

    /// <summary>
    /// Convert cart entity to DTO for API response.
    /// </summary>
    private async Task<CartDto> ConvertToCartDto(ShoppingCart cart)
    {
        var cartItems = cart.Items.Select(item => new CartItemDto
        {
            Id = item.Id.ToString(),
            PhotoId = item.PhotoId.ToString(),
            PhotoFilename = item.PhotoDetails.Filename,
            PhotoThumbnailUrl = item.PhotoDetails.ThumbnailUrl,
            PhotoDimensions = new PhotoDimensionsDto
            {
                Width = item.PhotoDetails.Dimensions.Width,
                Height = item.PhotoDetails.Dimensions.Height,
                AspectRatio = item.PhotoDetails.Dimensions.AspectRatio
            },
            PrintSelections = item.PrintSelections.Select(ps => new CartPrintSelectionDto
            {
                SizeCode = ps.SizeCode,
                SizeName = ps.SizeName,
                Quantity = ps.Quantity,
                UnitPrice = ps.UnitPrice,
                LineTotal = ps.LineTotal
            }).ToList(),
            PhotoTotal = item.PhotoTotal,
            AddedAt = item.AddedAt
        }).ToList();

        return new CartDto
        {
            Items = cartItems,
            Summary = new CartSummaryDto
            {
                TotalPhotos = cart.Summary.TotalPhotos,
                TotalPrints = cart.Summary.TotalPrints,
                Subtotal = cart.Summary.Subtotal,
                Tax = cart.Summary.EstimatedTax,
                Total = cart.Summary.EstimatedTotal
            }
        };
    }

    /// <summary>
    /// Recalculate cart totals and update summary.
    /// </summary>
    private async Task RecalculateCartTotals(ShoppingCart cart)
    {
        cart.Summary.TotalPhotos = cart.Items.Count;
        cart.Summary.TotalPrints = cart.Items.Sum(item => item.PrintSelections.Sum(ps => ps.Quantity));
        cart.Summary.Subtotal = cart.Items.Sum(item => item.PhotoTotal);

        // Use default tax rate for estimation (actual calculation during checkout)
        var defaultTaxRate = await GetTaxRateForState("MA"); // Default state
        cart.Summary.EstimatedTax = cart.Summary.Subtotal * defaultTaxRate;
        cart.Summary.EstimatedTotal = cart.Summary.Subtotal + cart.Summary.EstimatedTax;
    }

    /// <summary>
    /// Get tax rate for specific state from system settings.
    /// </summary>
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

                // Fall back to default rate
                if (taxSettings.Value.TryGetValue("default", out var defaultValue) &&
                    defaultValue.IsNumeric)
                {
                    return (decimal)defaultValue.AsDouble;
                }
            }

            // Hard-coded fallback if settings not found
            return 0.0625m; // 6.25% default rate
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting tax rate for state {State}, using default", state);
            return 0.0625m; // 6.25% fallback rate
        }
    }
}