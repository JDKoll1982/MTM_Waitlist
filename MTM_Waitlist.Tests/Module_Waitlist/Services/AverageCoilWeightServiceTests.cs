using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The average-coil-weight read goes to the receiving store on every call: the demo short-circuit and its
/// sample catalog were retired, so there is no cached or sample path left to exercise (FR-001, FR-014, SC-013).
/// </summary>
[TestClass]
public sealed class AverageCoilWeightServiceTests
{
    [TestMethod]
    public async Task Service_AverageCoilWeightServiceTests_Resolve_RunsQueryAndReturnsWholePounds()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightAsync("MMC0001000");

        Assert.AreEqual(5200m, result);
        Assert.AreEqual(1, helper.QueryCallCount);
        Assert.AreEqual(MySqlDatabaseTarget.MtmReceivingApplication, helper.LastDatabaseTarget);
    }

    [TestMethod]
    public async Task Service_AverageCoilWeightServiceTests_Resolve_UsesTheReceivingStoredProcedureAndNoInlineStatement()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5200m) });
        var service = new AverageCoilWeightService(helper);

        await service.ResolveAverageCoilWeightAsync("MMC0001000");

        // FR-015 / constitution III: the read goes through the receiving store's procedure, and no
        // statement text is executed from the application. This is the regression guard for T088 —
        // reintroducing the inline SELECT would fail both assertions.
        Assert.AreEqual(
            "sp_receiving_history_average_coil_weight",
            helper.LastStoredProcedureName,
            "The average coil weight must be read through the receiving store's procedure.");
        Assert.AreEqual(0, helper.SqlCallCount, "No inline statement text may be executed.");
        Assert.AreEqual("MMC0001000", helper.LastParameters?["@p_part_id"], "The procedure takes the part id.");
    }

    [TestMethod]
    public async Task Service_AverageCoilWeightServiceTests_Resolve_RoundsToWholePounds()
    {
        var helper = new StubMySqlHelperServer(rows: new[] { AverageRow(5199.6m) });
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightAsync("MMC0001000");

        Assert.AreEqual(5200m, result);
    }

    [TestMethod]
    public async Task Service_AverageCoilWeightServiceTests_Resolve_NoRowsReturnsNothing()
    {
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightAsync("MMC0001000");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task Service_AverageCoilWeightServiceTests_Resolve_EmptyPartReturnsNothingWithoutQuery()
    {
        var helper = new StubMySqlHelperServer(rows: Array.Empty<Dictionary<string, object?>>());
        var service = new AverageCoilWeightService(helper);

        var result = await service.ResolveAverageCoilWeightAsync("   ");

        Assert.IsNull(result);
        Assert.AreEqual(0, helper.QueryCallCount);
    }

    private static Dictionary<string, object?> AverageRow(decimal average) => new()
    {
        ["AverageWeight"] = average,
    };

    [TestMethod]
    public void Service_AverageCoilWeightServiceTests_CoilWeightText_WritesTheWeightWithThisMachinesSeparators()
    {
        // FR-024: the figure uses the machine's own grouping, so a machine that groups with a space or a dot writes the
        // weight that way instead of the invariant comma.
        var original = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            StringAssert.StartsWith(CoilWeightText.From(4820m), "4,820", "the invariant comma is not the only grouping.");

            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            StringAssert.StartsWith(CoilWeightText.From(4820m), "4.820", "a culture that groups with a dot must be honoured.");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }

        Assert.AreEqual(string.Empty, CoilWeightText.From(null), "a part with no history has no weight to write.");
    }

    [TestMethod]
    public void Service_AverageCoilWeightServiceTests_CoilWeightText_TakesItsUnitFromTheResourceAndNotFromTheCode()
    {
        // The unit word is translated with the application, so it lives in the resource file and never as a literal in
        // the code that composes the weight (FR-024). The read is the file itself, because a resource lookup answers
        // with its own key in a host that has no application.
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var resources = File.ReadAllText(Path.Combine(root, "Strings", "en-us", "Resources.resw"));
        var composition = File.ReadAllText(
            Path.Combine(root, "MTM_Waitlist.Core", "Helpers", "CoilWeightText.cs"));

        StringAssert.Contains(
            resources,
            $"name=\"{CoilWeightText.UnitKey}\"",
            "the weight's unit is not in the resource file, so it can never be translated");

        StringAssert.Contains(
            composition,
            $"{nameof(CoilWeightText.UnitKey)}.GetLocalized()",
            "the unit is not resolved from the resource where the weight is composed");

        Assert.IsFalse(
            composition.Contains("\"lb\"", StringComparison.Ordinal),
            "the unit is written into the code as a literal, so no translation can change it");
    }

    private sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        /// <summary>Every read, whether it went through a procedure or through inline statement text.</summary>
        public int QueryCallCount { get; private set; }

        /// <summary>Reads issued as raw statement text — must stay 0 (FR-015).</summary>
        public int SqlCallCount { get; private set; }

        public MySqlDatabaseTarget? LastDatabaseTarget { get; private set; }

        public string? LastStoredProcedureName { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            QueryCallCount++;
            LastStoredProcedureName = storedProcedureName;
            LastParameters = parameters;
            LastDatabaseTarget = databaseTarget;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => Task.FromResult(1);


    }
}
