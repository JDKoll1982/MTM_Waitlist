using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// One source of truth for how a request's remaining-time value is coloured, so the waitlist card and the
/// details page cannot drift apart.
/// </summary>
/// <remarks>
/// The thresholds mirror the card's long-standing scheme: overdue, or due within a quarter hour, is red; due
/// within half an hour is amber; anything further out is green. Brushes are created per call rather than
/// cached in a static field because XAML objects carry thread affinity, so a shared instance would bind the
/// palette to whichever thread happened to touch it first.
/// </remarks>
public static class RemainingTimePalette
{
    /// <summary>Red: overdue, or due within 15 minutes.</summary>
    public static Brush Overdue => new SolidColorBrush(Colors.IndianRed);

    /// <summary>Amber: due within 30 minutes.</summary>
    public static Brush NearDue => new SolidColorBrush(Colors.Goldenrod);

    /// <summary>Green: comfortably within the allotted time.</summary>
    public static Brush Comfortable => new SolidColorBrush(Colors.MediumSeaGreen);

    /// <summary>
    /// Picks the brush for a remaining-time value.
    /// </summary>
    /// <param name="remainingTimeText">The formatted value, for example "01:20", "Overdue", or "New".</param>
    /// <param name="isOverdue">The row's overdue flag; <see langword="true"/> always wins over the text.</param>
    /// <returns>The brush to paint the value with.</returns>
    public static Brush For(string? remainingTimeText, bool isOverdue)
    {
        if (isOverdue || IsOverdueText(remainingTimeText))
        {
            return Overdue;
        }

        // "New" (no target time) and anything unparseable keep the comfortable colour.
        if (string.IsNullOrWhiteSpace(remainingTimeText)
            || !TimeSpan.TryParse(remainingTimeText, out var parsed))
        {
            return Comfortable;
        }

        var minutesRemaining = parsed.TotalMinutes;
        if (minutesRemaining <= 15)
        {
            return Overdue;
        }

        return minutesRemaining <= 30 ? NearDue : Comfortable;
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
