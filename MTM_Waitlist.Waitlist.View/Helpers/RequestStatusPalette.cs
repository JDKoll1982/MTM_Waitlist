using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The lifecycle states a request's status badge can represent. One role per stored status, so the colour a
/// viewer sees is decided in one place rather than in markup.
/// </summary>
public enum RequestStatusBadgeRole
{
    /// <summary>The stored status is missing or unrecognised, so no badge is warranted.</summary>
    None,

    /// <summary>Waiting — queued, nobody has claimed it yet.</summary>
    Waiting,

    /// <summary>In Progress — a handler has claimed it.</summary>
    InProgress,

    /// <summary>Done — finished.</summary>
    Done,

    /// <summary>Cancelled — withdrawn or refused, and kept rather than deleted.</summary>
    Cancelled,
}

/// <summary>
/// One source of truth for the colours of the request card's status badge, so the badge reads the same on the
/// list, in the detail page's header and anywhere else it is drawn.
/// </summary>
/// <remarks>
/// <para>
/// The badge used to be a single accent-blue pill for every state, which meant the one element whose whole job
/// is to say <em>where a request is</em> said nothing at all — a viewer had to read the label. Each role now
/// has its own colour, and the four are deliberately far apart in hue as well as in lightness so they stay
/// distinguishable to somebody who cannot tell the greens and reds apart: a mid grey, a blue, a green and a
/// red, in that order of progress.
/// </para>
/// <para>
/// The colours are the accessible Fluent values rather than the light <c>SystemFillColor*</c> theme tokens.
/// Those tokens are built to carry a <em>dark</em> glyph on top (that is how an <c>InfoBar</c> uses them), so
/// white text on them fails contrast — <c>SystemFillColorCaution</c> is <c>#FCE100</c>, on which white is
/// unreadable. These values all clear 4.5:1 against white, which is what the badge's text needs.
/// </para>
/// <para>
/// Brushes are created per call rather than cached in a static field because XAML objects carry thread
/// affinity, so a shared instance would bind the palette to whichever thread happened to touch it first.
/// </para>
/// </remarks>
public static class RequestStatusPalette
{
    /// <summary>Mid grey: queued. Neutral on purpose — waiting is the resting state, not a problem.</summary>
    public static Color WaitingBackground => Color.FromArgb(255, 95, 102, 112);

    /// <summary>Blue: claimed and being worked.</summary>
    public static Color InProgressBackground => Color.FromArgb(255, 15, 108, 189);

    /// <summary>Green: finished.</summary>
    public static Color DoneBackground => Color.FromArgb(255, 16, 124, 16);

    /// <summary>Red: withdrawn or refused.</summary>
    public static Color CancelledBackground => Color.FromArgb(255, 196, 43, 28);

    /// <summary>The badge's text colour. Every background above clears 4.5:1 against it.</summary>
    public static Color BadgeForeground => Colors.White;

    /// <summary>
    /// Maps a stored status to the badge role it should be painted with.
    /// </summary>
    /// <param name="status">
    /// The stored lifecycle status — <c>Pending</c>, <c>Accepted</c>, <c>Completed</c> or <c>Canceled</c>.
    /// Both spellings of cancelled are accepted because the store holds one and English copy uses the other.
    /// </param>
    /// <returns>The role to paint, or <see cref="RequestStatusBadgeRole.None"/> when there is no recognised status.</returns>
    public static RequestStatusBadgeRole RoleFor(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "pending" => RequestStatusBadgeRole.Waiting,
            "accepted" => RequestStatusBadgeRole.InProgress,
            "completed" => RequestStatusBadgeRole.Done,
            "canceled" or "cancelled" => RequestStatusBadgeRole.Cancelled,
            _ => RequestStatusBadgeRole.None,
        };

    /// <summary>
    /// The badge's background for a stored status.
    /// </summary>
    /// <param name="status">The stored lifecycle status.</param>
    /// <returns>The background colour; transparent for an unrecognised status, which draws no badge anyway.</returns>
    public static Color BackgroundFor(string? status) => RoleFor(status) switch
    {
        RequestStatusBadgeRole.Waiting => WaitingBackground,
        RequestStatusBadgeRole.InProgress => InProgressBackground,
        RequestStatusBadgeRole.Done => DoneBackground,
        RequestStatusBadgeRole.Cancelled => CancelledBackground,
        _ => Colors.Transparent,
    };

    /// <summary>The badge's background brush for a stored status.</summary>
    /// <param name="status">The stored lifecycle status.</param>
    /// <returns>A new brush, created per call.</returns>
    public static Brush BackgroundBrushFor(string? status) => new SolidColorBrush(BackgroundFor(status));

    /// <summary>The badge's text brush. One colour, because every background is chosen to carry it.</summary>
    /// <returns>A new brush, created per call.</returns>
    public static Brush ForegroundBrush() => new SolidColorBrush(BadgeForeground);
}
