namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Per-user "alert me on new waitlist requests" toggle + the file-09 new-request toast decision. Owns the single
/// setting key and folds the persisted toggle into <c>RequestAlertGate</c> so callers ask one async question.
/// A toast only fires on a request-created signal when the per-user toggle is ON <em>and</em> the app is packaged
/// (MSIX); OFF or unpackaged = no-op. The backing <see cref="ILocalSettingsService"/> store is per-Windows-user.
/// </summary>
public interface INewRequestAlertService
{
    /// <summary>The local-settings key that stores the per-user toggle.</summary>
    string SettingKey { get; }

    /// <summary>Reads the per-user toggle, defaulting to OFF when never set.</summary>
    Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the per-user toggle value.</summary>
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// The file-09 toast decision for a request-created signal: true only when a new request was actually created,
    /// the stored per-user toggle is ON, and the app is packaged so <c>IAppNotificationService</c> can show it.
    /// </summary>
    Task<bool> ShouldNotifyOnCreatedAsync(bool requestCreatedSignal, bool isPackaged, CancellationToken cancellationToken = default);
}
