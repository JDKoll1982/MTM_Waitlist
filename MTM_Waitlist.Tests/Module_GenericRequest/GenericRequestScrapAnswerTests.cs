using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The scrap type a generic request records on itself and reads back from its own stored answers.
/// </summary>
/// <remarks>
/// No schema change, no new procedure, and the same shape the part and dunnage answers already prove. The card
/// reads it for every generic scrap request on the board, so a stored row that cannot be read answers nothing
/// rather than throwing.
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapAnswerTests
{
    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_AStatedTypeRoundTripsUnderItsOwnKey()
    {
        var recorded = new GenericRequestScrapAnswer { ScrapType = "Galvanized Steel" };

        var answers = recorded.ToAnswers();
        var readBack = GenericRequestScrapAnswer.TryRead(answers);

        Assert.AreEqual("Galvanized Steel", answers[GenericRequestScrapAnswer.ScrapTypeKey]);
        Assert.IsNotNull(readBack);
        Assert.AreEqual("Galvanized Steel", readBack!.ScrapType);
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_ToAnswers_ABlankTypeRecordsNoKeyAtAll()
    {
        var answers = new GenericRequestScrapAnswer { ScrapType = "   " }.ToAnswers();

        Assert.AreEqual(0, answers.Count, "A request that never stated a type records no scrap answer.");
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_KeyComparisonIgnoresCase()
    {
        var answers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SCRAP.TYPE"] = "Skeleton",
        };

        var readBack = GenericRequestScrapAnswer.TryRead(answers);

        Assert.IsNotNull(readBack);
        Assert.AreEqual("Skeleton", readBack!.ScrapType);
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_TheStoredWordingIsKeptExceptForSurroundingSpaces()
    {
        var readBack = GenericRequestScrapAnswer.TryRead(new Dictionary<string, string>
        {
            [GenericRequestScrapAnswer.ScrapTypeKey] = "  3003 Aluminum  ",
        });

        Assert.IsNotNull(readBack);
        Assert.AreEqual(
            "3003 Aluminum",
            readBack!.ScrapType,
            "The requester's own wording is kept, because that is what the plant was told (FR-011).");
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_AMissingOrBlankRecordAnswersNothing()
    {
        Assert.IsNull(GenericRequestScrapAnswer.TryRead(null), "A request with no answers states no scrap type.");

        Assert.IsNull(
            GenericRequestScrapAnswer.TryRead(new Dictionary<string, string>()),
            "A request with no scrap key states no scrap type.");

        Assert.IsNull(
            GenericRequestScrapAnswer.TryRead(new Dictionary<string, string>
            {
                [GenericRequestScrapAnswer.ScrapTypeKey] = "   ",
            }),
            "A blank type is no type.");
    }
}
