using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// This computer's copy of one part's picture, kept under its own collection so the two part systems cannot be one
/// file (FR-002, FR-030).
/// </summary>
/// <remarks>
/// The copy is made the first time a part is drawn rather than in one pass at startup: there are more parts than
/// anyone will look at in a session, and copying them all to make a screen fast is the slow thing the copy was
/// meant to avoid.
/// </remarks>
public interface IPartPictureCacheStore
{
    /// <summary>
    /// Copies one part's picture onto this computer when it is not already here, and answers the file to draw.
    /// </summary>
    /// <param name="collectionFolder">The part's collection folder, which decides the cache folder.</param>
    /// <param name="relativePathBelowCollection">
    /// The picture's path below its collection folder, which is the path the copy is given.
    /// </param>
    /// <param name="sourcePath">
    /// The picture on the share, or <see langword="null"/> or blank when there is nothing to copy from.
    /// </param>
    /// <returns>
    /// The copy's path when this computer holds one — the one just made, or the one already here — and
    /// <see langword="null"/> when there is no copy and none could be made.
    /// </returns>
    /// <remarks>
    /// Nothing is ever removed here. A source that cannot be reached is recorded and the copy already here is
    /// left where it is (FR-031), and an empty source is not evidence that the picture was taken away (FR-032):
    /// the two rules a cache that deletes on a network blip gets wrong.
    /// </remarks>
    string? EnsureLocalCopy(string collectionFolder, string relativePathBelowCollection, string? sourcePath);
}

/// <inheritdoc cref="IPartPictureCacheStore" />
public sealed class PartPictureCacheStore : IPartPictureCacheStore
{
    /// <inheritdoc />
    public string? EnsureLocalCopy(string collectionFolder, string relativePathBelowCollection, string? sourcePath)
    {
        if (string.IsNullOrWhiteSpace(collectionFolder) || string.IsNullOrWhiteSpace(relativePathBelowCollection))
        {
            return null;
        }

        var cachePath = ImageCachePaths.CachePathFor(
            ImageCachePaths.PartCollectionCacheFolder(collectionFolder),
            relativePathBelowCollection);

        var existingCopy = File.Exists(cachePath) ? cachePath : null;

        // An empty source says nothing about the picture: this computer keeps what it has and nothing is removed.
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return existingCopy;
        }

        if (ImageCachePaths.ShouldCopyToCache(sourcePath, cachePath) is false)
        {
            // The copy matches its original by size and last-written time, so the whole point of the cache holds:
            // a part drawn for the second time reads a local file and touches the share not at all.
            return cachePath;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

            File.Copy(sourcePath, cachePath, overwrite: true);

            // The copy carries the share's own last-written time, so the next draw compares like with like
            // instead of copying every picture again for ever.
            File.SetLastWriteTimeUtc(cachePath, File.GetLastWriteTimeUtc(sourcePath));

            return cachePath;
        }
        catch (Exception ex)
        {
            // Recorded rather than thrown: the picture source is a share, a share goes away, and a screen that
            // cannot copy a picture still has to open (FR-031).
            StartupDebugLog.Error(
                "PartPictureCache",
                ex,
                $"The picture for '{relativePathBelowCollection}' could not be copied into the local cache; this computer keeps the copy it already has, if any.");

            return existingCopy;
        }
    }
}
