namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// SINGLE SOURCE for the Infor Visual status-code sets used by <see cref="RequestDispositionClassifier"/>.
///
/// This is the one place to edit when the real Infor WORK_ORDER / OPERATION status codes change. The
/// schema tells us these columns are single-character codes:
///   WORK_ORDER.STATUS  (nchar(1))  — order state
///   OPERATION.STATUS   (nchar(1))  — operation state
///
/// ✅ CONFIRMED against live Infor Visual data (2026-09-08, VISUAL/MTMFG, ENUM_CODES table +
/// observed row counts) — file 14 Phase 6:
///   WORK_ORDER.STATUS  in use:  C=68,278  R=680  U=42,138  X=1,404
///   OPERATION.STATUS   in use:  C=244,112 R=2,840 U=13,046  X=9,030
///   (F = Firmed is defined by the system but has 0 rows in this install.)
///   ENUM_CODES (TABLE_NAME='WORK_ORDER'/'OPERATION', COLUMN_NAME='STATUS') maps:
///     C = Closed, F = Firmed, R = Released, U = Unreleased, X = Cancelled.
///
/// Confirmed derivation rules (from the live schema / data — file 14 Phase 6):
///   - Outside Service is driven by OPERATION vendor/service fields, NOT by a status code, so it needs
///     no code entry here. Confirmed live: ~26.9k OPERATION rows carry a non-empty VENDOR_ID /
///     SERVICE_ID / SERVICE_PART_ID (RESOURCE_ID = 'OUTSIDE_SERVICE').
///   - FG vs WIP is driven by whether the order/operation is open (in process) or closed/completed:
///       OPEN (→ WIP)   = R (Released), U (Unreleased); F (Firmed) is also an open/planned state
///                        (defined but currently unused in this install).
///       CLOSED (→ FG)  = C (Closed).
///       X (Cancelled)  = deliberately in NEITHER set — a cancelled order must never classify as FG
///                        (and only as WIP if open quantity still physically exists).
///   - WORK_ORDER.PROD_ORDER_TYPE is all NULL in this install (112,500 rows) — NOT a disposition
///     driver. Do not gate on it.
///   - Open work-order quantity = WORK_ORDER.DESIRED_QTY - WORK_ORDER.RECEIVED_QTY. Infor's own
///     MT_WIP_INVENTORY is empty (unused); MTM floor WIP/FG quantities live in the separate MTM WIP
///     Application (MySQL, `mtm_wip_application_winforms.inv_inventory` keyed by part/location/operation,
///     with finished-goods areas marked in `md_locations`, e.g. FG / DC-FG / FLOOR - FINISHED GOODS).
/// </summary>
public static class RequestDispositionStatusCodes
{
    /// <summary>
    /// WORK_ORDER.STATUS / OPERATION.STATUS codes that mean the work is still OPEN / in process (→ WIP).
    /// CONFIRMED 2026-09-08 against live data (ENUM_CODES): R = Released, U = Unreleased, F = Firmed.
    /// F (Firmed) is defined but currently has 0 rows in this install; kept because a firmed order is
    /// never closed/completed.
    /// </summary>
    public static readonly IReadOnlySet<string> OpenStatusCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "R", // Released   — confirmed in use (WORK_ORDER 680 / OPERATION 2,840)
            "U", // Unreleased — confirmed in use (WORK_ORDER 42,138 / OPERATION 13,046)
            "F", // Firmed     — defined by ENUM_CODES; open/planned (0 rows in this install)
        };

    /// <summary>
    /// WORK_ORDER.STATUS / OPERATION.STATUS codes that mean the work is CLOSED / completed (→ Finished
    /// Goods when stock is on hand at a finished-goods location). CONFIRMED 2026-09-08 against live data
    /// (ENUM_CODES): C = Closed.
    /// </summary>
    public static readonly IReadOnlySet<string> ClosedStatusCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "C", // Closed — confirmed in use (WORK_ORDER 68,278 / OPERATION 244,112)
        };
}
