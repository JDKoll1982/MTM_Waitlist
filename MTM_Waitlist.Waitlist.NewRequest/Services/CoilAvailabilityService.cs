using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

public sealed class CoilAvailabilityService : ICoilAvailabilityService
{
    public Task<WaitlistCoilInfo> GetCoilForJobAsync(string? workCenter, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Live Infor Visual coil lookup is not wired yet; keep coil available (legacy behavior)
        // so the Coil request type is not hidden until the live SQL-queue source lands (FR-019, task T099).
        StartupDebugLog.Info("NewRequestJobType", $"Live coil source not configured; assuming coil available for work center '{workCenter ?? string.Empty}'.");
        return Task.FromResult(new WaitlistCoilInfo { HasCoil = true });
    }
}
