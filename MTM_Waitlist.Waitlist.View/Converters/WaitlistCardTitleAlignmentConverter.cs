using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Waitlist.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Where the detail zone's title pair sits, given the number of cells the row filled (FR-024, FR-026,
/// FR-027).
/// </summary>
/// <remarks>
/// Bind the row's <c>Cells.Count</c> to the zone's <c>VerticalAlignment</c>. A row that filled no cells
/// resolved no data, so the title pair is the only thing the zone has to draw and it is centred in the card
/// rather than left at the top of a full-height zone, which reads as a card whose contents failed to load;
/// a row with data of its own keeps its title at the top, over its cells. The rule is
/// <see cref="WaitlistCardCellLayout.TitleIsCentred" />, the same place the panel's shape is decided, so the
/// two cannot disagree about what an empty row is.
/// </remarks>
public sealed class WaitlistCardTitleAlignmentConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language) =>
        WaitlistCardCellLayout.TitleIsCentred(WaitlistCardCellLayout.CellCountFrom(value))
            ? VerticalAlignment.Center
            : VerticalAlignment.Top;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
