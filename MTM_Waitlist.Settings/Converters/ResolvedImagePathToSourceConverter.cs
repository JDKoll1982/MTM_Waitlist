using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MTM_Waitlist.Module_Settings.Converters;

/// <summary>
/// Turns a resolved image path into a bitmap. Handles absolute and UNC paths from the shared
/// folder as well as app-relative asset paths, and falls back to the packaged default when the
/// file cannot be reached so a broken share never blanks the preview.
/// </summary>
public sealed class ResolvedImagePathToSourceConverter : IValueConverter
{
    private const string FallbackAsset = "ms-appx:///Assets/Images/default-request-type.png";

    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var path = value as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return Fallback();
        }

        try
        {
            if (Path.IsPathRooted(path))
            {
                return File.Exists(path)
                    ? new BitmapImage(new Uri(path))
                    : Fallback();
            }

            var appRelative = Path.Combine(AppContext.BaseDirectory, path);
            if (File.Exists(appRelative))
            {
                return new BitmapImage(new Uri(appRelative));
            }

            var packaged = path.Replace('\\', '/').TrimStart('/');
            return new BitmapImage(new Uri($"ms-appx:///{packaged}"));
        }
        catch (UriFormatException)
        {
            return Fallback();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static BitmapImage Fallback() => new(new Uri(FallbackAsset));
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
