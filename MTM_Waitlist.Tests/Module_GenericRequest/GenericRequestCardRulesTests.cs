using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Controls.Helpers;
using MTM_Waitlist.GenericRequest.Controls.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The generic card draws its own variant's cells and nothing else, and never draws a cell it has no value for
/// (015-generic-wc-ui-shell FR-015, FR-016, FR-018; 016-generic-wc-request-foundation FR-022 to FR-025).
/// </summary>
[TestClass]
public sealed class GenericRequestCardRulesTests
{
    private const string StatedAnswer = "Bring the spare riser table to Press 4";

    private static GenericRequestCardModel Model(GenericRequestCardVariant variant) => new()
    {
        RequestNumber = "GR-1001",
        Variant = variant,
        WorkCenter = "Laser 1",
        CategoryText = "Pickup",
        ItemName = "Stated part",
        StatedValue = StatedAnswer,
        PartNumber = "5052/AL-1",
        DunnageType = "Aluminum sheet",
        ScrapType = "aluminum chips",
        DieNumber = "D-1001",
        Quantity = 4,
        NeededBy = new DateTimeOffset(2026, 10, 30, 7, 0, 0, TimeSpan.Zero),
        RaisedBy = "J. Koll",
        RaisedAt = new DateTimeOffset(2026, 10, 29, 9, 0, 0, TimeSpan.Zero),
        History =
        [
            new GenericRequestCardHistoryEntry(new DateTimeOffset(2026, 10, 29, 9, 0, 0, TimeSpan.Zero), "Raised"),
        ],
    };

    [TestMethod]
    public void Audit_GenericRequestCardRulesTests_EachVariantDrawsOnlyItsOwnCell()
    {
        var expected = new Dictionary<GenericRequestCardVariant, string>
        {
            [GenericRequestCardVariant.Answer] = StatedAnswer,
            [GenericRequestCardVariant.Part] = "5052/AL-1",
            [GenericRequestCardVariant.Dunnage] = "Aluminum sheet",
            [GenericRequestCardVariant.Scrap] = "aluminum chips",
            [GenericRequestCardVariant.Die] = "D-1001",
        };

        foreach (var (variant, ownValue) in expected)
        {
            var values = GenericRequestCardRules.CellsFor(Model(variant)).Select(cell => cell.Value).ToList();

            Assert.IsTrue(
                values.Contains(ownValue),
                $"the {variant} variant does not draw its own value '{ownValue}'");

            foreach (var (otherVariant, otherValue) in expected.Where(pair => pair.Key != variant))
            {
                Assert.IsFalse(
                    values.Contains(otherValue),
                    $"the {variant} variant draws '{otherValue}', which belongs to the {otherVariant} variant");
            }
        }
    }

    [TestMethod]
    public void Audit_GenericRequestCardRulesTests_NoCellIsBlank()
    {
        foreach (var variant in Enum.GetValues<GenericRequestCardVariant>())
        {
            var cells = GenericRequestCardRules.CellsFor(Model(variant));

            // Work center, category, item, the variant's own cell, quantity and needed-by: the six facts a full
            // generic request has, whatever it is about.
            Assert.AreEqual(6, cells.Count, $"the {variant} variant drew {cells.Count} cells, expected six");

            foreach (var cell in cells)
            {
                Assert.IsFalse(
                    string.IsNullOrWhiteSpace(cell.Value),
                    $"the {variant} variant drew a blank cell, which the card must never do");
            }
        }
    }

    [TestMethod]
    public void Audit_GenericRequestCardRulesTests_AbsentValueDrawsNoCell()
    {
        var sparse = new GenericRequestCardModel
        {
            Variant = GenericRequestCardVariant.Answer,
            WorkCenter = "Laser 1",
            ItemName = "Stated part",
            StatedValue = StatedAnswer,
        };

        var cells = GenericRequestCardRules.CellsFor(sparse);

        Assert.AreEqual(3, cells.Count, "a card with no category, no quantity and no needed-by should draw three cells");
    }

    [TestMethod]
    public void Audit_GenericRequestCardRulesTests_ARequestWithNoDescriptionDrawsNoDescriptionCell()
    {
        var withoutDescription = Model(GenericRequestCardVariant.Answer);

        var withDescription = new GenericRequestCardModel
        {
            RequestNumber = withoutDescription.RequestNumber,
            Variant = withoutDescription.Variant,
            WorkCenter = withoutDescription.WorkCenter,
            CategoryText = withoutDescription.CategoryText,
            ItemName = withoutDescription.ItemName,
            StatedValue = withoutDescription.StatedValue,
            Quantity = withoutDescription.Quantity,
            NeededBy = withoutDescription.NeededBy,
            Description = "The one in bay 4 is cracked.",
        };

        var withoutCells = GenericRequestCardRules.CellsFor(withoutDescription);
        var withCells = GenericRequestCardRules.CellsFor(withDescription);

        Assert.AreEqual(
            withoutCells.Count + 1,
            withCells.Count,
            "a description should add exactly one cell, and its absence should add none");

        Assert.IsTrue(
            withCells.Any(cell => cell.Value == "The one in bay 4 is cracked."),
            "the description should be drawn when the requester wrote one");
    }
}
