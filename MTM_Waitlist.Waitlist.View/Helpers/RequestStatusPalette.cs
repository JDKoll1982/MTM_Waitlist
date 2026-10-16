using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Waitlist.Models;

using Windows.UI;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// One source of truth for the colours of the request card's status column, so the column reads the same on the
/// list, in the detail page's header and anywhere else it is drawn.
/// </summary>
/// <remarks>
/// <para>
/// The column used to be a single accent-blue pill for every state, which meant the one element whose whole job
/// is to say <em>where a request is</em> said nothing at all — a viewer had to read the label. Every state now
/// has a colour of its own, and the four lifecycle ones are deliberately far apart in hue as well as in
/// lightness so they stay distinguishable to somebody who cannot tell the greens and reds apart: a mid grey, a
/// blue, a green and a red, in that order of progress.
/// </para>
/// <para>
/// The column itself carries one of <b>three</b> themes rather than nine colours (FR-019, FR-072): the neutral
/// one for a lifecycle word, a <em>lighter red</em> for a condition a handler reported, and a deep red for an
/// overdue request. The two reds are the point — a condition must never be mistaken for an overdue request, and
/// the overdue red must never be mistaken for anything else, which is what keeps an overdue row its own state
/// under every sort however far down the list it sits (FR-020, SC-007). The overdue red is deliberately the
/// darkest thing the column ever draws, so the two reds separate by lightness as well as by hue.
/// </para>
/// <para>
/// <b>Every column colour is chosen as text on the card, and there is one per surface.</b> The three themes were
/// first tuned when the column was a coloured pill carrying white text, so two of them were picked to clear
/// 4.5:1 <em>against white</em>. The pill is gone (FR-008) and the word is now the <em>foreground</em>, which
/// inverts the requirement: the handler-condition red read at 2.7:1 as text on the light card, and the overdue
/// red at 2.4:1 on the dark one. Both were real states nobody could read, on the two surfaces the application
/// ships. Each theme therefore carries a value for each surface, and each value clears 4.5:1 against
/// <see cref="LightCardBackground"/> or <see cref="DarkCardBackground"/>. Both pairs keep the one invariant that
/// matters — the condition red is the lighter red and the overdue red the darker one, on either surface.
/// </para>
/// <para>
/// The colours are the accessible Fluent values rather than the light <c>SystemFillColor*</c> theme tokens.
/// Those tokens are built to carry a <em>dark</em> glyph on top (that is how an <c>InfoBar</c> uses them), so
/// white text on them fails contrast — <c>SystemFillColorCaution</c> is <c>#FCE100</c>, on which white is
/// unreadable.
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

    /// <summary>
    /// The <c>normal</c> column theme: the one colour a column whose word is a lifecycle word is drawn in
    /// (FR-019).
    /// </summary>
    /// <remarks>
    /// The same neutral the <see cref="Waiting"/> role carries, deliberately: a lifecycle word is the resting
    /// state of the column, so waiting, claimed, finished and withdrawn are told apart by the word rather than
    /// by four separate treatments that a reader would have to learn.
    /// </remarks>
    public static Color NormalColumnColor => WaitingBackground;

    /// <summary>
    /// The <c>handler-condition</c> column theme: a lighter red than the overdue theme, shown for as long as a
    /// material handler has reported a condition (FR-019, FR-072).
    /// </summary>
    /// <remarks>
    /// Lighter than the overdue red in fact and not only in name — <c>RequestStatusPaletteTests</c> holds the two
    /// to that, because a condition mistaken for an overdue request, or the reverse, is the one confusion this
    /// pair exists to prevent (SC-007). Dark enough to read as text on a light card, which the lighter of the two
    /// themes was not: it was chosen to carry white text and read at 2.7:1 as a foreground.
    /// </remarks>
    public static Color HandlerConditionColumnColor => Color.FromArgb(255, 192, 57, 43);

    /// <summary>
    /// The <c>overdue</c> column theme: the deep red an overdue request is drawn in, and the darkest colour the
    /// column ever carries (FR-014, FR-019).
    /// </summary>
    /// <remarks>
    /// Darker than the condition red so the two separate by lightness as well as by hue, and distinct from the
    /// normal theme, which is what lets an overdue request stay visibly its own state wherever it sorts
    /// (FR-020, SC-007).
    /// </remarks>
    public static Color OverdueColumnColor => Color.FromArgb(255, 176, 36, 24);

    /// <summary>
    /// The <c>normal</c> theme on the dark surface: the same neutral, lightened until it reads as text on the
    /// dark card.
    /// </summary>
    public static Color NormalColumnColorOnDark => Color.FromArgb(255, 185, 193, 204);

    /// <summary>
    /// The <c>handler-condition</c> theme on the dark surface. Still lighter than the overdue theme there, which
    /// is the invariant the two reds exist for.
    /// </summary>
    public static Color HandlerConditionColumnColorOnDark => Color.FromArgb(255, 255, 158, 138);

    /// <summary>
    /// The <c>overdue</c> theme on the dark surface: still the darker of the two reds, but light enough to be
    /// read as the status word on a dark card.
    /// </summary>
    public static Color OverdueColumnColorOnDark => Color.FromArgb(255, 255, 107, 90);

    /// <summary>The badge's text colour. Every background above clears 4.5:1 against it.</summary>
    public static Color BadgeForeground => Colors.White;

    /// <summary>
    /// The card's own fill on the light surface, which is what the column's colours are read against.
    /// </summary>
    /// <remarks>
    /// The Fluent <c>LayerFillColorDefault</c> value over the light window background, written out because
    /// contrast arithmetic needs a background and a unit test has no theme to resolve a theme resource from. The
    /// column's six colours are held to 4.5:1 against these two by <c>RequestStatusPaletteTests</c>, so a later
    /// change to any of them is measured rather than eyeballed.
    /// </remarks>
    public static Color LightCardBackground => Color.FromArgb(255, 243, 243, 243);

    /// <summary>The card's own fill on the dark surface, the counterpart of <see cref="LightCardBackground"/>.</summary>
    public static Color DarkCardBackground => Color.FromArgb(255, 30, 30, 30);

    /// <summary>
    /// Maps a status to the role it should be painted with.
    /// </summary>
    /// <param name="status">
    /// Either a stored lifecycle status (<c>Pending</c>, <c>Accepted</c>, <c>Completed</c>, <c>Canceled</c> —
    /// both spellings of cancelled are accepted, because the store holds one and English copy uses the other) or
    /// one of the words the column draws: the four lifecycle words, the overdue marking, or one of the four
    /// conditions (FR-012, FR-014, FR-102).
    /// </param>
    /// <returns>The role to paint, or <see cref="RequestStatusBadgeRole.None"/> when there is no recognised status.</returns>
    /// <remarks>
    /// The conditions are matched ordinally and exactly through <see cref="HandlerStatusCatalog"/>, so a
    /// differently cased, padded or otherwise altered value is not a condition and falls through to the overdue
    /// marking or the lifecycle word rather than claiming a report nobody made. The lifecycle words are matched
    /// case-insensitively like the stored statuses, because copy may be cased either way.
    /// </remarks>
    public static RequestStatusBadgeRole RoleFor(string? status)
    {
        if (HandlerStatusCatalog.IsCondition(status))
        {
            return RoleForCondition(status!);
        }

        return status?.Trim().ToLowerInvariant() switch
        {
            "pending" or "waiting" => RequestStatusBadgeRole.Waiting,
            "accepted" or "in progress" => RequestStatusBadgeRole.InProgress,
            "completed" or "done" => RequestStatusBadgeRole.Done,
            "canceled" or "cancelled" => RequestStatusBadgeRole.Cancelled,
            "overdue" => RequestStatusBadgeRole.Overdue,
            _ => RequestStatusBadgeRole.None,
        };
    }

    /// <summary>
    /// The role one of the four conditions is painted with.
    /// </summary>
    /// <param name="condition">One of the four verbatim condition strings.</param>
    /// <returns>The condition's own role, or <see cref="RequestStatusBadgeRole.None"/> when the value is not one of the four.</returns>
    public static RequestStatusBadgeRole RoleForCondition(string? condition) =>
        condition switch
        {
            "Product Buried" => RequestStatusBadgeRole.ProductBuried,
            "Cant Find Product" => RequestStatusBadgeRole.CantFindProduct,
            "Changing Forklift" => RequestStatusBadgeRole.ChangingForklift,
            "Changing Propane" => RequestStatusBadgeRole.ChangingPropane,
            _ => RequestStatusBadgeRole.None,
        };

    /// <summary>
    /// The colour a role is drawn in — the state's own colour, which for a condition is the lighter red of the
    /// handler-condition theme and for the overdue marking is the overdue theme's red (FR-019, FR-072).
    /// </summary>
    /// <param name="role">The role to paint.</param>
    /// <returns>The colour, transparent for <see cref="RequestStatusBadgeRole.None"/>, which draws nothing.</returns>
    public static Color ColorFor(RequestStatusBadgeRole role) => role switch
    {
        RequestStatusBadgeRole.Waiting => WaitingBackground,
        RequestStatusBadgeRole.InProgress => InProgressBackground,
        RequestStatusBadgeRole.Done => DoneBackground,
        RequestStatusBadgeRole.Cancelled => CancelledBackground,
        RequestStatusBadgeRole.Overdue => OverdueColumnColor,
        RequestStatusBadgeRole.ProductBuried
            or RequestStatusBadgeRole.CantFindProduct
            or RequestStatusBadgeRole.ChangingForklift
            or RequestStatusBadgeRole.ChangingPropane => HandlerConditionColumnColor,
        _ => Colors.Transparent,
    };

    /// <summary>
    /// The colour a status is drawn in.
    /// </summary>
    /// <param name="status">A stored lifecycle status, or one of the words the column draws.</param>
    /// <returns>The colour; transparent for a status the palette does not know, which draws nothing.</returns>
    public static Color BackgroundFor(string? status) => ColorFor(RoleFor(status));

    /// <summary>The brush a role is drawn in.</summary>
    /// <param name="role">The role to paint.</param>
    /// <returns>A new brush, created per call.</returns>
    public static Brush BrushFor(RequestStatusBadgeRole role) => new SolidColorBrush(ColorFor(role));

    /// <summary>The brush a status is drawn in.</summary>
    /// <param name="status">A stored lifecycle status, or one of the words the column draws.</param>
    /// <returns>A new brush, created per call.</returns>
    public static Brush BackgroundBrushFor(string? status) => BrushFor(RoleFor(status));

    /// <summary>The badge's text brush. One colour, because every background is chosen to carry it.</summary>
    /// <returns>A new brush, created per call.</returns>
    public static Brush ForegroundBrush() => new SolidColorBrush(BadgeForeground);

    /// <summary>
    /// The colour one of the three column themes is drawn in, for the surface it is actually on.
    /// </summary>
    /// <param name="theme">The column theme the row is in.</param>
    /// <param name="isDarkSurface">Whether the card the column is drawn on is the dark one.</param>
    /// <returns>The colour to paint the status word and the ring in.</returns>
    /// <remarks>
    /// The surface is passed in rather than read from the application, because the theme is chosen per person and
    /// applied to the shell's own element (<c>ThemeSelectorService</c>), so the application's requested theme is
    /// not the theme a given card is drawn in. The card knows its own actual theme, so it is the one that answers
    /// this.
    /// </remarks>
    public static Color ColumnColorFor(StatusColumnTheme theme, bool isDarkSurface) => theme switch
    {
        StatusColumnTheme.HandlerCondition => isDarkSurface
            ? HandlerConditionColumnColorOnDark
            : HandlerConditionColumnColor,
        StatusColumnTheme.Overdue => isDarkSurface
            ? OverdueColumnColorOnDark
            : OverdueColumnColor,
        _ => isDarkSurface
            ? NormalColumnColorOnDark
            : NormalColumnColor,
    };

    /// <summary>
    /// The brush one of the three column themes is drawn in, for the surface it is actually on.
    /// </summary>
    /// <param name="theme">The column theme the row is in.</param>
    /// <param name="isDarkSurface">Whether the card the column is drawn on is the dark one.</param>
    /// <returns>A new brush, created per call.</returns>
    public static Brush ColumnBrushFor(StatusColumnTheme theme, bool isDarkSurface) =>
        new SolidColorBrush(ColumnColorFor(theme, isDarkSurface));
}
