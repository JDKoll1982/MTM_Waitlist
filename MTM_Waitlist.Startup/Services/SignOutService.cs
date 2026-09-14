using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Signs out by <b>ending the session</b> (FR-034).
/// <para>
/// The order is the whole point and is the reason this is a service rather than four lines on the shell:
/// the remembered-credential and session keys are cleared <b>first</b>, so the restarted instance cannot read
/// them and restore the session; only then is a fresh copy launched and this process exited. Clearing the
/// displayed name while the session persists is not signing out, which is what FR-034 rules out in as many
/// words.
/// </para>
/// <para>
/// A relaunch that cannot be performed is reported rather than swallowed (FR-026). The keys stay cleared in
/// that case too: the person asked to end the session, and a refused restart must not leave them apparently
/// signed in.
/// </para>
/// </summary>
public sealed class SignOutService : ISignOutService
{
    /// <summary>The resource key of the plain-language report shown when the restart could not be performed.</summary>
    public const string RestartFailedMessageKey = "Shell_SignOut.RestartFailed";

    private readonly ILocalSettingsService _localSettingsService;
    private readonly IAppProcessRestarter _restarter;
    private readonly IAppLifecycleService _lifecycle;

    public SignOutService(
        ILocalSettingsService localSettingsService,
        IAppProcessRestarter restarter,
        IAppLifecycleService lifecycle)
    {
        ArgumentNullException.ThrowIfNull(localSettingsService);
        ArgumentNullException.ThrowIfNull(restarter);
        ArgumentNullException.ThrowIfNull(lifecycle);

        _localSettingsService = localSettingsService;
        _restarter = restarter;
        _lifecycle = lifecycle;
    }

    /// <inheritdoc />
    public async Task<SignOutResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartupDebugLog.Info("SignOutService", "Sign out requested; clearing the remembered credential and session keys.");

        // 1. Nothing the restarted instance reads may survive: this is what makes it ask for credentials
        //    instead of restoring the session (FR-034).
        foreach (var key in SignInSessionKeys.All)
        {
            await _localSettingsService.ResetSettingAsync(key, cancellationToken).ConfigureAwait(false);
        }

        // 2. Relaunch, then leave. A relaunch that cannot be performed is reported, never assumed (FR-026).
        bool started;
        try
        {
            started = _restarter.Restart();
        }
        catch (Exception ex)
        {
            StartupDebugLog.Error("SignOutService", ex, "Signing out could not relaunch the application; the person is being told so.");
            return SignOutResult.Refused(ResolveRestartFailedMessage());
        }

        if (!started)
        {
            StartupDebugLog.Error(
                "SignOutService",
                new InvalidOperationException("The application could not be relaunched."),
                "Signing out could not relaunch the application; the person is being told so.");
            return SignOutResult.Refused(ResolveRestartFailedMessage());
        }

        StartupDebugLog.Info("SignOutService", "The replacement instance is running; exiting the signed-in session.");
        _lifecycle.Exit();
        return SignOutResult.Success();
    }

    /// <summary>
    /// The plain-language report for a refused restart, resolved through the resource mechanism with a readable
    /// fallback so the person is never shown a bare resource key (FR-022, FR-026).
    /// </summary>
    public static string ResolveRestartFailedMessage()
    {
        const string fallback =
            "You have been signed out, but the application could not restart. Close it and open it again to sign in.";
        var localized = RestartFailedMessageKey.GetLocalized();
        return string.IsNullOrWhiteSpace(localized)
            || string.Equals(localized, RestartFailedMessageKey, StringComparison.Ordinal)
                ? fallback
                : localized;
    }
}
