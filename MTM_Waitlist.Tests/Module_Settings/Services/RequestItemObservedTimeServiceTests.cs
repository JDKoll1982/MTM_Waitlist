using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// US5 (FR-019, FR-024, FR-026, SC-008). The observed average is one stored procedure over
/// <c>accepted_utc</c> → <c>completed_utc</c>, and the rules that make it trustworthy live in that procedure's
/// text: <b>completed requests only</b>, a request missing either endpoint excluded, a released-then-completed
/// request contributing <b>exactly once</b> through its final pair, <b>no time window</b>, and an Item with no
/// completed request yielding <b>no value</b> rather than a fabricated zero.
/// </summary>
/// <remarks>
/// The procedure is asserted from its own artifact rather than described, because no check in this suite can
/// reach a live MySQL host — and a rule that is only written in a sentence is a rule that can drift.
/// </remarks>
[TestClass]
public sealed class RequestItemObservedTimeServiceTests
{
    private const string Item = "pickup-coil";
    private const string UnobservedItem = "deliver-coil";

    // ── The procedure's rules, read from the artifact that ships ────────────────────────────────────────

    [TestMethod]
    public void TheObservedAverage_CountsCompletedRequestsOnly()
    {
        StringAssert.Contains(ProcedureBody(), "status = 'Completed'");
    }

    [TestMethod]
    public void TheObservedAverage_MeasuresFromAcceptanceToCompletion()
    {
        var body = ProcedureBody();

        StringAssert.Contains(body, "accepted_utc");
        StringAssert.Contains(body, "completed_utc");
        StringAssert.Contains(
            body,
            "TIMESTAMPDIFF(SECOND, q.accepted_utc, q.completed_utc)",
            "The endpoints of the measurement are acceptance and completion (FR-019).");
    }

    [TestMethod]
    public void TheObservedAverage_ExcludesARequestMissingEitherEndpoint()
    {
        var body = ProcedureBody();

        StringAssert.Contains(body, "accepted_utc IS NOT NULL");
        StringAssert.Contains(body, "completed_utc IS NOT NULL");
    }

    [TestMethod]
    public void TheObservedAverage_CountsAReleasedThenCompletedRequestExactlyOnce()
    {
        var body = ProcedureBody();

        Assert.IsFalse(
            body.Contains("released_utc", StringComparison.OrdinalIgnoreCase),
            "Reading released_utc as a second interval is how such a request would be counted twice (FR-019).");

        Assert.AreEqual(
            1,
            Regex.Matches(body, "TIMESTAMPDIFF").Count,
            "One interval per row: the final accepted → completed pair.");

        StringAssert.Contains(body, "COUNT(*)", "One row is one request, so a request cannot be counted twice.");
    }

    [TestMethod]
    public void TheObservedAverage_AppliesNoTimeWindow()
    {
        var body = ProcedureBody();

        foreach (var windowed in new[] { "BETWEEN", "DATE_SUB", "INTERVAL", "CURDATE", "NOW()", "UTC_TIMESTAMP" })
        {
            Assert.IsFalse(
                body.Contains(windowed, StringComparison.OrdinalIgnoreCase),
                $"The average covers all of an Item's completed requests, so it carries no '{windowed}' window.");
        }
    }

    // ── The composition the screen consumes ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetObservedTimesAsync_ReadsTheObservedAverageProcedure()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();

        var service = new RequestItemObservedTimeService(
            helper,
            new RequestItemCatalogService(),
            new FakeRequestItemConfigurationService());

        await service.GetObservedTimesAsync();

