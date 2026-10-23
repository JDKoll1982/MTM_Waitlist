namespace MTM_Waitlist.Mock.Models;

/// <summary>One result row of read shape 5, <c>disposition_input</c>.</summary>
/// <remarks>
/// <see cref="WorkOrderStatus"/> is the raw Visual status code. Its MEANING is owned solely by the
/// caller's status vocabulary; the cache never reinterprets it (FR-018).
/// </remarks>
public sealed record VisualDispositionInputRow
{
    /// <summary>The raw Visual work-order status code.</summary>
    public string WorkOrderStatus { get; init; } = string.Empty;

    /// <summary>The still-open work-order quantity.</summary>
    public decimal OpenWorkOrderQuantity { get; init; }

    /// <summary>The finished-goods quantity on hand.</summary>
    public decimal FinishedGoodsQuantity { get; init; }

    /// <summary>Whether the work order has an outside-vendor operation.</summary>
    public bool HasOutsideVendorOperation { get; init; }

    /// <summary>
    /// The <c>RESOURCE_ID</c> of the operation that follows the job's own at the asked work centre, or
    /// <see langword="null"/> when there is none.
    /// </summary>
    public string? NextOperationResourceId { get; init; }

    /// <summary>
    /// Whether work is still owed on the part — the job's own operation has not closed, or an operation follows
    /// it — or <see langword="null"/> when the read did not carry the routing (a cached row written before it was
    /// read). <see langword="false"/> means that operation has closed and nothing follows.
    /// </summary>
    public bool? HasRemainingOperation { get; init; }

    /// <summary>
    /// Whether the part has customer-order lines — i.e. it is a part the plant sells — or <see langword="null"/>
    /// when the read did not carry it. It is what separates Finished Goods from WIP once nothing remains in the
    /// routing: a part nothing buys exists only to feed another work order.
    /// </summary>
    public bool? HasCustomerDemand { get; init; }
}
