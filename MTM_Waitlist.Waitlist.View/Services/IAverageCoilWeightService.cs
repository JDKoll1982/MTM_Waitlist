namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Resolves the coil "Average coil weight" readout from
/// <c>mtm_receiving_application.receiving_history</c>.
/// </summary>
/// <remarks>
/// Average coil weight = AVG(<c>quantity</c>) across every receiving_history row whose
/// <c>part_id</c> matches the coil part. Each row is one received skid; its <c>quantity</c>
/// is the skid weight the material handler actually grasps (some skids hold 2+ coils, so we
/// average the whole skid, not individual coils). Current stock is irrelevant here.
/// Routing follows the mock-toggle rule: <c>Feature.RecvMockData</c> ON returns sample data,
/// OFF runs the real query against the receiving application database.
/// </remarks>
public interface IAverageCoilWeightService
{
    /// <summary>Returns a display string such as "5,000 lb", or empty when nothing is found.</summary>
    Task<string> ResolveAverageCoilWeightTextAsync(string partId, CancellationToken cancellationToken = default);
}
