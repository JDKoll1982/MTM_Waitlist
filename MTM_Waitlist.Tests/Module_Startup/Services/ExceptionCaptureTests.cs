using System.Text.Json.Nodes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Logging;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The capture: a fault is recorded whole — its type, its message, its stack and every exception in its chain,
/// including each of an aggregate's independent failures — and a recording that fails raises nothing
/// (`contracts/logging-contract.md` §1.1, §1.4; FR-032, FR-037, SC-016).
/// </summary>
/// <remarks>
/// The chain is read back as the JSON the store would hold rather than through a convenience wrapper, because
/// what is being proved is the shape the store receives. No test here touches a database: the writer is given a
/// store seam that refuses everything, which is the fault this file is about.
/// </remarks>
[TestClass]
public sealed class ExceptionCaptureTests
{
    [TestMethod]
    public void Serialize_WhenAFaultHasAnInnerFault_KeepsEveryExceptionWithItsDepthAndIndex()
    {
        // Arrange
        var fault = new InvalidOperationException(
            "the outer fault",
            new TimeoutException("the inner fault"));

        // Act
        var detail = ExceptionDetailSerializer.Serialize(fault);

        // Assert: FR-032 — the chain, not the outermost message alone.
        Assert.IsNotNull(detail);
        var nodes = JsonNode.Parse(detail)!.AsArray();

        Assert.AreEqual(2, nodes.Count, "the inner fault was lost");
        Assert.AreEqual(0, nodes[0]!["level"]!.GetValue<int>());
        Assert.AreEqual(0, nodes[0]!["index"]!.GetValue<int>());
        StringAssert.Contains(nodes[0]!["message"]!.GetValue<string>(), "the outer fault");
        Assert.AreEqual(1, nodes[1]!["level"]!.GetValue<int>());
        Assert.AreEqual(0, nodes[1]!["index"]!.GetValue<int>());
        StringAssert.Contains(nodes[1]!["type"]!.GetValue<string>(), nameof(TimeoutException));
    }

    [TestMethod]
    public void Serialize_WhenTheFaultIsAnAggregate_ContributesEveryIndependentFailure()
    {
        // Arrange: three failures that are not one another's cause; reducing this to the first is what FR-032
        // forbids.
        var fault = new AggregateException(
            new InvalidOperationException("first"),
            new TimeoutException("second"),
            new ArgumentException("third"));

        // Act
        var nodes = JsonNode.Parse(ExceptionDetailSerializer.Serialize(fault)!)!.AsArray();

        // Assert
        Assert.AreEqual(4, nodes.Count, "an aggregate was reduced to fewer than all of its failures");

        var messages = nodes.Select(node => node!["message"]!.GetValue<string>()).ToList();
        CollectionAssert.Contains(messages, "first");
        CollectionAssert.Contains(messages, "second");
        CollectionAssert.Contains(messages, "third");
    }

    [TestMethod]
    public void Serialize_WhenTheChainIsLongerThanTheNodeCeiling_IsTruncatedWithItsMarker()
    {
        // Arrange: one more exception than the ceiling allows.
        var fault = new InvalidOperationException("level 0");
        for (var depth = 1; depth <= ExceptionDetailSerializer.MaxNodes; depth++)
        {
            fault = new InvalidOperationException($"level {depth}", fault);
        }

        // Act
        var nodes = JsonNode.Parse(ExceptionDetailSerializer.Serialize(fault)!)!.AsArray();

        // Assert: cut at a node boundary with the marker set, so a short fault is never mistaken for a cut one
        // (`contracts/logging-contract.md` §1.1).
        Assert.AreEqual(ExceptionDetailSerializer.MaxNodes, nodes.Count);
        Assert.IsTrue(nodes[0]!["truncated"]!.GetValue<bool>(), "a truncated chain must say so");
        StringAssert.Contains(nodes[0]!["full"]!.GetValue<string>(), "level 0");
    }

