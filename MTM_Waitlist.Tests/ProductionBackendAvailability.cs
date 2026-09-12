using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests;

/// <summary>
/// The precondition shared by the tests whose premise is that the production backend is <b>unreachable</b>.
/// </summary>
/// <remarks>
/// <para>
/// A handful of unit tests assert the <i>failure</i> path of the real <c>MySqlHelperServer</c> — that a
/// submission reports a persistence failure, that a save reports "no rows" — by constructing the production
/// helper server and relying on there being no store behind it. That holds on a developer workstation and
/// fails on the cache host, where <c>MTM_WAITLIST_DB_CONNECTION_STRING</c> is set for the service's benefit
/// (T147, and the service's own startup gate).
/// </para>
/// <para>
/// The failure is not merely a red test: <b>the write succeeds</b>, so the assertion fails <i>and</i> the run
/// leaves rows in the operational <c>mtm_waitlist</c> store. These tests therefore report inconclusive when a
/// live connection is configured, the same way the live-database integration suites report inconclusive when
/// one is not (see <c>MockMirrorRefreshWriterIntegrationTests</c> and its siblings).
/// </para>
/// <para>
/// <b>What this does not cover.</b> The helper server also falls back to its configured
/// <c>StartupDatabaseOptions.ConnectionString</c> when both variables are unset, so a machine whose
/// <c>appsettings.json</c> points at a reachable host without any environment variable would still trip the
/// tests. Detecting that would mean resolving the connection the way production does, which is the logic the
/// tests are meant to observe rather than duplicate.
/// </para>
/// </remarks>
internal static class ProductionBackendAvailability
{
    /// <summary>
    /// The variables <c>MySqlHelperServer</c> consults for the <c>mtm_waitlist</c> target, in its own order.
    /// </summary>
    private static readonly string[] s_waitlistConnectionStringVariables =
    [
        "MTM_WAITLIST_DB_CONNECTION_STRING",
        "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING",
    ];

    /// <summary>
    /// Reports the calling test inconclusive when a live <c>mtm_waitlist</c> connection is configured.
    /// </summary>
    /// <remarks>
    /// Call this <b>before</b> touching the service: the point is to avoid the write, not to reinterpret its
    /// result afterwards.
    /// </remarks>
    public static void SkipWhenConfigured()
    {
        var configured = s_waitlistConnectionStringVariables
            .Any(variable => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)));

        if (!configured)
        {
            return;
        }

        Assert.Inconclusive(
            "A live mtm_waitlist connection is configured in this environment, so the production backend is "
            + "reachable and this test's 'backend unavailable' premise cannot hold. Skipping rather than "
            + "asserting a failure that cannot occur and writing to the operational store.");
    }
}
