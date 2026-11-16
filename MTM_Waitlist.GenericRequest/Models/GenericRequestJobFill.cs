namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// Which of the four things the job-fill offer is doing, for one request.
/// </summary>
/// <remarks>
/// It only moves forward. A declined offer is answered and is never offered again inside the request it belongs to,
/// because the choice is per request and a refusal is not a defect (FR-003, A3).
/// </remarks>
public enum GenericRequestJobFillOfferState
{
    /// <summary>The step that asks has not loaded, so nothing is known about a job yet.</summary>
    NotChecked,

    /// <summary>A job is running at the work center and the offer is on screen.</summary>
    Offered,

    /// <summary>The offer was answered: accepted and filled, or declined and left blank.</summary>
    Answered,

    /// <summary>The values could not be read after the requester accepted, so nothing was seeded.</summary>
    Unavailable,
}

/// <summary>
/// The job the offer is about, as far as naming it goes.
/// </summary>
/// <remarks>
/// A job is named before its values are asked for: the offer has to say which job it means, and reading the values
/// first would decide for the requester what FR-002 says they decide (research R5).
/// </remarks>
public sealed class GenericRequestJobFillCandidate
{
    /// <summary>The running job's work order, which is what the offer names.</summary>
    public string WorkOrder { get; init; } = string.Empty;
}

/// <summary>
/// The values a running job carried, taken only after the requester accepted the offer.
/// </summary>
/// <remarks>
/// <para>
/// This is the only place that knows an answer came from the job, so it answers one question per answer: whether that
/// answer is still the job's. A flag is a fact about where the value came from, not a comparison made when the screen
/// draws: a value the requester typed that happens to equal the job's is not "from the job" (FR-006, FR-007,
/// research R8).
/// </para>
/// <para>
/// The values are candidates. The part and sequence are still proved by the part step's own reads, and a seeded die is
/// the same row the die search would return, so the fill never bypasses what its owning chunk proves (FR-010).
/// </para>
/// </remarks>
public sealed class GenericRequestJobFill
{
    private GenericRequestJobFill()
    {
    }

    /// <summary>The job the values were taken from. Never empty, because a fill that cannot name its job is not one.</summary>
    public string WorkOrder { get; private init; } = string.Empty;

    /// <summary>The job's part number, or empty when it carries none and the requester is asked instead (FR-008).</summary>
    public string PartNumber { get; private init; } = string.Empty;

    /// <summary>The job's sequence number, or empty when it carries none.</summary>
    public string Sequence { get; private init; } = string.Empty;

    /// <summary>The job's first real die, or empty. The placeholder a job with no die carries is never one.</summary>
    public string DieNumber { get; private init; } = string.Empty;

    /// <summary>The dunnage type of the job's first dunnage part, or empty.</summary>
    public string DunnageType { get; private init; } = string.Empty;

    /// <summary>The job's first dunnage part number, or empty.</summary>
    public string DunnagePart { get; private init; } = string.Empty;

    /// <summary>Whether the part the draft holds is still the job's.</summary>
    public bool IsPartFromJob { get; private set; }

    /// <summary>Whether the sequence the draft holds is still the job's.</summary>
    public bool IsSequenceFromJob { get; private set; }

    /// <summary>Whether the dies the draft holds are still the job's.</summary>
    public bool IsDieFromJob { get; private set; }

    /// <summary>Whether the dunnage part the draft holds is still the job's.</summary>
    public bool IsDunnageFromJob { get; private set; }

    /// <summary>Whether the job supplied anything at all, so the step can say the request was filled.</summary>
    public bool HasAnySeededValue => IsPartFromJob || IsSequenceFromJob || IsDieFromJob || IsDunnageFromJob;

    /// <summary>
    /// The fill these values describe, or <see langword="null"/> when the job cannot be named.
    /// </summary>
    /// <remarks>
    /// A value the job does not carry is empty and its flag starts false, so "the job states nothing here" and "the
    /// requester changed this" are the same answer to every caller that draws the qualifier (FR-008).
    /// </remarks>
    public static GenericRequestJobFill? FromJob(
        string? workOrder,
        string? partNumber = null,
        string? sequence = null,
        string? dieNumber = null,
        string? dunnageType = null,
        string? dunnagePart = null)
    {
        var named = workOrder?.Trim() ?? string.Empty;
        if (named.Length == 0)
        {
            return null;
        }

        var part = partNumber?.Trim() ?? string.Empty;
        var step = sequence?.Trim() ?? string.Empty;
        var die = dieNumber?.Trim() ?? string.Empty;
        var type = dunnageType?.Trim() ?? string.Empty;
        var dunnage = dunnagePart?.Trim() ?? string.Empty;

        return new GenericRequestJobFill
        {
            WorkOrder = named,
            PartNumber = part,
            Sequence = step,
            DieNumber = die,
            DunnageType = type,
            DunnagePart = dunnage,
            IsPartFromJob = part.Length > 0,
            IsSequenceFromJob = step.Length > 0,
            IsDieFromJob = die.Length > 0,
            IsDunnageFromJob = dunnage.Length > 0,
        };
    }

    /// <summary>
    /// The requester changed the part. The sequence goes with it, because a sequence without its part means nothing.
    /// </summary>
    public void ClearPart()
    {
        IsPartFromJob = false;
        IsSequenceFromJob = false;
    }

    /// <summary>The requester changed the sequence, so it is no longer the job's.</summary>
    public void ClearSequence() => IsSequenceFromJob = false;

    /// <summary>The requester changed which dies the request needs.</summary>
    public void ClearDie() => IsDieFromJob = false;

    /// <summary>The requester changed the dunnage part.</summary>
    public void ClearDunnage() => IsDunnageFromJob = false;

    /// <summary>
    /// Every seeded answer is no longer the job's, which is what a changed item means (FR-009).
    /// </summary>
    /// <remarks>
    /// The work order stays, because saying which job the offer came from is still true. What stops being true is that
    /// any answer in the request was taken from it.
    /// </remarks>
    public void ClearAll()
    {
        IsPartFromJob = false;
        IsSequenceFromJob = false;
        IsDieFromJob = false;
        IsDunnageFromJob = false;
    }
}
