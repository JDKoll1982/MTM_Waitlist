namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// One Infor Visual <c>GetDispositionInput.sql</c> result row (WORK_ORDER/OPERATION/PART snapshot).
/// </summary>
/// <remarks>
/// It lives in a file of its own rather than beside <see cref="RequestDispositionMapper"/> because the repository's
/// C#/XAML naming rule requires a public type's file to be named after it, and this row is part of the mapper's
/// public surface — a caller can hold one and hand it back. Keeping it where it was made the file carry two public
/// types with only one of their names, which the guard reports.
/// </remarks>
public sealed record InforDispositionRow
{
    public string? WorkOrderStatus { get; init; }

    public decimal OpenWorkOrderQuantity { get; init; }

    public decimal FinishedGoodsQuantity { get; init; }

    public bool HasOutsideVendorOperation { get; init; }

    /// <summary>
    /// The <c>RESOURCE_ID</c> of the operation that follows the one the job is set up on, or <see langword="null"/>
    /// when the routing was not read or the job's operation has none after it.
    /// </summary>
    public string? NextOperationResourceId { get; init; }

    /// <summary>
    /// Whether work is still owed on the part, or <see langword="null"/> when the routing was not read.
    /// <see langword="true"/> covers both "the job's own operation has not closed yet" and "an operation follows
    /// it"; <see langword="false"/> means that operation has closed and nothing follows, so what the job produces
    /// is the finished product.
    /// </summary>
    public bool? HasRemainingOperation { get; init; }

    /// <summary>
    /// Whether the part has customer-order lines — it is a part the plant sells — or <see langword="null"/> when
    /// the read did not carry it. This is what decides Finished Goods once nothing remains in the routing.
    /// </summary>
    public bool? HasCustomerDemand { get; init; }
}
