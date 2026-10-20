using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

/// <summary>
/// The card's cell model (<c>data-model.md</c> §7): one label-and-value cell, and the rule that a cell which
/// resolved nothing is never a cell the card draws.
/// </summary>
[TestClass]
public sealed class WaitlistLineCardCellTests
{
    /// <summary>
    /// FR-028 and FR-055: every cell reads as `Label: value` on one line, apart from the die-shop cell, which
    /// carries no label because its phrase is the whole of it. The label-less cell is the die-shop cell and nothing
    /// else, so a cell built with a label is never label-less.
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_WaitlistLineCardCell_IsLabelLessForTheDieShopCellAndNoOther()
    {
        var quantityCell = new WaitlistLineCardCell
        {
            Label = "CardCellQuantityInHouse",
            Value = "1,240",
            Order = 2,
        };

        var dieShopCell = new WaitlistLineCardCell
        {
            Value = "Take To Die Shop",
            Order = 3,
        };

        Assert.IsFalse(
            quantityCell.IsLabelLess,
            "A cell with a label draws that label on top, so it is never label-less.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(quantityCell.Label), "The label is the resource key the card resolves.");
        Assert.IsTrue(
            dieShopCell.IsLabelLess,
            "The die-shop cell is the one cell with no label; its phrase is drawn as the value (FR-028, FR-055).");
        Assert.AreEqual("Take To Die Shop", dieShopCell.Value, "The die-shop cell's value is the phrase itself.");
        Assert.IsTrue(dieShopCell.HasValue, "A label-less cell still has to have resolved something to be drawn.");
    }

    /// <summary>
    /// FR-028: a cell draws a hairline only where another cell meets it — along the top of the second row and the
    /// left of the second column — so the cell area carries no border around its own outside edge.
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_CellsToDraw_OnlyTheCellsWithANeighbourDrawAHairline()
    {
        var cells = WaitlistLineCardCell.CellsToDraw(
        [
            new WaitlistLineCardCell { Label = "One", Value = "1", Order = 1 },
            new WaitlistLineCardCell { Label = "Two", Value = "2", Order = 2 },
            new WaitlistLineCardCell { Label = "Three", Value = "3", Order = 3 },
            new WaitlistLineCardCell { Label = "Four", Value = "4", Order = 4 },
        ]);

        Assert.AreEqual(4, cells.Count);
        Assert.IsFalse(cells[0].DrawsLeftHairline, "The cell in the first row and first column has no neighbour above or to its left, so it draws no hairline.");
        Assert.IsFalse(cells[0].DrawsTopHairline, "The cell in the first row and first column has no neighbour above or to its left, so it draws no hairline.");
        Assert.IsTrue(cells[1].DrawsLeftHairline, "The cell beside another one is not separated from it.");
        Assert.IsFalse(cells[1].DrawsTopHairline, "A cell on the first row draws no hairline above it, or the cell area would carry a border around its outside edge.");
        Assert.IsFalse(cells[2].DrawsLeftHairline, "The first cell of the second row draws no hairline on its left, or the cell area would carry a border on its outside edge.");
        Assert.IsTrue(cells[2].DrawsTopHairline, "The second row is not separated from the first.");
        Assert.IsTrue(cells[3].DrawsLeftHairline && cells[3].DrawsTopHairline, "The fourth cell is separated from the two cells it meets.");
    }

    /// <summary>
    /// FR-026: a short panel keeps the same rule, so a single cell spans the width with no border around it and a
    /// three-cell panel's spanning cell draws only the line above it.
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_CellsToDraw_AShortPanelDrawsNoOuterBorder()
    {
        var single = WaitlistLineCardCell.CellsToDraw([new WaitlistLineCardCell { Label = "One", Value = "1" }]);
        Assert.IsFalse(single[0].DrawsLeftHairline || single[0].DrawsTopHairline, "The only cell of a one-cell panel draws no hairline at all.");

        var three = WaitlistLineCardCell.CellsToDraw(
        [
            new WaitlistLineCardCell { Label = "One", Value = "1", Order = 1 },
            new WaitlistLineCardCell { Label = "Two", Value = "2", Order = 2 },
            new WaitlistLineCardCell { Label = "Three", Value = "3", Order = 3 },
        ]);

        Assert.IsTrue(three[1].DrawsLeftHairline, "The second cell of the top row is not separated from the first.");
        Assert.IsFalse(three[2].DrawsLeftHairline, "The spanning cell draws a hairline on its left, so the cell area would carry a border on its outside edge.");
        Assert.IsTrue(three[2].DrawsTopHairline, "The spanning cell is not separated from the row above it.");
    }

    /// <summary>
    /// FR-006 and FR-018: a cell that resolved no value is not added to a card's cells at all, so nothing is
    /// padded and the panel's geometry follows the cells that remain (FR-026).
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_CellsToDraw_ACellWithNoValueIsNeverAdded()
    {
        var candidates = new WaitlistLineCardCell?[]
        {
            new WaitlistLineCardCell { Label = "CardCellCoilNumber", Value = "MMC0001000", Order = 1 },
            new WaitlistLineCardCell { Label = "CardCellQuantityInHouse", Value = "   ", Order = 2 },
            new WaitlistLineCardCell { Label = "CardCellAverageCoilWeight", Value = string.Empty, Order = 3 },
            null,
            new WaitlistLineCardCell { Label = "CardCellCoilDescription", Value = "Steel coil", Order = 4 },
        };

        var cells = WaitlistLineCardCell.CellsToDraw(candidates);

        Assert.AreEqual(2, cells.Count, "Only the cells that resolved a value are added.");
        CollectionAssert.AreEqual(
            new[] { "MMC0001000", "Steel coil" },
            cells.Select(cell => cell.Value).ToArray(),
            "The cells that remain keep their declared order.");
        Assert.IsFalse(
            cells.Any(cell => !cell.HasValue),
            "Nothing padded with an empty cell ever reaches the card.");
    }

    /// <summary>No declared field resolved, so the card draws no cells and its panel collapses (FR-026).</summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_CellsToDraw_NothingResolvedDrawsNoCells()
    {
        Assert.AreEqual(0, WaitlistLineCardCell.CellsToDraw(null).Count);
        Assert.AreEqual(
            0,
            WaitlistLineCardCell.CellsToDraw([new WaitlistLineCardCell { Label = "CardCellPartType", Order = 1 }]).Count);
    }

    /// <summary>
    /// FR-078: only a cell that was handed both a part and a scope opens a picture, so the card never offers an
    /// enlarge affordance over a cell that has nothing to open.
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_OpensReferencePicture_NeedsBothAPartAndAScope()
    {
        var hopperSizeCell = new WaitlistLineCardCell
        {
            Label = "CardCellSize",
            Value = "Large",
            Order = 1,
            ReferencePartNumber = "Large",
            ReferencePartScope = "Visual",
        };

        var textOnlyCell = new WaitlistLineCardCell { Label = "CardCellCoilNumber", Value = "MMC0001000", Order = 1 };

        Assert.IsTrue(hopperSizeCell.OpensReferencePicture, "The hopper's Size cell opens that size's reference image.");
        Assert.IsFalse(textOnlyCell.OpensReferencePicture, "A cell handed no part opens no picture at all.");
        Assert.IsFalse(
            new WaitlistLineCardCell { ReferencePartNumber = "Large" }.OpensReferencePicture,
            "A part with no scope is not a picture the cell can read, so it opens none.");
    }

    /// <summary>
    /// FR-080: a size whose picture is missing or unusable draws the application's one shared placeholder rather
    /// than a blank, and a path that did resolve is what the cell draws.
    /// </summary>
    [TestMethod]
    public void Model_WaitlistLineCardCellTests_EffectiveReferenceImagePath_FallsBackToTheSharedPlaceholder()
    {
        var unresolvedCell = new WaitlistLineCardCell
        {
            Label = "CardCellSize",
            Value = "Medium",
            Order = 1,
            ReferencePartNumber = "Medium",
            ReferencePartScope = "Visual",
        };

        Assert.AreEqual(
            MTM_Waitlist.Module_Shared.Helpers.ImagePicturePolicy.NoImagePath,
            unresolvedCell.EffectiveReferenceImagePath,
            "Nothing resolved, so the cell draws the shared placeholder rather than a blank (FR-080).");

        unresolvedCell.ResolvedReferenceImagePath = @"C:\pictures\Medium.png";

        Assert.AreEqual(
            @"C:\pictures\Medium.png",
            unresolvedCell.EffectiveReferenceImagePath,
            "A picture that did resolve is the one the cell draws.");
    }
}
