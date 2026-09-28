using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="INewRequestAlertService"/>
/// <remarks>
/// The switch is held against the person in the store, so turning alerts off on one computer turns them off
/// everywhere that person signs in (FR-023, SC-008). It is a person's preference and not a machine's: a desk
/// shared by three shifts should not carry the last operator's choice into the next shift.
/// </remarks>
public sealed class NewRequestAlertService : INewRequestAlertService
{
    /// <summary>The scoped-preference key the alerts switch is stored under, against the person.</summary>
    public const string SettingKeyName = "User.NewRequestAlertsEnabled";

    private readonly IScopedPreferenceStore _preferences;

    public NewRequestAlertService(IScopedPreferenceStore preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
    }

    public string SettingKey => SettingKeyName;

    public async Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _preferences.ReadFlagAsync(SettingKeyName, PreferenceScope.Person, cancellationToken).ConfigureAwait(false) ?? false;
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _preferences.WriteFlagAsync(SettingKeyName, PreferenceScope.Person, enabled, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // This write is started from a toggle that has already moved on screen, so nothing is waiting to hear
            // that it failed. Recording it is what keeps the failure from becoming an unobserved task.
            AppLog.Error("NewRequestAlerts", ex, "The new-request alerts preference could not be stored; it applies to this session only.");
        }
    }

    public async Task<bool> ShouldNotifyOnCreatedAsync(
        bool requestCreatedSignal,
        bool isPackaged,
        CancellationToken cancellationToken = default)
    {
        var enabled = await GetEnabledAsync(cancellationToken).ConfigureAwait(false);
        return RequestAlertGate.ShouldNotifyOnCreated(requestCreatedSignal, enabled, isPackaged);
    }
}
