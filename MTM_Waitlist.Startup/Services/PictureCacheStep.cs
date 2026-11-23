using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The picture refresh: it brings this computer's copies of the shared pictures up to date, and it answers for the
/// catalogue's <c>picture-cache</c> entry (`contracts/launch-step-contract.md` §1.1; FR-002, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// <b>It cannot stop the launch, whatever it does.</b> The catalogue marks this entry best effort and bounds it at
/// thirty seconds, and the runner turns any failure into a skip, so a share that is down costs the launch nothing
/// but a line on the feed (FR-026). That is deliberate: the application is perfectly usable reading pictures from
/// the share, and it is not usable without a shell.
/// </para>
/// <para>
/// <b>A source that could not be read is reported, not hidden, and nothing is thrown away.</b> The mirror leaves
/// every copy already on this computer exactly where it is and names the source it could not read, because
/// emptying the cache during a network blip would take the pictures away precisely when the cache is the only
/// thing still working (edge case in <c>spec.md</c>).
/// </para>
/// <para>
/// <b>It calls a service, and the service does the copying.</b> Nothing here walks a folder tree and nothing here
/// touches the store, so the same mirror the Settings screen drives by hand is the one the launch runs.
/// </para>
/// </remarks>
public sealed class PictureCacheStep : ILaunchStep
{
    /// <summary>
    /// The catalogue entry this step answers for. Shared with the hold that keeps the launch window on a refresh
    /// that did not work, so the two cannot disagree about which line they are talking about.
    /// </summary>
    internal const string StepId = "picture-cache";

    /// <summary>
    /// The module name every entry this step records carries.
    /// </summary>
    private const string LogModule = "PictureCache";

    /// <summary>
    /// How long a refresh may take before its own duration is recorded where the store keeps it. Under this the
    /// step is invisible to the person waiting; over it, the number is the evidence they come back for.
    /// </summary>
    private const double SlowRefreshSeconds = 2;

    private readonly LaunchStep _descriptor;
    private readonly IImageCacheSyncService _pictureCache;

