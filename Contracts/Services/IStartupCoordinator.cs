using MTM_Waitlist.Models;

namespace MTM_Waitlist.Contracts.Services;

public interface IStartupCoordinator
{
    Task<StartupResult> RunAsync(CancellationToken cancellationToken = default);
}
