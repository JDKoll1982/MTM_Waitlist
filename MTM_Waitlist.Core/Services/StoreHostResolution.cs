namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// The one place a connection string is handed to the driver, and the only place a development workstation may
/// point it somewhere other than where it was configured.
/// </summary>
/// <remarks>
/// <para>
/// <b>What ships, and what does not.</b> The repository is developed on a home workstation that is off the plant
/// network and carries its own MySQL server, so a build made there has to be able to fall back to this machine or
/// nothing can be run at the weekend. A build made for the plant must not: the plant has no second server to fall
/// back to, and a silent substitution would let a save land in a copy the plant never reads.
/// </para>
/// <para>
/// So the substitution is <b>compiled out</b> rather than switched off. <c>LOCAL_STORE_FALLBACK</c> is defined for
/// every configuration except <c>Release</c>, and both publish profiles force <c>Release</c>, so the published
/// application contains no substitution and no <c>localhost</c> default at all — not a dormant branch, not a
/// variable to be flipped on a plant machine, but code that is not in the binary. A Release build always talks to
/// the server it was configured with, and fails loudly when that server does not answer.
/// </para>
/// <para>
/// <b>Why the seam exists instead of calling the fallback directly.</b> There were three call sites in two
/// assemblies, and each had its own idea of when to substitute. Routing them through one method means the
/// published behaviour is decided once, in one file, and a reviewer can see it without hunting.
/// </para>
/// </remarks>
public static class StoreHostResolution
{
    /// <summary>
    /// Returns the connection string to open. In a Release build this is the string it was given, unchanged.
    /// </summary>
    /// <param name="connectionString">The connection string resolved from configuration.</param>
    /// <returns>The string to open.</returns>
    public static string? Apply(string? connectionString)
    {
#if LOCAL_STORE_FALLBACK
        // Development builds only: the configured server when it answers, otherwise this machine when that
        // answers, otherwise the configured string again so the caller's own failure path reports the outage.
        return MySqlHostFallback.Apply(connectionString);
#else
        return connectionString;
#endif
    }
}
