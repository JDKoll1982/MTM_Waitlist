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
    /// The three collection folders a picture is kept under, one per collection: the application's own pictures,
    /// the Infor Visual parts, and the WIP floor parts.
    /// </summary>
    /// <remarks>
    /// The names are spelled once, in <see cref="PartPictureLayout"/>, so this class and the layout cannot
    /// disagree about them.
    /// </remarks>
    public static readonly IReadOnlyList<string> CollectionFolderNames =
    [
        PartPictureLayout.WaitlistCollection,
        PartPictureLayout.VisualCollection,
        PartPictureLayout.WipCollection,
    ];

    /// <summary>
    /// The three kind folders inside the application's own collection, one per scope: the request items, the
    /// category families and the work centres.
    /// </summary>
    /// <remarks>
    /// They exist so no two kinds can resolve to one file. The item <c>other</c> and the category <c>Other</c>
    /// differ only by letter case and a file name cannot tell them apart, so neither is renamed and each keeps its
    /// own folder. The names are the scope values themselves, kept verbatim.
    /// </remarks>
    public static readonly IReadOnlyList<string> ApplicationOwnKindFolderNames =
    [
        "work_center",
        "request_item",
        "request_category",
    ];

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
    /// rooted path, which is how a picture was written before the layout changed. A relative value in either the
    /// pre-move or the post-move shape is answered: see <see cref="ToCurrentLayout"/>.
    /// </param>
    /// <returns>The path to read, or <see langword="null"/> when there is nothing to resolve.</returns>
    /// <remarks>
    /// Reading both layouts is what lets the recorded-path move happen in any order and be interrupted without
    /// leaving a picture unresolvable: the files and the rows move at different moments, so a reader that
    /// understood only one shape would answer null in the gap between them.
    /// </remarks>
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

        return Path.Combine(NormalizeSeparators(root), ToCurrentLayout(relative));
    }

    /// <summary>
    /// The same recorded value, expressed in the layout the share uses now.
    /// </summary>
    /// <param name="storedPath">A recorded relative path, in either layout.</param>
    /// <returns>
    /// The path under the collection folder the value belongs to. A value that already names a collection is
    /// returned unchanged, a pre-move value that names one of the application's three kinds gains the
    /// <c>Waitlist</c> collection in front of it, and a value this method does not recognise is left exactly where
    /// it is rather than being guessed at.
    /// </returns>
    public static string ToCurrentLayout(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return string.Empty;
        }

        var normalized = NormalizeSeparators(storedPath);
        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0 || IsCollectionFolder(segments[0]))
        {
            return normalized;
        }

        var collection = PartPictureLayout.CollectionFolderFor(segments[0]);

        // An unrecognised leading folder is left alone: moving a file the move does not understand is how a
        // picture ends up somewhere no reader looks.
        return collection is null
            ? normalized
            : Path.Combine(collection, normalized);
    }

    /// <summary>Whether a folder name is one of the three collection folders.</summary>
    /// <param name="folderName">The leading folder of a recorded path.</param>
    /// <returns><see langword="true"/> for <c>Waitlist</c>, <c>Visual</c> or <c>WIP</c>.</returns>
    public static bool IsCollectionFolder(string? folderName) =>
        !string.IsNullOrWhiteSpace(folderName)
        && CollectionFolderNames.Contains(folderName.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Joins a folder and a file name the way every path in this application is compared: separators unified, so a
    /// path an operator typed with forward slashes matches one built with backslashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizeSeparators(string path) =>
        path.Trim().Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
}
