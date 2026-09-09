using System.IO;

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MTM_Waitlist.Module_Setup.Services;

/// <summary>
/// Resolves Dunnage image files referenced by DB-relative paths (for example
/// <c>Parts/Steel Rack.png</c>, <c>Types/DunnageType-Boxes.png</c>) to absolute file
/// paths under the shared Dunnage image root — the same root the MTM Receiving
/// Application writes under its <c>Dunnage.Application.DefaultImageLocation</c> setting
/// (verified live: <c>X:\Software Development\Live Applications\Shared\Images\Dunnage</c>).
/// </summary>
public static class DunnageImagePathResolver
{
    private const string EnvironmentVariable = "MTM_DUNNAGE_IMAGE_ROOT";

    /// <summary>
    /// Default shared root for Dunnage images. Matches the MTM Receiving Application's
    /// configured <c>Dunnage.Application.DefaultImageLocation</c> and the waitlist
    /// <c>DunnageImageOptions:RootFolder</c> appsettings value.
    /// </summary>
    public const string DefaultRootFolder = @"X:\Software Development\Live Applications\Shared\Images\Dunnage";

    private static string? _configuredRootFolder;

    /// <summary>Effective Dunnage image root (env override &gt; configured &gt; default).</summary>
    public static string RootFolder
    {
        get
        {
            var environment = Environment.GetEnvironmentVariable(EnvironmentVariable)?.Trim();
            if (!string.IsNullOrWhiteSpace(environment))
            {
                return environment;
            }

            return string.IsNullOrWhiteSpace(_configuredRootFolder)
                ? DefaultRootFolder
                : _configuredRootFolder!;
        }
    }

    /// <summary>Overrides the root from configuration (appsettings <c>DunnageImageOptions:RootFolder</c>).</summary>
    public static void ConfigureRootFolder(string? rootFolder)
    {
        _configuredRootFolder = string.IsNullOrWhiteSpace(rootFolder) ? null : rootFolder.Trim();
    }

    /// <summary>
    /// Returns the absolute file path for a DB-relative Dunnage image path, or <c>null</c>
    /// when the path is blank or the file does not exist on disk.
    /// </summary>
    public static string? GetDisplayPath(string? relativeImagePath)
    {
        if (string.IsNullOrWhiteSpace(relativeImagePath))
        {
            return null;
        }

        var normalized = relativeImagePath.Trim().Replace('/', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(normalized))
        {
            return File.Exists(normalized) ? normalized : null;
        }

        var combined = Path.Combine(RootFolder, normalized);
        return File.Exists(combined) ? combined : null;
    }

    /// <summary>
    /// Creates an <see cref="ImageSource"/> for a DB-relative Dunnage image path, or
    /// <c>null</c> when the file is not present on disk.
    /// </summary>
    public static ImageSource? CreateImageSource(string? relativeImagePath)
    {
        var displayPath = GetDisplayPath(relativeImagePath);
        if (string.IsNullOrWhiteSpace(displayPath))
        {
            return null;
        }

        try
        {
            return new BitmapImage(new Uri(displayPath));
        }
        catch
        {
            return null;
        }
    }
}
