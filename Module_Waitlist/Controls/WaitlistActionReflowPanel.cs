using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Waitlist.View.Helpers;

using Windows.Foundation;

namespace MTM_Waitlist.Module_Waitlist.Controls;

/// <summary>
/// Lays a card's action buttons out in the three fixed columns <see cref="WaitlistActionZoneLayout" /> decides,
/// filling left to right and top to bottom, with the buttons that are not drawn skipped rather than leaving a
/// hole (FR-061, FR-063).
/// </summary>
/// <remarks>
/// The rule itself - how many columns there are, which place a drawn button takes and how many rows that needs -
/// is in the view library, where it can be checked without a window. This panel is the part that has to run
/// inside the application: it asks that rule for each button's place and arranges them accordingly.
/// </remarks>
public sealed class WaitlistActionReflowPanel : Panel
{
    /// <summary>The number of columns the zone keeps, whatever is drawn in it (FR-061).</summary>
    public const int ColumnCount = WaitlistActionZoneLayout.ColumnCount;

    /// <summary>The gap between two columns, and between two rows.</summary>
    private const double Gap = 6;

    protected override Size MeasureOverride(Size availableSize)
    {
        var (itemWidth, itemHeight, drawn) = MeasureChildren();

        var rows = WaitlistActionZoneLayout.RowCountFor(drawn);
        return new Size(
            (ColumnCount * itemWidth) + ((ColumnCount - 1) * Gap),
            rows == 0 ? 0 : (rows * itemHeight) + ((rows - 1) * Gap));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (_, itemHeight, _) = MeasureChildren();

        // The columns share the final width rather than each keeping its measured one, so a button given a
        // larger minimum than its neighbours still leaves the columns even.
        var columnWidth = Math.Max(0, (finalSize.Width - ((ColumnCount - 1) * Gap)) / ColumnCount);

        var index = 0;
        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Visible)
            {
                continue;
            }

            var (row, column) = WaitlistActionZoneLayout.SlotFor(index);

            child.Arrange(new Rect(
                column * (columnWidth + Gap),
                row * (itemHeight + Gap),
                columnWidth,
                itemHeight));

            index++;
        }

        return finalSize;
    }

    /// <summary>
    /// The largest drawn button, and how many there are. A button that is not drawn is measured anyway — its
    /// own size is still wanted the moment it becomes visible again — but it never takes a slot.
    /// </summary>
    private (double Width, double Height, int Drawn) MeasureChildren()
    {
        var width = 0d;
        var height = 0d;
        var drawn = 0;

        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (child.Visibility != Visibility.Visible)
            {
                continue;
            }

            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
            drawn++;
        }

        return (width, height, drawn);
    }
}
