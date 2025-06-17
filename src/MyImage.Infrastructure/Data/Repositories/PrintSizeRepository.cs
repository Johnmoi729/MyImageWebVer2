using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Common;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for PrintSize entity operations.
/// Handles print size configuration and pricing management for administrators.
/// Implements Requirement 11 where "admin will decide the price and other things".
/// </summary>
public class PrintSizeRepository : IPrintSizeRepository
{
    private readonly IMongoCollection<PrintSize> _printSizes;
    private readonly ILogger<PrintSizeRepository> _logger;

    public PrintSizeRepository(MongoDbContext context, ILogger<PrintSizeRepository> logger)
    {
        _printSizes = context.PrintSizes;
        _logger = logger;
    }

    /// <summary>
    /// Get all active print sizes for customer display.
    /// Returns only sizes available for ordering, sorted by display order.
    /// Used in photo selection interface and cart operations.
    /// </summary>
    public async Task<List<PrintSize>> GetActiveAsync()
    {
        try
        {
            var filter = Builders<PrintSize>.Filter.Eq("metadata.isActive", true);
            var sort = Builders<PrintSize>.Sort.Ascending("metadata.sortOrder");

            var printSizes = await _printSizes.Find(filter).Sort(sort).ToListAsync();

            _logger.LogDebug("Retrieved {Count} active print sizes", printSizes.Count);
            return printSizes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active print sizes");
            throw;
        }
    }

    /// <summary>
    /// Get all print sizes for admin management.
    /// Includes inactive sizes for administrative control.
    /// </summary>
    public async Task<List<PrintSize>> GetAllAsync()
    {
        try
        {
            var sort = Builders<PrintSize>.Sort
                .Descending("metadata.isActive")
                .Ascending("metadata.sortOrder");

            var printSizes = await _printSizes.Find(_ => true).Sort(sort).ToListAsync();

            _logger.LogDebug("Retrieved {Count} print sizes for admin", printSizes.Count);
            return printSizes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all print sizes");
            throw;
        }
    }

    /// <summary>
    /// Get print size by size code for cart operations.
    /// Used when customers select print sizes for photos.
    /// </summary>
    public async Task<PrintSize?> GetBySizeCodeAsync(string sizeCode)
    {
        try
        {
            var filter = Builders<PrintSize>.Filter.And(
                Builders<PrintSize>.Filter.Eq(ps => ps.SizeCode, sizeCode),
                Builders<PrintSize>.Filter.Eq("metadata.isActive", true)
            );

            var printSize = await _printSizes.Find(filter).FirstOrDefaultAsync();

            if (printSize != null)
            {
                _logger.LogDebug("Found print size: {SizeCode} -> {DisplayName}", sizeCode, printSize.DisplayName);
            }

            return printSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting print size by code: {SizeCode}", sizeCode);
            throw;
        }
    }

    /// <summary>
    /// Get multiple print sizes by codes for batch operations.
    /// Used during cart processing and order creation.
    /// </summary>
    public async Task<List<PrintSize>> GetBySizeCodesAsync(List<string> sizeCodes)
    {
        try
        {
            var filter = Builders<PrintSize>.Filter.And(
                Builders<PrintSize>.Filter.In(ps => ps.SizeCode, sizeCodes),
                Builders<PrintSize>.Filter.Eq("metadata.isActive", true)
            );

            var printSizes = await _printSizes.Find(filter).ToListAsync();

            _logger.LogDebug("Retrieved {Count} print sizes for {RequestedCount} codes",
                printSizes.Count, sizeCodes.Count);

            return printSizes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting print sizes by codes");
            throw;
        }
    }

    /// <summary>
    /// Get specific print size by ObjectId.
    /// Used for admin operations and detailed lookup.
    /// </summary>
    public async Task<PrintSize?> GetByIdAsync(ObjectId id)
    {
        try
        {
            var printSize = await _printSizes.Find(ps => ps.Id == id).FirstOrDefaultAsync();
            return printSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting print size by ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Create new print size.
    /// Used by admin to add new print options.
    /// </summary>
    public async Task<PrintSize> CreateAsync(PrintSize printSize)
    {
        try
        {
            printSize.Metadata.CreatedAt = DateTime.UtcNow;
            printSize.Metadata.UpdatedAt = DateTime.UtcNow;
            printSize.Pricing.LastUpdated = DateTime.UtcNow;

            await _printSizes.InsertOneAsync(printSize);

            _logger.LogInformation("Created new print size: {SizeCode} - {DisplayName}",
                printSize.SizeCode, printSize.DisplayName);

            return printSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create print size: {SizeCode}", printSize.SizeCode);
            throw;
        }
    }

    /// <summary>
    /// Update existing print size.
    /// Used by admin for price changes and status updates.
    /// </summary>
    public async Task<PrintSize> UpdateAsync(PrintSize printSize)
    {
        try
        {
            printSize.Metadata.UpdatedAt = DateTime.UtcNow;
            printSize.Pricing.LastUpdated = DateTime.UtcNow;

            var filter = Builders<PrintSize>.Filter.Eq(ps => ps.Id, printSize.Id);
            await _printSizes.ReplaceOneAsync(filter, printSize);

            _logger.LogInformation("Updated print size: {SizeCode} - {DisplayName}",
                printSize.SizeCode, printSize.DisplayName);

            return printSize;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update print size: {Id}", printSize.Id);
            throw;
        }
    }

    /// <summary>
    /// Check if size code already exists.
    /// Prevents duplicate size codes during creation.
    /// </summary>
    public async Task<bool> SizeCodeExistsAsync(string sizeCode)
    {
        try
        {
            var count = await _printSizes.CountDocumentsAsync(ps => ps.SizeCode == sizeCode);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking size code existence: {SizeCode}", sizeCode);
            throw;
        }
    }
}