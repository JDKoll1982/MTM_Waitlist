using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace MTM_Waitlist.Module_Settings.Converters;

/// <summary>
/// Paints the role badge: the colour the badge lookup answers for a role, so a role's appearance is decided in
/// one place and the view only carries it (FR-105).
/// </summary>
/// <remarks>
/// The value bound here is <c>RoleBadgeCatalog</c>'s <c>#AARRGGBB</c> string rather than a brush, because the
/// colour belongs to the lookup and a brush built during a load would outlive the theme it was built for. An
/// unreadable value is answered with no brush at all, which leaves the element inheriting its foreground rather
/// than painting it an arbitrary colour.
/// </remarks>
public sealed class HexColorToBrushConverter : IValueConverter
{
    private const int HexLength = 8;

    /// <inheritdoc />
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var hex = (value as string)?.Trim().TrimStart('#');

        return hex is { Length: HexLength } && IsHex(hex)
            ? new SolidColorBrush(Color.FromArgb(
                Parse(hex, 0),
                Parse(hex, 2),
                Parse(hex, 4),
                Parse(hex, 6)))
            : null;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static bool IsHex(string text) =>
        text.All(character => Uri.IsHexDigit(character));

    private static byte Parse(string text, int offset) =>
        byte.Parse(text.Substring(offset, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
}