    [TestMethod]
    public void Serialize_WhenTheFaultIsShort_CarriesNoTruncationMarker()
    {
        // Arrange
        var fault = new InvalidOperationException("a short fault");

        // Act
        var nodes = JsonNode.Parse(ExceptionDetailSerializer.Serialize(fault)!)!.AsArray();

        // Assert
        Assert.IsNull(nodes[0]!["truncated"]);
    }

    [TestMethod]
    public void Serialize_WhenAMemberThrowsOnRead_ReplacesItRatherThanLosingTheEntry()
    {
        // Arrange: a fault whose own message cannot be read.
        var fault = new HostileException();

        // Act
        var nodes = JsonNode.Parse(ExceptionDetailSerializer.Serialize(fault)!)!.AsArray();

        // Assert
        Assert.AreEqual(ExceptionDetailSerializer.UnreadableMarker, nodes[0]!["message"]!.GetValue<string>());
    }

    [TestMethod]
    public void Serialize_WhenThereIsNoFault_AnswersNothing()
    {
        // Assert: an entry raised without an exception carries no chain (`contracts/logging-contract.md` §1.1).
        Assert.IsNull(ExceptionDetailSerializer.Serialize(null));
    }

    [TestMethod]
    public void Serialize_WhenAMessageCarriesAParameterValue_DoesNotWriteTheValue()
    {
        // Arrange: the shape a provider uses when it puts a statement's parameters into its message.
        var fault = new InvalidOperationException(
            "Duplicate entry for @p_display_name=Shop floor station rejected");

        // Act
        var detail = ExceptionDetailSerializer.Serialize(fault);

        // Assert: FR-036, §6 — the name may be recorded, the value may not.
        Assert.IsNotNull(detail);
        StringAssert.Contains(detail, "@p_display_name");
        Assert.IsFalse(
            detail.Contains("Shop floor station", StringComparison.Ordinal),
            "a parameter value reached the store");
    }

    [TestMethod]
    public async Task FlushAsync_WhenTheStoreRefusesEveryWrite_AbsorbsItAndRaisesNoSecondDiagnostic()
    {
        // Arrange: the store is the fault being recorded.
        var store = new RefusingMySqlHelperServer();
        var writer = new StoreLogWriter(store);

        writer.Enqueue(new StoreLogRecord(
            "entry-1",
            "correlation-1",
            "Error",
            "launch",
            "Failure",
            "system",
            null,
            "test-workstation",
            null,
            "Startup",
            nameof(InvalidOperationException),
            "the original fault",
            null,
            null,
            null));

        // Act: the flush is the only awaiting member, and it must not throw (FR-037, SC-016).
        await writer.FlushAsync(CancellationToken.None);

        // Assert: the write was attempted and its failure was dropped rather than reported in place of the
        // original fault.
        Assert.IsTrue(store.Attempts > 0, "the queued entry was never written");
    }

    /// <summary>A fault whose own members cannot be read, for the defensive path.</summary>
    private sealed class HostileException : Exception
    {
        public override string Message => throw new InvalidOperationException("this member refuses to be read");
    }

    /// <summary>A store seam that refuses every write, so the writer's own failure path is what is exercised.</summary>
    private sealed class RefusingMySqlHelperServer : IMySqlHelperServer
    {
        public int Attempts { get; private set; }

        public Task<int> ExecuteStoredProcedureNonQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
        {
            Attempts++;

            return Task.FromException<int>(new InvalidOperationException("the store refused the connection"));
        }

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteStoredProcedureQueryAsync(
            string storedProcedureName,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("The writer only writes.");

        public Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteSqlQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written (constitution III).");

        public Task<int> ExecuteSqlNonQueryAsync(
            string sql,
            IReadOnlyDictionary<string, object?> parameters,
            MySqlDatabaseTarget databaseTarget,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("No statement text is written (constitution III).");
    }
}
