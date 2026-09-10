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
}
