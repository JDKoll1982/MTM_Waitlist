using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist;

/// <summary>
/// The scrap type a generic scrap request's card line names.
/// </summary>
/// <remarks>
/// An Item whose identifier names the scrap type reads it from the job on a work-order request. A generic request
/// has no job, so its own stored answer is the only place the type can come from; without it the line falls back to
/// the Item's display name and the card names the wrong thing (FR-005). Every case here reads the rendered line
/// rather than the context handed to it, because the fallback happens one step further out.
/// </remarks>
[TestClass]
public sealed class WaitlistRequestTitlesScrapTests
{
    [TestMethod]
    public void Model_WaitlistRequestTitlesScrapTests_ResolveLine2_AGenericScrapRequestNamesTheStatedType()
    {
        var request = ScrapRequest(isGeneric: true, statedType: "Galvanized Steel");

        var line = Line2(request);

        Assert.AreEqual(
            "Galvanized Steel",
            line.Text,
            "The card names the type the requester stated, not the Item's display name (FR-005).");
        Assert.IsTrue(line.IsResolved, "The line resolved nothing, so the card would draw its own display name.");
    }

    [TestMethod]
    public void Model_WaitlistRequestTitlesScrapTests_ResolveLine2_AStatedTypeOutranksAJobTheWorkCenterHappensToRun()
    {
        var request = ScrapRequest(isGeneric: true, statedType: "Galvanized Steel");
        var job = RequestJobPartAvailability.All with { ScrapType = "3003 Aluminum" };

        var line = Line2(request, job);

        Assert.AreEqual(
            "Galvanized Steel",
            line.Text,
            "Two scrap types on one card is the disagreement FR-007 exists to prevent, so the stated one wins.");
    }

    [TestMethod]
    public void Model_WaitlistRequestTitlesScrapTests_ResolveLine2_ARequestStatingNoTypeIsNotFilledWithTheItemName()
    {
        var line = Line2(ScrapRequest(isGeneric: true, statedType: null));

        Assert.AreNotEqual("", line.Text, "The line still says what kind of request it is.");
        Assert.AreNotEqual(
            "Scrap",
            line.Text.Trim(),
            "A request that states no type must not have one invented for it.");
    }

    [TestMethod]
    public void Model_WaitlistRequestTitlesScrapTests_ResolveLine2_AWorkOrderRequestStillReadsTheTypeItsJobDecided()
    {
        var job = RequestJobPartAvailability.None with { ScrapType = "3003 Aluminum" };

        var line = Line2(ScrapRequest(isGeneric: false, statedType: "Galvanized Steel"), job);

        Assert.AreEqual(
            "3003 Aluminum",
            line.Text,
            "A work-order card keeps reading the decision stored on its job; this chunk does not change it.");
    }

    private static RequestItemLine2Result Line2(WaitlistRequest request, RequestJobPartAvailability? job = null) =>
        WaitlistRequestTitles.ResolveLine2(
            request.ItemDefinition,
            WaitlistRequestTitles.ResolveContext(request, job));

    private static WaitlistRequest ScrapRequest(bool isGeneric, string? statedType) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = "Scrap",
        Item = "pickup-scrap",
        IsGeneric = isGeneric,
        Answers = statedType is null
            ? null
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [GenericRequestScrapAnswer.ScrapTypeKey] = statedType,
            },
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };
}
