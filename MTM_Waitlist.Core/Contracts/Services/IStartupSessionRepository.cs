using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

public interface IStartupSessionRepository
{
    Task<DateTimeOffset?> ReadServerTimeUtcAsync(CancellationToken cancellationToken = default);

    Task<StartupSessionSnapshot> ReadSessionSnapshotAsync(
        string username,
        string hostnameNormalized,
        string macAddressNormalized,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports whether the named account still holds its temporary default password, so startup can prompt for
    /// a new one before the sign-in form is shown.
    /// </summary>
    /// <param name="username">The account name, matched against the store's normalized username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="StartupPasswordResetRequirement.None"/> when no prompt is warranted — the account has a real
    /// password, or it (or the store) could not be resolved.
    /// </returns>
    Task<StartupPasswordResetRequirement> ReadPasswordResetRequirementAsync(
        string username,
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
