using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Data.Services;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The live die search: what it asks, what it answers, and what each of the three answers means
/// (020-generic-wc-die-search FR-002, FR-003, FR-004, FR-005, FR-006, FR-009).
/// </summary>
[TestClass]
public sealed class GenericRequestDieDataTests
{
    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ADieNumberAnswersThatDieAndIsMatchedAsTyped()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));
        var dies = GenericRequestReadShapes.DieData(executor);

        var result = await dies.ReadDiesAsync("FGT0602-01");

        Assert.AreEqual(GenericRequestPresentation.Populated, result.Presentation);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("FGT0602-01", result.Items[0].DieNumber);
        Assert.AreEqual("U-B0-01", result.Items[0].Location);
        Assert.AreEqual(
            "FGT0602-01",
            Convert.ToString(executor.LastParameters!["Term"]),
            "The term reaches the read as the person typed it, once normalised (FR-004).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ATermNoDieMatchesAnswersEmptyRatherThanUnavailable()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Ok([])));

        var result = await dies.ReadDiesAsync("FGT9999-99");

        Assert.AreEqual(GenericRequestPresentation.Empty, result.Presentation);
        Assert.AreEqual(0, result.Count, "A miss is an answer, not a failure (FR-002).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_APartNumberReturnsEveryDieItOwnsWithItsLocation()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Ok(
        [
            DieRow("FGT0602-01", "DIE ONE", "U-B0-01"),
            DieRow("FGT0602-02", "DIE TWO", "U-B0-02"),
            DieRow("FGT0603-01", "DIE THREE", "U-B1-01"),
        ])));

        var result = await dies.ReadDiesAsync("PART-9003");

        Assert.AreEqual(3, result.Count, "Every die the part owns is listed, not just one (FR-003, SC-002).");
        CollectionAssert.AreEqual(
            new[] { "FGT0602-01", "FGT0602-02", "FGT0603-01" },
            result.Items.Select(option => option.DieNumber).ToArray());
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_TheTermReachesTheReadNormalised()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([]));
        var dies = GenericRequestReadShapes.DieData(executor);

        await dies.ReadDiesAsync("fgt1000");

        Assert.AreEqual(
            "FGT0001000",
            Convert.ToString(executor.LastParameters!["Term"]),
            "The same normalisation the part search applies: upper case, and padded for a known prefix (FR-004).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ABareNumberIsTriedOnceAsADieNumber()
    {
        var executor = new FakeVisualQueryExecutor((_, parameters) =>
            string.Equals(Convert.ToString(parameters["Term"]), "FGT0001000", StringComparison.Ordinal)
                ? VisualQueryOutcome.Ok([DieRow("FGT0001000", "DIE", "U-B0-01")])
                : VisualQueryOutcome.Ok([]));

        var result = await GenericRequestReadShapes.DieData(executor).ReadDiesAsync("1000");

        Assert.AreEqual(GenericRequestPresentation.Populated, result.Presentation);
        Assert.AreEqual("FGT0001000", result.Items[0].DieNumber);
        Assert.AreEqual(2, executor.ExecuteCount, "The prefixed die number is tried once, not searched repeatedly.");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_AnUnreachableSourceAnswersUnavailableWhenThereIsNoMirror()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Unreachable("the plant system is down")));

        var result = await dies.ReadDiesAsync("PART-9003");

        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            result.Presentation,
            "A host with no mtm_mock mirror has nothing to answer from, so an unreachable source is reported rather "
            + "than filled with an invented empty answer (FR-005).");
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_AnUnreachableSourceIsAnsweredFromTheMirrorInsteadOfRefused()
    {
        var executor = Executor(_ => VisualQueryOutcome.Unreachable("the plant system is down"));
        var mirror = new StubMirrorStore(
            ("sp_visual_die_search_get", [DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));

        var result = await GenericRequestReadShapes.DieData(executor, mirror: mirror).ReadDiesAsync("PART-9003");

        Assert.AreEqual(
            GenericRequestPresentation.Populated,
            result.Presentation,
            "the mirror answers when the plant system cannot be reached, exactly as the work-order flow's reads do");
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("FGT0602-01", result.Items[0].DieNumber);
        Assert.AreEqual("U-B0-01", result.Items[0].Location);
        CollectionAssert.AreEqual(
            new[] { "sp_visual_die_search_get" },
            mirror.Reads,
            "the shape reads its own procedure, so the cached answer is the same answer (FR-004)");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ASettledUnreachableVerdictIsAnsweredFromTheMirrorWithoutAskingVisual()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));
        var mirror = new StubMirrorStore(
            ("sp_visual_die_search_get", [DieRow("FGT7777-01", "CACHED DIE", "U-B9-09")]));

        var dies = GenericRequestReadShapes.DieData(
            executor,
            new StubVisualReachability(VisualReadStatus.Cached),
            mirror);

        var result = await dies.ReadDiesAsync("FGT7777-01");

        Assert.AreEqual(GenericRequestPresentation.Populated, result.Presentation);
        Assert.AreEqual("FGT7777-01", result.Items[0].DieNumber, "the cached die is the answer the probe's verdict allows");
        Assert.AreEqual(0, executor.ExecuteCount, "the verdict is the probe's, so no connect attempt is spent rediscovering it");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_AFailedReadAnswersUnavailable()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Failed("bad column")));

        var result = await dies.ReadDiesAsync("PART-9003");

        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            result.Presentation,
            "A read that failed proves nothing about the die, so no die is accepted from it (FR-002).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ADieWithNoLocationSaysSoInsteadOfDrawingBlank()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Ok(
        [
            DieRow("FGT0602-01", "BASE DIE", null),
        ])));

        var result = await dies.ReadDiesAsync("FGT0602-01");

        Assert.AreEqual(string.Empty, result.Items[0].Location);
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(result.Items[0].LocationText),
            "A row with no location draws the step's own wording, never a blank cell (FR-006, SC-005).");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_APlaceholderDescriptionIsKeptAsTheDescription()
    {
        var dies = GenericRequestReadShapes.DieData(Executor(_ => VisualQueryOutcome.Ok(
        [
            DieRow("FGT0602-01", "Die still needs location", "U-B0-01"),
        ])));

        var result = await dies.ReadDiesAsync("FGT0602-01");

        Assert.AreEqual(
            "Die still needs location",
            result.Items[0].Description,
            "The plant's placeholder is the die's own description here, and is never read as its location (FR-009).");
        Assert.AreEqual("U-B0-01", result.Items[0].Location);
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_AnEmptyTermReadsNothingAtAll()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));

        var result = await GenericRequestReadShapes.DieData(executor).ReadDiesAsync("   ");

        Assert.AreEqual(GenericRequestPresentation.Empty, result.Presentation);
        Assert.AreEqual(0, executor.ExecuteCount, "Nothing is asked of the plant for a term nobody typed.");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ASettledUnreachableVerdictAnswersWithoutTouchingVisual()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));
        var dies = GenericRequestReadShapes.DieData(executor, new StubVisualReachability(VisualReadStatus.Cached));

        var result = await dies.ReadDiesAsync("FGT0602-01");

        Assert.AreEqual(
            GenericRequestPresentation.Unavailable,
            result.Presentation,
            "the probe already knows the source cannot be reached and there is no mirror, so the search says so");
        Assert.AreEqual(
            0,
            executor.ExecuteCount,
            "the answer is the probe's, so no connect attempt is spent rediscovering it");
    }

    [TestMethod]
    public async Task Audit_GenericRequestDieDataTests_ALiveVerdictStillAsksVisual()
    {
        var executor = Executor(_ => VisualQueryOutcome.Ok([DieRow("FGT0602-01", "BASE DIE", "U-B0-01")]));
        var dies = GenericRequestReadShapes.DieData(executor, new StubVisualReachability(VisualReadStatus.Live));

        var result = await dies.ReadDiesAsync("FGT0602-01");

        Assert.AreEqual(GenericRequestPresentation.Populated, result.Presentation);
        Assert.AreEqual(1, executor.ExecuteCount, "a reachable source is still asked");
    }

    private static FakeVisualQueryExecutor Executor(Func<string, VisualQueryOutcome> outcome) =>
        new((script, _) => outcome(script));

    private static Dictionary<string, object?> DieRow(string dieNumber, string? description, string? location) =>
        GenericRequestPartDataTests.Row(
            ("DieNumber", dieNumber),
            ("Description", description),
            ("Location", location),
            ("Association", "Die"));
}
