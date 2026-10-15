using System.Collections.Generic;
using System.Linq;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One resolved cell of a card's detail grid: the label a person reads on top, and the value beneath it
/// (<c>data-model.md</c> §7).
/// </summary>
/// <remarks>
/// <para>
/// It is a distinct model from <c>WaitlistField</c>, which the request detail page uses: this one is the card's
/// cell, and it carries the declared order the grid fills in.
/// </para>
/// <para>
/// <b>A cell with no value is never added at all.</b> <see cref="CellsToDraw"/> is the one place a card's cells
/// are collected, and it drops every cell that resolved nothing — so a card is shorter rather than padded with
/// an empty cell, no empty cell is drawn, and the panel's geometry follows the number of cells that remain
/// (FR-006, FR-018, FR-026).
/// </para>
/// </remarks>
public sealed class WaitlistLineCardCell
{
    /// <summary>
    /// The label as a localized resource key resolved at bind time, never a literal (FR-104). It is empty on the
    /// one cell that has no label at all — the die-shop cell, whose phrase is drawn as the value with nothing
    /// above it (FR-028, FR-055) — and that is why <see cref="IsLabelLess"/> is true for that cell and no other.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// The value the cell shows, or empty when nothing resolved for it. For the label-less die-shop cell this is
    /// the phrase itself (`Take To Die Shop` on a pickup, `Located In Die Shop` on a deliver), resolved from its
    /// own resource before it lands here (FR-055, FR-104).
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Whether anything resolved for this cell. False means the cell is not one the card draws.
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Whether the cell draws without a label above it. True for the die-shop cell only, and by construction: it
    /// is the one cell produced with no label, because its phrase IS its value (FR-028, FR-055).
    /// </summary>
    public bool IsLabelLess => string.IsNullOrWhiteSpace(Label);

    /// <summary>
    /// The declared order of the field the cell was produced for, so the grid fills in declaration order
    /// (FR-026).
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// The part whose reference picture this cell opens, or empty for the many cells that open none
    /// (FR-078). On the hopper Items it is the size word itself, which is the key that size's reference
    /// picture is filed under.
    /// </summary>
    public string ReferencePartNumber { get; init; } = string.Empty;

    /// <summary>
    /// The picture scope <see cref="ReferencePartNumber"/> is read from, or empty when the cell opens no
    /// picture. Handed in rather than assumed, so the cell stays plain data that knows no storage root.
    /// </summary>
    public string ReferencePartScope { get; init; } = string.Empty;

    /// <summary>
    /// The reference picture's resolved path, filled by the list before this cell reached the bound row.
    /// Empty when nothing resolved, which draws the application's one shared placeholder rather than a blank
    /// (FR-080).
    /// </summary>
    public string ResolvedReferenceImagePath { get; set; } = string.Empty;

    /// <summary>Whether this cell opens a reference picture at all, whatever the picture resolved to.</summary>
    public bool OpensReferencePicture =>
        !string.IsNullOrWhiteSpace(ReferencePartNumber) && !string.IsNullOrWhiteSpace(ReferencePartScope);

    /// <summary>
    /// The picture the cell's value draws: the hopper's reference image, and the application's one shared
    /// no-image placeholder when the size's picture is missing or unusable (FR-078, FR-080). The converter
    /// applies the picture rule as well, so a path naming an unreadable file lands on the placeholder too.
    /// </summary>
    public string EffectiveReferenceImagePath =>
        string.IsNullOrWhiteSpace(ResolvedReferenceImagePath)
            ? ImagePicturePolicy.NoImagePath
            : ResolvedReferenceImagePath;

    /// <summary>
    /// The cells a card draws, in declared order, with every candidate that resolved no value left out entirely
    /// (FR-006, FR-018). Pure, so the rule is testable without building a page.
    /// </summary>
    /// <param name="candidates">The cells the row resolved, in any order. Nulls are ignored.</param>
    /// <returns>One cell per resolved field, in declared order; nothing at all when nothing resolved.</returns>
    public static IReadOnlyList<WaitlistLineCardCell> CellsToDraw(IEnumerable<WaitlistLineCardCell?>? candidates)
    {
        if (candidates is null)
        {
            return System.Array.Empty<WaitlistLineCardCell>();
        }

        return candidates
            .Where(cell => cell is not null && cell.HasValue)
            .Select(cell => cell!)
            .OrderBy(cell => cell.Order)
            .ToArray();
    }
}
