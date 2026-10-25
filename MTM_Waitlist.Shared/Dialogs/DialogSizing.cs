namespace MTM_Waitlist.Module_Shared.Dialogs;

/// <summary>
/// The one sizing scale every dialog wears (014-adopt-lab-design-system, FR-004).
/// </summary>
/// <remarks>
/// <para>
/// The values live in <c>Styles/Dialogs.xaml</c>; the constants here are the fallbacks used when a dictionary has not
/// been merged yet, which happens in a unit test and in the first moments of start-up. Both sides carry the same
/// numbers, and the sizing audit checks that the dictionary still holds them.
/// </para>
/// <para>
/// The figures are set by the smallest supported screen — a 15-inch display at 150% scaling, roughly 1280 by 720
/// effective pixels. A 720-wide dialog leaves the window that owns it visible on both sides, and a 640-tall body fits
/// under a title and above the buttons inside that window.
/// </para>
/// </remarks>
public static class DialogSizing
{
    /// <summary>Still readable on a small screen.</summary>
    public const double MinWidthFallback = 480;

    /// <summary>The width a dialog opens at, leaving its owner visible on both sides.</summary>
    public const double PreferredWidthFallback = 720;

    /// <summary>Never outgrows the 1080 page column.</summary>
    public const double MaxWidthFallback = 880;

    /// <summary>Fits inside a 720-tall window with a title above and buttons below.</summary>
    public const double MaxBodyHeightFallback = 640;

    /// <summary>One line plus padding never collapses to nothing.</summary>
    public const double BodyMinHeightFallback = 120;

    /// <summary>The resource key for <see cref="MinWidthFallback"/>.</summary>
    public const string MinWidthKey = "DialogMinWidth";

    /// <summary>The resource key for <see cref="PreferredWidthFallback"/>.</summary>
    public const string PreferredWidthKey = "DialogPreferredWidth";

    /// <summary>The resource key for <see cref="MaxWidthFallback"/>.</summary>
    public const string MaxWidthKey = "DialogMaxWidth";

    /// <summary>The resource key for <see cref="MaxBodyHeightFallback"/>.</summary>
    public const string MaxBodyHeightKey = "DialogMaxBodyHeight";

    /// <summary>The resource key for <see cref="BodyMinHeightFallback"/>.</summary>
    public const string BodyMinHeightKey = "DialogBodyMinHeight";

    /// <summary>The declared minimum width, or the fallback when the dictionary is not loaded.</summary>
    public static double MinWidth => Read(MinWidthKey, MinWidthFallback);

    /// <summary>The declared opening width, or the fallback when the dictionary is not loaded.</summary>
    public static double PreferredWidth => Read(PreferredWidthKey, PreferredWidthFallback);

    /// <summary>The declared maximum width, or the fallback when the dictionary is not loaded.</summary>
    public static double MaxWidth => Read(MaxWidthKey, MaxWidthFallback);

    /// <summary>The declared maximum body height, or the fallback when the dictionary is not loaded.</summary>
    public static double MaxBodyHeight => Read(MaxBodyHeightKey, MaxBodyHeightFallback);

    /// <summary>The declared minimum body height, or the fallback when the dictionary is not loaded.</summary>
    public static double BodyMinHeight => Read(BodyMinHeightKey, BodyMinHeightFallback);

    /// <summary>
    /// Reads a declared size, falling back when there is no application, no dictionary, no entry, or an entry of the
    /// wrong type. A dialog must never fail to open because a resource is missing.
    /// </summary>
    /// <param name="key">The resource key to read.</param>
    /// <param name="fallback">The value to use when the key does not resolve to a number.</param>
    public static double Read(string key, double fallback)
    {
        try
        {
            if (Microsoft.UI.Xaml.Application.Current?.Resources is { } resources
                && resources.TryGetValue(key, out var value)
                && value is double number
                && number > 0)
            {
                return number;
            }
        }
        catch (Exception)
        {
            // A missing or half-built resource dictionary is not a reason to refuse to open a dialog.
        }

        return fallback;
    }
}
