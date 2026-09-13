using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Paints the request card's status badge: one colour per lifecycle state, so the badge says where a request
/// is without the viewer having to read the label.
/// </summary>
/// <remarks>
/// Bind this to the row's stored <c>Status</c>. The stored value is used rather than the badge's display text
/// on purpose — copy can be reworded without silently changing a colour, and the row is rebuilt on every
/// refresh, so no change notification is needed for the brush to follow it.
/// <para>
/// Pass <c>Foreground</c> as the converter parameter for the text brush; anything else returns the background.
/// </para>
/// </remarks>
public sealed class RequestStatusToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        IsForegroundRequest(parameter)
            ? RequestStatusPalette.ForegroundBrush()
            : RequestStatusPalette.BackgroundBrushFor(value as string);

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static bool IsForegroundRequest(object? parameter) =>
        parameter is string text && text.Trim().Equals("Foreground", StringComparison.OrdinalIgnoreCase);
}
