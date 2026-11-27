using System.Globalization;

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

/// <summary>
/// The strict route's three guarantees, each proved against the shipped seam: a provider's failure reaches the
/// caller instead of the neutral answer the lenient route gives (T123), the failure is never retried, and a unit
/// whose second statement fails leaves nothing of its first statement behind.
/// </summary>
/// <remarks>
/// <para>
/// The first two cases need no store: a connection string that names a closed port on this machine is left alone
/// by <see cref="MySqlHostFallback"/>, so the provider's own connect failure is what comes back, in about the time
/// a refused socket takes.
/// </para>
/// <para>
/// The rollback case needs a live <c>mtm_waitlist</c> and is gated on
/// <c>MTM_WAITLIST_TEST_DB_CONNECTION_STRING</c>, reported as skipped rather than passing when it is absent. It
/// writes one defect type under a recognisable name and removes it afterwards, so a broken rollback is proved and
/// then cleaned up rather than left in the store.
/// </para>
/// </remarks>
[TestClass]
public sealed class MySqlHelperServerStrictRouteTests
{
    /// <summary>The store the live case proves rollback against; absent means the case is skipped.</summary>
    private const string TestConnectionStringVariable = "MTM_WAITLIST_TEST_DB_CONNECTION_STRING";

    /// <summary>
    /// The seam resolves the waitlist store from this variable before the options, so a live pass points it at the
    /// store the test variable names and puts it back afterwards.
    /// </summary>
    private const string SeamConnectionStringVariable = "MTM_WAITLIST_DB_CONNECTION_STRING";

    private const string DefectTypesListProcedure = "sp_waitlist_defect_types_get_all";
    private const string DefectTypesInsertProcedure = "sp_waitlist_defect_types_insert";
    private const string DefectTypesDeleteProcedure = "sp_waitlist_defect_types_delete";

    /// <summary>A procedure no store has: calling it is the second statement that cannot run.</summary>
    private const string MissingProcedureName = "sp_mtm_strict_rollback_probe";

    private string? _originalSeamConnectionString;
    private MySqlHelperServer? _liveHelper;
    private string _probeDefectName = string.Empty;

    [TestInitialize]
    public void PointTheSeamAtTheLiveStore()
    {
        var connectionString = Environment.GetEnvironmentVariable(TestConnectionStringVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Only the rollback case needs this; the other two cases read the variable's absence as "no live store".
            return;
        }

        _originalSeamConnectionString = Environment.GetEnvironmentVariable(SeamConnectionStringVariable);
        Environment.SetEnvironmentVariable(SeamConnectionStringVariable, connectionString);

        _liveHelper = new MySqlHelperServer(
            Options.Create(new WaitlistDatabaseOptions { ConnectionString = connectionString }));

        // The column is VARCHAR(100); the prefix makes the row this run's own.
        _probeDefectName = $"ZZ.STRICT.ROLLBACK.{Guid.NewGuid():N}"[..40];
    }

    [TestCleanup]
    public async Task RestoreTheEnvironmentAndRemoveTheProbeRowAsync()
    {
        try
        {
            if (_liveHelper is not null && !string.IsNullOrWhiteSpace(_probeDefectName))
            {
                await DeleteProbeRowAsync(_liveHelper).ConfigureAwait(false);
            }
        }
        finally
        {
            if (_liveHelper is not null)
            {
                Environment.SetEnvironmentVariable(SeamConnectionStringVariable, _originalSeamConnectionString);
            }
        }
    }

    /// <remarks>
    /// The claim is the difference between the two postures: the lenient route turns this same failure into an
    /// empty answer, which is right for a screen and wrong for a caller that has to read the provider's own reason
    /// — a duplicate key, a signalled refusal, a missing procedure.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerStrictRouteTests_AProviderFailureReachesTheStrictCallerInsteadOfANeutralAnswer()
    {
        var helper = CreateUnreachableHelper();

        await Assert.ThrowsExactlyAsync<MySqlException>(
            () => helper.ExecuteStrictAsync(
                new MySqlStrictScopeRequest(MySqlDatabaseTarget.MtmWaitlist),
                (scope, token) => scope.QueryAsync("sp_any_read", new Dictionary<string, object?>(), token)))
            .ConfigureAwait(false);
    }

    /// <remarks>
    /// The same store, the same closed port, the lenient route: an empty answer and no exception, which is the
    /// neutral result the strict route exists to avoid.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerStrictRouteTests_TheLenientRouteOnTheSameStorePutsTheFailureNeutralInstead()
    {
        var helper = CreateUnreachableHelper();

        var rows = await helper.ExecuteStoredProcedureQueryAsync(
            "sp_any_read",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);

        Assert.AreEqual(0, rows.Count, "The lenient route answers the neutral result rather than throwing.");
    }

