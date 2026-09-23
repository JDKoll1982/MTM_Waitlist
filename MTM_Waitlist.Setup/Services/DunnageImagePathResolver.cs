using System.IO;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Setup.Services;

/// <summary>
/// Resolves Dunnage image files referenced by DB-relative paths (for example
/// <c>Parts/Steel Rack.png</c>, <c>Types/DunnageType-Boxes.png</c>) to absolute file
/// paths under the shared Dunnage image root — the same root the MTM Receiving
/// Application writes under its <c>Dunnage.Application.DefaultImageLocation</c> setting
/// (on this site that share is reached as <c>X:</c>; it is named here as <c>\\mtmanu-fs01\Expo Drive</c> so the
/// root works on a machine that has no drive mapping).
/// </summary>
public static class DunnageImagePathResolver
{
    private const string EnvironmentVariable = "MTM_DUNNAGE_IMAGE_ROOT";

    /// <summary>
    /// Default shared root for Dunnage images. Matches the MTM Receiving Application's
    /// configured <c>Dunnage.Application.DefaultImageLocation</c> and the waitlist
    /// <c>DunnageImageOptions:RootFolder</c> appsettings value.
    /// </summary>
    /// <remarks>
    /// Written as the share's own name rather than as the <c>X:</c> drive letter that reaches it on this site
    /// (<c>X:</c> is <c>\\mtmanu-fs01\Expo Drive</c>): a drive letter is only meaningful on a machine that has the
    /// mapping, and this root has to be readable from every workstation.
    /// </remarks>
    public const string DefaultRootFolder = @"\\mtmanu-fs01\Expo Drive\Software Development\Live Applications\Shared\Images\Dunnage";

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
            return File.Exists(normalized) ? PreferCachedCopy(normalized) : null;
        }

        var combined = Path.Combine(RootFolder, normalized);
        return File.Exists(combined) ? PreferCachedCopy(combined) : null;
    }

    /// <summary>
    /// The copy of a Dunnage picture on this computer, when one has been cached, so a card reads a local file
    /// instead of reaching across the network for every paint. The share remains the fallback, so a picture this
    /// computer has never managed to copy still shows.
    /// </summary>
    /// <param name="resolvedPath">The picture's path on the share.</param>
    /// <returns>The cached copy's path when there is one, otherwise the share's.</returns>
    private static string PreferCachedCopy(string resolvedPath) =>
        ImageCachePaths.TryGetCachedCopy(RootFolder, ImageCachePaths.ResolveDunnageCacheFolder(), resolvedPath)
        ?? resolvedPath;
}
