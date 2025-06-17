using MyImage.Core.Entities;

namespace MyImage.Core.Interfaces.Repositories
{
    /// <summary>
    /// Repository interface for SystemSettings operations.
    /// Defines contract for managing application configuration settings.
    /// </summary>
    public interface ISystemSettingsRepository
    {
        /// <summary>
        /// Gets a system setting by its unique key.
        /// Used throughout application for configuration lookup.
        /// </summary>
        /// <param name="key">The unique key of the setting</param>
        /// <returns>The setting if found, null otherwise</returns>
        Task<SystemSettings?> GetByKeyAsync(string key);

        /// <summary>
        /// Creates a new setting if it doesn't exist, updates if it does exist.
        /// Uses the Key field as the unique identifier for upsert operations.
        /// FIXED: Now properly handles MongoDB _id field immutability.
        /// </summary>
        /// <param name="setting">The setting to upsert</param>
        /// <returns>The upserted setting</returns>
        Task<SystemSettings> UpsertAsync(SystemSettings setting);

        /// <summary>
        /// Gets all system settings for admin management.
        /// Used in admin interface for configuration overview.
        /// </summary>
        /// <returns>List of all system settings</returns>
        Task<List<SystemSettings>> GetAllAsync();
    }
}