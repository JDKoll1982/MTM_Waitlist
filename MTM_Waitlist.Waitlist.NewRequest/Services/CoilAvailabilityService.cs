using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class CoilAvailabilityService : ICoilAvailabilityService
{
    private const string InforVisualMockDataSettingKey = "Feature.InforVisualMockData";

    private readonly ILocalSettingsService? _localSettingsService;

    public CoilAvailabilityService(ILocalSettingsService? localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isMockDataEnabled = _localSettingsService is not null
            && (_localSettingsService.ReadSettingAsync<bool?>(InforVisualMockDataSettingKey).GetAwaiter().GetResult() ?? false);

        if (isMockDataEnabled)
        {
            var coil = SampleJobCoilCatalog.GetCoilForJob(workCenter);
            StartupDebugLog.Info("NewRequestJobType", $"Mock coil availability resolved for work center '{workCenter ?? string.Empty}'. HasCoil={coil.HasCoil}, CoilNumber='{coil.CoilNumber}'.");
            return Task.FromResult(coil);
        }

        // Live Infor Visual coil lookup is not wired yet; keep coil available (legacy behavior)
        // so the Coil request type is not hidden until the live SQL-queue source lands.
        StartupDebugLog.Info("NewRequestJobType", $"Live coil source not configured; assuming coil available for work center '{workCenter ?? string.Empty}'.");
        return Task.FromResult(new WaitlistCoilInfo { HasCoil = true });
    }
}
