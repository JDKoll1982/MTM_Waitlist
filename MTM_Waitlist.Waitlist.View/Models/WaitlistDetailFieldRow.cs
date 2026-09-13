using System.Collections.Generic;
using System.Linq;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One row of the request page's declared-field grid: one declared field, or two side by side.
/// </summary>
/// <remarks>
/// <para>
/// The grid lands here rather than on the card (FR-007, <c>data-model.md</c> §7). Fields are laid out in their
/// declared order, two per row, and a field that is <b>alone on its row</b> takes that row's full width.
/// </para>
/// <para>
/// The span is derived from the <b>declared field count</b> and never from the Item's identity (§D9, FR-006).
/// That is what gives an Item configured with a single field — and any Item that later gains an odd count — the
/// full-width row the design asks for, without a per-Item layout variant: an odd declared count simply leaves
/// its last row with one field, and that field spans both columns.
/// </para>
/// </remarks>
public sealed class WaitlistDetailFieldRow
{
    /// <summary>The row's first field, or null when the row carries nothing.</summary>
    public WaitlistDetailTemplateField? First { get; init; }

    /// <summary>The row's second field, or null when the first is alone on its row.</summary>
    public WaitlistDetailTemplateField? Second { get; init; }

    /// <summary>
    /// How many of the row's two columns the first field occupies: <c>2</c> when it is alone on the row,
    /// <c>1</c> when a second field shares it.
    /// </summary>
    public int FirstSpan { get; init; }

    /// <summary>Whether the row carries a first field to draw.</summary>
    public bool HasFirst => First is not null;

    /// <summary>Whether the row carries a second field to draw.</summary>
    public bool HasSecond => Second is not null;

    /// <summary>
    /// Chunks already-ordered declared fields into two-per-row rows, deriving each row's span from that row's
    /// own field count. Pure, so the span rule is testable without building a page.
    /// </summary>
    /// <param name="declaredOrder">The declared fields, already in declared order.</param>
    /// <returns>One row per pair, in order; nothing at all when there are no fields.</returns>
    public static IReadOnlyList<WaitlistDetailFieldRow> RowsFor(IEnumerable<WaitlistDetailTemplateField>? declaredOrder)
    {
        var fields = (declaredOrder ?? Enumerable.Empty<WaitlistDetailTemplateField>()).ToList();
        if (fields.Count == 0)
        {
            return System.Array.Empty<WaitlistDetailFieldRow>();
        }

        var rows = new List<WaitlistDetailFieldRow>((fields.Count + 1) / 2);
        for (var index = 0; index < fields.Count; index += 2)
        {
            var hasSecond = index + 1 < fields.Count;
            rows.Add(new WaitlistDetailFieldRow
            {
                First = fields[index],
                Second = hasSecond ? fields[index + 1] : null,
                FirstSpan = hasSecond ? 1 : 2,
            });
        }

        return rows;
    }
}
