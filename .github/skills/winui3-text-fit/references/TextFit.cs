using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace WinUI3.TextFit;

/// <summary>
/// Applies <see cref="ShrinkToFit"/> to a live <see cref="TextBlock"/>. The arithmetic lives in
/// <see cref="ShrinkToFit"/> so it can be tested; this only does the WinUI part — measure, ask for
/// the next size, repeat.
/// </summary>
public static class TextFit
{
    /// <summary>
    /// Reduces <paramref name="text"/>'s FontSize until it fits <paramref name="availableWidth"/>
    /// effective pixels, or until the floor is reached. Returns the size it settled on.
    /// </summary>
    public static double ShrinkToWidth(this TextBlock text, double availableWidth, double minFontSize = ShrinkToFit.DefaultMinFontSize)
    {
        if (text is null || !double.IsFinite(availableWidth) || availableWidth <= 0)
        {
            return text?.FontSize ?? 0;
        }

        // The proportional estimate is close but text metrics are not linear, so re-measure a bounded
        // number of times rather than trusting one pass.
        const int maxPasses = 8;
        for (var pass = 0; pass < maxPasses; pass++)
        {
            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var measured = text.DesiredSize.Width;

            if (!ShrinkToFit.NeedsShrinking(measured, availableWidth, text.FontSize, minFontSize))
            {
                break;
            }

            var next = ShrinkToFit.NextFontSize(measured, availableWidth, text.FontSize, minFontSize);
            if (next >= text.FontSize)
            {
                break;
            }

            text.FontSize = next;
        }

        text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return text.FontSize;
    }
}
