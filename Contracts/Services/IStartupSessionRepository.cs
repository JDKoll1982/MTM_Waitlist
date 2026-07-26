using MTM_Waitlist.Models;

namespace MTM_Waitlist.Contracts.Services;

public interface IStartupSessionRepository
{
    Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default);

    Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(
        string username,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken = default);
}
