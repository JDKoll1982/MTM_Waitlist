using MTM_Waitlist.Models;

namespace MTM_Waitlist.Contracts.Services;

public interface IStartupCoordinator
{
    Task<StartupResult> RunAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default, bool retryDatabasePhaseOnly = false);
}
