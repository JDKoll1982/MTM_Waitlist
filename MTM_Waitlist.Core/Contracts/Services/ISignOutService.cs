namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Signing out of the application (FR-033, FR-034).
/// <para>
/// Sign out <b>ends the session</b>: it clears the remembered-credential and session keys the sign-in path
/// reads, relaunches the application and exits this process, so the person lands back at the sign-in screen
/// even on a computer where "remember me" was set and the session had been restored. Clearing the displayed
/// name while the session persists is not signing out and must never be presented as though it were.
/// </para>
/// </summary>
public interface ISignOutService
{
    /// <summary>
    /// Ends the session and brings the application back at the sign-in screen. The outcome is returned rather
    /// than swallowed, so a relaunch that could not be performed is reported in plain language and the person
    /// is never left apparently signed in (FR-026, FR-034).
    /// </summary>
    Task<SignOutResult> SignOutAsync(CancellationToken cancellationToken = default);
}
