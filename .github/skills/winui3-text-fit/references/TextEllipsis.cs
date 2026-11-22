namespace WinUI3.TextFit;

/// <summary>
/// Middle ellipsis for identifiers. A part number's start and end both carry meaning — the family
/// prefix and the revision suffix — so end-trimming (what <c>TextTrimming</c> does) throws away the
/// half a reader needs. Pure BCL, so it is testable without a XAML host.
/// </summary>
public static class TextEllipsis
{
    /// <summary>
    /// Returns <paramref name="text"/> with the middle replaced by "..." so the result is at most
    /// <paramref name="maxChars"/> characters and keeps both ends.
    /// </summary>
    public static string Middle(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        if (maxChars <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxChars)
        {
            return text;
        }

        if (maxChars < 3)
        {
            return text[..maxChars];
        }

        if (maxChars == 3)
        {
            return "...";
        }

        var sides = maxChars - 3;
        var left = sides / 2 + (sides % 2);
        var right = sides - left;

        return string.Concat(text.AsSpan(0, left), "...", text.AsSpan(text.Length - right));
    }
}
