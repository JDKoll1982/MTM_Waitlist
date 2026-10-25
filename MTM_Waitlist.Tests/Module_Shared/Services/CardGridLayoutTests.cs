using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Tests.Module_Shared.Services;

/// <summary>
/// The one rule that decides how many cards fit across a container and how wide each one is.
/// </summary>
/// <remarks>
/// Three pages used to answer this for themselves and disagreed: two pinned every card to three columns and 190 px
/// tall, one pinned 208, and one measured only after its items changed. The rule is here so the answer is one
/// answer, and so it can be checked without a window.
/// </remarks>
[TestClass]
public sealed class CardGridLayoutTests
{
    /// <summary>
    /// The cards fill the width they were given exactly: no leftover strip on the right, and the last card never
    /// spills past the edge into a horizontal scrollbar.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_TheRowFillsTheWidthItWasGiven()
    {
        foreach (var width in new[] { 900d, 1103d, 1280d, 1913d })
        {
            var solution = CardGridLayout.Solve(width, 300, 12);

            var used = (solution.Columns * solution.CardWidth) + ((solution.Columns - 1) * 12);

            Assert.IsTrue(solution.IsSolved, $"{width} px solved to no cards at all.");
            Assert.IsTrue(
                used <= width + 0.001,
                $"{width} px: {solution.Columns} cards of {solution.CardWidth} px need {used} px, so the row spills past its container.");
            Assert.IsTrue(
                width - used < 12 + solution.Columns,
                $"{width} px: {solution.Columns} cards of {solution.CardWidth} px leave {width - used} px unused, so a card could have been wider.");
        }
    }

    /// <summary>
    /// A wider container carries more cards rather than wider ones, once one more card still clears the minimum.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_AWiderContainerTakesMoreCardsInsteadOfStretchingThem()
    {
        var narrow = CardGridLayout.Solve(1000, 300, 12);
        var wide = CardGridLayout.Solve(1600, 300, 12);

        Assert.AreEqual(3, narrow.Columns, "1000 px with a 300 px floor is three cards across.");
        Assert.AreEqual(5, wide.Columns, "1600 px with a 300 px floor is five cards across.");
        Assert.IsTrue(
            wide.CardWidth < narrow.CardWidth || wide.CardWidth <= 320,
            "the wider container must not simply blow the same three cards up");
    }

    /// <summary>
    /// The row height is not part of this rule: it is declared on each grid's own panel, so widening a window
    /// changes how many cards fit across and never how tall a row is.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_TheRuleIsHorizontalOnly()
    {
        var solution = CardGridLayout.Solve(1600, 300);

        Assert.IsTrue(solution.IsSolved);
        Assert.AreEqual(
            0,
            typeof(CardGridSolution).GetProperties().Count(property => property.Name.Contains("Height", StringComparison.Ordinal)),
            "the solution must not carry a height, or the row height would start following the container");
    }

    /// <summary>
    /// A card is never drawn narrower than the floor, however many columns the ceiling allows.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_ACardIsNeverNarrowerThanTheFloor()
    {
        var solution = CardGridLayout.Solve(640, 300, 12);

        Assert.AreEqual(2, solution.Columns, "640 px holds two 300 px cards and the gutter between them.");
        Assert.IsTrue(solution.CardWidth >= 300, $"a card came out {solution.CardWidth} px, under the 300 px floor");
    }

    /// <summary>
    /// An unmeasured container, or a floor that makes no sense, solves to nothing rather than to a guess, so a
    /// caller leaves the panel alone.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_AnUnusableContainerSolvesToNothing()
    {
        foreach (var width in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
        {
            Assert.IsFalse(CardGridLayout.Solve(width, 300).IsSolved, $"{width} solved to a grid.");
        }

        foreach (var floor in new[] { 0d, -20d, double.NaN })
        {
            Assert.IsFalse(CardGridLayout.Solve(1200, floor).IsSolved, $"a floor of {floor} solved to a grid.");
        }
    }

    /// <summary>
    /// A ceiling keeps a row from carrying more cards than the page is willing to show.
    /// </summary>
    [TestMethod]
    public void Layout_CardGridLayoutTests_ACeilingCapsTheColumnsAndWidensTheCards()
    {
        var solution = CardGridLayout.Solve(1600, 200, 12, maximumColumns: 4);

        Assert.AreEqual(4, solution.Columns, "the page asked for no more than four across");
        Assert.IsTrue(solution.CardWidth > 200, "the cards take the width the ceiling freed up");
    }
}
