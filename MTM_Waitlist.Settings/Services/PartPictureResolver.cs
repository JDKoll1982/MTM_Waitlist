using Microsoft.Extensions.Logging;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Shared.Services;

namespace MTM_Waitlist.Module_Settings.Services;

/// <inheritdoc cref="IPartPictureResolver"/>
/// <summary>
/// Resolves a part's picture for every surface that draws one.
/// </summary>
/// <remarks>
/// <para>
/// Three answers, in this order, and the order is the point: this machine's own copy when it holds one, because a
/// local file is faster than a share and survives the share being busy; otherwise the file on the share, so a
/// picture appears the first time a part is drawn rather than waiting for a copy step; otherwise nothing, and the
/// caller draws the one shared placeholder.
/// </para>
/// <para>
/// A picture is answered only when it meets the application's existing acceptance rule — it exists, it can be
/// read, it is square and it is at least 48 pixels a side. The rule is not relaxed here and is not tightened for
/// anything this resolver does not touch: a picture the rule refuses is not an error and not a picture.
/// </para>
/// <para>
/// A store that cannot be read answers nothing rather than throwing. A surface drawing a card must not fail
/// because a picture could not be looked up, and the placeholder is the honest answer for "there is no picture to
/// draw". The screens that must report an unavailable store still do so on their own.
/// </para>
/// </remarks>
public sealed class PartPictureResolver : IPartPictureResolver
{
    private readonly IImageOverrideReadService _readService;
    private readonly IImageStorageConfigurationResolver _configurationResolver;
    private readonly ILogger<PartPictureResolver> _logger;

    /// <summary>
    /// This computer's copies of the parts it has drawn, or null on a host that keeps none.
    /// </summary>
    /// <remarks>
    /// Optional, so a host with caching switched off — and every test that only cares what a recorded path resolves
    /// to — draws from the source rather than failing to construct the reader.
    /// </remarks>
    private readonly IPartPictureCacheStore? _cacheStore;

    /// <summary>
    /// The extensions a hand-placed picture may carry, in the order they are looked for, which is the order the
    /// application's own image rules list them.
    /// </summary>
    private static readonly string[] AcceptedExtensions = [".png", ".jpg", ".jpeg"];

    public PartPictureResolver(
        IImageOverrideReadService readService,
        IImageStorageConfigurationResolver configurationResolver,
        ILogger<PartPictureResolver> logger,
        IPartPictureCacheStore? cacheStore = null)
    {
        _readService = readService ?? throw new ArgumentNullException(nameof(readService));
        _configurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheStore = cacheStore;
    }

    /// <inheritdoc />
    public string? ResolveRelativeStoredPath(string scope, string partNumber, string extension) =>
        PartPictureLayout.RelativePathFor(scope, partNumber, extension);

    /// <inheritdoc />
    public async Task<string?> ResolvePartPicturePathAsync(
        string scope,
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        if (PartPictureLayout.IsPartScope(scope) is false || string.IsNullOrWhiteSpace(partNumber))
        {
            return null;
        }

        var normalizedPart = partNumber.Trim();

        ImageOverride? stored;
        try
        {
            stored = await _readService.GetOverrideAsync(scope, normalizedPart, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "The picture for {Scope} part {PartNumber} could not be looked up; the surface will draw the shared placeholder.",
                scope,
                normalizedPart);
            return null;
        }

        if (stored is null || stored.IsActive is false || string.IsNullOrWhiteSpace(stored.ImagePath))
        {
            // FR-028: a picture placed by hand, named after its part, is used without the application having been
            // used to add it. There is no row to name the file, so the part's own number is the name to look for.
            return await ResolveDroppedFileAsync(scope, normalizedPart).ConfigureAwait(false);
        }

        var root = await ResolveRootAsync().ConfigureAwait(false);
        var usable = ResolveUsableFile(scope, stored.ImagePath, root);

        // FR-030: the first time a part is drawn, its picture is copied onto this computer. The copy is what the
        // next draw reads, which is what makes the second draw read local disk instead of the share.
        return usable is null
            ? null
            : await EnsureLocalCopyAsync(scope, stored.ImagePath, usable).ConfigureAwait(false) ?? usable;
    }

    /// <summary>
    /// The configured picture root, or null when it could not be read.
    /// </summary>
    /// <returns>The root, or <see langword="null"/>.</returns>
    private async Task<string?> ResolveRootAsync()
    {
        try
        {
            return await _configurationResolver.GetSharedFolderPathAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The configured picture root could not be read; resolving the share directly.");
            return null;
        }
    }

    /// <summary>
    /// The file a part owns by name, for a part with no recorded row: the part's number with any extension the
    /// application draws, in the folder its number puts it in.
    /// </summary>
    /// <param name="scope">The part's store scope, which decides the collection folder.</param>
    /// <param name="partNumber">The part number, which is the file's name.</param>
    /// <returns>The path of a usable picture, or <see langword="null"/>.</returns>
    /// <remarks>
    /// A file the acceptance rule rejects is not a picture and not a fault: it answers null, and the caller draws
    /// the shared placeholder. The extensions are tried in the order the application's own image rules list them,
    /// so a machine holding two extensions of one part draws the same one every time. A hand-placed file is copied
    /// onto this computer exactly as a recorded one is, because the part is drawn either way.
    /// </remarks>
    private async Task<string?> ResolveDroppedFileAsync(string scope, string partNumber)
    {
        var root = await ResolveRootAsync().ConfigureAwait(false);

        foreach (var extension in AcceptedExtensions)
        {
            var relative = PartPictureLayout.RelativePathFor(scope, partNumber, extension);
            if (string.IsNullOrWhiteSpace(relative))
            {
                continue;
            }

            var usable = ResolveUsableFile(scope, relative, root);
            if (usable is null)
            {
                continue;
            }

            return await EnsureLocalCopyAsync(scope, relative, usable).ConfigureAwait(false) ?? usable;
        }

        return null;
    }

