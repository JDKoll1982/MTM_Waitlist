namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// SINGLE SOURCE for the Infor Visual status-code sets used by <see cref="RequestDispositionClassifier"/>.
///
/// This is the one place to edit when the real Infor WORK_ORDER / OPERATION status codes are confirmed
/// against live data. The schema tells us these columns are single-character codes:
///   WORK_ORDER.STATUS  (nchar(1))  — order state
///   OPERATION.STATUS   (nchar(1))  — operation state
///
/// Confirmed derivation rules (from the Infor schema / guide — file 14 Phase 6):
///   - Outside Service is driven by OPERATION vendor/service fields, NOT by a status code, so it needs
///     no code entry here.
///   - FG vs WIP is driven by whether the order/operation is open (in process) or closed/completed.
///
/// ⚠️ STATUS-CODE VALUES BELOW ARE PLACEHOLDERS. Infor's exact single-char code set is not yet
/// confirmed against a live pull (the cloud portal is login-gated). Replace these string sets once the
/// codes are observed. The classifier logic does not need to change.
/// </summary>
public static class RequestDispositionStatusCodes
{
    /// <summary>
    /// WORK_ORDER.STATUS / OPERATION.STATUS codes that mean the work is still OPEN / in process (→ WIP).
    /// TODO(2026-09-07): confirm against live Infor data. Common candidates: 'O' open, 'R' released,
    /// 'N' not started. Remove any that are wrong once observed.
    /// </summary>
    public static readonly IReadOnlySet<string> OpenStatusCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "O", // Open (candidate — confirm)
            "R", // Released (candidate — confirm)
            "N", // Not started (candidate — confirm)
        };

    /// <summary>
    /// WORK_ORDER.STATUS / OPERATION.STATUS codes that mean the work is CLOSED / completed (→ Finished Goods
    /// when stock is on hand). TODO(2026-09-07): confirm against live Infor data. Common candidates:
    /// 'C' closed/completed, 'P' complete. Remove any that are wrong once observed.
    /// </summary>
    public static readonly IReadOnlySet<string> ClosedStatusCodes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "C", // Closed (candidate — confirm)
            "P", // Complete (candidate — confirm)
        };
}
