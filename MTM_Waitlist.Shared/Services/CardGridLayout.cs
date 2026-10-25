namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// How many cards fit across a container, and how wide each one is, so a row fills the width it was given.
/// </summary>
/// <remarks>
/// <para>
/// This is the one place the rule lives. It used to be written out four times — three columns hard-coded in
/// <c>SetupWorkCenterPage</c>, the same three copied into <c>NewRequestWorkCenterPage</c>, a third variant in
/// <c>LuggerArrangementPage</c>, and a fixed card height in two of them — which is why the cards stopped
/// following their container: a hard-coded column count cannot know that the window grew, and a hard-coded height
/// cannot know that a card's own content needs more room.
/// </para>
/// <para>
/// A card is never narrower than the caller's minimum, and the row is always filled exactly: the leftover pixels
/// from the division are dropped rather than left as a ragged right edge, so the last card in a row never spills
/// past the container and no horizontal scrollbar appears.
/// </para>
/// </remarks>
public static class CardGridLayout
{
    /// <summary>The gutter between cards when the caller names none, in effective pixels.</summary>
    public const double DefaultGap = 12;

    /// <summary>
    /// Solves the grid for a container of <paramref name="availableWidth"/>.
    /// </summary>
    /// <param name="availableWidth">The container's width, in effective pixels.</param>
    /// <param name="minimumCardWidth">
    /// The narrowest a card may be drawn. It is what decides where one more column would make the cards too small
    /// to read, so it is the only number a page has to choose.
    /// </param>
    /// <param name="gap">The space between two cards, and between the cards and the edges.</param>
    /// <param name="maximumColumns">
    /// A ceiling on the columns, or zero for no ceiling — a row simply carries as many cards as fit.
    /// </param>
    /// <returns>
    /// The column count and the width to give each card. A container that has not been measured yet — or a
    /// minimum that makes no sense — yields no columns rather than a guess, so a caller can leave the grid alone.
    /// </returns>
    public static CardGridSolution Solve(
        double availableWidth,
        double minimumCardWidth,
        double gap = DefaultGap,
        int maximumColumns = 0)
    {
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            return CardGridSolution.None;
        }

        if (double.IsNaN(minimumCardWidth) || double.IsInfinity(minimumCardWidth) || minimumCardWidth <= 0)
        {
            return CardGridSolution.None;
        }

        var gutter = double.IsNaN(gap) || double.IsInfinity(gap) || gap < 0 ? 0 : gap;

        // Columns that fit: one more card needs its own width plus the gap that separates it from the last.
        var columns = (int)Math.Floor((availableWidth + gutter) / (minimumCardWidth + gutter));
        columns = Math.Max(1, columns);

        if (maximumColumns > 0)
        {
            columns = Math.Min(columns, maximumColumns);
        }

        var cardWidth = Math.Floor((availableWidth - ((columns - 1) * gutter)) / columns);

        // A container narrower than its own gutters still draws one card, at the width it has.
        return cardWidth < 1
            ? new CardGridSolution(1, Math.Max(1, Math.Floor(availableWidth)))
            : new CardGridSolution(columns, cardWidth);
    }
}

/// <summary>The column count and card width a container solves to.</summary>
/// <param name="Columns">How many cards fit across, or zero when the container could not be solved.</param>
/// <param name="CardWidth">The width to give each card, in effective pixels; zero when unsolved.</param>
public readonly record struct CardGridSolution(int Columns, double CardWidth)
{
    /// <summary>No solution: the container has no usable width, so nothing should be resized from it.</summary>
    public static CardGridSolution None => new(0, 0);

    /// <summary>Whether a solution was found, which is what a caller checks before touching a panel.</summary>
    public bool IsSolved => Columns > 0 && CardWidth > 0;
}
