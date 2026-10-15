using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// How many columns a detail panel slot spans, given the number of cells the row filled and the slot the
/// binding names (FR-026, FR-027).
/// </summary>
/// <remarks>
/// Bind the row's <c>Cells.Count</c> to a slot's <c>Grid.ColumnSpan</c>, naming the slot in the converter
/// parameter. A one-cell panel has its cell span the full width and a three-cell panel has its third cell span
/// the bottom row; every other slot keeps its own column, which leaves a four-cell panel as two rows of two.
/// </remarks>
public sealed class WaitlistCardCellSpanConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        WaitlistCardCellLayout.ColumnSpanFor(
            WaitlistCardCellLayout.SlotFrom(parameter),
            WaitlistCardCellLayout.CellCountFrom(value));

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
