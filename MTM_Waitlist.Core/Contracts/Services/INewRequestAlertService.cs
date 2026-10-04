namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// A person's "alert me on new waitlist requests" toggle + the file-09 new-request toast decision. Owns the single
/// setting key and folds the stored toggle into <c>RequestAlertGate</c> so callers ask one async question.
/// A toast only fires on a request-created signal when that person's toggle is ON <em>and</em> the app is packaged
/// (MSIX); OFF or unpackaged = no-op. The toggle is held against the person in the store, so it travels with them
/// rather than staying on the computer it was set on (FR-023).
/// </summary>
public interface INewRequestAlertService
{
    /// <summary>The scoped-preference key that stores the person's toggle.</summary>
    string SettingKey { get; }

    /// <summary>Reads the person's toggle, defaulting to OFF when never set.</summary>
    Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores the person's toggle value.</summary>
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// The file-09 toast decision for a request-created signal: true only when a new request was actually created,
    /// the stored per-user toggle is ON, and the app is packaged so <c>IAppNotificationService</c> can show it.
    /// </summary>
    Task<bool> ShouldNotifyOnCreatedAsync(bool requestCreatedSignal, bool isPackaged, CancellationToken cancellationToken = default);
}
