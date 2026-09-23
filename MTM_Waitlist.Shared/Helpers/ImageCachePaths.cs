namespace MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// Where the local picture cache lives, what may go in it, and the one rule that decides whether a picture has to
/// be copied again.
/// </summary>
/// <remarks>
/// <para>
/// The application reads its pictures from the share, and a share can be slow, busy or briefly absent. The cache
/// is the local copy every screen draws from, refreshed at startup. This is the same arrangement the MTM Receiving
/// Application uses for Dunnage pictures
/// (<c>Module_Dunnage/Helpers/Helper_DunnageImagePaths.cs</c> and <c>Services/Service_DunnageImageStorage.cs</c>),
/// narrowed to the two sources this application reads: the waitlist picture root and the Dunnage image root.
/// </para>
/// <para>
/// Both sources keep their own folder under <see cref="LocalCacheRoot"/>, so the same file name in the two trees
/// cannot collide, and so one source being unreachable cannot disturb the other's copies.
/// </para>
/// </remarks>
public static class ImageCachePaths
{
    /// <summary>The folder under the application's local data that holds every cached picture.</summary>
    public const string CacheFolderName = "ImageCache";

    /// <summary>The cache folder holding the waitlist picture tree.</summary>
    public const string WaitlistFolderName = "waitlist";

    /// <summary>The cache folder holding the Dunnage picture tree.</summary>
    public const string DunnageFolderName = "dunnage";

    /// <summary>
    /// The extensions a picture can have. The cache carries only these, exactly as the image screens accept only
    /// these, so a stray file on the share is neither copied nor counted as something to keep.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedExtensions = [".png", ".jpg", ".jpeg"];

    /// <summary>
    /// A sub-folder the application owns for its own working files. It is never mirrored in the other direction:
    /// a working file left behind must not be mistaken for a picture, and must not be deleted as an orphan.
    /// </summary>
    public const string TempFolderName = "Temp";

