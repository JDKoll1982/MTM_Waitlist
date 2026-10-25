using System.Globalization;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The detail panel's geometry, read off the number of cells the Item fills
/// (<c>specs/012-waitlist-card-layout/data-model.md</c> §8, FR-024, FR-026, FR-027, FR-103, D20).
/// </summary>
/// <remarks>
/// <para>
/// The panel is the detail zone's two lower rows of a two-column grid: four cells fill two rows of two; three
/// put the first two on the top row and let the third span the bottom row; two leave the bottom row empty, so
/// it collapses; one spans the full width; none leaves both rows empty and centres the title pair in the zone
/// it would otherwise sit at the top of. Every one of those is the same rule read at a different count —
/// <b>the last cell of an odd count spans the whole row it sits on</b> — which is why the geometry is derived
/// here rather than hand-set per Item.
/// </para>
/// <para>
/// This is the one place the rule lives. The card's templates ask it, through the two converters the
/// dictionary declares, for each slot's span and whether the slot is drawn at all; the tests ask it for every
/// count from none to four. A card is therefore the same shape and the same height as any other card of the
/// same Item, and an Item with fewer cells is shorter rather than padded (FR-009, FR-103).
/// </para>
/// </remarks>
public static class WaitlistCardCellLayout
{
    /// <summary>
    /// The most cells the panel carries, which is the tallest arrangement the rules describe: four, as two
    /// rows of two (D20). A declaration that grew beyond four would have its extra fields left undrawn rather
    /// than overflow the panel.
    /// </summary>
    public const int PanelSlotCount = 4;

    /// <summary>The panel's two columns.</summary>
    public const int PanelColumnCount = 2;

    /// <summary>The panel's two rows, which are the detail zone's lower two.</summary>
    public const int PanelRowCount = PanelSlotCount / PanelColumnCount;

    /// <summary>
    /// The number of slots the panel fills for a cell count, capped at the four the geometry describes.
    /// </summary>
    /// <param name="cellCount">The cells the Item filled after the card's own rules were applied.</param>
    /// <returns>How many slots are drawn, from none to <see cref="PanelSlotCount"/>.</returns>
    public static int FilledSlots(int cellCount) => Math.Clamp(cellCount, 0, PanelSlotCount);

    /// <summary>
    /// Whether a slot draws anything at all for a cell count. A slot with no cell behind it is collapsed
    /// rather than left as an empty cell, which is what keeps a short panel from being padded (FR-006).
    /// </summary>
    /// <param name="slot">The slot's zero-based position, in row-major order.</param>
    /// <param name="cellCount">The cells the Item filled.</param>
    public static bool IsDrawn(int slot, int cellCount) =>
        slot >= 0 && slot < FilledSlots(cellCount);

    /// <summary>The panel row a slot sits on: 0 the top row, 1 the bottom one.</summary>
    /// <param name="slot">The slot's zero-based position, in row-major order.</param>
    public static int RowFor(int slot) => slot / PanelColumnCount;

    /// <summary>The panel column a slot sits in: 0 the left one, 1 the right one.</summary>
    /// <param name="slot">The slot's zero-based position, in row-major order.</param>
    public static int ColumnFor(int slot) => slot % PanelColumnCount;

    /// <summary>
    /// How many columns a slot spans for a cell count: the whole row when it is the last cell of an odd
    /// count — the single-cell panel, and the three-cell panel whose third cell spans the bottom row —
    /// otherwise its own column (FR-026, FR-027).
    /// </summary>
    /// <param name="slot">The slot's zero-based position, in row-major order.</param>
    /// <param name="cellCount">The cells the Item filled.</param>
    /// <returns><c>2</c> for a spanning slot, otherwise <c>1</c>. A slot that draws nothing spans one.</returns>
    public static int ColumnSpanFor(int slot, int cellCount)
    {
        var filled = FilledSlots(cellCount);

        return IsDrawn(slot, cellCount) && slot == filled - 1 && ColumnFor(slot) == 0
            ? PanelColumnCount
            : 1;
    }

    /// <summary>
    /// Whether the panel's bottom row carries anything, which is what tells a two-cell panel from a
    /// three-or-four-cell one: with two, the bottom row collapses rather than holding an empty cell
    /// (FR-006, FR-026).
    /// </summary>
    /// <param name="cellCount">The cells the Item filled.</param>
    public static bool CarriesBottomRow(int cellCount) => FilledSlots(cellCount) > PanelColumnCount;

    /// <summary>
    /// The slot a template's binding names in its converter parameter, so one rule answers for all four slots
    /// rather than the dictionary holding four near-identical converters.
    /// </summary>
    /// <param name="parameter">The converter parameter, a slot's zero-based position as text.</param>
    /// <returns>The slot, or <c>-1</c> when the parameter names none.</returns>
    public static int SlotFrom(object? parameter) =>
        parameter is string text
        && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var slot)
            ? slot
            : -1;

    /// <summary>
    /// The number of cells a bound value carries. The card binds a row's <c>Cells.Count</c>, and anything
    /// else — a page that has not filled its cells yet, a host with no row — is no cells at all rather than a
    /// guess.
    /// </summary>
    /// <param name="value">The bound value.</param>
    /// <returns>The cell count, or zero when the value is not one.</returns>
    public static int CellCountFrom(object? value) =>
        value is int count ? Math.Max(count, 0) : 0;
}
