using MyImage.Core.Entities;
using MyImage.Core.DTOs.Common;
using MongoDB.Bson;

namespace MyImage.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for SystemSettings entity operations.
/// Handles application configuration storage and retrieval.
/// </summary>
public interface ISystemSettingsRepository
{
    /// <summary>
    /// Get setting value by key.
    /// Used throughout application for configuration lookup.
    /// </summary>
    /// <param name="key">Setting key identifier</param>
    /// <returns>Setting entity if found</returns>
    Task<SystemSettings?> GetByKeyAsync(string key);

    /// <summary>
    /// Update or create setting value.
    /// Used by admin to modify system configuration.
    /// </summary>
    /// <param name="setting">Setting to update or create</param>
    /// <returns>Updated setting entity</returns>
    Task<SystemSettings> UpsertAsync(SystemSettings setting);

    /// <summary>
    /// Get all system settings for admin management.
    /// Used in admin interface for configuration overview.
    /// </summary>
    /// <returns>All system settings</returns>
    Task<List<SystemSettings>> GetAllAsync();
}