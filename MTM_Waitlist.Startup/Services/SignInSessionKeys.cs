namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The local keys the sign-in path reads and writes, in one place so signing out cannot clear a set of keys
/// that has drifted from the set signing in uses (FR-034).
/// <para>
/// Four of them are the remembered credential — asking to be remembered writes the username and password and
/// sets the flag, and a restarted instance with those still present restores the session instead of asking for
/// credentials. The fifth pair is the session token and its expiry, which the sign-in path persists only after
/// the computer gate has passed.
/// </para>
/// </summary>
public static class SignInSessionKeys
{
    /// <summary>Whether the person asked to be remembered on this computer.</summary>
    public const string RememberPassword = "Login.RememberPassword";

    /// <summary>The remembered username.</summary>
    public const string RememberedUsername = "Login.RememberedUsername";

    /// <summary>The remembered password.</summary>
    public const string RememberedPassword = "Login.RememberedPassword";

    /// <summary>The local session token.</summary>
    public const string SessionToken = "Startup.Session.Token";

    /// <summary>When the local session token expires.</summary>
    public const string SessionExpiresUtc = "Startup.Session.ExpiresUtc";

    /// <summary>
    /// Every key a restarted instance would read to restore the previous session, in the order signing out
    /// clears them. Clearing these is what makes the restarted application ask for credentials.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        RememberPassword,
        RememberedUsername,
        RememberedPassword,
        SessionToken,
        SessionExpiresUtc,
    };
}
