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
/// Derivation priority (see file 14 Phase 6 research, and FR-092 for the unchanged production order):
///   1. OutsideService — the operation that follows the job's own is the subcontracted one
///      (its <c>RESOURCE_ID</c> is the plant's <c>OUTSIDE_SERVICE</c> shop resource). The legacy vendor fields on
///      the operation remain the signal when the routing could not be read.
///   2. Ncm — the stock occupies a non-conforming (NCM) location. It is decided from the <b>location</b> and
///      from nothing else, because NCM is a location state and not a production type (FR-093). It is checked
///      before the two production arms so quarantined stock is never reported as Finished Goods or WIP, and
///      after Outside Service so the documented priority is unchanged.
///   3. WorkInProcess — a further operation remains in the routing, so the part is still being made.
///   4. FinishedGoods — no further operation remains: what the job's own operation produces is the finished
///      product. This is what the plant's tag print reads (research 2026-10-23), and it is the only test that can
///      name Finished Goods for a job that is still running — in-process stock is never sitting at a shipping
///      location yet, so the stock quantity is a confirmation, never the discriminator.
///   5. Unknown — no rule matched.
/// <para>
/// The routing arms answer only when the read supplied them; a snapshot without them (the floor-stock half, or a
/// cached row written before the routing was read) falls through to the stock/quantity/status arms exactly as
/// before, so nothing regresses where the routing is unavailable.
/// </para>
/// </summary>
public static class RequestDispositionClassifier
{
    /// <summary>
    /// Snapshot of the fields needed to derive disposition. This keeps the classifier a pure function
    /// (no database access) so it is trivially unit-testable and can be fed by any disposition source.
    /// <para>
    /// There is deliberately no place for a part's recorded item type: FR-093 requires NCM to come from the
    /// location the stock occupies, never from the type the plant recorded against the part, so the classifier
    /// has nothing of that kind to read.
    /// </para>
    /// </summary>
    public readonly record struct DispositionInput(
        string? WorkOrderStatus,
        decimal FinishedGoodsQuantity,
        decimal OpenWorkOrderQuantity,
        bool HasOutsideVendorOperation,
        decimal NonConformingQuantity,
        string? NextOperationResourceId = null,
        bool? HasRemainingOperation = null)
    {
        /// <summary>Whether a finished-goods (shipping) location holds stock for this part.</summary>
        public bool HasFinishedGoodsOnHand => FinishedGoodsQuantity > 0m;

        /// <summary>Whether open quantity remains on the work order / WIP inventory.</summary>
        public bool HasOpenWorkOrderQuantity => OpenWorkOrderQuantity > 0m;

        /// <summary>
        /// Whether the stock occupies a non-conforming (NCM) location. The quantity is the stock found at such
        /// a location, so a positive figure is the location signal itself rather than a second reading of it.
        /// </summary>
        public bool HasNonConformingStock => NonConformingQuantity > 0m;

        /// <summary>
        /// Whether the read supplied the job's routing — the operation that follows the one it is set up on.
        /// <see langword="false"/> on the floor-stock half and on a cached row written before the routing was
        /// read; the classifier then falls through to the stock/quantity/status arms.
        /// </summary>
        public bool HasRouting => HasRemainingOperation.HasValue;

        /// <summary>Whether a further operation remains after the job's own — the part is still being made.</summary>
        public bool HasRemainingWork => HasRemainingOperation == true;

        /// <summary>Whether the routing is exhausted — what the job's own operation produces is the finished product.</summary>
        public bool HasNoRemainingWork => HasRemainingOperation == false;

        /// <summary>
        /// Whether the operation that follows the job's own is the plant's subcontracted one. Its
        /// <c>RESOURCE_ID</c> is the whole signal: Infor Visual routes subcontracted work to the
        /// <c>OUTSIDE_SERVICE</c> shop resource, which is what the plant's tag print reads (research 2026-10-23).
        /// </summary>
        public bool NextOperationIsOutsideService =>
            string.Equals(
                NextOperationResourceId?.Trim(),
                RequestDispositionStatusCodes.OutsideServiceResourceId,
                StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Classifies a single part/job snapshot into a disposition.</summary>
    public static RequestDisposition Classify(DispositionInput input)
    {
        // 1. Outside service takes precedence: the work that follows the job's own operation is subcontracted.
        //    The vendor fields on the operation answer the same question while the routing is unread.
        if (input.NextOperationIsOutsideService
            || (!input.HasRouting && input.HasOutsideVendorOperation))
        {
            return RequestDisposition.OutsideService;
        }

        // 2. Non-conforming stock: the location it occupies is the whole signal, and such stock is never a
        // production type — not Finished Goods and not WIP (FR-093).
        if (input.HasNonConformingStock)
        {
            return RequestDisposition.Ncm;
        }

        // 3./4. The routing decides between the two production types: work still ahead of the part makes it WIP,
        //    and a routing that ends at this operation makes what the job produces its finished product.
        if (input.HasRemainingWork)
        {
            return RequestDisposition.WorkInProcess;
        }

        if (input.HasNoRemainingWork)
        {
            return RequestDisposition.FinishedGoods;
        }

        // 5. No routing was read: the stock, open quantity and status are all there is to go on.

        // Finished goods: closed work order + stock on hand at a shipping location.
        if (input.HasFinishedGoodsOnHand && !input.HasOpenWorkOrderQuantity && IsClosedStatus(input.WorkOrderStatus))
        {
            return RequestDisposition.FinishedGoods;
        }

        // Work in process: open quantity still on the order / WIP.
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
