namespace WinUI3.TextFit;

/// <summary>
/// The arithmetic behind shrink-to-fit labels. WinUI 3 has no auto-shrink text, and the obvious
/// loop — subtract half a point, re-measure, repeat — costs a measure pass per step. Estimating
/// proportionally reaches the answer in one or two passes. Text width is not perfectly linear in
/// font size, so the caller still re-measures and repeats; this type only decides the next size.
/// Kept free of WinUI types so the rule can be tested without a XAML host.
/// </summary>
public static class ShrinkToFit
{
    /// <summary>Below this a label is illegible; wrapping or ellipsis is the better answer.</summary>
    public const double DefaultMinFontSize = 8;

    /// <summary>
    /// The font size to try next, given the width the text wanted at <paramref name="currentFontSize"/>.
    /// Returns <paramref name="currentFontSize"/> when the text already fits or is at the floor, so a
    /// caller loop terminates.
    /// </summary>
    public static double NextFontSize(
        double measuredWidth,
        double availableWidth,
        double currentFontSize,
        double minFontSize = DefaultMinFontSize)
    {
        if (!double.IsFinite(measuredWidth) || !double.IsFinite(availableWidth) || !double.IsFinite(currentFontSize))
        {
            return currentFontSize;
        }

        if (!double.IsFinite(minFontSize) || minFontSize < 0)
        {
            minFontSize = 0;
        }

        if (availableWidth <= 0 || currentFontSize <= minFontSize || measuredWidth <= availableWidth)
        {
            return currentFontSize;
        }

        var exact = currentFontSize * (availableWidth / measuredWidth);

        // Half-point granularity, always at least one step down: a 0.3% overflow that rounds back to
        // the current size would spin the caller's loop forever.
        var stepped = Math.Floor(exact * 2) / 2;
        var candidate = Math.Min(currentFontSize - 0.5, stepped);

        return Math.Max(minFontSize, candidate);
    }

    /// <summary>True when the text is wider than its slot and still has room above the floor.</summary>
    public static bool NeedsShrinking(
        double measuredWidth,
        double availableWidth,
        double currentFontSize,
        double minFontSize = DefaultMinFontSize)
        => double.IsFinite(measuredWidth)
           && double.IsFinite(availableWidth)
           && availableWidth > 0
           && measuredWidth > availableWidth
           && currentFontSize > minFontSize;
}
