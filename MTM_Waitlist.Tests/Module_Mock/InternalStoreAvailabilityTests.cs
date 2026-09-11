using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// FR-021's internal-store availability gate: a failing internal store is retried a bounded number of times,
/// then reported as a per-screen <c>Unavailable</c> state that a manual retry can clear — never substituted
/// with sample rows, never cached (FR-001/FR-027), and never turned into a persistent banner.
/// </summary>
/// <remarks>
/// The store used here is pointed at a closed local port, so the read fails the way a real outage fails
/// (connection refused) without needing a database. The retry delays are replaced with a no-op delegate so the
/// attempt count is asserted without waiting seven seconds; the delays themselves are asserted separately.
/// </remarks>
[TestClass]
public sealed class InternalStoreAvailabilityTests
{
    /// <summary>
    /// A connection string that cannot succeed: nothing listens on port 1, so the connection is refused
    /// immediately rather than timing out.
    /// </summary>
    private const string UnreachableConnectionString =
        "Server=127.0.0.1;Port=1;Database=mtm_waitlist;User ID=nobody;Password=nobody;ConnectionTimeout=1;";

    private static readonly string[] s_connectionEnvironmentVariables =
    [
        "MTM_WAITLIST_DB_CONNECTION_STRING",
        "MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING",
        "MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING",
        "MTM_WIP_APPLICATION_DB_CONNECTION_STRING",
        "MTM_MOCK_DB_CONNECTION_STRING",
    ];

    private readonly Dictionary<string, string?> _originalEnvironment = new(StringComparer.OrdinalIgnoreCase);

