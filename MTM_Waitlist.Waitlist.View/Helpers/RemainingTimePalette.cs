using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// One source of truth for how a request's remaining-time value is coloured, so the waitlist card and the
/// details page cannot drift apart.
/// </summary>
/// <remarks>
/// <para>
/// The thresholds mirror the card's long-standing scheme: overdue, or due within a quarter hour, is red; due
/// within half an hour is amber; anything further out is green. That rule reads the formatted text, which is
/// what the details page binds, and it stays for that reason.
/// </para>
/// <para>
/// The card's status column does not draw a countdown of its own: its ring carries the row's own fraction —
/// <c>SampleOrder.RingValue</c>, the share of the allotment the request has used — and the column is coloured
/// from that same number through <see cref="ColourForFraction"/>. <see cref="FractionFor"/> is the one place the
/// formatted text turns back into that fraction, so the ring, the text beside it and the colour of both are
/// reading one scale rather than three that have to be kept in step by hand (FR-013, FR-017).
/// </para>
/// <para>
/// Each picker answers a <see cref="Color"/> and is wrapped by a brush in the property or overload beside it:
/// a <c>SolidColorBrush</c> can only be built on a UI thread, so the decision is kept apart from the object that
/// carries it and stays readable without a window.
/// </para>
/// </remarks>
public static class RemainingTimePalette
{
    /// <summary>Red: overdue, or due within 15 minutes.</summary>
    public static Color OverdueColour => Colors.IndianRed;

    /// <summary>Amber: due within 30 minutes.</summary>
    public static Color NearDueColour => Colors.Goldenrod;

    /// <summary>Green: comfortably within the allotted time.</summary>
    public static Color ComfortableColour => Colors.MediumSeaGreen;

    /// <summary>Red, as a brush, for a surface that has to paint with it.</summary>
    public static Brush Overdue => new SolidColorBrush(OverdueColour);

    /// <summary>Amber, as a brush, for a surface that has to paint with it.</summary>
    public static Brush NearDue => new SolidColorBrush(NearDueColour);

    /// <summary>Green, as a brush, for a surface that has to paint with it.</summary>
    public static Brush Comfortable => new SolidColorBrush(ComfortableColour);

    /// <summary>
    /// The share of its allotment a request has used once it counts as nearly due — half of the window.
    /// </summary>
    /// <remarks>
    /// The ring's own scale, not a countdown: the ring fills from zero to one as the allotted time is used
    /// (FR-013), so the amber point is the fraction the ring has to have drawn to be amber.
    /// </remarks>
    public const double NearDueFraction = 0.5d;

    /// <summary>
    /// The share of its allotment a request has used once its whole window is gone — the point the ring is full
    /// and any further time is overrun (FR-014).
    /// </summary>
    public const double OverdueFraction = 1d;

    /// <summary>
    /// Picks the colour for a remaining-time value.
    /// </summary>
    /// <param name="remainingTimeText">The formatted value, for example "01:20", "Overdue", or "New".</param>
    /// <param name="isOverdue">The row's overdue flag; <see langword="true"/> always wins over the text.</param>
    /// <returns>The colour to paint the value in.</returns>
    public static Color ColourFor(string? remainingTimeText, bool isOverdue)
    {
        if (isOverdue || IsOverdueText(remainingTimeText))
        {
            return OverdueColour;
        }

        // "New" (no target time) and anything unparseable keep the comfortable colour.
        if (string.IsNullOrWhiteSpace(remainingTimeText)
            || !TimeSpan.TryParse(remainingTimeText, out var parsed))
        {
            return ComfortableColour;
        }

        var minutesRemaining = parsed.TotalMinutes;
        if (minutesRemaining <= 15)
        {
            return OverdueColour;
        }

        return minutesRemaining <= 30 ? NearDueColour : ComfortableColour;
    }

    /// <summary>
    /// Picks the brush for a remaining-time value, over <see cref="ColourFor"/>.
    /// </summary>
    /// <param name="remainingTimeText">The formatted value, for example "01:20", "Overdue", or "New".</param>
    /// <param name="isOverdue">The row's overdue flag; <see langword="true"/> always wins over the text.</param>
    /// <returns>The brush to paint the value with.</returns>
    public static Brush For(string? remainingTimeText, bool isOverdue) =>
        new SolidColorBrush(ColourFor(remainingTimeText, isOverdue));

    /// <summary>
    /// Picks the colour for the ring's own fraction — the same number the ring is filled with, so the colour and
    /// the arc can never describe different moments (FR-013, FR-017).
    /// </summary>
    /// <param name="usedFraction">
    /// The share of the allotment used, as drawn by the ring: zero at the start, one when the window is gone, and
    /// above one for the overrun an overdue request carries (FR-014).
    /// </param>
    /// <param name="isOverdue">The row's overdue flag; <see langword="true"/> always wins over the fraction.</param>
    /// <returns>The colour to paint the ring, and the value inside it, in.</returns>
    public static Color ColourForFraction(double usedFraction, bool isOverdue) =>
        isOverdue || usedFraction >= OverdueFraction
            ? OverdueColour
            : usedFraction >= NearDueFraction ? NearDueColour : ComfortableColour;

    /// <summary>
    /// Picks the brush for the ring's own fraction, over <see cref="ColourForFraction"/>.
    /// </summary>
    /// <param name="usedFraction">The share of the allotment used, as drawn by the ring.</param>
    /// <param name="isOverdue">The row's overdue flag; <see langword="true"/> always wins over the fraction.</param>
    /// <returns>The brush to paint the ring, and the value inside it, with.</returns>
    public static Brush ForFraction(double usedFraction, bool isOverdue) =>
        new SolidColorBrush(ColourForFraction(usedFraction, isOverdue));

    /// <summary>
    /// The ring fraction a formatted remaining-time value corresponds to.
    /// </summary>
    /// <remarks>
    /// The one conversion between the two ways the row states the same thing: the countdown it prints and the
    /// share of the allotment the ring draws. The formatted value carries no overrun, so an overdue value
    /// resolves to the whole allotment used and the ring's own number carries the rest; a value with no allotment
    /// to measure against has no fraction and reads as zero.
    /// </remarks>
    /// <param name="remainingTimeText">The formatted value, for example "01:20" or "Overdue".</param>
    /// <param name="allottedMinutes">The minutes the Item allots to the request.</param>
    /// <returns>The share of the allotment still to run, as a fraction of it.</returns>
    public static double FractionFor(string? remainingTimeText, int allottedMinutes)
    {
        if (allottedMinutes <= 0)
        {
            return 0d;
        }

        if (IsOverdueText(remainingTimeText))
        {
            return OverdueFraction;
        }

        if (!TimeSpan.TryParse(remainingTimeText, out var remaining))
        {
            return 0d;
        }

        var used = 1d - (remaining.TotalMinutes / allottedMinutes);
        return used > 0d ? used : 0d;
    }

    /// <summary>
    /// Whether the formatted value already says the request is overdue, which is the case for a row whose
    /// overdue flag is set.
    /// </summary>
    /// <param name="remainingTimeText">The formatted value.</param>
    /// <returns><see langword="true"/> when the value reads "Overdue".</returns>
    public static bool IsOverdueText(string? remainingTimeText) =>
        string.Equals(remainingTimeText?.Trim(), "Overdue", StringComparison.OrdinalIgnoreCase);
}
