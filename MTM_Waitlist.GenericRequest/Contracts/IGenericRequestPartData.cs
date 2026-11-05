using MTM_Waitlist.GenericRequest.Models;

namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// The three questions the part step asks: what a typed term completes to, what sequences a part carries, and how
/// much is on hand at a chosen sequence.
/// </summary>
/// <remarks>
/// <para>
/// The seam is deliberately this narrow. Editing the wide <see cref="IGenericRequestStepData"/> would tie the
/// part step to every other step's answer, and faking nine methods to test three is not a test. When the flow's
/// live implementation needs the part's answers it forwards to the same data class through this interface.
/// </para>
/// <para>
/// Every read is live and none is cached, mirrored or estimated: FR-017 forbids substituting a stored sequence
/// list or quantity. An unreachable system answers <see cref="GenericRequestStepResult{T}.Unavailable"/> with the
/// attempt time, so the screen offers a retry rather than a guess. Only a cancellation propagates.
/// </para>
/// </remarks>
public interface IGenericRequestPartData
{
    /// <summary>
    /// The parts a typed term completes to, drawn from Infor Visual and from the WIP floor, each row naming the
    /// system that supplied it (FR-001).
    /// </summary>
    Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
        string term,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every sequence attributable to <paramref name="partNumber"/>, each number once, with every source that
    /// supplied it (FR-004). A sequence whose every operation is cancelled is not offered (FR-005).
    /// </summary>
    Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
        string partNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// How much of <paramref name="partNumber"/> is on hand at <paramref name="sequence"/>, decided by that
    /// sequence's operation state read at this moment (FR-009, FR-011).
    /// </summary>
    Task<GenericRequestQuantityReading> ReadQuantityAsync(
        string partNumber,
        string sequence,
        CancellationToken cancellationToken = default);
}
