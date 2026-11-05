using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Data.Services;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The live part answer: completions drawn from both systems and the merged sequence list
/// (017-generic-wc-stated-part FR-001, FR-004, FR-005, FR-006).
/// </summary>
[TestClass]
public sealed class GenericRequestPartDataTests
{
    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_CompletionsComeFromBothSystemsAndNameTheirSource()
    {
        var partData = Create(
            script => script.EndsWith("GetPartMatches.sql", StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Ok([Row(("PartNumber", "100-AAA"), ("Description", "Bracket"))])
                : VisualQueryOutcome.Ok([]),
            floorRows: [Row(("PartNumber", "200-BBB"))]);

        var result = await partData.ReadCompletionsAsync("10");

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("100-AAA", result.Items[0].PartNumber);
        Assert.AreEqual("Bracket", result.Items[0].Description);
        Assert.AreEqual(SequenceSource.VisualOwnPart, result.Items[0].Source);
        Assert.AreEqual("200-BBB", result.Items[1].PartNumber);
        Assert.AreEqual(SequenceSource.WipFloor, result.Items[1].Source);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_APartVisualDoesNotKnowIsOfferedWithTheFloorNamed()
    {
        var partData = Create(
            _ => VisualQueryOutcome.Ok([]),
            floorRows: [Row(("PartNumber", "200-BBB"))]);

        var result = await partData.ReadCompletionsAsync("200");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(SequenceSource.WipFloor, result.Items[0].Source);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_AnUnreachableSourceMakesTheCompletionsUnavailable()
    {
        var partData = Create(
            _ => VisualQueryOutcome.Unreachable("the plant system is down"),
            floorRows: [Row(("PartNumber", "200-BBB"))]);

        var result = await partData.ReadCompletionsAsync("200");

        Assert.AreEqual(GenericRequestPresentation.Unavailable, result.Presentation);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_ASequenceTwoSourcesNameAppearsOnceWithBothSources()
    {
        var partData = Create(
            script => script.EndsWith("GetPartSequences.sql", StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Ok(
                [
                    SequenceRow("20", "CNC LATHE", "R", "OwnPart", cancelledOnly: 0),
                ])
                : VisualQueryOutcome.Ok([]),
            floorRows: [Row(("Sequence", "20"))]);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual(1, result.Count);

        var sequence = result.Items[0];
        Assert.AreEqual("20", sequence.Sequence);
        Assert.AreEqual("CNC LATHE", sequence.Description);
        Assert.AreEqual("R", sequence.OperationStatus);
        CollectionAssert.AreEqual(
            new[] { SequenceSource.VisualOwnPart, SequenceSource.WipFloor },
            sequence.Sources.ToArray());
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_ASequenceCarriesTheNextResourceItsWipTagPrints()
    {
        var partData = Create(
            script => script.EndsWith("GetPartSequences.sql", StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Ok(
                [
                    SequenceRow("90", "ASSEMBLY", "R", "OwnPart", cancelledOnly: 0, isLastOperation: 0, nextResource: "Outside Service"),
                    SequenceRow("900", "FINAL", "R", "OwnPart", cancelledOnly: 0, isLastOperation: 1),
                ])
                : VisualQueryOutcome.Ok([]),
            floorRows: []);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual(2, result.Count);
        Assert.IsFalse(result.Items[0].IsLastOperation, "an operation with a successor is not the finished good");
        Assert.AreEqual("Outside Service", result.Items[0].NextResource, "a WIP tag names the resource it goes to next");
        Assert.IsTrue(result.Items[1].IsLastOperation, "the last operation the order still has to run is the finished good");
        Assert.AreEqual(string.Empty, result.Items[1].NextResource, "the last operation has nowhere further to go");
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_ACancelledOnlySequenceIsNotOffered()
    {
        var partData = Create(
            script => script.EndsWith("GetPartSequences.sql", StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Ok(
                [
                    SequenceRow("30", "PRESS", "X", "OwnPart", cancelledOnly: 1),
                ])
                : VisualQueryOutcome.Ok([]),
            floorRows: []);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual(GenericRequestPresentation.Empty, result.Presentation);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_AnOperationThatIsNotANumberIsNotASequence()
    {
        var partData = Create(
            _ => VisualQueryOutcome.Ok([]),
            floorRows: [Row(("Sequence", "N/A"))]);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_SequencesAreOrderedByNumber()
    {
        var partData = Create(
            _ => VisualQueryOutcome.Ok([]),
            floorRows: [Row(("Sequence", "10")), Row(("Sequence", "9"))]);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual("9", result.Items[0].Sequence);
        Assert.AreEqual("10", result.Items[1].Sequence);
    }

    [TestMethod]
    public async Task Audit_GenericRequestPartDataTests_TheSameNumberFromBothVisualRelationsKeepsBothSources()
    {
        var partData = Create(
            script => script.EndsWith("GetPartSequences.sql", StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Ok(
                [
                    SequenceRow("40", "MILL", "U", "OwnPart", cancelledOnly: 0),
                    SequenceRow("40", "MILL", "U", "Requirement", cancelledOnly: 0),
                ])
                : VisualQueryOutcome.Ok([]),
            floorRows: []);

        var result = await partData.ReadSequencesAsync("100-AAA");

        Assert.AreEqual(1, result.Count);
        CollectionAssert.AreEqual(
            new[] { SequenceSource.VisualOwnPart, SequenceSource.VisualRequirement },
            result.Items[0].Sources.ToArray());
    }

    private static GenericRequestPartData Create(
        Func<string, VisualQueryOutcome> visual,
        IReadOnlyList<Dictionary<string, object?>> floorRows)
    {
        var executor = new FakeVisualQueryExecutor((script, _) => visual(script));
        var floor = new WipFloorInventoryService(new StubMySqlHelperServer(floorRows));
        return new GenericRequestPartData(executor, floor);
    }

    internal static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            row[key] = value;
        }

        return row;
    }

    private static Dictionary<string, object?> SequenceRow(
        string sequence,
        string description,
        string status,
        string source,
        int cancelledOnly,
        int isLastOperation = 0,
        string nextResource = "") =>
        Row(
            ("Sequence", sequence),
            ("Description", description),
            ("OperationStatus", status),
            ("IsLastOperation", isLastOperation),
            ("NextResource", nextResource),
            ("SequenceSource", source),
            ("CancelledOnly", cancelledOnly));

    /// <summary>The floor's own store, answering the rows a test configured for every script it runs.</summary>
    internal sealed class StubMySqlHelperServer : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public StubMySqlHelperServer(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Dictionary<string, object?>>>(Array.Empty<Dictionary<string, object?>>());

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rows);

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
