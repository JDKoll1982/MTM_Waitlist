using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The dunnage answer a generic request records on itself and reads back from its own stored answers.
/// </summary>
/// <remarks>
/// This is the whole record: no schema change, no new procedure, and the same shape the part answer already proves.
/// A stored row that cannot be read answers nothing rather than throwing, because the card and the detail read it
/// for every request on the board.
/// </remarks>
[TestClass]
public sealed class GenericRequestDunnageAnswerTests
{
    [TestMethod]
    public void Model_GenericRequestDunnageAnswerTests_AllFourKeysRoundTrip()
    {
        var recorded = new GenericRequestDunnageAnswer
        {
            PartNumber = "DNG-7788",
            TypeName = "Rack",
            PicturePath = @"X:\Dunnage\Parts\rack.png",
            Offered = false,
        };

        var answers = recorded.ToAnswers();
        var readBack = GenericRequestDunnageAnswer.TryRead(answers);

        Assert.IsNotNull(readBack);
        Assert.AreEqual("DNG-7788", readBack!.PartNumber);
        Assert.AreEqual("Rack", readBack.TypeName);
        Assert.AreEqual(@"X:\Dunnage\Parts\rack.png", readBack.PicturePath);
        Assert.IsFalse(readBack.Offered);

        CollectionAssert.AreEquivalent(
            new[]
            {
                GenericRequestDunnageAnswer.PartKey,
                GenericRequestDunnageAnswer.TypeKey,
                GenericRequestDunnageAnswer.PictureKey,
                GenericRequestDunnageAnswer.OfferedKey,
            },
            answers.Keys.ToArray());
    }

    [TestMethod]
    public void Model_GenericRequestDunnageAnswerTests_KeyComparisonIgnoresCase()
    {
        var answers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["DUNNAGE.PART"] = "DNG-1",
            ["Dunnage.Type"] = "Tote",
            ["dunnage.PICTURE"] = @"X:\Dunnage\Parts\tote.png",
            ["DUNNAGE.OFFERED"] = "true",
        };

        var readBack = GenericRequestDunnageAnswer.TryRead(answers);

        Assert.IsNotNull(readBack);
        Assert.AreEqual("DNG-1", readBack!.PartNumber);
        Assert.AreEqual("Tote", readBack.TypeName);
        Assert.IsTrue(readBack.Offered);
    }

    [TestMethod]
    public void Model_GenericRequestDunnageAnswerTests_APartThatLeftTheCatalogueReadsBackAsNotOffered()
    {
        var answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [GenericRequestDunnageAnswer.PartKey] = "DNG-GONE",
            [GenericRequestDunnageAnswer.OfferedKey] = "false",
        };

        var readBack = GenericRequestDunnageAnswer.TryRead(answers);

        Assert.IsNotNull(readBack, "The part is kept on the request even though the catalogue no longer offers it.");
        Assert.IsFalse(readBack!.Offered, "A part that has left the catalogue is recorded as no longer offered.");
    }

    [TestMethod]
    public void Model_GenericRequestDunnageAnswerTests_AMissingOrMalformedRecordAnswersNothing()
    {
        Assert.IsNull(GenericRequestDunnageAnswer.TryRead(null), "A request with no answers states no dunnage.");

        Assert.IsNull(
            GenericRequestDunnageAnswer.TryRead(new Dictionary<string, string>()),
            "A request with no part key states no dunnage.");

        Assert.IsNull(
            GenericRequestDunnageAnswer.TryRead(new Dictionary<string, string>
            {
                [GenericRequestDunnageAnswer.PartKey] = "   ",
            }),
            "A blank part number is no part.");

        Assert.IsNull(
            GenericRequestDunnageAnswer.TryRead(new Dictionary<string, string>
            {
                [GenericRequestDunnageAnswer.PartKey] = "DNG-1",
                [GenericRequestDunnageAnswer.OfferedKey] = "perhaps",
            }),
            "An offered value that is not a boolean cannot be read, so nothing is claimed either way.");
    }
}