    /// <summary>
    /// The cache folder this application uses when nobody has chosen another one:
    /// <c>%LOCALAPPDATA%\MTM_Waitlist\ImageCache</c>.
    /// </summary>
    /// <remarks>
    /// Per-user local application data rather than a folder beside the executable: the install folder is not
    /// writable for every account that runs the app, and an application update must not take the cache with it.
    /// </remarks>
    public static string DefaultCacheRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MTM_Waitlist",
        CacheFolderName);

    private static string? _configuredCacheRoot;

    /// <summary>
    /// The root of the local cache: the configured folder when one has been set, and
    /// <see cref="DefaultCacheRoot"/> when none has.
    /// </summary>
    public static string LocalCacheRoot =>
        string.IsNullOrWhiteSpace(_configuredCacheRoot) ? DefaultCacheRoot : _configuredCacheRoot!;

    /// <summary>
    /// Adopts the configured cache folder, so every path this class builds agrees with the setting the IT
    /// Department or a Developer chose.
    /// </summary>
    /// <param name="configuredCacheRoot">The configured folder, or null or blank for the default.</param>
    /// <remarks>
    /// <para>
    /// Configured in one place at startup, before anything reads the cache, which is the same arrangement the
    /// Dunnage image root uses (<c>DunnageImagePathResolver.ConfigureRootFolder</c>).
    /// </para>
    /// <para>
    /// The configured value is expanded and then required to be absolute. A folder written as
    /// <c>%LOCALAPPDATA%\MTM_Waitlist\ImageCache</c> is a <b>relative</b> path as far as the file system is
    /// concerned, so taking it literally creates a folder <i>named</i> <c>%LOCALAPPDATA%</c> wherever the
    /// application happened to be started from — the cache lands beside the executable instead of in the
    /// account's own local application data, and the store's shared setting is what promises the latter. That is
    /// not hypothetical: it is how 156 cached pictures came to be committed to this repository under
    /// <c>%LOCALAPPDATA%/MTM_Waitlist/ImageCache</c>.
    /// </para>
    /// </remarks>
    public static void SetCacheRoot(string? configuredCacheRoot) =>
        _configuredCacheRoot = string.IsNullOrWhiteSpace(configuredCacheRoot)
            ? null
            : ExpandConfiguredCacheRoot(configuredCacheRoot);

    /// <summary>
    /// Expands the environment variables in a configured cache folder, and answers the shipped default when what
    /// is left is not an absolute folder.
    /// </summary>
    /// <param name="configuredCacheRoot">The configured folder as it was written.</param>
    /// <returns>An absolute folder, or <see cref="DefaultCacheRoot"/>.</returns>
    private static string ExpandConfiguredCacheRoot(string configuredCacheRoot)
    {
        var expanded = Environment.ExpandEnvironmentVariables(configuredCacheRoot.Trim());

        // A variable that could not be resolved leaves its percent signs behind, and anything not rooted gets
        // resolved against the working directory — which is the defect this guards against. Both take the default:
        // an unexpected folder in the right place beats a correct-looking folder beside the executable.
        return expanded.Contains('%') || Path.IsPathFullyQualified(expanded) is false
            ? DefaultCacheRoot
            : expanded;
    }

    /// <summary>
    /// The places the MTM Receiving Application's own Dunnage cache can be, in the order they are looked in.
    /// </summary>
    /// <remarks>
    /// That application caches Dunnage pictures into <c>{its application folder}\Assets\DunnageImages</c> and never
    /// lets the folder be configured (<c>SetLocalCacheRootFolder</c> is declared and never called), so the folder
    /// is found by looking where that application can be installed rather than by reading a setting. When one of
    /// these exists it is <b>shared</b> rather than duplicated — one copy of a Dunnage picture per machine.
    /// </remarks>
    public static IReadOnlyList<string> ReceivingApplicationDunnageCaches { get; } =
    [
        Path.Combine(AppContext.BaseDirectory, "Assets", "DunnageImages"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MTM_Receiving_Application",
            "Assets",
            "DunnageImages"),
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "MTM_Receiving_Application",
            "Assets",
            "DunnageImages"),
    ];

    /// <summary>
    /// The Dunnage cache folder to write into: the receiving application's own cache when this machine already has
    /// one, and this application's own folder when it does not.
    /// </summary>
    /// <returns>The folder to mirror the Dunnage pictures into.</returns>
    public static string ResolveDunnageCacheFolder() =>
        ReceivingApplicationDunnageCaches.FirstOrDefault(Directory.Exists)
            ?? Path.Combine(LocalCacheRoot, DunnageFolderName);

    /// <summary>Whether a file is one of the picture kinds the cache carries.</summary>
    /// <param name="path">The file name or path to test.</param>
    /// <returns><see langword="true"/> for a supported picture extension.</returns>
    public static bool IsSupportedPicture(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a path, relative to a cache or share root, is one the synchronisation must leave alone.
    /// </summary>
    /// <param name="relativePath">A path relative to the root being walked.</param>
    /// <returns><see langword="true"/> for the application's own working folder.</returns>
    public static bool ShouldSkipFromSync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalized = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        return normalized.StartsWith(TempFolderName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, TempFolderName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The cached path for a file named relative to its source root.
    /// </summary>
    /// <param name="cacheRoot">The cache folder for that source.</param>
    /// <param name="relativePath">The file's path relative to the source root.</param>
    /// <returns>The path the cached copy has, or would have.</returns>
    public static string CachePathFor(string cacheRoot, string relativePath) =>
        Path.Combine(
            cacheRoot,
            relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

    /// <summary>
    /// The cached copy of a picture, when this computer holds one.
    /// </summary>
    /// <param name="sourceRoot">The root the picture was read from.</param>
    /// <param name="cacheRoot">The cache folder mirroring that root.</param>
    /// <param name="path">The picture's path: rooted under <paramref name="sourceRoot"/>, or relative to it.</param>
    /// <returns>The cached file's path when it exists, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// This is the other half of the cache, and the half that makes it worth having: a screen asks here first and
    /// reads a local file, falling back to the share only when this computer has never managed to copy it. The MTM
    /// Receiving Application does the same thing (<c>Helper_DunnageImagePaths.GetDisplayAbsolutePath</c>).
    /// </remarks>
    public static string? TryGetCachedCopy(string? sourceRoot, string? cacheRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(cacheRoot))
        {
            return null;
        }

        var normalized = path.Trim()
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        string relativePath;

        if (Path.IsPathRooted(normalized))
        {
            if (string.IsNullOrWhiteSpace(sourceRoot))
            {
                return null;
            }

            var root = Path.GetFullPath(sourceRoot.Trim());
            var candidate = Path.GetFullPath(normalized);

            // A rooted path outside the source root is not one of this root's pictures, so there is no cached
            // copy to point at.
            if (candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) is false)
            {
                return null;
            }

            relativePath = Path.GetRelativePath(root, candidate);
        }
        else
        {
            relativePath = normalized;
        }

        var cached = CachePathFor(cacheRoot, relativePath);

        return File.Exists(cached) ? cached : null;
    }

    /// <summary>
    /// Whether a picture has to be copied into the cache again.
    /// </summary>
    /// <param name="sharedPath">The picture on the share.</param>
    /// <param name="cachePath">Where its cached copy is, or would be.</param>
    /// <returns>
    /// <see langword="true"/> when there is no cached copy, or when the copy's size or last-written time differs
    /// from the share's.
    /// </returns>
    /// <remarks>
    /// Size and last-written time are the whole comparison, which is the rule the MTM Receiving Application uses
    /// (<c>Service_DunnageImageStorage.ShouldCopyToLocalCache</c>). It is deliberately not a hash: hashing every
    /// picture on every startup would read the entire share over the network, which is the cost the cache exists to
    /// avoid. The copy is stamped with the share's own last-written time so the two stay comparable — without that
    /// stamp every cached picture would look different on the next run and be copied again for ever.
    /// </remarks>
    public static bool ShouldCopyToCache(string? sharedPath, string? cachePath)
    {
        if (string.IsNullOrWhiteSpace(sharedPath) || string.IsNullOrWhiteSpace(cachePath))
        {
            return false;
        }

        var cached = new FileInfo(cachePath);
        if (cached.Exists is false)
        {
            return true;
        }

        var shared = new FileInfo(sharedPath);
        if (shared.Exists is false)
        {
            return false;
        }

        return shared.Length != cached.Length || shared.LastWriteTimeUtc != cached.LastWriteTimeUtc;
    }
}
