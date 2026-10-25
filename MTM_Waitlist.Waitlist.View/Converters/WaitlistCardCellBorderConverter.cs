using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// The hairline a detail cell draws, given the panel slot the cell was laid out in (FR-028).
/// </summary>
/// <remarks>
/// A cell closes itself off from the cell above it and the cell to its left, and from nothing else: the first
/// row and the first column draw no hairline at all, so the cell area has no border around its outside edge and
/// the only lines a person sees are the separators between cells. Binding this to a cell's
/// <c>BorderThickness</c> is what keeps that rule in one place instead of an offset per slot in every Item's
/// template.
/// </remarks>
public sealed class WaitlistCardCellBorderConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is WaitlistLineCardCell cell
            ? new Thickness(
                cell.DrawsLeftHairline ? 1 : 0,
                cell.DrawsTopHairline ? 1 : 0,
                0,
                0)
            : new Thickness(0);

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
