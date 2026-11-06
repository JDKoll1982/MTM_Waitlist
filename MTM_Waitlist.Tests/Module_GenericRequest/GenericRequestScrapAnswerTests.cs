using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The scrap type a generic request states, recorded on its own answers column and read back from it.
/// </summary>
/// <remarks>
/// The wording the requester chose is what the card draws, so these assertions are the whole of the guarantee that a
/// later arrangement edit cannot rewrite what the plant was told (FR-005, FR-011).
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapAnswerTests
{
    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_ToAnswers_RecordsTheStatedTypeUnderTheOneKey()
    {
        var answers = new GenericRequestScrapAnswer("5052 Aluminum").ToAnswers();

        Assert.AreEqual(1, answers.Count, "A scrap answer records one key and adds no second.");
        Assert.AreEqual("5052 Aluminum", answers[GenericRequestScrapAnswer.ScrapTypeKey]);
        Assert.AreEqual("scrap.type", GenericRequestScrapAnswer.ScrapTypeKey);
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_ReturnsTheRequestersOwnWording()
    {
        var stored = new GenericRequestScrapAnswer("5052 Aluminium").ToAnswers();

        var read = GenericRequestScrapAnswer.TryRead(stored);

        Assert.IsNotNull(read);
        Assert.AreEqual(
            "5052 Aluminium",
            read!.ScrapType,
            "The spelling the requester chose is kept exactly, so an arrangement rename cannot rewrite it (FR-011).");
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_IgnoresTheKeysCase()
    {
        var stored = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SCRAP.TYPE"] = "Steel",
        };

        var read = GenericRequestScrapAnswer.TryRead(stored);

        Assert.IsNotNull(read, "A dictionary that does not compare keys case-insensitively is still read correctly.");
        Assert.AreEqual("Steel", read!.ScrapType);
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_AnswersNothingForAMissingKey()
    {
        Assert.IsNull(GenericRequestScrapAnswer.TryRead(new Dictionary<string, string>()));
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_AnswersNothingForAnEmptyOrWhitespaceValue()
    {
        var empty = new Dictionary<string, string> { [GenericRequestScrapAnswer.ScrapTypeKey] = string.Empty };
        var blank = new Dictionary<string, string> { [GenericRequestScrapAnswer.ScrapTypeKey] = "   " };

        Assert.IsNull(GenericRequestScrapAnswer.TryRead(empty), "An empty value states no type rather than throwing.");
        Assert.IsNull(GenericRequestScrapAnswer.TryRead(blank), "A whitespace value states no type rather than throwing.");
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_AnswersNothingForNoDictionary()
    {
        Assert.IsNull(GenericRequestScrapAnswer.TryRead(null));
    }

    [TestMethod]
    public void Model_GenericRequestScrapAnswerTests_TryRead_TrimsTheStoredValue()
    {
        var stored = new Dictionary<string, string> { [GenericRequestScrapAnswer.ScrapTypeKey] = "  Steel  " };

        var read = GenericRequestScrapAnswer.TryRead(stored);

        Assert.IsNotNull(read);
        Assert.AreEqual("Steel", read!.ScrapType, "Surrounding spaces are not part of the type the requester stated.");
    }
}
