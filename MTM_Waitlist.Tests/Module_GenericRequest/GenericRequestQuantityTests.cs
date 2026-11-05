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
/// The chosen sequence's operation state decides who owns the quantity, and the three answers stay distinguishable
/// (017-generic-wc-stated-part FR-009, FR-010, FR-011, FR-013, FR-014, FR-017).
/// </summary>
[TestClass]
public sealed class GenericRequestQuantityTests
{
    private const string StateScript = "GetPartSequenceState.sql";
    private const string WarehouseScript = "GetPartWarehouseQuantities.sql";

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_AnOpenOperationIsAnsweredByTheFloor()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 12), ("LocationCount", 2))]);
        var partData = Create(State("R", isLastOperation: 0), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(GenericRequestQuantityKind.OnHand, reading.Kind);
        Assert.AreEqual(12m, reading.Figure);
        Assert.AreEqual(SequenceSource.WipFloor, reading.AnsweredBy);
        Assert.AreEqual(GenericRequestPartStage.Wip, reading.Stage);
        Assert.AreEqual(1, floor.QueryCount, "an open sequence asks the floor exactly once");
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_AClosedOperationIsAnsweredByVisualByPartNumber()
    {
        var floor = new RecordingFloor([]);
        var partData = Create(State("C", isLastOperation: 1), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(GenericRequestQuantityKind.OnHand, reading.Kind);
        Assert.AreEqual(30m, reading.Figure);
        Assert.AreEqual(SequenceSource.VisualOwnPart, reading.AnsweredBy);
        Assert.AreEqual(GenericRequestPartStage.FinishedGood, reading.Stage);
        Assert.AreEqual(0, floor.QueryCount, "a closed sequence never asks the floor");
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_AnOperationHandingThePartToOutsideServiceIsWorkInProgress()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 12), ("LocationCount", 2))]);
        var partData = Create(State("R", isLastOperation: 0, nextResource: "Outside Service"), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "90");

        Assert.AreEqual(GenericRequestQuantityKind.OnHand, reading.Kind);
        Assert.AreEqual(12m, reading.Figure, "an outside-service operation still has material on the floor");
        Assert.AreEqual(GenericRequestPartStage.Wip, reading.Stage, "outside service is a next resource, not a type");
        Assert.AreEqual(1, floor.QueryCount, "an open sequence asks the floor exactly once");
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_TheLastOpenOperationIsTheFinishedGoodEvenWhileStillOnTheFloor()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 5), ("LocationCount", 1))]);
        var partData = Create(State("R", isLastOperation: 1, nextResource: string.Empty), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "900");

        Assert.AreEqual(5m, reading.Figure);
        Assert.AreEqual(SequenceSource.WipFloor, reading.AnsweredBy, "the state still decides who owns the quantity");
        Assert.AreEqual(GenericRequestPartStage.FinishedGood, reading.Stage, "the last operation prints the FG tag");
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_AnUnrecordedStateAsksTheFloorFirst()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 7), ("LocationCount", 1))]);
        var partData = Create(State(status: null, isLastOperation: 0), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(7m, reading.Figure);
        Assert.AreEqual(SequenceSource.WipFloor, reading.AnsweredBy);
        Assert.AreEqual(GenericRequestPartStage.Unknown, reading.Stage);
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_TheFloorHoldingNoneFallsBackToVisualByPartNumber()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 0), ("LocationCount", 0))]);
        var partData = Create(State(status: null, isLastOperation: 0), floor);

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(30m, reading.Figure);
        Assert.AreEqual(SequenceSource.VisualOwnPart, reading.AnsweredBy);
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_ZeroOnHandIsAFigureRatherThanAnAbsence()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 0), ("LocationCount", 0))]);
        var partData = Create(State("U", isLastOperation: 0), floor, visualFigure: 0m);

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(GenericRequestQuantityKind.OnHand, reading.Kind);
        Assert.AreEqual(0m, reading.Figure);
        Assert.IsTrue(reading.IsAnswered);
    }

    [TestMethod]
    public async Task Audit_GenericRequestQuantityTests_AnUnreadableStateIsUnavailableAndReportsNoFigure()
    {
        var floor = new RecordingFloor([Row(("OperationQuantity", 12), ("LocationCount", 2))]);
        var executor = new FakeVisualQueryExecutor(
            (script, _) => script.EndsWith(StateScript, StringComparison.OrdinalIgnoreCase)
                ? VisualQueryOutcome.Unreachable("the plant system is down")
                : VisualQueryOutcome.Ok([Row(("WarehouseQuantity", 30))]));

        var partData = new GenericRequestPartData(executor, new WipFloorInventoryService(floor));

        var reading = await partData.ReadQuantityAsync("100-AAA", "20");

        Assert.AreEqual(GenericRequestPresentation.Unavailable, reading.Presentation);
        Assert.IsNull(reading.Figure);
        Assert.IsFalse(reading.IsAnswered);
    }

    private static Func<string, IReadOnlyDictionary<string, object?>, VisualQueryOutcome> State(
        string? status,
        int isLastOperation,
        string nextResource = "") =>
        (script, _) => script.EndsWith(StateScript, StringComparison.OrdinalIgnoreCase)
            ? VisualQueryOutcome.Ok(
            [
                Row(("OperationStatus", status), ("IsLastOperation", isLastOperation), ("NextResource", nextResource)),
            ])
            : VisualQueryOutcome.Ok([]);

    private static GenericRequestPartData Create(
        Func<string, IReadOnlyDictionary<string, object?>, VisualQueryOutcome> visual,
        RecordingFloor floor,
        decimal visualFigure = 30m)
    {
        var executor = new FakeVisualQueryExecutor(
            (script, parameters) => script.EndsWith(StateScript, StringComparison.OrdinalIgnoreCase)
                ? visual(script, parameters)
                : VisualQueryOutcome.Ok([Row(("WarehouseQuantity", visualFigure))]));

        return new GenericRequestPartData(executor, new WipFloorInventoryService(floor));
    }

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] values)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            row[key] = value;
        }

        return row;
    }

    /// <summary>The floor's store, answering the same rows to every script and counting how often it was asked.</summary>
    private sealed class RecordingFloor : IMySqlHelperServer
    {
        private readonly IReadOnlyList<Dictionary<string, object?>> _rows;

        public RecordingFloor(IReadOnlyList<Dictionary<string, object?>> rows) => _rows = rows;

        public int QueryCount { get; private set; }

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
            CancellationToken cancellationToken = default)
        {
            QueryCount++;
            return Task.FromResult(_rows);
        }

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
