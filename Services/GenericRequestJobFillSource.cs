using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Services;

/// <summary>
/// Answers the generic request flow's two questions about the job running at a work center.
/// </summary>
/// <remarks>
/// <para>
/// This lives in the application project on purpose: it is the composition root, the only place that may see both
/// <see cref="IGenericRequestJobFillData"/> (Module_GenericRequest) and
/// <see cref="IActiveJobItemResolverService"/> (Module_Setup). Neither module gains a reference to the other, which is
/// the same arrangement <see cref="RequestJobAvailabilityProvider"/> uses.
/// </para>
/// <para>
/// The read is the same one the work-order flow already performs (assumption A2): an active setup job at the work
/// center, read live from the internal store. Nothing here is cached, mirrored or estimated.
/// </para>
/// </remarks>
public sealed class GenericRequestJobFillSource : IGenericRequestJobFillData
{
    private const string LogCategory = "WaitlistGenericRequest";

    private readonly IActiveJobItemResolverService _activeJobItemResolver;

    public GenericRequestJobFillSource(IActiveJobItemResolverService activeJobItemResolver)
    {
        ArgumentNullException.ThrowIfNull(activeJobItemResolver);

        _activeJobItemResolver = activeJobItemResolver;
    }

    /// <summary>
    /// Name the running job without reading what it carries, so the requester can be offered it before anything is
    /// taken from it (FR-002).
    /// </summary>
    /// <remarks>
    /// <para>
    /// No offer is made to a person who may not see the job's data (FR-015). This application declares no permission
    /// that governs seeing a job, and every signed-in person who can open the request flow can already see the job
    /// data the fill would show, so there is nothing to ask here today.
    /// </para>
    /// <para>
    /// // simplified: no permission gates the offer, because none is declared for job visibility. When one is added,
    /// the check belongs here, before the read, so a refusal answers Empty and the offer is never drawn.
    /// </para>
    /// </remarks>
    public async Task<GenericRequestStepResult<GenericRequestJobFillCandidate>> ProbeAsync(
        string workCenter,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var snapshot = await _activeJobItemResolver
                .ResolveAsync(workCenter, cancellationToken)
                .ConfigureAwait(false);

            return snapshot is null || snapshot.WorkOrder.Trim().Length == 0
                ? GenericRequestStepResult<GenericRequestJobFillCandidate>.Empty(attemptedAtUtc)
                : GenericRequestStepResult<GenericRequestJobFillCandidate>.Populated(
                    (MTM_Waitlist.GenericRequest.Models.GenericRequestJobFillCandidate[])[new GenericRequestJobFillCandidate { WorkOrder = snapshot.WorkOrder.Trim() }],
                    attemptedAtUtc);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A probe that fails draws no offer. Nothing has been accepted at this point, so there is no partly
            // filled request to report: the step is what it is at a work center with no job.
            AppLog.Error(LogCategory, ex, $"Reading whether a job is running at '{workCenter}' failed.");
            return GenericRequestStepResult<GenericRequestJobFillCandidate>.Unavailable(attemptedAtUtc);
        }
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestJobFill>> ReadAsync(
        string workCenter,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var snapshot = await _activeJobItemResolver
                .ResolveAsync(workCenter, cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is null)
            {
                // The job went away between the offer and the acceptance. Nothing is seeded, and the step reads as
                // the one a work center with no job shows.
                return GenericRequestStepResult<GenericRequestJobFill>.Empty(attemptedAtUtc);
            }

            var dunnage = snapshot.DunnageParts.FirstOrDefault();

            var fill = GenericRequestJobFill.FromJob(
                snapshot.WorkOrder,
                snapshot.PartNumber,
                snapshot.SequenceNumber,
                snapshot.PrimaryDie?.PartNumber,
                dunnage?.DunnageTypeName,
                dunnage?.PartNumber);

            if (fill is null)
            {
                return GenericRequestStepResult<GenericRequestJobFill>.Empty(attemptedAtUtc);
            }

            AppLog.Info(
                LogCategory,
                $"The job-fill offer was accepted at work center '{workCenter}' for job '{fill.WorkOrder}'.");

            return GenericRequestStepResult<GenericRequestJobFill>.Populated((MTM_Waitlist.GenericRequest.Models.GenericRequestJobFill[])[fill], attemptedAtUtc);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, $"Reading the running job at '{workCenter}' failed.");
            return GenericRequestStepResult<GenericRequestJobFill>.Unavailable(attemptedAtUtc);
        }
    }
}
