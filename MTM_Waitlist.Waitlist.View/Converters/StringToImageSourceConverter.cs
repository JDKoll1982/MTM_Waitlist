using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MTM_Waitlist.Module_Waitlist.Converters;

public sealed class StringToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string fileName || string.IsNullOrWhiteSpace(fileName))
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/coil.png"));
        }

        return new BitmapImage(new Uri($"ms-appx:///Assets/{fileName}"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}