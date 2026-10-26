using System.Globalization;
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
/// <param name="ExcludedTopLevelFolders">
/// Folders directly under <paramref name="SourceRoot"/> that this mirror leaves alone, by name. The two part
/// collections are named here because their pictures are this computer's business one part at a time
/// (<see cref="PartPictureCacheStore"/>) rather than something to be copied in one pass at startup (FR-030).
/// </param>
/// <param name="ArchiveKeepDays">
/// How long a replaced picture is kept under this root before it is removed, or zero to leave the archive alone.
/// The period is the store's setting, supplied when a run starts rather than compiled here (FR-011, FR-037).
/// </param>
public sealed record ImageCacheSource(
    string Label,
    string SourceRoot,
    string CacheRoot,
    IReadOnlyList<string>? ExcludedTopLevelFolders = null,
    int ArchiveKeepDays = 0);

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
/// <b>A cancelled run stops quietly and reports what it did.</b> The launch stops this step the moment it passes
/// its stated maximum, and the walk answers that by returning the counts it has reached rather than by throwing:
/// the refresh is best effort by contract, and an exception escaping a thread-pool thread is reported as a crash
/// in a step that cannot be allowed to fail (FR-026). A cancelled walk never deletes anything, because the share
/// was only half read and every copy it had not compared yet would look like a copy whose original is gone.
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
        // screen's own thread. The token is handed to the walk rather than to the task, because a walk that is
        // stopped has to answer with what it managed to do: an exception raised on a thread-pool thread is
        // reported as an unhandled fault, and this step's whole contract is that it cannot fail (FR-026).
        try
        {
            return await Task.Run(() => Synchronize(sources, cancellationToken)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Stopped before the walk began, so nothing was done — which is exactly what this result says.
            return ImageCacheSyncResult.NothingToDo;
        }
    }

    /// <summary>
    /// The whole synchronisation, over an explicit set of sources, so the rule can be tested without a share.
    /// </summary>
    /// <param name="sources">The picture sources to mirror.</param>
    /// <param name="cancellationToken">
    /// Stops the walk. A cancelled walk returns the counts it reached rather than throwing, and never deletes
    /// anything it has not yet compared against the share.
    /// </param>
    /// <returns>A summary of what was copied and removed.</returns>
    public static ImageCacheSyncResult Synchronize(
        IReadOnlyList<ImageCacheSource> sources,
        CancellationToken cancellationToken = default)
    {
        var copied = 0;
        var removed = 0;
        var archivesRemoved = 0;
        var notCopied = 0;
        var skipped = new List<string>();
        var destinationsUnavailable = new List<string>();

        foreach (var source in sources)
        {
            // A stopped run ends here rather than throwing. The launch cancels this step the moment it passes its
            // stated maximum, and an exception escaping a thread-pool thread is what the debugger reports as an
            // unhandled fault in a step that is best effort by contract (FR-026). What is already copied stays
            // copied: the next run compares against it and finishes the job.
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(source.SourceRoot) || Directory.Exists(source.SourceRoot) is false)
            {
                skipped.Add(source.Label);
                continue;
            }

            if (ImageCachePaths.TryPrepareCacheFolder(source.CacheRoot) is false)
            {
                // The pictures are readable and this account cannot write where the copies go. Settling that here,
                // before the walk, is what keeps the two faults apart: reporting this one as an unreachable share
                // sends an administrator to look at a share that is perfectly healthy, and says nothing about the
                // folder that actually needs fixing.
                destinationsUnavailable.Add(source.Label);
                continue;
            }

            try
            {
                var sourceResult = SynchronizeSource(source, cancellationToken);
                copied += sourceResult.Copied;
                removed += sourceResult.Removed;
                archivesRemoved += sourceResult.ArchivesRemoved;
                notCopied += sourceResult.NotCopied;
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

        return new ImageCacheSyncResult(copied, removed, skipped, archivesRemoved)
        {
            DestinationsUnavailable = destinationsUnavailable,
            NotCopied = notCopied,
        };
    }

    /// <summary>Mirrors one source, returning what it did.</summary>
    /// <remarks>
    /// The caller has already established that the source can be read and that the copies can be written: this walk
    /// is only ever entered for a source that has a readable root and a usable destination. A cancelled walk leaves
    /// before the deletion pass, because a half-read share cannot tell a copy whose original is gone from one whose
    /// original was in the part it never reached.
    /// </remarks>
    private static (int Copied, int Removed, int ArchivesRemoved, int NotCopied) SynchronizeSource(
        ImageCacheSource source,
        CancellationToken cancellationToken)
    {
        // What the share holds, gathered before anything is deleted, so the deletion pass can only ever remove a
        // copy whose original was genuinely looked for and not found.
        var onShare = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var copied = 0;
        var notCopied = 0;

        foreach (var sharedPath in Directory.EnumerateFiles(source.SourceRoot, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // The share was only half read, so the deletion pass below must not run: every copy whose original
                // sits in the part that was never read would look like a copy whose original is gone, and the
                // pictures this machine is still drawing would be deleted out from under it.
                return (copied, 0, 0, notCopied);
            }

            if (ImageCachePaths.IsSupportedPicture(sharedPath) is false)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(source.SourceRoot, sharedPath);
            if (ImageCachePaths.ShouldSkipFromSync(relativePath) || IsExcluded(source, relativePath))
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

            try
            {
                File.Copy(sharedPath, cachePath, overwrite: true);

                // The copy carries the share's own last-written time. Without that stamp every cached picture would
                // differ from its original on the next run and be copied again for ever.
                File.SetLastWriteTimeUtc(cachePath, File.GetLastWriteTimeUtc(sharedPath));
                copied++;
            }
            catch (Exception)
            {
                // One picture that will not copy — held open on this machine, or a name the file system refuses —
                // must not abandon the other hundred and forty and leave the cache half built with nothing said.
                // The count carries the fault out to the caller, which reports it.
                notCopied++;
            }
        }

        var removed = 0;

        foreach (var cachePath in Directory.EnumerateFiles(source.CacheRoot, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Safe to stop here: the share was read in full, so every copy removed so far was removed for the
                // right reason and the rest are simply left for the next run.
                return (copied, removed, 0, notCopied);
            }

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

        // The archive cleanup runs as part of the same visit to the root rather than in a step of its own: the run
        // is already there, the period is already in hand, and a picture that was replaced stops taking up space
        // without anybody having to remember to tidy up (FR-011, FR-037).
        var archivesRemoved = RemoveExpiredArchives(source, cancellationToken);

        return (copied, removed, archivesRemoved, notCopied);
    }

    /// <summary>
    /// Whether a path, relative to a source root, belongs to a folder this mirror leaves alone.
    /// </summary>
    /// <param name="source">The source being walked.</param>
    /// <param name="relativePath">A path relative to that source's root.</param>
    /// <returns><see langword="true"/> for a folder the source's own store owns instead.</returns>
    private static bool IsExcluded(ImageCacheSource source, string relativePath)
    {
        if (source.ExcludedTopLevelFolders is null || source.ExcludedTopLevelFolders.Count == 0)
        {
            return false;
        }

        var separator = relativePath.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
        var topLevel = separator < 0 ? relativePath : relativePath[..separator];

        return source.ExcludedTopLevelFolders.Contains(topLevel, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes the archived pictures under a source that are past the configured retention period.
    /// </summary>
    /// <param name="source">The source being walked.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>How many archived files were removed.</returns>
    /// <remarks>
    /// Only files whose own name carries the day they were archived are considered, because that is the only date
    /// the writer records: the file itself is <em>copied</em> when it is replaced, so its last-written time is the
    /// replaced picture's, not the archiving's. A file this cannot date is left where it is, which is the safe
    /// direction: an archive kept too long costs disk, an archive removed too early costs a picture nobody can get
    /// back. A source with no period set keeps its archives for ever.
    /// </remarks>
    private static int RemoveExpiredArchives(ImageCacheSource source, CancellationToken cancellationToken)
    {
        if (source.ArchiveKeepDays <= 0)
        {
            return 0;
        }

        // Whole days, because a whole day is all an archive's name carries: the period is inclusive, so a picture
        // archived on the day the period names is still inside it.
        var cutoff = DateTime.Today.AddDays(-source.ArchiveKeepDays);
        var removed = 0;

        // The archive folders are found by name and read on their own, rather than by reading every file under the
        // root a second time: this runs on the splash screen's thread, and the walk of the picture tree is the cost
        // the local copy exists to keep off the screens.
        foreach (var archiveFolder in Directory.EnumerateDirectories(
            source.SourceRoot,
            AppStoragePaths.ArchiveFolderName,
            SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var path in Directory.EnumerateFiles(archiveFolder, "*", SearchOption.AllDirectories))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (ImageCachePaths.IsSupportedPicture(path) is false)
                {
                    continue;
                }

                var archivedOn = ReadArchivedOn(path);
                if (archivedOn is null || archivedOn.Value.Date >= cutoff)
                {
                    continue;
                }

                File.Delete(path);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>
    /// The day a file was archived, read from its own name, or <see langword="null"/> when its name does not say.
    /// </summary>
    /// <param name="path">The archived file's path.</param>
    /// <returns>The day it was archived, or <see langword="null"/>.</returns>
    /// <remarks>
    /// The two names this understands are the two the application writes
    /// (<c>ImageStorageService.ArchiveExistingFile</c> and <c>ImageStorageService.DeleteStoredImageAsync</c>):
    /// <c>{name}-MM-dd-yyyy-NN{extension}</c> beside the picture it replaced, and
    /// <c>{name}_{yyyyMMdd_HHmmss}{extension}</c> for a picture that was deleted. The whole last part of the name
    /// has to be date-shaped, so a part number that happens to look like a date is not mistaken for one.
    /// </remarks>
    private static DateTime? ReadArchivedOn(string path)
    {
        var archiveFolder = Path.GetFileName(Path.GetDirectoryName(path));
        if (string.Equals(archiveFolder, AppStoragePaths.ArchiveFolderName, StringComparison.OrdinalIgnoreCase) is false)
        {
            return null;
        }

        var stem = Path.GetFileNameWithoutExtension(path);

        // {name}-MM-dd-yyyy-NN — the day is split by the same separator as the name before it, and the sequence
        // number after it is what makes the shape unambiguous.
        var segments = stem.Split('-');
        if (segments.Length >= 4
            && int.TryParse(segments[^1], out var sequence)
            && sequence is >= 1 and <= 99
            && DateTime.TryParseExact(
                $"{segments[^4]}-{segments[^3]}-{segments[^2]}",
                "MM-dd-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var archivedOn))
        {
            return archivedOn;
        }

        // {name}_{yyyyMMdd_HHmmss} — the day is the first eight characters after the last underscore.
        var underscore = stem.LastIndexOf('_');
        if (underscore >= 0
            && stem.Length - underscore - 1 >= 8
            && DateTime.TryParseExact(
                stem.Substring(underscore + 1, 8),
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var deletedOn))
        {
            return deletedOn;
        }

        return null;
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
