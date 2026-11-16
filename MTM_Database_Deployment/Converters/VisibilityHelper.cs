using Microsoft.UI.Xaml;

namespace MTM_Database_Deployment.Converters;

/// <summary>
/// x:Bind function helpers for visibility. A converter cannot be used here: the binding root of this
/// window is a <see cref="Microsoft.UI.Xaml.Window"/>, which is not a framework element, so the
/// generated code cannot build a converter lookup root.
/// </summary>
public static class Show
{
    public static Visibility If(bool flag) => flag ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility IfNot(bool flag) => flag ? Visibility.Collapsed : Visibility.Visible;
}