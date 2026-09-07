using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IMockToggleService"/>
public sealed class MockToggleService : IMockToggleService
{
    /// <summary>The master/visual key: the single "Mock Data" toggle shown in Settings.</summary>
    public const string MasterKeyName = "Feature.InforVisualMockData";

    /// <summary>The mirror key kept equal to the master (receiving/flatstock readers).</summary>
    public const string MirrorKeyName = "Feature.RecvMockData";

    private readonly ILocalSettingsService _localSettingsService;

    public MockToggleService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public string MasterKey => MasterKeyName;

    public string MirrorKey => MirrorKeyName;

    public async Task<bool> GetEffectiveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _localSettingsService.ReadSettingAsync<bool?>(MasterKeyName).ConfigureAwait(false) ?? false;
    }

    public async Task SetAsync(bool value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Keep both keys equal so every existing reader sees the same effective mock-data value.
        await _localSettingsService.SaveSettingAsync(MasterKeyName, value).ConfigureAwait(false);
        await _localSettingsService.SaveSettingAsync(MirrorKeyName, value).ConfigureAwait(false);
    }
}
