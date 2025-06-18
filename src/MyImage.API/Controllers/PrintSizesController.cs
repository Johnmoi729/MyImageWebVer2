using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.PrintSizes;
using MyImage.Core.DTOs.Common;

namespace MyImage.API.Controllers;

/// <summary>
/// Print sizes controller for customer-facing operations.
/// This controller provides public access to active print sizes and their pricing
/// for use in the shopping cart and photo selection process.
/// 
/// Unlike the admin controller, this controller only returns active print sizes
/// and doesn't require authentication for basic price information viewing.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PrintSizesController : ControllerBase
{
    private readonly IPrintSizeRepository _printSizeRepository;
    private readonly ILogger<PrintSizesController> _logger;

    /// <summary>
    /// Initialize print sizes controller with required repository.
    /// </summary>
    /// <param name="printSizeRepository">Print size data access repository</param>
    /// <param name="logger">Logger for print size operations</param>
    public PrintSizesController(
        IPrintSizeRepository printSizeRepository,
        ILogger<PrintSizesController> logger)
    {
        _printSizeRepository = printSizeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all active print sizes for customer selection.
    /// Returns only active print sizes with current pricing, sorted by display order.
    /// This endpoint is used in the photo selection interface and shopping cart.
    /// 
    /// No authentication required as pricing information is public for browsing.
    /// </summary>
    /// <returns>Active print sizes with current pricing</returns>
    /// <response code="200">Print sizes retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [AllowAnonymous] // FIXED: Allow anonymous access for public pricing information
    [ProducesResponseType(typeof(ApiResponse<List<PrintSizeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PrintSizeDto>>>> GetActivePrintSizes()
    {
        try
        {
            _logger.LogDebug("Retrieving active print sizes for customer display");

            // Get only active print sizes sorted by display order
            var printSizes = await _printSizeRepository.GetActiveAsync();

            // Convert to customer-facing DTOs (exclude admin-only information)
            var printSizeDtos = printSizes.Select(ps => new PrintSizeDto
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
                RecommendedHeight = ps.Dimensions.PixelRequirements.RecommendedHeight
            }).ToList();

            _logger.LogDebug("Retrieved {Count} active print sizes", printSizeDtos.Count);

            return Ok(ApiResponse<List<PrintSizeDto>>.SuccessResponse(
                printSizeDtos,
                $"Retrieved {printSizeDtos.Count} available print sizes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active print sizes");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve print sizes",
                    "An unexpected error occurred while retrieving print sizes"));
        }
    }

    /// <summary>
    /// Get specific print size by size code.
    /// Returns detailed information about a specific print size including pricing and requirements.
    /// Used for detailed display in photo selection interface.
    /// </summary>
    /// <param name="sizeCode">Print size code (e.g., "4x6", "5x7")</param>
    /// <returns>Print size details if found and active</returns>
    /// <response code="200">Print size details retrieved successfully</response>
    /// <response code="404">Print size not found or inactive</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{sizeCode}")]
    [AllowAnonymous] // FIXED: Allow anonymous access for public pricing information
    [ProducesResponseType(typeof(ApiResponse<PrintSizeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PrintSizeDto>>> GetPrintSize(string sizeCode)
    {
        try
        {
            _logger.LogDebug("Retrieving print size details for code: {SizeCode}", sizeCode);

            // Get active print size by code
            var printSize = await _printSizeRepository.GetBySizeCodeAsync(sizeCode);

            if (printSize == null)
            {
                _logger.LogDebug("Print size not found or inactive: {SizeCode}", sizeCode);
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "Print size not found",
                    $"Print size '{sizeCode}' not found or is currently unavailable"));
            }

            // Convert to customer-facing DTO
            var printSizeDto = new PrintSizeDto
            {
                Id = printSize.Id.ToString(),
                SizeCode = printSize.SizeCode,
                DisplayName = printSize.DisplayName,
                Width = printSize.Dimensions.Width,
                Height = printSize.Dimensions.Height,
                Unit = printSize.Dimensions.Unit,
                Price = printSize.Pricing.BasePrice,
                Currency = printSize.Pricing.Currency,
                IsActive = printSize.Metadata.IsActive,
                MinWidth = printSize.Dimensions.PixelRequirements.MinWidth,
                MinHeight = printSize.Dimensions.PixelRequirements.MinHeight,
                RecommendedWidth = printSize.Dimensions.PixelRequirements.RecommendedWidth,
                RecommendedHeight = printSize.Dimensions.PixelRequirements.RecommendedHeight
            };

            _logger.LogDebug("Retrieved print size: {SizeCode} - {DisplayName} (${Price})",
                printSize.SizeCode, printSize.DisplayName, printSize.Pricing.BasePrice);

            return Ok(ApiResponse<PrintSizeDto>.SuccessResponse(
                printSizeDto,
                $"Print size {sizeCode} details retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving print size: {SizeCode}", sizeCode);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to retrieve print size",
                    "An unexpected error occurred while retrieving print size details"));
        }
    }

    /// <summary>
    /// Get print size recommendations for a photo based on its dimensions.
    /// Analyzes photo dimensions against print size requirements to recommend
    /// which sizes will produce good quality prints.
    /// 
    /// This helps customers make informed decisions about print quality.
    /// </summary>
    /// <param name="photoWidth">Photo width in pixels</param>
    /// <param name="photoHeight">Photo height in pixels</param>
    /// <returns>Print sizes with quality recommendations</returns>
    /// <response code="200">Recommendations retrieved successfully</response>
    /// <response code="400">Invalid photo dimensions</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("recommendations")]
    [AllowAnonymous] // FIXED: Allow anonymous access for public recommendations
    [ProducesResponseType(typeof(ApiResponse<List<PrintSizeRecommendationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<PrintSizeRecommendationDto>>>> GetRecommendations(
        [FromQuery] int photoWidth,
        [FromQuery] int photoHeight)
    {
        try
        {
            _logger.LogDebug("Getting print size recommendations for photo: {Width}x{Height}",
                photoWidth, photoHeight);

            // Validate photo dimensions
            if (photoWidth <= 0 || photoHeight <= 0)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid photo dimensions",
                    "Photo width and height must be positive numbers"));
            }

            if (photoWidth > 50000 || photoHeight > 50000)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Invalid photo dimensions",
                    "Photo dimensions are unreasonably large"));
            }

            // Get all active print sizes
            var printSizes = await _printSizeRepository.GetActiveAsync();

            // Generate recommendations based on photo dimensions
            var recommendations = printSizes.Select(ps =>
            {
                var quality = DetermineQuality(photoWidth, photoHeight, ps);
                return new PrintSizeRecommendationDto
                {
                    PrintSize = new PrintSizeDto
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
                        RecommendedHeight = ps.Dimensions.PixelRequirements.RecommendedHeight
                    },
                    QualityRating = quality.Rating,
                    QualityDescription = quality.Description,
                    IsRecommended = quality.Rating >= 3 // 3+ stars recommended
                };
            })
            .OrderByDescending(r => r.QualityRating) // Best quality first
            .ThenBy(r => r.PrintSize.Price) // Then by price
            .ToList();

            _logger.LogDebug("Generated recommendations for {Count} print sizes", recommendations.Count);

            return Ok(ApiResponse<List<PrintSizeRecommendationDto>>.SuccessResponse(
                recommendations,
                $"Generated recommendations for {recommendations.Count} print sizes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating print size recommendations");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse(
                    "Failed to generate recommendations",
                    "An unexpected error occurred while generating recommendations"));
        }
    }

    /// <summary>
    /// Determine print quality rating for a photo with specific print size.
    /// Compares photo resolution against print size requirements.
    /// </summary>
    /// <param name="photoWidth">Photo width in pixels</param>
    /// <param name="photoHeight">Photo height in pixels</param>
    /// <param name="printSize">Print size to evaluate against</param>
    /// <returns>Quality rating and description</returns>
    private static (int Rating, string Description) DetermineQuality(
        int photoWidth,
        int photoHeight,
        Core.Entities.PrintSize printSize)
    {
        var requirements = printSize.Dimensions.PixelRequirements;

        // Calculate scaling factors for both dimensions
        var scaleX = (double)requirements.RecommendedWidth / photoWidth;
        var scaleY = (double)requirements.RecommendedHeight / photoHeight;

        // Use the larger scale to see if photo can fill the print size
        var scale = Math.Max(scaleX, scaleY);

        // Calculate effective resolution when scaled to print size
        var effectiveWidth = (int)(photoWidth / scale);
        var effectiveHeight = (int)(photoHeight / scale);

        // Determine quality based on resolution
        if (effectiveWidth >= requirements.RecommendedWidth && effectiveHeight >= requirements.RecommendedHeight)
        {
            return (5, "Excellent quality - High resolution recommended for this print size");
        }
        else if (effectiveWidth >= requirements.MinWidth * 1.5 && effectiveHeight >= requirements.MinHeight * 1.5)
        {
            return (4, "Very good quality - Well above minimum requirements");
        }
        else if (effectiveWidth >= requirements.MinWidth && effectiveHeight >= requirements.MinHeight)
        {
            return (3, "Good quality - Meets minimum requirements");
        }
        else if (effectiveWidth >= requirements.MinWidth * 0.8 && effectiveHeight >= requirements.MinHeight * 0.8)
        {
            return (2, "Fair quality - Slightly below minimum, may show some pixelation");
        }
        else
        {
            return (1, "Poor quality - Resolution too low for good print quality");
        }
    }
}

/// <summary>
/// DTO for print size recommendations including quality assessment.
/// </summary>
public class PrintSizeRecommendationDto
{
    /// <summary>
    /// Print size information
    /// </summary>
    public PrintSizeDto PrintSize { get; set; } = new();

    /// <summary>
    /// Quality rating from 1-5 stars based on photo resolution
    /// </summary>
    public int QualityRating { get; set; }

    /// <summary>
    /// Human-readable quality description
    /// </summary>
    public string QualityDescription { get; set; } = string.Empty;

    /// <summary>
    /// Whether this print size is recommended for the photo
    /// </summary>
    public bool IsRecommended { get; set; }
}