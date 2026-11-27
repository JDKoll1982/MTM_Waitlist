namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Resolves the coil "Average coil weight" readout from
/// <c>mtm_receiving_application.receiving_history</c>.
/// </summary>
/// <remarks>
/// <para>
/// Average coil weight = AVG(<c>quantity</c>) across every receiving_history row whose
/// <c>part_id</c> matches the coil part. Each row is one received skid; its <c>quantity</c>
/// is the skid weight the material handler actually grasps (some skids hold 2+ coils, so we
/// average the whole skid, not individual coils). Current stock is irrelevant here.
/// </para>
/// <para>
/// The receiving store is always read live: there is no sample/mock short-circuit (FR-001). The answer is a number of
/// pounds and nothing else: the unit and the machine's number formatting belong where the weight is written for a
/// person, which is <c>CoilWeightText</c> (FR-024).
/// </para>
/// </remarks>
public interface IAverageCoilWeightService
{
    /// <summary>
    /// The average received weight in whole pounds, or null when the part has no receiving history.
    /// </summary>
    Task<decimal?> ResolveAverageCoilWeightAsync(string partId, CancellationToken cancellationToken = default);
}
