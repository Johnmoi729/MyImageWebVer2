using MongoDB.Driver;
using MongoDB.Bson;
using MyImage.Core.Entities;
using MyImage.Core.Interfaces.Repositories;
using MyImage.Core.DTOs.Common;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace MyImage.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for SystemSettings entity operations.
/// Handles application configuration storage and retrieval.
/// FIXED: Corrected UpsertAsync method to handle _id field properly.
/// </summary>
public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly IMongoCollection<SystemSettings> _settings;
    private readonly ILogger<SystemSettingsRepository> _logger;

    public SystemSettingsRepository(MongoDbContext context, ILogger<SystemSettingsRepository> logger)
    {
        _settings = context.SystemSettings;
        _logger = logger;
    }

    /// <summary>
    /// Get setting value by key.
    /// </summary>
    public async Task<SystemSettings?> GetByKeyAsync(string key)
    {
        try
        {
            var filter = Builders<SystemSettings>.Filter.Eq(s => s.Key, key);
            var setting = await _settings.Find(filter).FirstOrDefaultAsync();

            if (setting != null)
            {
                _logger.LogDebug("Retrieved setting: {Key}", key);
            }

            return setting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting by key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// FIXED: Update or create setting value with proper _id handling.
    /// The original implementation was causing the immutable _id field error.
    /// </summary>
    public async Task<SystemSettings> UpsertAsync(SystemSettings setting)
    {
        try
        {
            // Ensure metadata is initialized
            setting.Metadata ??= new SettingMetadata();
            setting.Metadata.UpdatedBy ??= "system";
            setting.Metadata.LastUpdated = DateTime.UtcNow;

            // Check if setting already exists by Key (not by _id)
            var existingSetting = await GetByKeyAsync(setting.Key);

            if (existingSetting != null)
            {
                // Update existing setting - CRITICAL: preserve the original _id
                setting.Id = existingSetting.Id; // Use existing document's _id

                // Preserve original metadata if it exists
                if (existingSetting.Metadata != null)
                {
                    // Keep existing UpdatedBy if not specified
                    setting.Metadata.UpdatedBy = setting.Metadata.UpdatedBy ?? existingSetting.Metadata.UpdatedBy;
                }

                // Use _id-based filter for replacement to avoid _id conflicts
                var filter = Builders<SystemSettings>.Filter.Eq(s => s.Id, existingSetting.Id);
                await _settings.ReplaceOneAsync(filter, setting);

                _logger.LogInformation("Updated existing setting: {Key}", setting.Key);
            }
            else
            {
                // Create new setting - let MongoDB auto-generate _id
                // Do NOT set setting.Id explicitly

                await _settings.InsertOneAsync(setting);

                _logger.LogInformation("Created new setting: {Key}", setting.Key);
            }

            return setting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert setting: {Key}", setting.Key);
            throw;
        }
    }

    /// <summary>
    /// Alternative safer upsert implementation using UpdateOneAsync.
    /// This approach completely avoids _id conflicts by using MongoDB's update operators.
    /// </summary>
    public async Task<SystemSettings> UpsertAsyncSafe(SystemSettings setting)
    {
        try
        {
            // Ensure metadata is initialized
            setting.Metadata ??= new SettingMetadata();
            setting.Metadata.UpdatedBy ??= "system";
            setting.Metadata.LastUpdated = DateTime.UtcNow;

            var filter = Builders<SystemSettings>.Filter.Eq(s => s.Key, setting.Key);

            // Build update definition that never touches _id field
            var updateBuilder = Builders<SystemSettings>.Update
                .Set(s => s.Value, setting.Value)
                .Set(s => s.Metadata.Description, setting.Metadata.Description)
                .Set(s => s.Metadata.UpdatedBy, setting.Metadata.UpdatedBy)
                .Set(s => s.Metadata.LastUpdated, DateTime.UtcNow)
                .SetOnInsert(s => s.Key, setting.Key); // Only set on insert

            var options = new UpdateOptions { IsUpsert = true };

            await _settings.UpdateOneAsync(filter, updateBuilder, options);

            // Return the updated/inserted document
            var result = await GetByKeyAsync(setting.Key);

            _logger.LogInformation("Safely upserted setting: {Key}", setting.Key);
            return result ?? setting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to safely upsert setting: {Key}", setting.Key);
            throw;
        }
    }

    /// <summary>
    /// Get all system settings for admin management.
    /// </summary>
    public async Task<List<SystemSettings>> GetAllAsync()
    {
        try
        {
            var settings = await _settings.Find(_ => true).ToListAsync();

            _logger.LogDebug("Retrieved {Count} system settings", settings.Count);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all system settings");
            throw;
        }
    }
}