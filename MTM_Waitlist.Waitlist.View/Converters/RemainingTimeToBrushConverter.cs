using Microsoft.UI.Xaml.Data;
using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Paints a request's remaining-time value: red once overdue, amber when it is nearly due, green otherwise.
/// </summary>
/// <remarks>
/// Bind this to the formatted text, not to the row, so the brush follows the row's change notifications — the
/// overdue case is already encoded in the text ("Overdue"), which is what lets a single string drive it.
/// </remarks>
public sealed class RemainingTimeToBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        RemainingTimePalette.For(value as string, isOverdue: false);

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
