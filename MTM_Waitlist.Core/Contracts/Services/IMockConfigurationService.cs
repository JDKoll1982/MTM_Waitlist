using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Reads and writes the centrally-stored mock settings for the external data sources (Infor Visual +
/// Receiving) in <c>mtm_waitlist.config_settings_values</c> (all_users scope) so an Admin/Developer-set
/// value reflects across running clients. DB access is SP-only.
/// </summary>
public interface IMockConfigurationService
{
    /// <summary>Reads the central mock setting for one source; <c>IsPresent=false</c> if not configured.</summary>
    Task<MockSettingState> GetMockSettingAsync(ConnectionSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts the central mock setting for one source. <paramref name="updatedByUserId"/> is optional
    /// (null allowed). The service does not gate roles itself; callers must enforce role authorization.
    /// </summary>
    Task<bool> SetMockSettingAsync(ConnectionSource source, bool enabled, long? updatedByUserId, CancellationToken cancellationToken = default);
}
