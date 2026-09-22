namespace MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// The canonical locations the application reads and writes outside its own folder: where a configured picture
/// lives, and where the key files live.
/// </summary>
/// <remarks>
/// <para>
/// Both are stored as application-wide settings that IT Department and Developer may change, so the values here
/// are the <em>defaults</em> a machine uses before anybody changes them, and the fallback when the store cannot be
/// reached. Nothing else in the application spells these paths.
/// </para>
/// <para>
/// A picture is stored <b>relative</b> to <see cref="ImagesRootDefault"/> as
/// <c>{catagory}\{image}{extension}</c> — for example <c>request_item\pickup-coil.png</c>. Relative is what makes
/// the root changeable and what makes two machines agree: an absolute path written under one mapping is unreadable
/// under another, and moving the root would orphan every row. A path that is <em>not</em> relative is still read,
/// because rows written before that change name the file itself.
/// </para>
/// </remarks>
public static class AppStoragePaths
{
    /// <summary>
    /// The root every configured picture lives under, one folder per scope beneath it.
    /// </summary>
    /// <remarks>
    /// A uniform naming convention that reaches the same file on every machine: <c>X:</c> on this site is
    /// <c>\\mtmanu-fs01\Expo Drive</c>, and a drive letter is only meaningful on a machine that has the mapping.
    /// </remarks>
    public const string ImagesRootDefault =
        @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\MTM Waitlist Application\Images";

    /// <summary>
    /// The folder holding the key files, one per key, named <c>{keyname}.txt</c>.
    /// </summary>
    /// <remarks>
    /// Nothing reads these files yet: the application takes its secrets from <c>appsettings.json</c> and the
    /// environment. The folder is a setting now so that the first feature which needs a key reads it from one
    /// agreed place rather than inventing a second one.
    /// </remarks>
    public const string KeysFolderDefault =
        @"\\mtmanu-fs01\Expo Drive\MH_RESOURCE\Material_Handler\MTM Applications\Keys - DO NOT EDIT FILES\MTM Waitlist Application";

    /// <summary>The sub-folder beside the pictures that a replaced picture is archived into.</summary>
    public const string ArchiveFolderName = "Archive";

    /// <summary>
    /// The name a key is read from: <c>{keyname}.txt</c>, written out so a reader added later does not have to
    /// invent the convention.
    /// </summary>
    /// <param name="keyName">The key's name, without an extension.</param>
    /// <returns>The file name the key lives in.</returns>
    public static string KeyFileName(string keyName) => $"{keyName}.txt";

    /// <summary>
    /// Resolves a stored picture path against the picture root.
    /// </summary>
    /// <param name="root">The configured picture root. Ignored for a rooted <paramref name="storedPath"/>.</param>
    /// <param name="storedPath">
    /// What the store holds: a path relative to <paramref name="root"/> — how a picture is written now — or a
    /// rooted path, which is how a picture was written before the layout changed.
    /// </param>
    /// <returns>The path to read, or <see langword="null"/> when there is nothing to resolve.</returns>
    public static string? ResolvePicturePath(string? root, string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        var relative = NormalizeSeparators(storedPath);

        // A row written before pictures became relative already names the file, so it is taken as it stands: the
        // share it points at is the share an operator chose, and re-basing it on today's root would silently move
        // their picture to a file that is not there.
        if (Path.IsPathRooted(relative))
        {
            return relative;
        }

        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        return Path.Combine(NormalizeSeparators(root), relative);
    }

    /// <summary>
    /// Joins a folder and a file name the way every path in this application is compared: separators unified, so a
    /// path an operator typed with forward slashes matches one built with backslashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizeSeparators(string path) =>
        path.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
}
