using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Signing out: it ends the person's session in the store and this process's identity, and then restarts the
/// application so the person lands back at the sign-in screen (FR-011, SC-001).
/// </summary>
/// <remarks>
/// <para>
/// <b>The order is the whole point.</b> The session is ended in the store <b>first</b>, the identity is dropped
/// in this process second, and only then is a replacement instance started and this one left. Clearing the
/// displayed name while the session still stands is not signing out, which is what FR-011 rules out in as many
/// words: a restarted application would come back to a session that was never ended.
/// </para>
/// <para>
/// <b>A session that could not be ended does not restart.</b> A store that refuses the clearing leaves the
/// session standing, and restarting over it would present the person with a signed-in application they asked to
/// leave. The refusal is stated instead, and the person is told to try again (FR-011).
/// </para>
/// <para>
/// <b>A restart that fails is still a sign-out.</b> The session has been ended and the identity dropped by the
/// time the restart is attempted, so a failed relaunch leaves nothing signed in and the person is told to open
/// the application again. That is the spec's own edge case, and it is deliberately not a rollback.
/// </para>
/// <para>
/// <b>It keeps nothing on the machine.</b> There is no local session file, no remembered credential and no key
/// to clear: the session lives in the store and the identity in memory, so this type writes nothing anywhere
/// (FR-025).
/// </para>
/// </remarks>
public sealed class SignOutService
{
    /// <summary>The resource key of the plain-language report shown when the restart could not be performed.</summary>
    internal const string RestartFailedMessageKey = "Shell_SignOut.RestartFailed";
    /// <summary>The resource key of the report shown when the session could not be ended, so no restart was attempted.</summary>
    internal const string SessionNotEndedMessageKey = "Shell_SignOut.SessionNotEnded";

    /// <summary>The area a fault is recorded under, so the refusal has its cause beside it.</summary>
    private const string LogModule = "SignOut";

    /// <summary>What is said when the restart could not be performed, so the person is never shown a resource key.</summary>
    private const string FallbackRestartFailed =
        "You have been signed out, but the application could not restart. Close it and open it again to sign in.";

    /// <summary>What is said when the session could not be ended, which is why no restart was attempted.</summary>
    private const string FallbackSessionNotEnded =
        "The application could not end your session, so you are still signed in. Try signing out again.";

    private readonly LaunchSessionService _sessions;
    private readonly PersonIdentityService _person;
    private readonly IMachineFacts _machine;
    private readonly IProcessRestarter _restarter;

    /// <summary>Creates the service over the session, the identity, this computer's facts and the restart seam.</summary>
    /// <param name="sessions">The one place a session is ended (FR-011).</param>
    /// <param name="person">The one writer of the person identity, which this ends (FR-022).</param>
    /// <param name="machine">This computer's facts, which name the machine the session belongs to.</param>
    /// <param name="restarter">The restart seam, so a relaunch that cannot happen is reported rather than assumed.</param>
    public SignOutService(
        LaunchSessionService sessions,
        PersonIdentityService person,
        IMachineFacts machine,
        IProcessRestarter restarter)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _person = person ?? throw new ArgumentNullException(nameof(person));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
        _restarter = restarter ?? throw new ArgumentNullException(nameof(restarter));
    }

    /// <summary>
    /// Ends the session and brings the application back at the sign-in screen.
    /// </summary>
    /// <param name="cancellationToken">Cancels the store write.</param>
    /// <returns>
    /// The outcome, so a refused restart or a session that could not be ended is reported in plain language
    /// instead of leaving the person apparently signed in (FR-011).
    /// </returns>
    public async Task<SignOutOutcome> SignOutAsync(CancellationToken cancellationToken = default)
    {
        var userId = _person.UserId;
        var computerId = _machine.RegisteredComputer?.Id ?? 0;

        try
        {
            // 1. The session ends in the store before anything restarts. This is the whole of FR-011.
            await _sessions.ClearAsync(userId, computerId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The session still stands, so the process must not restart over it. Refusing is the safe direction:
            // the person asked to leave, and nothing has been left half-done behind them.
            AppLog.Error(LogModule, exception, "The session could not be ended, so signing out was refused.");

            return SignOutOutcome.Refused(ResolveMessage(SessionNotEndedMessageKey, FallbackSessionNotEnded));
        }

        // 2. The identity goes too, so nothing about the person survives in this process either. It is cleared
        //    whatever the restart does next, because the person asked to leave and a failed relaunch must not
        //    leave them apparently signed in.
        _person.Clear();

        bool started;

        try
        {
            started = _restarter.Restart();
        }
        catch (Exception exception)
        {
            AppLog.Error(LogModule, exception, "Signing out could not relaunch the application; the person is being told so.");

            return SignOutOutcome.Refused(ResolveMessage(RestartFailedMessageKey, FallbackRestartFailed));
        }

        if (!started)
        {
            AppLog.Error(
                LogModule,
                new InvalidOperationException("The application could not be relaunched."),
                "Signing out could not relaunch the application; the person is being told so.");

            return SignOutOutcome.Refused(ResolveMessage(RestartFailedMessageKey, FallbackRestartFailed));
        }

        AppLog.Info(LogModule, "The session has been ended and a replacement instance is starting; this process is going.");
        AppLifecycleHost.ExitApplication();

        return SignOutOutcome.Success();
    }

    /// <summary>The plain-language report for a refused restart, resolved with a readable fallback (FR-022).</summary>
    /// <returns>The message to show the person.</returns>
    public static string ResolveRestartFailedMessage()
        => ResolveMessage(RestartFailedMessageKey, FallbackRestartFailed);

    /// <summary>
    /// One message resolved through the resource mechanism, with a readable fallback so the person is never
    /// shown a resource key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">What to say when the resource cannot be resolved.</param>
    private static string ResolveMessage(string key, string fallback)
    {
        var localized = key.GetLocalized();

        return string.IsNullOrWhiteSpace(localized) || string.Equals(localized, key, StringComparison.Ordinal)
            ? fallback
            : localized;
    }
}

/// <summary>
/// What a sign-out attempt did (FR-011).
/// </summary>
/// <param name="Succeeded">Whether the session was ended and a replacement instance was started.</param>
/// <param name="Message">
/// The plain-language report shown when the sign-out could not be completed, resolved through the resource
/// mechanism. It is empty on success: nothing needs saying when the application is on its way back to the
/// sign-in screen.
/// </param>
public sealed record SignOutOutcome(bool Succeeded, string Message)
{
    /// <summary>The session was ended and the application is restarting.</summary>
    public static SignOutOutcome Success() => new(true, string.Empty);

    /// <summary>The sign-out did not complete, and the reason is the message.</summary>
    public static SignOutOutcome Refused(string message) => new(false, message);
}
