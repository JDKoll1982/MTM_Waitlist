namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 1, <c>work_order_lookup</c>.</summary>
/// <remarks>
/// The projection is identical live and cached (FR-004): the live read returns
/// <c>PartNumber, Description, WorkCenter</c> ordered by <c>wo.PART_ID</c>, and
/// <c>sp_visual_work_order_lookup_get</c> projects the same names in the same order.
/// </remarks>
public sealed record VisualWorkOrderLookupRow
{
    /// <summary>The part the work order builds.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The part description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The work centre the operation runs in.</summary>
    public string WorkCenter { get; init; } = string.Empty;
}
