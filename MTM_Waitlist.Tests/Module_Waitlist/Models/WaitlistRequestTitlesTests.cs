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
        Assert.AreEqual("Pickup", WaitlistRequestTitles.ResolveLine1(Find("pickup-coil")));
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
    public void ResolveLine2_PickupDie_ShowsTheDieTheRequestIsFor()
    {
        // Superseded 2026-09-20 (FR-054, D22). The identifier used to switch between the die's location and its
        // number depending on the captured destination, and the destination question is retired. What shapes the
        // card now is which die the request is for, so a job carrying two dies is two different cards.
        var request = new WaitlistRequest { Category = "Pickup", Item = "pickup-die", InputValue = "D-9001-PRESS BAY" };

        var result = WaitlistRequestTitles.ResolveLine2(
            Find("pickup-die"),
            WaitlistRequestTitles.ResolveContext(request, TwoDieJob()));

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(
            "D-9001-PRESS BAY",
            result.Text,
            "The card names the die the request is for, not the first die the job happens to carry (FR-057).");
    }

    [TestMethod]
    public void ResolveLine2_DieRequestWithNoStoredDie_FallsBackToTheJobsOwnDie()
    {
        // A request raised before the picker learned to record its die still has to render, so the job's primary
        // die stands in rather than leaving the card blank or inventing an identifier (FR-005, FR-026).
        var request = new WaitlistRequest { Category = "Pickup", Item = "pickup-die" };

        var result = WaitlistRequestTitles.ResolveLine2(
            Find("pickup-die"),
            WaitlistRequestTitles.ResolveContext(request, DieJob()));

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual("FGT0002000-DIE SHOP", result.Text);
    }

    [TestMethod]
    public void ResolveLine2_DieItems_ShowTheDieNumberAndItsLocation()
    {
        foreach (var itemId in new[] { "pickup-die", "deliver-die" })
        {
            var request = new WaitlistRequest { Category = "Pickup", Item = itemId };

            var result = WaitlistRequestTitles.ResolveLine2(
                Find(itemId),
                WaitlistRequestTitles.ResolveContext(request, DieJob()));

            Assert.IsTrue(result.IsResolved, $"{itemId} resolves its identifier from the job (FR-053).");
            Assert.AreEqual(
                "FGT0002000-DIE SHOP",
                result.Text,
                $"{itemId}'s identifier is the die's own number and where the die is.");
        }
    }

    [TestMethod]
    public void ResolveLine1_DieItems_NameTheRequestingJobsPartNumber()
    {
        foreach (var (itemId, expected) in new[] { ("pickup-die", "Pickup Die: PART-9003"), ("deliver-die", "Deliver Die: PART-9003") })
        {
            var request = new WaitlistRequest { Category = "Pickup", Item = itemId };

            var line1 = WaitlistRequestTitles.ResolveLine1(
                Find(itemId),
                WaitlistRequestTitles.ResolveContext(request, DieJob()));

            Assert.AreEqual(expected, line1, $"{itemId}'s first line names the part the die is assigned to.");
        }
    }

    [TestMethod]
    public void ResolveLine1_DieItemWithNoJob_StillSaysWhatKindOfRequestItIs()
    {
        var request = new WaitlistRequest { Category = "Deliver", Item = "deliver-die" };

        var line1 = WaitlistRequestTitles.ResolveLine1(
            request.ItemDefinition,
            WaitlistRequestTitles.ResolveContext(request));

        Assert.AreEqual(
            "Deliver Die:",
            line1,
            "An unresolvable job value degrades to the phrase — never a blank, never a bare template (FR-005).");
    }

    [TestMethod]
    public void ResolveContext_HandsJobValuesOnlyToTheItemsWhoseTemplatesNameThem()
    {
        // The die and job values are job data, and they are handed over only where a template asks for them — so
        // an Item that names none of them keeps the card it had (FR-053).
        var coil = WaitlistRequestTitles.ResolveContext(
            new WaitlistRequest { Category = "Pickup", Item = "pickup-coil" },
            DieJob());

        Assert.IsNull(coil.DieNumber, "pickup-coil's identifier is the coil's number, not the job's die.");
        Assert.IsNull(coil.DieLocation);
        Assert.IsNull(coil.JobPartNumber, "pickup-coil does not name the job's part number, so it is handed none.");
    }

    /// <summary>A job carrying a die, as the composition root maps it onto the wizard's snapshot.</summary>
    private static RequestJobPartAvailability DieJob() => RequestJobPartAvailability.None with
    {
        HasActiveJob = true,
        HasDie = true,
        JobPartNumber = "PART-9003",
        DieNumber = "FGT0002000",
        DieLocation = "DIE SHOP",
    };

    /// <summary>
    /// A job carrying <b>two</b> dies, so the first die the job carries is not the answer to "which die"
    /// (FR-057). The job's own primary die stays <c>FGT0002000</c> on purpose: that is the value a request that
    /// never recorded a die falls back to.
    /// </summary>
    private static RequestJobPartAvailability TwoDieJob() => DieJob().WithDies(
    [
        new RequestDiePart { PartNumber = "FGT0002000", Location = "DIE SHOP" },
        new RequestDiePart { PartNumber = "D-9001", Location = "PRESS BAY" },
    ]);

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
    public void ResolveLine2_DunnageRequest_ShowsThePartTheOperatorChose()
    {
        // FR-048, FR-051: the request stores the part the operator ended on — one the job carried, or their
        // substitute — and the card reads it back through the template's `{dunnage_part}` token.
        var request = new WaitlistRequest
        {
            Category = "Pickup",
            Item = "pickup-dunnage",
            InputValue = "DN-STL-4",
        };

        var result = WaitlistRequestTitles.ResolveLine2(
            request.ItemDefinition,
            WaitlistRequestTitles.ResolveContext(request));

        Assert.IsTrue(result.IsResolved, "A dunnage request that carries its part resolves its identifier.");
        Assert.AreEqual("DN-STL-4", result.Text);
    }

    [TestMethod]
    public void ResolveLine2_DunnageRequestWithNoStoredPart_ShowsTheDisplayNameAndReportsTheProblem()
    {
        var request = new WaitlistRequest { Category = "Pickup", Item = "pickup-dunnage" };

        var result = WaitlistRequestTitles.ResolveLine2(
            request.ItemDefinition,
            WaitlistRequestTitles.ResolveContext(request));

        Assert.IsFalse(result.IsResolved, "Nothing was stored, so nothing may be shown as the identifier.");
        Assert.AreEqual(
            RequestItemLine2Resolver.ResolveDisplayName(Find("pickup-dunnage")!),
            result.Text,
            "The Item's own display name stands in, never a blank and never an invented part.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Problem), "The configuration problem is stated, not hidden (FR-026).");
    }

    [TestMethod]
    public void ResolveContext_HandsTheDunnageTokenOnlyToAnItemThatAsksForIt()
    {
        // The mapping is keyed on the Item's own template, so a captured answer is never handed to a token that
        // Item does not use: pickup-coil's identifier is the job's part number, which this request does not hold.
        var coil = new WaitlistRequest { Category = "Pickup", Item = "pickup-coil", InputValue = "MMC0001000" };

        var context = WaitlistRequestTitles.ResolveContext(coil);

        Assert.IsNull(context.DunnagePart, "An Item that does not name the dunnage token must not be handed one.");
        Assert.AreEqual("MMC0001000", context.Answer, "The captured answer is still available to the tokens it belongs to.");
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
