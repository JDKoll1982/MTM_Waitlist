using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// What a sequence card says about where the sequence is inventoried: the systems holding it, each named once, in the
/// fixed source order.
/// </summary>
[TestClass]
public sealed class GenericRequestSequenceCardTests
{
    private static GenericRequestSequenceOption Sequence(params SequenceSource[] sources) => new(
        Sequence: "20",
        Description: "Press Room 800 Ton",
        Sources: sources,
        OperationStatus: string.Empty,
        IsLastOperation: false,
        NextResource: string.Empty,
        HasCancelledOnlyOperations: false);

    /// <summary>
    /// Both Infor Visual sources are one system, so a sequence Visual recorded twice does not read as two systems.
    /// </summary>
    [TestMethod]
    public void Unit_GenericRequestSequenceCardTests_TheTwoVisualSources_NameOneSystem()
    {
        var ownPart = Sequence(SequenceSource.VisualOwnPart).InventoriedWith;
        var both = Sequence(SequenceSource.VisualOwnPart, SequenceSource.VisualRequirement).InventoriedWith;

        Assert.AreEqual(ownPart, both, "a part Visual holds as its own and as a requirement is inventoried with Visual once");
    }

    /// <summary>
    /// A sequence both systems know names both of them, in the fixed source order.
    /// </summary>
    [TestMethod]
    public void Unit_GenericRequestSequenceCardTests_ASequenceBothSystemsKnow_NamesBothSystems()
    {
        var visual = SequenceSourceNames.SystemOf(SequenceSource.VisualOwnPart);
        var wip = SequenceSourceNames.SystemOf(SequenceSource.WipFloor);
        var line = Sequence(SequenceSource.VisualOwnPart, SequenceSource.WipFloor).InventoriedWith;

        Assert.AreNotEqual(visual, wip, "the two systems must not read as the same name");
        StringAssert.Contains(line, visual);
        StringAssert.Contains(line, wip);
        Assert.IsTrue(
            line.IndexOf(visual, StringComparison.Ordinal) < line.IndexOf(wip, StringComparison.Ordinal),
            "the systems are named in the fixed source order");
    }

    /// <summary>
    /// The stable names the request records are untouched by the drawn line: only what a person reads changed.
    /// </summary>
    [TestMethod]
    public void Unit_GenericRequestSequenceCardTests_TheRecordedSourceCodes_KeepTheStableNames()
    {
        var sequence = Sequence(SequenceSource.VisualOwnPart, SequenceSource.WipFloor);

        Assert.AreEqual("VisualOwnPart, WipFloor", sequence.SourceCodes);
    }
}
