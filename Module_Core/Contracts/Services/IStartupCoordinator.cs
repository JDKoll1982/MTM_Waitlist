using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupCoordinator
{
    Task<StartupResult> RunAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default, bool retryDatabasePhaseOnly = false);
}