    /// <summary>Creates the step over the mirror that owns this computer's copy of the shared pictures.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from rather than restating it.</param>
    /// <param name="pictureCache">The mirror every screen's pictures are read from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pictureCache"/> is <c>null</c>.</exception>
    public PictureCacheStep(LaunchStepCatalog catalog, IImageCacheSyncService pictureCache)
    {
        ArgumentNullException.ThrowIfNull(pictureCache);

        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _pictureCache = pictureCache;
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "The picture copies could not be refreshed",
            token => RefreshAsync(context, token),
            cancellationToken);
    }

    /// <summary>Runs the mirror and reports what it found, in the reader's own words.</summary>
    /// <param name="context">The launch context, whose feed carries the step's own progress lines.</param>
    /// <param name="cancellationToken">Cancelled when the launch is abandoned or the stated maximum passes.</param>
    private async Task<LaunchStepOutcome> RefreshAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();

        AppLog.Info(LogModule, "The picture refresh step has started.");

        // The walk reports as it goes, so the splash shows what the refresh is doing instead of a bar that cannot
        // move until the whole walk has finished (owner decision, 2026-11-23). The reports are coarse, and the feed
        // is the same record the rest of the launch writes to.
        var progress = new FeedProgress(context, _descriptor);

        var result = await _pictureCache.SynchronizeAsync(progress, cancellationToken).ConfigureAwait(false);

        var elapsedSeconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalSeconds;
        var elapsed = elapsedSeconds.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture);

        var summary =
            $"The picture refresh step answered after {elapsed} second(s): {result.Copied} copied, {result.Removed} removed, "
            + $"{result.ArchivesRemoved} replaced copies cleaned up, {result.NotCopied} could not be copied, "
            + $"{result.SourcesSkipped.Count} source(s) left alone, {result.DestinationsUnavailable.Count} whose folder "
            + "could not be written.";

        if (elapsedSeconds >= SlowRefreshSeconds)
        {
            // The store keeps warnings by default and drops progress lines, so a refresh slow enough to be noticed
            // on the splash is recorded at a level that survives — otherwise the number that explains the wait is
            // written where nobody will ever read it (owner decision, 2026-11-23).
            AppLog.Warn(LogModule, summary);
        }
        else
        {
            AppLog.Info(LogModule, summary);
        }

        if (result.DestinationsUnavailable.Count > 0)
        {
            // A destination that cannot be written is a different fault from an unreadable share, and it needs to
            // read as one: the share is healthy, so an administrator sent there finds nothing wrong. It is also the
            // fault that leaves a machine holding only the few pictures an elevated installer put there, which is
            // why it is recorded to the store and not only shown on the splash feed.
            AppLog.Error(
                LogModule,
                null,
                $"The picture copies could not be written onto this computer, so the copies already here are the "
                + $"ones being used. Sources whose folder could not be written to: "
                + $"{string.Join(", ", result.DestinationsUnavailable)}.");

            return new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                $"The pictures could not be copied onto this computer, so the copies already held here are the ones "
                + $"being used: {string.Join(", ", result.DestinationsUnavailable)}. The folder could not be written "
                + "to.",
                LaunchRemedySet.RetryOnly);
        }

        if (result.SourcesSkipped.Count > 0)
        {
            // The line names which share could not be read and which pictures are being drawn, because a picture
            // that is a day old must never read as a current one (FR-042, SC-019). It is reported as a failure so
            // the runner records it on the feed, and the launch carries on because the entry is best effort
            // (FR-026).
            AppLog.Error(
                LogModule,
                null,
                $"The picture share could not be read, so the copies held on this computer are the ones being used. "
                + $"Sources that could not be read: {string.Join(", ", result.SourcesSkipped)}.");

            return new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                $"The picture share could not be read, so the copies held on this computer are the ones being used: {string.Join(", ", result.SourcesSkipped)}.",
                LaunchRemedySet.RetryOnly);
        }

        var outcome = result.ChangedAnything
            ? $"The picture copies were refreshed: {result.Copied} copied, {result.Removed} removed and {result.ArchivesRemoved} replaced copies cleaned up."
            : "The picture copies were already up to date.";

        outcome += $" It took {elapsed} second(s).";

        if (result.NotCopied > 0)
        {
            // Said out loud rather than left to be discovered: the run copied the rest, so the cache is complete
            // except for these, and a person looking at a picture that will not appear deserves the count.
            AppLog.Error(
                LogModule,
                null,
                $"{result.NotCopied} picture(s) could not be copied although the folder they go to can be written. "
                + $"Copied {result.Copied}, removed {result.Removed}.");

            outcome += $" {result.NotCopied} could not be copied.";
        }

        return new LaunchStepOutcome(LaunchStepStatus.Succeeded, outcome, LaunchRemedySet.None);
    }

    /// <summary>Writes the mirror's own progress lines onto the launch feed, in the order they happen.</summary>
    /// <remarks>
    /// Deliberately not <see cref="Progress{T}"/>: that posts each report to the constructing thread's context, and
    /// a post that lands after the step has written its own completion line would leave the feed out of order. The
    /// mirror reports on the thread that is walking, and the feed is thread-safe, so the line is appended there.
    /// </remarks>
    private sealed class FeedProgress(LaunchStepContext context, LaunchStep step) : IProgress<ImageCacheSyncProgress>
    {
        /// <inheritdoc />
        public void Report(ImageCacheSyncProgress value)
        {
            ArgumentNullException.ThrowIfNull(value);

            context.Feed.Append(new LaunchFeedEntry(
                DateTimeOffset.UtcNow,
                step.Id,
                LaunchFeedEntryKind.SubOperation,
                $"{value.SourceLabel}: {value.Copied} copied, {value.Examined} checked",
                step.Target,
                null));
        }
    }
}
