using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The die a generic request records on itself and reads back from its own stored answers (020 FR-011, FR-012).
/// </summary>
/// <remarks>
/// No schema change and no new procedure: the die rides the answers column the part, dunnage and scrap answers
/// already use. The board reads it for every generic die request, so a stored row that cannot be read answers
/// nothing rather than throwing.
/// </remarks>
[TestClass]
public sealed class GenericRequestDieAnswerTests
{
    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_AStatedDieRoundTripsUnderItsOwnKeys()
    {
        var recorded = new GenericRequestDieAnswer { DieNumber = "FGT0602-01", DieLocation = "U-B0-01" };

        var answers = recorded.ToAnswers();
        var readBack = GenericRequestDieAnswer.TryRead(answers);

        Assert.AreEqual("FGT0602-01", answers[GenericRequestDieAnswer.DieNumberKey]);
        Assert.AreEqual("U-B0-01", answers[GenericRequestDieAnswer.DieLocationKey]);
        Assert.IsNotNull(readBack);
        Assert.AreEqual("FGT0602-01", readBack!.DieNumber);
        Assert.AreEqual("U-B0-01", readBack.DieLocation);
    }

    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_ToAnswers_ABlankDieRecordsNoKeyAtAll()
    {
        var answers = new GenericRequestDieAnswer { DieNumber = "   ", DieLocation = "U-B0-01" }.ToAnswers();

        Assert.AreEqual(0, answers.Count, "A request that never stated a die records no die answer (FR-011).");
    }

    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_ToAnswers_ADieWithNoLocationRecordsOnlyItsNumber()
    {
        var answers = new GenericRequestDieAnswer { DieNumber = "FGT0602-01" }.ToAnswers();

        Assert.AreEqual(1, answers.Count);
        CollectionAssert.Contains(
            answers.Keys.ToArray(),
            GenericRequestDieAnswer.DieNumberKey);
        Assert.IsFalse(
            answers.ContainsKey(GenericRequestDieAnswer.DieLocationKey),
            "An unknown location writes no key, so the card can tell absent from empty (FR-006).");
    }

    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_TryRead_KeyComparisonIgnoresCase()
    {
        var answers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DIE.NUMBER"] = "FGT0602-01",
            ["DIE.LOCATION"] = "U-B0-01",
        };

        var readBack = GenericRequestDieAnswer.TryRead(answers);

        Assert.IsNotNull(readBack);
        Assert.AreEqual("FGT0602-01", readBack!.DieNumber);
        Assert.AreEqual("U-B0-01", readBack.DieLocation);
    }

    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_TryRead_TheNumberIsKeptExceptForSurroundingSpaces()
    {
        var readBack = GenericRequestDieAnswer.TryRead(new Dictionary<string, string>
        {
            [GenericRequestDieAnswer.DieNumberKey] = "  FGT0602-01  ",
            [GenericRequestDieAnswer.DieLocationKey] = "  U-B0-01  ",
        });

        Assert.IsNotNull(readBack);
        Assert.AreEqual("FGT0602-01", readBack!.DieNumber);
        Assert.AreEqual("U-B0-01", readBack.DieLocation);
    }

    [TestMethod]
    public void Model_GenericRequestDieAnswerTests_TryRead_AMissingOrBlankDieAnswersNothing()
    {
        Assert.IsNull(GenericRequestDieAnswer.TryRead(null), "A request with no answers states no die.");

        Assert.IsNull(
            GenericRequestDieAnswer.TryRead(new Dictionary<string, string>()),
            "A request with no die key states no die.");

        Assert.IsNull(
            GenericRequestDieAnswer.TryRead(new Dictionary<string, string>
            {
                [GenericRequestDieAnswer.DieNumberKey] = "   ",
            }),
            "A blank number is no die.");
    }
}
