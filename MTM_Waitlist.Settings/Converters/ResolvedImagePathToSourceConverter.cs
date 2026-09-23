using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Converters;

/// <summary>
/// Turns a resolved image path into a bitmap the settings and waitlist surfaces can draw.
/// </summary>
/// <remarks>
///   <para>
///   This converter used to police its own fallbacks, and the branches disagreed: a missing relative path fell
///   through to a package URI that silently resolved to nothing, and the fallback it did have named a file —
///   <c>Assets/Placeholders/default-request-type.png</c> — that has never existed in this repository. Either way
///   the tile went blank rather than saying "no picture".
///   </para>
///   <para>
///   The whole decision now lives in <see cref="PictureSource"/>: absolute, UNC, application-relative and
///   package-relative paths are all resolved to a file that is checked with the application's picture rule, and
///   anything that is not a picture resolves to <see cref="ImagePicturePolicy.NoImagePath"/>.
///   </para>
/// </remarks>
public sealed class ResolvedImagePathToSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        PictureSource.FromPath(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Collapses an element when the bound boolean is false.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is bool b && b;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility visibility && visibility == Visibility.Visible;
}
