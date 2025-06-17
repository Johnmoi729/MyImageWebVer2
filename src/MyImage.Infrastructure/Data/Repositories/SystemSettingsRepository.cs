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
    /// Update or create setting value.
    /// </summary>
    public async Task<SystemSettings> UpsertAsync(SystemSettings setting)
    {
        try
        {
            setting.Metadata.LastUpdated = DateTime.UtcNow;

            var filter = Builders<SystemSettings>.Filter.Eq(s => s.Key, setting.Key);
            var options = new ReplaceOptions { IsUpsert = true };

            await _settings.ReplaceOneAsync(filter, setting, options);

            _logger.LogInformation("Upserted setting: {Key}", setting.Key);
            return setting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert setting: {Key}", setting.Key);
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