using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupSessionRepository
{
    Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default);

    Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(
        string username,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken = default);

    Task<StartupCredentialCheckResult> CheckCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAsync(
        long userId,
        string newPassword,
        CancellationToken cancellationToken = default);
}