    [TestInitialize]
    public void TestInitialize()
    {
        foreach (var name in s_connectionEnvironmentVariables)
        {
            _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (var entry in _originalEnvironment)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }

    [TestMethod]
    public void DefaultPolicy_AllowsThreeAttemptsWithOneTwoAndFourSecondDelays()
    {
        var policy = InternalStoreRetryPolicy.Default;

        Assert.AreEqual(3, policy.MaxAttempts, "FR-021 allows up to three attempts.");
        CollectionAssert.AreEqual(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) },
            policy.Delays.ToArray(),
            "FR-021 fixes the delays at approximately 1 s, 2 s, and 4 s.");
        Assert.AreEqual(TimeSpan.FromSeconds(4), policy.NextRetryDelay);
    }

    [TestMethod]
    public async Task WhenTheStoreNeverAnswers_RetriesThreeTimesThenReportsUnavailable()
    {
        var tracker = new StoreAvailabilityTracker();
        var helper = CreateUnreachableHelper(tracker);

        var rows = await helper.ExecuteStoredProcedureQueryAsync(
            "sp_waitlist_request_types_get",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(0, rows.Count, "A failed store read returns no rows — never sample data (FR-001).");

        var availability = tracker.Get(MySqlDatabaseTarget.MtmWaitlist);
        Assert.AreEqual(InternalStoreStatus.Unavailable, availability.Status);
        Assert.AreEqual(2, availability.RetryCount, "Three attempts are two retries.");
        Assert.IsNotNull(availability.LastAttemptUtc);
        Assert.IsNotNull(availability.NextRetryUtc, "The state tells the operator when a retry is worth trying.");
        StringAssert.Contains(availability.Message, "mtm_waitlist");
        Assert.AreEqual("mtm_waitlist", availability.StoreName);
    }

    [TestMethod]
    public async Task WhenTheStoreNeverAnswers_EveryAttemptIsMade()
    {
        // The attempt count is observable through the policy: a policy with a single allowed attempt must
        // report zero retries, while the default reports two — proving the loop is bounded by the policy.
        var tracker = new StoreAvailabilityTracker();
        var singleAttempt = new InternalStoreRetryPolicy(
            delay: (_, _) => Task.CompletedTask,
            maxAttempts: 1);
        var helper = new MySqlHelperServer(
            startupDatabaseOptions: Options.Create(new StartupDatabaseOptions { ConnectionString = UnreachableConnectionString }),
            storeAvailability: tracker,
            retryPolicy: singleAttempt);

        _ = await helper.ExecuteSqlQueryAsync(
            "SELECT 1", // never executed: the connection is refused first
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(
            0,
            tracker.Get(MySqlDatabaseTarget.MtmWaitlist).RetryCount,
            "A single-attempt policy makes exactly one attempt.");
    }

    [TestMethod]
    public async Task AnUnconfiguredStore_IsNotReportedAsUnavailable()
    {
        var tracker = new StoreAvailabilityTracker();
        var helper = new MySqlHelperServer(
            startupDatabaseOptions: Options.Create(new StartupDatabaseOptions { ConnectionString = string.Empty }),
            storeAvailability: tracker,
            retryPolicy: CreateInstantPolicy());

        var rows = await helper.ExecuteStoredProcedureQueryAsync(
            "sp_anything",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist);

        Assert.AreEqual(0, rows.Count);
        Assert.AreEqual(
            InternalStoreStatus.Unknown,
            tracker.Get(MySqlDatabaseTarget.MtmWaitlist).Status,
            "A store with no connection configured is an unconfigured deployment, not an outage.");
    }

    [TestMethod]
    public async Task CallerCancellation_IsPropagatedAndNotReportedAsUnavailable()
    {
        var tracker = new StoreAvailabilityTracker();
        var helper = CreateUnreachableHelper(tracker);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => helper.ExecuteStoredProcedureQueryAsync(
            "sp_anything",
            new Dictionary<string, object?>(),
            MySqlDatabaseTarget.MtmWaitlist,
            cts.Token));

        Assert.AreEqual(
            InternalStoreStatus.Unknown,
            tracker.Get(MySqlDatabaseTarget.MtmWaitlist).Status,
            "A cancelled read is not an unavailable store.");
    }

    [TestMethod]
    public void Tracker_RaisesAChangeOnlyWhenTheStateChanges()
    {
        var tracker = new StoreAvailabilityTracker();
        var notifications = 0;
        tracker.Changed += (_, _) => notifications++;

        tracker.RecordAvailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 1);
        tracker.RecordAvailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 1);
        Assert.AreEqual(1, notifications, "A healthy read is reported once, so a busy screen is not spammed.");

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "down");
        Assert.AreEqual(2, notifications);

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "still down");
        Assert.AreEqual(3, notifications, "A second failed manual retry must refresh what the screen shows.");

        tracker.RecordAvailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 1);
        Assert.AreEqual(4, notifications);
    }

    [TestMethod]
    public void PerScreenState_BecomesVisibleOnFailureAndClearsOnRecovery()
    {
        var tracker = new StoreAvailabilityTracker();
        using var state = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => Task.CompletedTask,
            tracker);

        Assert.IsFalse(state.IsUnavailable, "Nothing is shown before the store has been read.");

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "down");

        Assert.IsTrue(state.IsUnavailable);
        Assert.AreEqual("mtm_waitlist", state.StoreName);
        Assert.AreEqual(2, state.RetryCount);
        Assert.IsNotNull(state.LastAttemptUtc);
        Assert.IsNotNull(state.NextRetryUtc);
        Assert.AreEqual("down", state.Message);

        tracker.RecordAvailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 1);

        Assert.IsFalse(state.IsUnavailable, "Recovery clears the screen's state without any user action.");
    }

    [TestMethod]
    public async Task ManualRetry_ReRunsTheScreensLoadAndClearsTheStateOnceTheStoreReturns()
    {
        var tracker = new StoreAvailabilityTracker();
        var loadCount = 0;
        using var state = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ =>
            {
                loadCount++;
                // The retry reaches a store that now answers, so the seam records it available.
                tracker.RecordAvailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 1);
                return Task.CompletedTask;
            },
            tracker);

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "down");
        Assert.IsTrue(state.IsUnavailable);

        await state.RetryCommand.ExecuteAsync(null);

        Assert.AreEqual(1, loadCount, "The manual retry re-runs exactly this screen's load.");
        Assert.IsFalse(state.IsUnavailable);
    }

    [TestMethod]
    public async Task ManualRetry_ThatFailsAgain_LeavesTheStateVisibleWithFreshDetail()
    {
        var tracker = new StoreAvailabilityTracker();
        using var state = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => Task.CompletedTask,
            tracker);

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "first failure");
        var firstAttempt = state.LastAttemptUtc;

        await Task.Delay(10);
        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "second failure");

        Assert.IsTrue(state.IsUnavailable);
        Assert.AreEqual("second failure", state.Message);
        Assert.AreNotEqual(firstAttempt, state.LastAttemptUtc, "The screen shows the latest attempt, not a stale one.");
    }

    [TestMethod]
    public async Task ManualRetry_WhenTheLoadThrows_DoesNotEscapeFromTheCommand()
    {
        var tracker = new StoreAvailabilityTracker();
        using var state = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => throw new InvalidOperationException("load failed"),
            tracker);

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "down");

        // A retry failing again must not surface as an unhandled exception on top of the state being shown.
        await state.RetryCommand.ExecuteAsync(null);

        Assert.IsTrue(state.IsUnavailable);
        Assert.IsFalse(state.IsRetrying);
    }

    [TestMethod]
    public void PerScreenState_UnsubscribesFromTheTrackerOnDispose()
    {
        var tracker = new StoreAvailabilityTracker();
        var state = new InternalStoreUnavailableState(
            MySqlDatabaseTarget.MtmWaitlist,
            _ => Task.CompletedTask,
            tracker);
        state.Dispose();

        tracker.RecordUnavailable(MySqlDatabaseTarget.MtmWaitlist, DateTime.UtcNow, 3, DateTime.UtcNow, "down");

        Assert.IsFalse(state.IsUnavailable, "A disposed screen must not keep receiving store notifications.");
    }

    private static MySqlHelperServer CreateUnreachableHelper(IStoreAvailabilityTracker tracker) =>
        new(
            startupDatabaseOptions: Options.Create(new StartupDatabaseOptions { ConnectionString = UnreachableConnectionString }),
            storeAvailability: tracker,
            retryPolicy: CreateInstantPolicy());

    /// <summary>The shipped delays with a no-op wait, so the attempt count is asserted without waiting.</summary>
    private static InternalStoreRetryPolicy CreateInstantPolicy() =>
        new(delay: (_, _) => Task.CompletedTask);
}
