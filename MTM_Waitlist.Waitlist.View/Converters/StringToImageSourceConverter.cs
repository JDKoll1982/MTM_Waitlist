using System.IO;

using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Converters;

/// <summary>
/// Turns a bare asset file name into a bitmap, or into the application's no-image placeholder when there is no
/// name to resolve or no picture at the end of it.
/// </summary>
public sealed class StringToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string fileName && !string.IsNullOrWhiteSpace(fileName)
            ? PictureSource.FromPath(Path.Combine("Assets", fileName))
            : PictureSource.FromPlaceholder();

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}