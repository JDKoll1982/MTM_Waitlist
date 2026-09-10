using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Pure, config-driven classifier that derives an <see cref="RequestDisposition"/> (FG / WIP /
/// Outside Service) from the Infor Visual job + part + operation state for a work center's active
/// setup job.
///
/// Type/Category refactor (2026-09-07). Infor Visual is a job-shop ERP: a Finished-Product part's
/// disposition is NOT a single user-set field — it is derived. The only Infor-specific knowledge is
/// the set of WORK_ORDER / OPERATION status codes, which are deliberately isolated in
/// <see cref="RequestDispositionStatusCodes"/> (see that class). Change the codes there when they are
/// confirmed against live data; this classifier logic stays stable.
///
/// Derivation priority (see file 14 Phase 6 research):
///   1. OutsideService — the part's operation is subcontracted to an outside vendor
///      (OPERATION.VENDOR_ID / SERVICE_ID / SERVICE_PART_ID non-empty).
///   2. FinishedGoods — on-hand quantity &gt; 0 at a finished-goods / shipping location and the work
///      order is closed/completed (no open work-order quantity remaining).
///   3. WorkInProcess — open quantity still on the work order / MT_WIP_INVENTORY.
///   4. Unknown — no rule matched.
/// </summary>
public static class RequestDispositionClassifier
{
    /// <summary>
    /// Snapshot of the fields needed to derive disposition. This keeps the classifier a pure function
    /// (no database access) so it is trivially unit-testable and can be fed by any disposition source.
    /// </summary>
    public readonly record struct DispositionInput(
        string? WorkOrderStatus,
        decimal FinishedGoodsQuantity,
        decimal OpenWorkOrderQuantity,
        bool HasOutsideVendorOperation)
    {
        /// <summary>Whether a finished-goods (shipping) location holds stock for this part.</summary>
        public bool HasFinishedGoodsOnHand => FinishedGoodsQuantity > 0m;

        /// <summary>Whether open quantity remains on the work order / WIP inventory.</summary>
        public bool HasOpenWorkOrderQuantity => OpenWorkOrderQuantity > 0m;
    }

    /// <summary>Classifies a single part/job snapshot into a disposition.</summary>
    public static RequestDisposition Classify(DispositionInput input)
    {
        // 1. Outside service takes precedence: the part is sitting at (or heading to) a vendor.
        if (input.HasOutsideVendorOperation)
        {
            return RequestDisposition.OutsideService;
        }

        // 2. Finished goods: closed work order + stock on hand at a shipping location.
        if (input.HasFinishedGoodsOnHand && !input.HasOpenWorkOrderQuantity && IsClosedStatus(input.WorkOrderStatus))
        {
            return RequestDisposition.FinishedGoods;
        }

        // 3. Work in process: open quantity still on the order / WIP.
        if (input.HasOpenWorkOrderQuantity || IsOpenStatus(input.WorkOrderStatus))
        {
            return RequestDisposition.WorkInProcess;
        }

        return RequestDisposition.Unknown;
    }

    private static bool IsOpenStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && RequestDispositionStatusCodes.OpenStatusCodes.Contains(status);

    private static bool IsClosedStatus(string? status) =>
        !string.IsNullOrWhiteSpace(status) && RequestDispositionStatusCodes.ClosedStatusCodes.Contains(status);
}