    /// <remarks>
    /// A three-attempt policy is configured and its delay is counted, so "the strict route does not retry" is
    /// observed rather than assumed: a retried write is how one press becomes two.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerStrictRouteTests_TheStrictRouteIsNeverRetriedEvenUnderARetryPolicy()
    {
        var attempts = 0;
        var helper = CreateUnreachableHelper(() => attempts++);

        await Assert.ThrowsExactlyAsync<MySqlException>(
            () => helper.ExecuteStrictAsync(
                new MySqlStrictScopeRequest(MySqlDatabaseTarget.MtmWaitlist),
                (scope, token) => scope.NonQueryAsync("sp_any_write", new Dictionary<string, object?>(), token)))
            .ConfigureAwait(false);

        Assert.AreEqual(0, attempts, "A strict call is made once; the policy's delay is never reached.");
    }

    /// <remarks>
    /// The unit is the pair of statements the hot-work-center save makes: a write that succeeds and a second call
    /// that cannot run. The rollback is proved by the absence of the first statement's row, read afterwards through
    /// the lenient route on a fresh connection — so what is checked is the store, not a flag.
    /// </remarks>
    [TestMethod]
    public async Task Service_MySqlHelperServerStrictRouteTests_TheSecondStatementFailingRollsTheFirstOneBack()
    {
        if (_liveHelper is null)
        {
            Assert.Inconclusive(
                $"{TestConnectionStringVariable} is not set; skipping the strict-transaction rollback proof.");
        }

        var helper = _liveHelper!;

        await Assert.ThrowsExactlyAsync<MySqlException>(
            () => helper.ExecuteStrictAsync(
                new MySqlStrictScopeRequest(MySqlDatabaseTarget.MtmWaitlist, UseTransaction: true),
                async (scope, token) =>
                {
                    _ = await scope.NonQueryAsync(
                        DefectTypesInsertProcedure,
                        new Dictionary<string, object?>
                        {
                            ["p_defect_name"] = _probeDefectName,
                            ["p_description"] = "Strict-transaction rollback probe.",
                            ["p_sort_order"] = 9999,
                            ["p_created_by_user_id"] = null,
                        },
                        token).ConfigureAwait(false);

                    _ = await scope.NonQueryAsync(
                        MissingProcedureName,
                        new Dictionary<string, object?>(),
                        token).ConfigureAwait(false);

                    return 0;
                }))
            .ConfigureAwait(false);

        // Read back through the strict route on a fresh connection, so an unreachable store fails the case rather
        // than answering the empty result the lenient route would.
        var rows = await helper.ExecuteStrictAsync(
            new MySqlStrictScopeRequest(MySqlDatabaseTarget.MtmWaitlist),
            (scope, token) => scope.QueryAsync(DefectTypesListProcedure, new Dictionary<string, object?>(), token))
            .ConfigureAwait(false);

        Assert.IsFalse(
            rows.Any(row => string.Equals(ReadString(row, "defect_name"), _probeDefectName, StringComparison.Ordinal)),
            "The first statement is undone when the second one fails, so the store holds nothing from the unit.");
    }

    /// <summary>
    /// The seam pointed at a closed port on this machine, which <see cref="MySqlHostFallback"/> leaves alone
    /// because it is already this machine.
    /// </summary>
    /// <param name="onRetryDelay">Called whenever the lenient route's policy waits before another attempt.</param>
    private static MySqlHelperServer CreateUnreachableHelper(Action? onRetryDelay = null) =>
        new(
            waitlistDatabaseOptions: Options.Create(
                new WaitlistDatabaseOptions { ConnectionString = "Server=127.0.0.1;Port=1;Database=mtm_waitlist;" }),
            retryPolicy: new InternalStoreRetryPolicy(
                delay: (_, _) =>
                {
                    onRetryDelay?.Invoke();
                    return Task.CompletedTask;
                },
                maxAttempts: InternalStoreRetryPolicy.DefaultAttemptLimit));

    /// <summary>
    /// Removes the probe row if a failed rollback left it behind. A missing row is the passing case, so a failure to
    /// read or delete here must not replace the test's own verdict.
    /// </summary>
    private async Task DeleteProbeRowAsync(MySqlHelperServer helper)
    {
        IReadOnlyList<Dictionary<string, object?>> rows;
        try
        {
            rows = await helper.ExecuteStoredProcedureQueryAsync(
                DefectTypesListProcedure,
                new Dictionary<string, object?>(),
                MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return;
        }

        foreach (var row in rows.Where(row => string.Equals(ReadString(row, "defect_name"), _probeDefectName, StringComparison.Ordinal)))
        {
            if (!row.TryGetValue("id", out var id) || id is null || id is DBNull)
            {
                continue;
            }

            await helper.ExecuteStoredProcedureNonQueryAsync(
                DefectTypesDeleteProcedure,
                new Dictionary<string, object?> { ["p_id"] = Convert.ToInt64(id, CultureInfo.InvariantCulture) },
                MySqlDatabaseTarget.MtmWaitlist).ConfigureAwait(false);
        }
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> row, string columnName) =>
        row.TryGetValue(columnName, out var value) && value is not null && value is not DBNull
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            : string.Empty;
}
