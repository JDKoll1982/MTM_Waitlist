using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="INewRequestAlertService"/>
public sealed class NewRequestAlertService : INewRequestAlertService
{
    /// <summary>The per-user local-settings key. Scoped per Windows user by the backing store (packaged
    /// ApplicationData or the unpackaged JSON file).</summary>
    public const string SettingKeyName = "User.NewRequestAlertsEnabled";

    private readonly ILocalSettingsService _localSettingsService;

    public NewRequestAlertService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public string SettingKey => SettingKeyName;

    public async Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _localSettingsService.ReadSettingAsync<bool?>(SettingKeyName).ConfigureAwait(false) ?? false;
    }

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _localSettingsService.SaveSettingAsync(SettingKeyName, enabled).ConfigureAwait(false);
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
