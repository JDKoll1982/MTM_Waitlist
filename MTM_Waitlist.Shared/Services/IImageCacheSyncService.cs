using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Services;

/// <summary>
/// Mirrors the application's picture roots onto the local computer so every screen draws from local disk.
/// </summary>
/// <remarks>
/// The logic is the MTM Receiving Application's Dunnage cache synchronisation
/// (<c>Module_Dunnage/Services/Service_DunnageImageStorage.SyncLocalCacheAsync</c>), kept in the same shape so the
/// two applications behave alike on the same machine: walk the source, copy what is missing or changed, stamp the
/// copy with the source's own last-written time, delete copies whose original is gone, and leave empty folders
/// behind. This application runs it for both of its picture sources.
/// </remarks>
public interface IImageCacheSyncService
{
    /// <summary>
    /// Brings the local cache up to date with every picture source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A summary of what was copied and removed.</returns>
    /// <remarks>
    /// Best effort by design: a picture source that cannot be reached is reported and left exactly as it is, and no
    /// failure here may stop the application starting. The application is usable without a cache; it is not usable
    /// without a shell.
    /// </remarks>
    Task<ImageCacheSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default);
}

/// <summary>What one synchronisation did, so the startup step can say so rather than guess.</summary>
/// <param name="Copied">How many pictures were copied because they were missing or had changed.</param>
/// <param name="Removed">How many cached pictures were removed because their original is no longer there.</param>
/// <param name="SourcesSkipped">The sources that could not be read, so nothing under them was touched.</param>
/// <param name="ArchivesRemoved">
/// How many replaced pictures were cleaned up because their retention period had passed (FR-011).
/// </param>
public sealed record ImageCacheSyncResult(
    int Copied,
    int Removed,
    IReadOnlyList<string> SourcesSkipped,
    int ArchivesRemoved = 0)
{
    /// <summary>
    /// The sources whose local folder could not be written to, so nothing under them was copied.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="SourcesSkipped"/> on purpose, because the two faults have different remedies and
    /// the same words for both sends an administrator to the wrong one. A source here was <b>readable</b>: the
    /// pictures were there and the copies could not be put anywhere, which on a shop-floor machine usually means
    /// the folder belongs to an elevated install and this account may only read it.
    /// </remarks>
    public IReadOnlyList<string> DestinationsUnavailable { get; init; } = Array.Empty<string>();

    /// <summary>
    /// How many pictures could not be copied although their destination was usable.
    /// </summary>
    /// <remarks>
    /// One picture held open by another program, or a name this file system refuses, costs that picture and not
    /// the rest of the run. The count is what makes such a run visible rather than looking like an up-to-date one.
    /// </remarks>
    public int NotCopied { get; init; }

    /// <summary>A run that had nothing to do and nothing to report.</summary>
    public static ImageCacheSyncResult NothingToDo { get; } = new(0, 0, Array.Empty<string>());

    /// <summary>Whether anything about the cache changed.</summary>
    public bool ChangedAnything => Copied > 0 || Removed > 0 || ArchivesRemoved > 0;
}
