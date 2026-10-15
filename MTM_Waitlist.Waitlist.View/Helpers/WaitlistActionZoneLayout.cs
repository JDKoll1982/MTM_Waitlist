namespace MTM_Waitlist.Waitlist.View.Helpers;

/// <summary>
/// Where a card's action buttons go: three fixed columns, filled left to right and top to bottom, with the
/// buttons that are not drawn skipped rather than leaving a hole (FR-061 to FR-063).
/// </summary>
/// <remarks>
/// <para>
/// A button is drawn only while the row's own gate says the action is available, and an unavailable one is
/// removed rather than blanked (FR-062). In a grid with a declared row and column per button that leaves its
/// slot empty and the row reads as if a button failed to load, so the place a button occupies is worked out from
/// how many are drawn before it: with the fourth of six unavailable the five that remain read <c>1 2 3</c> /
/// <c>5 6</c>.
/// </para>
/// <para>
/// The rule lives here rather than in the panel that applies it so it can be checked without a window - the
/// panel is in the application project, which this library's tests do not reference, and the arithmetic is the
/// part that decides what a person sees.
/// </para>
/// </remarks>
public static class WaitlistActionZoneLayout
{
    /// <summary>The number of columns the zone keeps, whatever is drawn in it (FR-061).</summary>
    public const int ColumnCount = 3;

    /// <summary>
    /// The row and the column a drawn button occupies, both counting from zero.
    /// </summary>
    /// <param name="drawnIndex">The button's place among the buttons that are drawn, from zero.</param>
    /// <returns>The row and the column the button is placed in.</returns>
    public static (int Row, int Column) SlotFor(int drawnIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(drawnIndex);

        return (drawnIndex / ColumnCount, drawnIndex % ColumnCount);
    }

    /// <summary>
    /// The rows the drawn buttons need, which is the one dimension that shrinks with them (FR-063). No drawn
    /// buttons is no rows, so a row offering nothing collapses instead of keeping a strip of empty space.
    /// </summary>
    /// <param name="drawnCount">How many buttons are drawn.</param>
    public static int RowCountFor(int drawnCount)
        => drawnCount <= 0 ? 0 : ((drawnCount + ColumnCount - 1) / ColumnCount);

    /// <summary>
    /// The place each drawn button takes, in the order the actions are offered, from the gates that say which of
    /// them are drawn.
    /// </summary>
    /// <param name="offeredActions">
    /// One entry per action in the order the card declares them, true where the row's gate offers it.
    /// </param>
    /// <returns>One row and column per action that is drawn, in that order.</returns>
    public static IReadOnlyList<(int Row, int Column)> SlotsFor(IEnumerable<bool> offeredActions)
    {
        ArgumentNullException.ThrowIfNull(offeredActions);

        var slots = new List<(int Row, int Column)>();
        foreach (var offered in offeredActions)
        {
            if (offered)
            {
                slots.Add(SlotFor(slots.Count));
            }
        }

        return slots;
    }
}
