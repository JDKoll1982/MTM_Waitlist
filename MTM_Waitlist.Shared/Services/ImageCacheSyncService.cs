using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// One picture source to mirror locally: where the pictures are, and where their copies go.
/// </summary>
/// <param name="Label">
/// A short name for the source, used in the run's report and in a log line. Diagnostic, not user-facing copy:
/// the words a person reads on the splash screen come from the startup step's own resource key.
/// </param>
/// <param name="SourceRoot">The root on the share. A folder per kind of picture lives beneath it.</param>
/// <param name="CacheRoot">The local folder this source is mirrored into.</param>
public sealed record ImageCacheSource(string Label, string SourceRoot, string CacheRoot);

/// <summary>
/// Mirrors every picture source onto the local computer, carrying the MTM Receiving Application's Dunnage cache
/// rule so the two applications behave alike on the same machine.
/// </summary>
/// <remarks>
/// <para>
/// Per run, and per source: copy a picture when the cached copy is missing or its size or last-written time
/// differs; stamp the copy with the share's own last-written time; delete a cached picture whose original is gone;
/// remove folders left empty. Nothing else is touched.
/// </para>
/// <para>
/// A source that cannot be read — an unreachable share, or a walk that fails part way — is reported and left
/// <b>exactly as it is</b>. Emptying the cache on a network blip would take the pictures away precisely when the
/// cache is the only thing still working.
/// </para>
/// <para>
/// The sources are supplied as a factory rather than as values because both roots are settings that a person can
/// change while the application is running: the run reads them when it starts, not when the service is built.
/// </para>
/// </remarks>
public sealed class ImageCacheSyncService : IImageCacheSyncService
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<ImageCacheSource>>> _sources;

    /// <summary>Initializes a new ImageCacheSyncService.</summary>
    /// <param name="sources">Supplies the picture sources for a run.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="sources"/> is null.</exception>
    public ImageCacheSyncService(Func<CancellationToken, Task<IReadOnlyList<ImageCacheSource>>> sources)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    }

    /// <inheritdoc />
    public async Task<ImageCacheSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ImageCacheSource> sources;

        try
        {
            // Resolved now rather than when the service was built: the picture root is a setting, and one of the
            // sources may be a folder that only exists on some machines.
            sources = await _sources(cancellationToken).ConfigureAwait(false) ?? Array.Empty<ImageCacheSource>();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // A root that cannot be resolved at all — a store that will not answer the setting, or a share that
            // has gone — is the same answer as an unreachable source, and must never reach the caller as a
            // failure: this runs during startup, and a cache is not worth losing the shell for.
            return ImageCacheSyncResult.NothingToDo;
        }

        // Off the calling thread: this walks two folder trees over the network, and the caller is the splash
        // screen's own thread.
        return await Task.Run(() => Synchronize(sources, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The whole synchronisation, over an explicit set of sources, so the rule can be tested without a share.
    /// </summary>
    /// <param name="sources">The picture sources to mirror.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A summary of what was copied and removed.</returns>
    public static ImageCacheSyncResult Synchronize(
        IReadOnlyList<ImageCacheSource> sources,
        CancellationToken cancellationToken = default)
    {
        var copied = 0;
        var removed = 0;
        var skipped = new List<string>();

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(source.SourceRoot) || Directory.Exists(source.SourceRoot) is false)
            {
                skipped.Add(source.Label);
                continue;
            }

            try
            {
                var sourceResult = SynchronizeSource(source, cancellationToken);
                copied += sourceResult.Copied;
                removed += sourceResult.Removed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Reported rather than thrown, and reported *after* the walk: a source that failed half way is not
                // pruned, because the files it would have been compared against were never all read.
                skipped.Add(source.Label);
            }
        }

        return new ImageCacheSyncResult(copied, removed, skipped);
    }

    /// <summary>Mirrors one source, returning what it did.</summary>
    private static (int Copied, int Removed) SynchronizeSource(
        ImageCacheSource source,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(source.CacheRoot);

        // What the share holds, gathered before anything is deleted, so the deletion pass can only ever remove a
        // copy whose original was genuinely looked for and not found.
        var onShare = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var copied = 0;

        foreach (var sharedPath in Directory.EnumerateFiles(source.SourceRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ImageCachePaths.IsSupportedPicture(sharedPath) is false)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(source.SourceRoot, sharedPath);
            if (ImageCachePaths.ShouldSkipFromSync(relativePath))
            {
                continue;
            }

            onShare.Add(relativePath);

            var cachePath = ImageCachePaths.CachePathFor(source.CacheRoot, relativePath);
            var cacheDirectory = Path.GetDirectoryName(cachePath);
            if (string.IsNullOrWhiteSpace(cacheDirectory) is false)
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            if (ImageCachePaths.ShouldCopyToCache(sharedPath, cachePath) is false)
            {
                continue;
            }

            File.Copy(sharedPath, cachePath, overwrite: true);

            // The copy carries the share's own last-written time. Without that stamp every cached picture would
            // differ from its original on the next run and be copied again for ever.
            File.SetLastWriteTimeUtc(cachePath, File.GetLastWriteTimeUtc(sharedPath));
            copied++;
        }

        var removed = 0;

        foreach (var cachePath in Directory.EnumerateFiles(source.CacheRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ImageCachePaths.IsSupportedPicture(cachePath) is false)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(source.CacheRoot, cachePath);
            if (ImageCachePaths.ShouldSkipFromSync(relativePath) || onShare.Contains(relativePath))
            {
                continue;
            }

            File.Delete(cachePath);
            removed++;
        }

        RemoveEmptyFolders(source.CacheRoot);

        return (copied, removed);
    }

    /// <summary>
    /// Removes the folders the deletion pass emptied, deepest first, leaving the cache root itself and the
    /// application's own working folder alone.
    /// </summary>
    /// <param name="cacheRoot">The cache folder for one source.</param>
    private static void RemoveEmptyFolders(string cacheRoot)
    {
        if (Directory.Exists(cacheRoot) is false)
        {
            return;
        }

        foreach (var directory in Directory
            .EnumerateDirectories(cacheRoot, "*", SearchOption.AllDirectories)
            .OrderByDescending(path => path.Length))
        {
            var relativePath = Path.GetRelativePath(cacheRoot, directory);
            if (ImageCachePaths.ShouldSkipFromSync(relativePath))
            {
                continue;
            }

            if (string.Equals(directory, cacheRoot, StringComparison.OrdinalIgnoreCase)
                || Directory.EnumerateFileSystemEntries(directory).Any())
            {
                continue;
            }

            Directory.Delete(directory, recursive: false);
        }
    }
}
