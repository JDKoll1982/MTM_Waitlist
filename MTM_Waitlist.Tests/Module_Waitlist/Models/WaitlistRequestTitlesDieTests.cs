using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

/// <summary>
/// A generic die request's card names the die and where it lives, with no job to read either from
/// (020-generic-wc-die-search FR-012).
/// </summary>
/// <remarks>
/// A generic request stores both facts on its own answers when it is raised, because the board draws the card later
/// with no requesting job in hand.
/// </remarks>
[TestClass]
public sealed class WaitlistRequestTitlesDieTests
{
    [TestMethod]
    public void Model_WaitlistRequestTitlesDieTests_AChosenDieIsNamedWithItsLocation()
    {
        var request = GenericDieRequest("FGT0602-01", "U-B0-01");

        var context = WaitlistRequestTitles.ResolveContext(request);

        Assert.AreEqual("FGT0602-01", context.DieNumber, "the card names the die the request is for (FR-012)");
        Assert.AreEqual("U-B0-01", context.DieLocation, "and where that die lives");
    }

    [TestMethod]
    public void Model_WaitlistRequestTitlesDieTests_ADieWithNoRecordedLocationNamesTheDieAndNoLocation()
    {
        var request = GenericDieRequest("FGT0602-01", location: null);

        var context = WaitlistRequestTitles.ResolveContext(request);

        Assert.AreEqual("FGT0602-01", context.DieNumber);
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(context.DieLocation),
            "no location is invented for a die Visual records none for (FR-006)");
    }

    [TestMethod]
    public void Model_WaitlistRequestTitlesDieTests_TheStoredDieOutranksTheRequestsOwnValue()
    {
        var request = new WaitlistRequest
        {
            Category = "Pickup",
            Item = "pickup-die",
            IsGeneric = true,
            InputValue = "FGT0602-01",
            Answers = new GenericRequestDieAnswer
            {
                DieNumber = "FGT0602-01",
                DieLocation = "U-B0-01",
            }.ToAnswers(),
        };

        var line = WaitlistRequestTitles.ResolveLine2(Find("pickup-die"), WaitlistRequestTitles.ResolveContext(request));

        Assert.IsTrue(line.IsResolved);
        Assert.AreEqual("FGT0602-01", line.Text, "the card's second line names the die (FR-012)");
    }

    private static WaitlistRequest GenericDieRequest(string dieNumber, string? location) =>
        new()
        {
            Category = "Pickup",
            Item = "pickup-die",
            IsGeneric = true,
            InputValue = dieNumber,
            Answers = new GenericRequestDieAnswer
            {
                DieNumber = dieNumber,
                DieLocation = location ?? string.Empty,
            }.ToAnswers(),
        };

    private static RequestItemDefinition Find(string id) =>
        RequestItemCatalog.FindById(id) ?? throw new InvalidOperationException($"'{id}' is not a catalogued item.");
}
