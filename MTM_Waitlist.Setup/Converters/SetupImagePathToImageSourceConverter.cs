using System.IO;

using Microsoft.UI.Xaml.Data;

using MTM_Waitlist.Module_Setup.Services;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Setup.Converters;

/// <summary>
/// Turns a dunnage image path into a bitmap the setup surfaces can draw, and never into nothing.
/// </summary>
/// <remarks>
/// <para>
/// The fallback used to be <c>Assets/coil.png</c> — a photograph of a coil standing in for "this part has no
/// picture", which reads as a picture of something rather than as an absence. It is now the application's one
/// no-image placeholder, the same one every other surface draws.
/// </para>
/// <para>
/// The last-resort branch used to guess <c>Assets/{fileName}</c> from a DB-relative path such as
/// <c>Parts/Steel Rack.png</c>, which produced <c>Assets/Steel Rack.png</c> and, when that file did not exist, a
/// bitmap that silently failed to load. A path that reaches the fallback now draws the placeholder instead.
/// </para>
/// </remarks>
public sealed class SetupImagePathToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string rawPath || string.IsNullOrWhiteSpace(rawPath))
        {
            return PictureSource.FromPlaceholder();
        }

        var trimmedPath = rawPath.Trim();
        var normalized = trimmedPath.Replace('\\', '/').TrimStart('/');

        // An explicit application-relative asset is taken as it stands.
        if (normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            return PictureSource.FromPath(normalized);
        }

        // Dunnage part/type images are stored on the shared Dunnage image root and referenced by DB-relative
        // paths such as "Parts/Steel Rack.png" or "Types/DunnageType-Boxes.png". Absolute and UNC paths from the
        // receiving store are resolved by the same call, which answers null when there is no file to show.
        var dunnagePath = DunnageImagePathResolver.GetDisplayPath(trimmedPath);
        if (dunnagePath is not null)
        {
            return PictureSource.FromPath(dunnagePath);
        }

        // A value carrying a separator is a dunnage reference, and it has already been tried against the dunnage
        // root above: there is no package asset to guess at, so this is the "no picture" answer.
        if (trimmedPath.Contains('/') || trimmedPath.Contains('\\'))
        {
            return PictureSource.FromPlaceholder();
        }

        var fileName = Path.GetFileName(normalized);
        return string.IsNullOrWhiteSpace(fileName)
            ? PictureSource.FromPlaceholder()
            : PictureSource.FromPath(Path.Combine("Assets", fileName));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}