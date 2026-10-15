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
}