    /// <summary>
    /// Copies the part's picture onto this computer when it is not already here, and answers the copy.
    /// </summary>
    /// <param name="scope">The part's store scope, which decides the collection the copy belongs to.</param>
    /// <param name="storedOrRelativePath">The recorded path, or the relative path a hand-placed file is found at.</param>
    /// <param name="usablePath">The picture as this computer resolved it, on the share or in the cache.</param>
    /// <returns>The copy's path, or null when no copy was wanted or none could be made.</returns>
    /// <remarks>
    /// A copy is wanted only when this machine caches pictures at all: with caching switched off the source is the
    /// only place a picture lives, and writing one to local disk would be doing the opposite of what was asked.
    /// Nothing here removes anything, so a source that has gone away leaves the copy this machine already has.
    /// </remarks>
    private async Task<string?> EnsureLocalCopyAsync(string scope, string storedOrRelativePath, string usablePath)
    {
        if (_cacheStore is null)
        {
            return null;
        }

        var collectionFolder = PartPictureLayout.CollectionFolderFor(scope);
        if (collectionFolder is null || await IsLocalCopyWantedAsync().ConfigureAwait(false) is false)
        {
            return null;
        }

        var relative = AppStoragePaths.ToCurrentLayout(storedOrRelativePath);

        return _cacheStore.EnsureLocalCopy(collectionFolder, BelowCollection(relative, collectionFolder), usablePath);
    }

    /// <summary>Whether pictures are copied onto this computer at all.</summary>
    /// <returns><see langword="true"/> when a copy should be made.</returns>
    /// <remarks>
    /// A setting that cannot be read answers "no": the source path is always a correct answer, and the next draw
    /// asks the store again.
    /// </remarks>
    private async Task<bool> IsLocalCopyWantedAsync()
    {
        try
        {
            return await _configurationResolver.GetImageCacheEnabledAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Whether pictures are copied onto this computer could not be read; this picture is drawn from the source.");
            return false;
        }
    }

    /// <summary>
    /// The file a control draws for one recorded path: this machine's copy of it when there is one, the source
    /// otherwise, and null when neither is a picture the application accepts.
    /// </summary>
    /// <param name="scope">The part's store scope, which decides the cache folder the copy sits in.</param>
    /// <param name="storedPath">The recorded path, in either layout.</param>
    /// <param name="root">The configured picture root, or null when it could not be read.</param>
    /// <returns>The absolute path of a usable picture, or <see langword="null"/>.</returns>
    /// <remarks>
    /// The copy's folder is named for its collection, so the same file name under <c>Visual</c> and under <c>WIP</c>
    /// is two copies and can never be one file. The copy is found by the same relative path the row records, which
    /// is what keeps the two in step when the recorded layout moves.
    /// </remarks>
    internal static string? ResolveUsableFile(string scope, string storedPath, string? root)
    {
        var relative = AppStoragePaths.ToCurrentLayout(storedPath);

        var collectionFolder = PartPictureLayout.CollectionFolderFor(scope);
        if (collectionFolder is not null)
        {
            // The cache mirrors the share: the collection's own folder holds the part of the recorded path below
            // the collection. The collection is therefore named once, and the two systems sit in two folders on
            // this machine as well as on the share (FR-002).
            var copy = Path.Combine(
                ImageCachePaths.PartCollectionCacheFolder(collectionFolder),
                BelowCollection(relative, collectionFolder));

            var usableCopy = ImagePicturePolicy.ResolveUsableFile(copy);
            if (usableCopy is not null)
            {
                return usableCopy;
            }
        }

        return ImagePicturePolicy.ResolveUsableFile(AppStoragePaths.ResolvePicturePath(root, storedPath));
    }

    /// <summary>
    /// The part of a recorded path below its collection folder, with the platform's own separator.
    /// </summary>
    /// <param name="relative">The recorded path, in the current layout.</param>
    /// <param name="collectionFolder">The collection folder its scope belongs to.</param>
    /// <returns>The path below the collection, or the whole path when it does not begin with the collection.</returns>
    /// <remarks>
    /// The recorded path may use either separator, so the two are unified before the leading folder is dropped.
    /// A path this does not recognise is taken whole rather than skipped: looking in the wrong place costs one
    /// failed file read, while skipping would quietly leave a picture to the share for ever.
    /// </remarks>
    private static string BelowCollection(string relative, string collectionFolder)
    {
        var normalized = relative.Replace('/', Path.DirectorySeparatorChar);
        var prefix = collectionFolder + Path.DirectorySeparatorChar;

        return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? normalized[prefix.Length..]
            : normalized;
    }
}
