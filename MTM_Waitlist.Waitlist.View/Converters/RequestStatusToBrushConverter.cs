using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

using Windows.UI;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Paints the request card's status column: the column theme for the row's state, so the column says where a
/// request is without the viewer having to read the label.
/// </summary>
/// <remarks>
/// <para>
/// Bind this to the row's <see cref="SampleOrder.StatusTheme"/> with <c>StatusColumnTheme</c> as the converter
/// parameter for the whole column's brush — that brush colours the status word and the time ring together, so
/// the two can never disagree about which of the three themes the row is in (FR-019, FR-072). The stored status
/// is used rather than the displayed word wherever a status string is handed in, on purpose: copy can be
/// reworded without silently changing a colour, and the row is rebuilt on every refresh, so no change
/// notification is needed for the brush to follow it.
/// </para>
/// <para>
/// Pass <c>Foreground</c> as the converter parameter for the text brush; anything else resolves a brush for the
/// value itself, which may be a role, a theme, or a status word or stored status.
/// </para>
/// <para>
/// The decision of <em>which</em> colour a state is drawn in is <see cref="ColourFor"/>, which answers the
/// colour alone. <see cref="Convert"/> is the one-line seam WinUI needs on top of it: a
/// <c>SolidColorBrush</c> can only be built on a UI thread, so the decision is kept apart from the object that
/// carries it and can be read without a window.
/// </para>
/// </remarks>
public sealed class RequestStatusToBrushConverter : IValueConverter
{
    /// <summary>The converter parameter that asks for the palette's text brush.</summary>
    private const string ForegroundParameter = "Foreground";

    /// <summary>The converter parameter that asks for the whole status column's theme brush.</summary>
    private const string StatusColumnThemeParameter = "StatusColumnTheme";

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        new SolidColorBrush(ColourFor(value, parameter));

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    /// <summary>
    /// The colour a value is drawn in, and the whole of this converter's decision.
    /// </summary>
    /// <param name="value">
    /// The value handed to the converter: a <see cref="StatusColumnTheme"/>, a
    /// <see cref="RequestStatusBadgeRole"/>, or a status — a stored lifecycle status or one of the words the
    /// column draws.
    /// </param>
    /// <param name="parameter">
    /// <c>Foreground</c> for the palette's text colour, <c>StatusColumnTheme</c> for the whole column's theme
    /// colour, or anything else for the value's own colour.
    /// </param>
    /// <returns>
    /// The colour to paint, or <see cref="Colors.Transparent"/> when the value is not a state the column knows,
    /// which draws nothing rather than an empty shape (FR-018).
    /// </returns>
    /// <remarks>
    /// Internal rather than public because it exists for the same reason the palette's own colours are public
    /// values: the colour a viewer sees has to be readable without a UI thread, and a brush cannot be built
    /// without one.
    /// </remarks>
    internal static Color ColourFor(object? value, object? parameter) =>
        IsParameter(parameter, ForegroundParameter)
            ? RequestStatusPalette.BadgeForeground
            : IsParameter(parameter, StatusColumnThemeParameter)
                ? ColumnColour(value)
                : TryReadTheme(value, out var theme) ? ThemeColour(theme) : StatusColour(value);

    /// <summary>
    /// The colour the whole status column is drawn in.
    /// </summary>
    /// <remarks>
    /// The column's theme is taken from whatever the card hands over — the theme itself, its name, or, when a
    /// caller has only the row's state to hand, the role or the status it can derive a theme from. A theme the
    /// palette answers nothing for is what leaves a status word and a ring nobody can see, so every one of the
    /// three themes resolves a real colour here (FR-019, FR-072).
    /// </remarks>
    private static Color ColumnColour(object? value) =>
        TryReadTheme(value, out var theme) ? ThemeColour(theme) : StatusColour(value);

    /// <summary>
    /// The column's theme colour, from the palette (FR-019).
    /// </summary>
    private static Color ThemeColour(StatusColumnTheme theme) => theme switch
    {
        StatusColumnTheme.HandlerCondition => RequestStatusPalette.HandlerConditionColumnColor,
        StatusColumnTheme.Overdue => RequestStatusPalette.OverdueColumnColor,
        _ => RequestStatusPalette.NormalColumnColor,
    };

    /// <summary>
    /// The colour for a state, whether it arrives as a role or as the status it belongs to. A condition takes the
    /// lighter red and the overdue marking the deep one, through the same palette the themes are read from
    /// (FR-072).
    /// </summary>
    private static Color StatusColour(object? value) => value switch
    {
        RequestStatusBadgeRole role => RequestStatusPalette.ColorFor(role),
        string text => RequestStatusPalette.BackgroundFor(text),
        _ => Colors.Transparent,
    };

    /// <summary>
    /// Reads a status column theme from the value, whether it arrives as the enum or as its name.
    /// </summary>
    /// <param name="value">The value handed to the converter.</param>
    /// <param name="theme">The theme read, or <see cref="StatusColumnTheme.Normal"/> when there was none.</param>
    /// <returns><see langword="true"/> when the value carried a theme.</returns>
    private static bool TryReadTheme(object? value, out StatusColumnTheme theme)
    {
        if (value is StatusColumnTheme direct)
        {
            theme = direct;
            return true;
        }

        if (value is string name && Enum.TryParse(name, ignoreCase: true, out theme))
        {
            return true;
        }

        theme = StatusColumnTheme.Normal;
        return false;
    }

    private static bool IsParameter(object? parameter, string expected) =>
        parameter is string text && text.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase);
}
