using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The colours a lugger's status is drawn in: one lookup, so the card, the badge and any later surface cannot
/// paint the same status two different ways (FR-012, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// The values are <c>#AARRGGBB</c> strings rather than brushes, because a brush built during a load outlives the
/// theme it was built for; the view hands each one to the shared hex converter at paint time, which is the same
/// arrangement the role badge uses.
/// </para>
/// <para>
/// Every background below carries white text at the size the card draws it, so the badge is readable in both
/// themes: the four are the deep tones of the platform's success, neutral, caution and critical families rather
/// than their pale fills.
/// </para>
/// </remarks>
public static class LuggerStatusBadge
{
    /// <summary>The text colour over every status background, and the one the palette is chosen against.</summary>
    public const string Foreground = "#FFFFFFFF";

    /// <summary>The colour for a lugger that can take material now.</summary>
    public const string Available = "#FF107C10";

    /// <summary>The colour for a lugger that waits behind another.</summary>
    public const string Backup = "#FF5D5A58";

    /// <summary>The colour for a lugger that is full.</summary>
    public const string Full = "#FFC77700";

    /// <summary>The colour for a lugger a lead has taken out of use.</summary>
    public const string DoNotUse = "#FFC42B1C";

    /// <summary>The colour for a status this build does not know, so an unknown one still draws a badge.</summary>
    public const string Unknown = "#FF5D5A58";

    /// <summary>The background a status is drawn on, or the unknown colour for one this build does not know.</summary>
    /// <param name="status">One of the four <see cref="LuggerStatus"/> constants.</param>
    public static string BackgroundFor(string? status) => status?.Trim() switch
    {
        LuggerStatus.Available => Available,
        LuggerStatus.Backup => Backup,
        LuggerStatus.Full => Full,
        LuggerStatus.DoNotUse => DoNotUse,
        _ => Unknown,
    };
}
