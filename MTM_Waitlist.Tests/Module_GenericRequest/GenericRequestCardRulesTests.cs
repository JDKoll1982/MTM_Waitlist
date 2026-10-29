using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Controls.Helpers;
using MTM_Waitlist.GenericRequest.Controls.Models;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The generic card draws its own variant's cells and nothing else, and never draws a cell it has no value for
/// (015-generic-wc-ui-shell, FR-015, FR-016, FR-018).
/// </summary>
[TestClass]
public sealed class GenericRequestCardRulesTests
{
    private static GenericRequestCardModel Model(GenericRequestCardVariant variant) => new()
    {
        RequestNumber = "GR-1001",
        Variant = variant,
        WorkCenter = "Laser 1",
        ItemName = "Stated part",
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

            Assert.AreEqual(5, cells.Count, $"the {variant} variant drew {cells.Count} cells, expected five");

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
            Variant = GenericRequestCardVariant.Part,
            WorkCenter = "Laser 1",
            ItemName = "Stated part",
            PartNumber = "5052/AL-1",
        };

        var cells = GenericRequestCardRules.CellsFor(sparse);

        Assert.AreEqual(3, cells.Count, "a card with no quantity and no needed-by should draw three cells");
    }
}
