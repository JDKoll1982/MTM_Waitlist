using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Whether a detail panel slot draws anything, given the number of cells the row filled and the slot the
/// binding names (FR-006, FR-026, FR-103).
/// </summary>
/// <remarks>
/// Bind the row's <c>Cells.Count</c> to a slot's <c>Visibility</c>, naming the slot in the converter
/// parameter. A slot with no cell behind it collapses, so a short panel is shorter rather than padded, and
/// the panel's rows follow the number of cells the same way for every request of the same Item.
/// </remarks>
public sealed class WaitlistCardCellVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        WaitlistCardCellLayout.IsDrawn(
            WaitlistCardCellLayout.SlotFrom(parameter),
            WaitlistCardCellLayout.CellCountFrom(value))
                ? Visibility.Visible
                : Visibility.Collapsed;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
