using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for PrintSize entity operations.
/// Handles print size configuration and pricing management.
/// </summary>
public interface IPrintSizeRepository
{
    /// <summary>
    /// Get all active print sizes for customer display.
    /// Returns only sizes available for ordering, sorted by display order.
    /// </summary>
    /// <returns>Active print sizes in display order</returns>
    Task<List<PrintSize>> GetActiveAsync();

    /// <summary>
    /// Get all print sizes for admin management.
    /// Includes inactive sizes for administrative control.
    /// </summary>
    /// <returns>All print sizes regardless of status</returns>
    Task<List<PrintSize>> GetAllAsync();

    /// <summary>
    /// Get specific print size by ObjectId.
    /// Used for admin operations and detailed lookup.
    /// </summary>
    /// <param name="id">PrintSize ObjectId</param>
    /// <returns>Print size if found</returns>
    Task<PrintSize?> GetByIdAsync(ObjectId id);

    /// <summary>
    /// Get print size by size code for cart operations.
    /// Used when customers select print sizes for photos.
    /// </summary>
    /// <param name="sizeCode">Size code like "4x6", "5x7"</param>
    /// <returns>Print size if found and active</returns>
    Task<PrintSize?> GetBySizeCodeAsync(string sizeCode);

    /// <summary>
    /// Get multiple print sizes by codes for batch operations.
    /// Used during cart processing and order creation.
    /// </summary>
    /// <param name="sizeCodes">Collection of size codes</param>
    /// <returns>Collection of matching print sizes</returns>
    Task<List<PrintSize>> GetBySizeCodesAsync(List<string> sizeCodes);

    /// <summary>
    /// Create new print size.
    /// Used by admin to add new print options.
    /// </summary>
    /// <param name="printSize">Print size to create</param>
    /// <returns>Created print size</returns>
    Task<PrintSize> CreateAsync(PrintSize printSize);

    /// <summary>
    /// Update existing print size.
    /// Used by admin for price changes and status updates.
    /// </summary>
    /// <param name="printSize">Print size with updates</param>
    /// <returns>Updated print size</returns>
    Task<PrintSize> UpdateAsync(PrintSize printSize);

    /// <summary>
    /// Check if size code already exists.
    /// Prevents duplicate size codes during creation.
    /// </summary>
    /// <param name="sizeCode">Size code to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> SizeCodeExistsAsync(string sizeCode);
}