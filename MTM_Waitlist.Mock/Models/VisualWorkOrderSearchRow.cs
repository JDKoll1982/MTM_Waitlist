namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 8, <c>work_order_search</c>.</summary>
/// <remarks>
/// The projection is identical live and cached (FR-004): the live search returns
/// <c>WorkOrder, PartNumber, PartDescription, CustomerName, WorkCenter, Status, WantDate</c>, and
/// <c>sp_visual_work_order_search_get</c> projects the same names in the same order.
/// </remarks>
public sealed record VisualWorkOrderSearchRow
{
    /// <summary>The work order's canonical <c>WO-######</c> base id — the value the entry page accepts.</summary>
    public string WorkOrder { get; init; } = string.Empty;

    /// <summary>The part the work order builds.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The part description.</summary>
    public string PartDescription { get; init; } = string.Empty;

    /// <summary>
    /// The customer's real name, as Infor Visual records it on the order's allocation. Empty for a stock
    /// order, which has no customer.
    /// </summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>
    /// The work centre the job runs in: the resource of its first operation that is a real work centre.
    /// </summary>
    /// <remarks>
    /// A group resource (<c>500-GRP</c>, <c>090-GRP</c>, …) is a routing grouping rather than a place a job is set
    /// up, so it is skipped: the value here is the first operation whose resource is typed <c>W</c> and does not end
    /// in <c>-GRP</c>. A work order with no such operation is not a production job and is not returned at all
    /// (operator requirement, 2026-11-04).
    /// </remarks>
    public string WorkCenter { get; init; } = string.Empty;

    /// <summary>The Visual status code: <c>R</c> released, <c>U</c> unreleased, <c>F</c> firmed.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The order's desired want date, when Visual carries one.</summary>
    public DateTimeOffset? WantDate { get; init; }
}
