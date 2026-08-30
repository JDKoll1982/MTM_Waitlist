using System.IO;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MTM_Waitlist.Module_Setup.Converters;

public sealed class SetupImagePathToImageSourceConverter : IValueConverter
{
    private static readonly ImageSource FallbackImage = new BitmapImage(new Uri("ms-appx:///Assets/coil.png"));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string rawPath || string.IsNullOrWhiteSpace(rawPath))
        {
            return FallbackImage;
        }

        var trimmedPath = rawPath.Trim();

        try
        {
            if (Uri.TryCreate(trimmedPath, UriKind.Absolute, out var absoluteUri))
            {
                return new BitmapImage(absoluteUri);
            }

            // UNC paths (for example \\server\share\image.png) need absolute URI conversion.
            if (trimmedPath.StartsWith("\\\\", StringComparison.Ordinal))
            {
                return new BitmapImage(new Uri(trimmedPath, UriKind.Absolute));
            }

            var normalized = trimmedPath.Replace('\\', '/').TrimStart('/');
            if (normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                return new BitmapImage(new Uri($"ms-appx:///{normalized}"));
            }

            var fileName = Path.GetFileName(normalized);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return new BitmapImage(new Uri($"ms-appx:///Assets/{fileName}"));
            }
        }
        catch
        {
            return FallbackImage;
        }

        return FallbackImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}