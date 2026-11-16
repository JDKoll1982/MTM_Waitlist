using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// The two questions a generic request asks about the job running at its work center.
/// </summary>
/// <remarks>
/// <para>
/// Two operations rather than one, because they answer two different things at two different moments: naming the job
/// is what lets the requester be offered it, and its values are read only once they accept (FR-001, FR-002). The
/// implementation lives at the composition root, because no library in this solution references the Setup module whose
/// active-job read it wraps (research R4).
/// </para>
/// <para>
/// The read is of an internal store, so it is always live. Nothing here is cached, mirrored or estimated.
/// </para>
/// </remarks>
public interface IGenericRequestJobFillData
{
    /// <summary>
    /// Whether a job is running at <paramref name="workCenter"/>, named so the requester can be offered it.
    /// </summary>
    /// <remarks>
    /// Populated with one candidate when there is a job, Empty when there is none, Unavailable when the read failed. A
    /// failed probe draws no offer: a question that cannot be named is not a question (FR-004).
    /// </remarks>
    Task<GenericRequestStepResult<GenericRequestJobFillCandidate>> ProbeAsync(
        string workCenter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The values the running job carries. Called only after the requester accepted the offer.
    /// </summary>
    /// <remarks>
    /// Populated with the fill when the job was read, Empty when it went away between the offer and this read, and
    /// Unavailable when the read failed, in which case the attempt time it carries is what the step reports beside its
    /// retry and nothing is seeded (FR-016).
    /// </remarks>
    Task<GenericRequestStepResult<GenericRequestJobFill>> ReadAsync(
        string workCenter,
        CancellationToken cancellationToken = default);
}