        Assert.AreEqual("sp_waitlist_request_item_observed_average_get", helper.ExecutedQueries[0].Sql);
    }

    [TestMethod]
    public async Task GetObservedTimesAsync_ShowsTheConfiguredMinutesBesideTheObservedAverage()
    {
        var configurations = new FakeRequestItemConfigurationService();
        configurations.Configure(Item, 30);

        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(Item, completedCount: 4, averageSeconds: 80 * 60));

        var pairs = await Build(helper, configurations).GetObservedTimesAsync();
        var pair = pairs.Single(candidate => candidate.Item == Item);

        Assert.AreEqual(TimeSpan.FromMinutes(30), pair.ConfiguredMinutes, "Configured: how long it is allowed to take.");
        Assert.AreEqual(TimeSpan.FromMinutes(80), pair.ObservedAverage, "Observed: how long it has actually taken.");
        Assert.AreEqual(4, pair.CompletedRequestCount);
        Assert.AreNotEqual(
            pair.ConfiguredMinutes,
            pair.ObservedAverage,
            "They are two values with two meanings, and a screen that showed one twice would prove nothing.");
    }

    [TestMethod]
    public async Task GetObservedTimesAsync_ForAnItemWithNoCompletedRequest_YieldsNoValueRatherThanAZero()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(Item, completedCount: 4, averageSeconds: 80 * 60));

        var pairs = await Build(helper, new FakeRequestItemConfigurationService()).GetObservedTimesAsync();
        var unobserved = pairs.Single(candidate => candidate.Item == UnobservedItem);

        Assert.IsNull(unobserved.ObservedAverage, "Absence, never a fabricated zero (FR-026).");
        Assert.IsFalse(unobserved.HasObservedAverage);
        Assert.AreEqual(0, unobserved.CompletedRequestCount);
    }

    [TestMethod]
    public async Task GetObservedTimesAsync_IsARead_ItNeverWritesTheObservedValueBack()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueQueryResult(Row(Item, completedCount: 4, averageSeconds: 80 * 60));

        await Build(helper, new FakeRequestItemConfigurationService()).GetObservedTimesAsync();

        Assert.AreEqual(
            0,
            helper.ExecutedNonQueries.Count,
            "The observed value is display data; the only writer of a configured allotment is the minutes editor (FR-018).");
    }

    [TestMethod]
    public async Task GetObservedTimesAsync_ReturnsOneRowPerCataloguedItemInCatalogOrder()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();

        var pairs = await Build(helper, new FakeRequestItemConfigurationService()).GetObservedTimesAsync();

        CollectionAssert.AreEqual(
            RequestItemCatalog.Items.Select(item => item.Id).ToArray(),
            pairs.Select(pair => pair.Item).ToArray());
    }

    [TestMethod]
    public async Task GetObservedTimesAsync_NamesEachRowThroughTheResourceMechanism()
    {
        var helper = new FakeMySqlHelperServer();
        helper.EnqueueEmptyQueryResult();

        var pairs = await Build(helper, new FakeRequestItemConfigurationService()).GetObservedTimesAsync();

        foreach (var pair in pairs)
        {
            Assert.IsFalse(
                string.IsNullOrWhiteSpace(pair.DisplayName),
                $"'{pair.Item}' shows a name, never a blank row (FR-022).");
            Assert.IsFalse(
                pair.DisplayName.StartsWith("RequestItem.", StringComparison.Ordinal),
                "A missing resource entry falls back to readable text, never to a bare resource key.");
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────────────────

    private static RequestItemObservedTimeService Build(
        FakeMySqlHelperServer helper,
        FakeRequestItemConfigurationService configurations) =>
        new(helper, new RequestItemCatalogService(), configurations);

    private static Dictionary<string, object?> Row(string item, int completedCount, int averageSeconds) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["item"] = item,
            ["completed_request_count"] = completedCount,
            ["average_seconds"] = averageSeconds,
        };

    /// <summary>The procedure's statement body, with its explanatory comment lines removed.</summary>
    private static string ProcedureBody()
    {
        var path = Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(),
            "Database",
            "StoredProcedures",
            "sp_waitlist_request_item_observed_average_get",
            "create.sql");

        Assert.IsTrue(File.Exists(path), $"The observed-average procedure was not found at '{path}'.");

        return string.Join(
            Environment.NewLine,
            File.ReadAllLines(path).Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));
    }
}
