using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The part answer travels out with the request and back with it, so the card states what the requester saw
/// (017-generic-wc-stated-part FR-016, FR-018).
/// </summary>
[TestClass]
public sealed class GenericRequestPartAnswerTests
{
    [TestMethod]
    public void Audit_GenericRequestPartAnswerTests_TheAnswerSurvivesTheRoundTrip()
    {
        var written = new GenericRequestPartAnswer
        {
            PartNumber = "100-AAA",
            Sequence = "20",
            SequenceSource = "Infor Visual (own part), WIP floor",
            QuantityOnHand = 12.5m,
            AnsweredBy = "WipFloor",
            Stage = "Wip",
        };

        var read = GenericRequestPartAnswer.TryRead(written.ToAnswers());

        Assert.IsNotNull(read);
        Assert.AreEqual("100-AAA", read!.PartNumber);
        Assert.AreEqual("20", read.Sequence);
        Assert.AreEqual("Infor Visual (own part), WIP floor", read.SequenceSource);
        Assert.AreEqual(12.5m, read.QuantityOnHand);
        Assert.AreEqual("WipFloor", read.AnsweredBy);
        Assert.AreEqual("Wip", read.Stage);
    }

    [TestMethod]
    public void Audit_GenericRequestPartAnswerTests_AnOutsideServiceOperationStoresTheWipStageAndItsFigure()
    {
        var read = GenericRequestPartAnswer.TryRead(
            new GenericRequestPartAnswer
            {
                PartNumber = "100-AAA",
                Sequence = "90",
                QuantityOnHand = 12m,
                AnsweredBy = "WipFloor",
                Stage = "Wip",
            }.ToAnswers());

        Assert.IsNotNull(read);
        Assert.AreEqual(12m, read!.QuantityOnHand, "a subcontracted operation still holds material on the floor");
        Assert.AreEqual("Wip", read.Stage, "outside service is a next resource, not a third stage");
    }

    [TestMethod]
    public void Audit_GenericRequestPartAnswerTests_ARequestThatStatesNoPartReadsBackAsNone()
    {
        Assert.IsNull(GenericRequestPartAnswer.TryRead(null));
        Assert.IsNull(GenericRequestPartAnswer.TryRead(new Dictionary<string, string>()));
        Assert.IsNull(
            GenericRequestPartAnswer.TryRead(new Dictionary<string, string> { [GenericRequestPartAnswer.SequenceKey] = "20" }),
            "a sequence without a part is not a part answer");
    }
}
