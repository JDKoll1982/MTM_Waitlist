using System.Runtime.InteropServices;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// A single labeled step shown in the shell header progress stepper while the
/// user is inside a multi-step workflow (Work Center Setup or New Request).
/// </summary>
public sealed partial class HeaderStep : ObservableObject
{
    [ObservableProperty]
    public partial string Label
    {
        get; set;
    } = string.Empty;

    [ObservableProperty]
    public partial HeaderStepState State
    {
        get; set;
    } = HeaderStepState.Pending;

    /// <summary>1-based step number rendered inside the step circle.</summary>
    [ObservableProperty]
    public partial int StepNumber
    {
        get; set;
    }

    /// <summary>Whether this is the first step (no left connector is drawn).</summary>
    [ObservableProperty]
    public partial bool IsFirst
    {
        get; set;
    }

    /// <summary>Whether this is the last step (no right connector is drawn).</summary>
    [ObservableProperty]
    public partial bool IsLast
    {
        get; set;
    }

    /// <summary>
    /// Whether the immediately preceding step is complete; drives the color of the
    /// connector line to the left of this step's circle.
    /// </summary>
    [ObservableProperty]
    public partial bool PreviousComplete
    {
        get; set;
    }

    /// <summary>
    /// Brush for the connector line to the left of the step circle. Blue once the
    /// previous step is complete, otherwise a neutral divider gray.
    /// </summary>
    public Brush LeftConnectorBrush => PreviousComplete
        ? ResolveThemeBrush("AccentFillColorDefaultBrush", "#FF0078D4")
        : ResolveThemeBrush("DividerStrokeColorDefaultBrush", "#FF9E9E9E");

    /// <summary>
    /// Brush for the connector line to the right of the step circle. Blue once this
    /// step is complete, otherwise a neutral divider gray.
    /// </summary>
    public Brush RightConnectorBrush => State == HeaderStepState.Complete
        ? ResolveThemeBrush("AccentFillColorDefaultBrush", "#FF0078D4")
        : ResolveThemeBrush("DividerStrokeColorDefaultBrush", "#FF9E9E9E");

    /// <summary>Visibility of the left connector line (hidden for the first step).</summary>
    public Visibility LeftConnectorVisibility => IsFirst ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Visibility of the right connector line (hidden for the last step).</summary>
    public Visibility RightConnectorVisibility => IsLast ? Visibility.Collapsed : Visibility.Visible;

    private static Brush ResolveThemeBrush(string resourceKey, string fallbackHex)
    {
        try
        {
            if (Application.Current?.Resources is { } resources
                && resources.TryGetValue(resourceKey, out var value)
                && value is Brush brush)
            {
                return brush;
            }
        }
        catch (COMException)
        {
            // Resource lookup can fail very early in startup; fall back to a fixed color.
        }

        return new SolidColorBrush(ColorFromHex(fallbackHex));
    }

    private static Color ColorFromHex(string hex)
    {
        var value = hex.TrimStart('#');
        return Color.FromArgb(
            byte.Parse(value[..2], System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
            byte.Parse(value.Substring(6, 2), System.Globalization.NumberStyles.HexNumber));
    }
}
