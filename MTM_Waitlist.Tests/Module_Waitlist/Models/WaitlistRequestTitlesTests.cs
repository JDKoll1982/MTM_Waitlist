using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

/// <summary>
/// The card's two lines, resolved from the Item the request was raised with (T056, FR-005, FR-029). The
/// resolver is Item-keyed and has no legacy type/subtype path, so these checks are also the guard that the
/// retired pair cannot come back as a second way to name a request.
/// </summary>
[TestClass]
public sealed class WaitlistRequestTitlesTests
{
    [TestMethod]
    public void ResolveLine1_IsTheItemsUmbrellaPhrase()
    {
        Assert.AreEqual("Pickup", WaitlistRequestTitles.ResolveLine1(Find("pickup-die")));
        Assert.AreEqual("Deliver", WaitlistRequestTitles.ResolveLine1(Find("deliver-coil")));
        Assert.AreEqual("Assist", WaitlistRequestTitles.ResolveLine1(Find("assist-coil-turn")));
        Assert.AreEqual("Other", WaitlistRequestTitles.ResolveLine1(Find("other")));
    }

    [TestMethod]
    public void ResolveLine1_UsesTheItemsOwnPhraseForTheWrongMaterialItems()
    {
        // The two wrong-material first lines are pinned verbatim (contract §2).
        Assert.AreEqual("Wrong Coil Bring:", WaitlistRequestTitles.ResolveLine1(Find("deliver-wrong-coil")));
        Assert.AreEqual("Wrong Flatstock Bring:", WaitlistRequestTitles.ResolveLine1(Find("deliver-wrong-flatstock")));
    }

    [TestMethod]
    public void ResolveLine1_IsEmptyWhenTheStoredCodeIsNotCatalogued()
    {
        // No legacy fallback: an Item the catalog does not describe gets no invented phrase.
        Assert.AreEqual(string.Empty, WaitlistRequestTitles.ResolveLine1(null));
        Assert.AreEqual(string.Empty, WaitlistRequestTitles.ResolveLine1(RequestItemCatalog.FindById("no-such-item")));
    }

    [TestMethod]
    public void ResolveLine2_ResolvesAFixedIdentifier()
    {
        var riserTable = WaitlistRequestTitles.ResolveLine2(Find("pickup-riser-table"), new RequestItemLine2Context());
        Assert.IsTrue(riserTable.IsResolved);
        Assert.AreEqual("Riser Table", riserTable.Text);

        Assert.AreEqual("Hopper", WaitlistRequestTitles.ResolveLine2(Find("pickup-hopper"), new RequestItemLine2Context()).Text);
        Assert.AreEqual("Hopper", WaitlistRequestTitles.ResolveLine2(Find("deliver-hopper"), new RequestItemLine2Context()).Text);
    }

    [TestMethod]
    public void ResolveLine2_ResolvesTheCapturedAnswerForTheFreeTextItem()
    {
        var result = WaitlistRequestTitles.ResolveLine2(
            Find("other"),
            new RequestItemLine2Context(Answer: "Skid 4471 is on the wrong dock"));

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual("Skid 4471 is on the wrong dock", result.Text);
    }

    [TestMethod]
    public void ResolveLine2_PickupDie_ShowsTheLocationAtHomeLocationAndTheNumberOtherwise()
    {
        // Home Location is the value that switches the die's second line (contract §3).
        var atHome = WaitlistRequestTitles.ResolveLine2(
            Find("pickup-die"),
            new RequestItemLine2Context(DieNumber: "D-4471", DieLocation: "Rack 12", Destination: "Home Location"));
        Assert.IsTrue(atHome.IsResolved);
        Assert.AreEqual("Rack 12", atHome.Text);

        var toDieShop = WaitlistRequestTitles.ResolveLine2(
            Find("pickup-die"),
            new RequestItemLine2Context(DieNumber: "D-4471", DieLocation: "Rack 12", Destination: "Die Shop"));
        Assert.IsTrue(toDieShop.IsResolved);
        Assert.AreEqual("D-4471", toDieShop.Text);
    }

    [TestMethod]
    public void ResolveLine2_ReadsWhatTheJobCarries()
    {
        var result = WaitlistRequestTitles.ResolveLine2(
            Find("pickup-coil"),
            new RequestItemLine2Context(PartNumber: "MMC0001000"));

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual("MMC0001000", result.Text);
    }

    [TestMethod]
    public void ResolveLine2_UnresolvableToken_RendersTheDisplayNameAndReportsTheProblem()
    {
        // A job-derived token the request does not carry is reported, never invented and never blank (FR-026).
        var result = WaitlistRequestTitles.ResolveLine2(Find("pickup-coil"), new RequestItemLine2Context());

        Assert.IsFalse(result.IsResolved, "A token with no source must be reported as unresolved.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Text), "The card must never be left with a blank identifier.");
        Assert.AreEqual(
            RequestItemLine2Resolver.ResolveDisplayName(Find("pickup-coil")!),
            result.Text,
            "An unresolvable template renders the Item's own display name.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Problem), "The configuration problem must be stated, not hidden.");
        Assert.IsFalse(
            result.Problem.StartsWith("RequestItem", StringComparison.Ordinal),
            "The report is plain language, never a bare resource key.");
    }

    [TestMethod]
    public void ResolveLine2_WithNoItem_ReportsTheProblemRatherThanInventingAnIdentifier()
    {
        var result = WaitlistRequestTitles.ResolveLine2(null, new RequestItemLine2Context(Answer: "anything"));

        Assert.IsFalse(result.IsResolved);
        Assert.AreEqual(string.Empty, result.Text);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Problem));
    }

    [TestMethod]
    public void TheResolverExposesNoLegacyPairOverload()
    {
        // FR-003/FR-023: no (requestType, subtype) entry point may exist for the card's lines.
        var overloads = typeof(WaitlistRequestTitles)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(string)))
            .Select(method => method.Name)
            .ToArray();

        Assert.AreEqual(
            0,
            overloads.Length,
            $"WaitlistRequestTitles still exposes a string-keyed entry point ({string.Join(", ", overloads)}), which is the retired type/subtype path coming back.");
    }

    private static RequestItemDefinition? Find(string itemId) => RequestItemCatalog.FindById(itemId);
}
