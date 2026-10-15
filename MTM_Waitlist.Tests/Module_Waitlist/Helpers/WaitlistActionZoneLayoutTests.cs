using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Waitlist.View.Helpers;

namespace MTM_Waitlist.Tests.Module_Waitlist.Helpers;

/// <summary>
/// The rule that decides where a card's action buttons go: three fixed columns, the rest compacted leftwards,
/// and only the number of rows shrinking (FR-061 to FR-063, D44).
/// </summary>
/// <remarks>
/// It is checked here rather than in the card's markup because the markup can only say which panel is used. The
/// arithmetic is what a person actually sees, so it is the part worth pinning: a card offering four of its five
/// actions must not draw a hole where the fifth would have been.
/// </remarks>
[TestClass]
public sealed class WaitlistActionZoneLayoutTests
{
    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_KeepsThreeColumnsWhateverIsDrawn()
    {
        Assert.AreEqual(3, WaitlistActionZoneLayout.ColumnCount, "The action zone is not three columns wide (FR-061, D44).");

        for (var drawn = 1; drawn <= 6; drawn++)
        {
            var slots = WaitlistActionZoneLayout.SlotsFor(Enumerable.Repeat(true, drawn));

            Assert.IsTrue(
                slots.All(slot => slot.Column >= 0 && slot.Column < WaitlistActionZoneLayout.ColumnCount),
                $"{drawn} action(s) are placed outside the zone's three columns (FR-061).");

            // Three or more actions have to reach the third column: a rule that stopped at two would leave the
            // zone narrower than the three columns it keeps.
            if (drawn >= WaitlistActionZoneLayout.ColumnCount)
            {
                Assert.AreEqual(
                    WaitlistActionZoneLayout.ColumnCount - 1,
                    slots.Max(slot => slot.Column),
                    $"{drawn} actions do not reach the third column (FR-061).");
            }
        }
    }

    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_FiveActionsAreTwoRowsInRowMajorOrder()
    {
        // Five is the most a card offers (Accept, Cancel, Give back, Complete, Open details) and it is the count
        // that takes the second row: three across the top, two beneath (FR-061, D44).
        var slots = WaitlistActionZoneLayout.SlotsFor(Enumerable.Repeat(true, 5));

        CollectionAssert.AreEqual(
            new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1) },
            slots.ToArray(),
            "Five actions are not three across and two down, in row-major order (FR-061, D44).");

        Assert.AreEqual(2, WaitlistActionZoneLayout.RowCountFor(5), "Five actions do not need two rows (FR-061).");
    }

    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_TheRuleHoldsForAnyCountNotJustTheCardsOwn()
    {
        // The rule is about a count, not about a particular list of actions, so it is checked at its widest too:
        // a sixth action added later still lands in row-major order without the panel changing.
        var slots = WaitlistActionZoneLayout.SlotsFor(Enumerable.Repeat(true, 6));

        CollectionAssert.AreEqual(
            new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1), (1, 2) },
            slots.ToArray(),
            "Six actions are not three across and two down, in row-major order (FR-061).");

        Assert.AreEqual(2, WaitlistActionZoneLayout.RowCountFor(6), "Six actions do not need two rows (FR-061).");
    }

    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_AMissingActionLetsTheRestShiftLeft()
    {
        // The fourth of the card's five actions is unavailable, which is the case the rule exists for: the fifth
        // action takes the empty place, and only the row count shrinks (FR-062, FR-063).
        var offered = new[] { true, true, true, false, true, true };

        CollectionAssert.AreEqual(
            new[] { (0, 0), (0, 1), (0, 2), (1, 0), (1, 1) },
            WaitlistActionZoneLayout.SlotsFor(offered).ToArray(),
            "An unavailable action leaves a hole rather than letting the buttons after it shift left (FR-063).");

        Assert.AreEqual(2, WaitlistActionZoneLayout.RowCountFor(5), "Five actions do not need two rows (FR-063).");
    }

    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_TheRowsShrinkButTheColumnsDoNot()
    {
        // One, two and three drawn actions all sit on the first row, and the row count says so - the zone keeps
        // its three columns and only loses rows.
        foreach (var drawn in new[] { 1, 2, 3 })
        {
            Assert.AreEqual(1, WaitlistActionZoneLayout.RowCountFor(drawn), $"{drawn} action(s) do not fit on one row (FR-063).");
        }

        foreach (var drawn in new[] { 4, 5, 6 })
        {
            Assert.AreEqual(2, WaitlistActionZoneLayout.RowCountFor(drawn), $"{drawn} action(s) do not need two rows (FR-063).");
        }

        Assert.AreEqual(0, WaitlistActionZoneLayout.RowCountFor(0), "A row offering no action keeps a row of empty space (FR-063).");
    }

    [TestMethod]
    public void Model_WaitlistActionZoneLayoutTests_Layout_AButtonOfferedAloneTakesTheFirstPlace()
    {
        // The one action a closed request offers - Open details - must sit in the first column rather than
        // wherever the action it replaced used to be.
        CollectionAssert.AreEqual(
            new[] { (0, 0) },
            WaitlistActionZoneLayout.SlotsFor(new[] { false, false, false, false, true, false }).ToArray(),
            "The only offered action does not take the first place in the row (FR-063).");
    }
}
