namespace MTM_Waitlist.Module_Startup.Options;

/// <summary>
/// The launch window's own size, so the surface a launch shows can be sized for a workstation without a code
/// change (checklist 8.2c).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every value has a usable default.</b> A missing section, a missing key or a number too small to draw the
/// surface leaves the defaults here in force rather than leaving the window unsized, because a launch window that
/// cannot be sized is still a window that can show a launch, and a sizing failure must never become a launch
/// failure.
/// </para>
/// <para>
/// <b>The default is the smallest the surface reads at.</b> What the window holds is the mark, the progress bar
/// with its count, the one line stating the work under way, the strip a stopped launch states its cause in and the
/// button bar, so the default is the shape those need rather than a dialog's shape. A workstation that wants a
/// larger launch surface states the two figures here.
/// </para>
/// </remarks>
public sealed class LaunchWindowOptions
{
    /// <summary>The settings section this type is bound from.</summary>
    public const string SectionName = "LaunchWindowOptions";

    /// <summary>
    /// The narrowest width this window will apply. A setting below it is treated as "no usable value", so a
    /// mistyped figure cannot produce a window too small to read.
    /// </summary>
    public const int MinimumUsableWidth = 360;

    /// <summary>
    /// The shortest height this window will apply. A setting below it is treated as "no usable value" for the
    /// same reason the width floor exists.
    /// </summary>
    public const int MinimumUsableHeight = 240;

    /// <summary>The width in device-independent pixels. The default is the smallest the surface reads at.</summary>
    public int Width { get; set; } = 460;

    /// <summary>The height in device-independent pixels. The default is the smallest the surface reads at.</summary>
    public int Height { get; set; } = 420;

    /// <summary>
    /// Whether both stated dimensions are large enough to draw the feed around.
    /// </summary>
    /// <remarks>
    /// The window asks this rather than comparing the two numbers itself, so the floor is stated once and a
    /// resize that is refused is refused for the same reason everywhere.
    /// </remarks>
    public bool IsUsable => Width >= MinimumUsableWidth && Height >= MinimumUsableHeight;
}
