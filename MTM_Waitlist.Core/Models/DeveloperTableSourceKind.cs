namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Which data family a Developer-editable table belongs to, so the editor knows which read/write service and
/// edit path to route to.
/// </summary>
public enum DeveloperTableSourceKind
{
    /// <summary>A <c>mock_*</c> table edited via <c>MockMasterDataService</c> (per-table SPs).</summary>
    Mock,

    /// <summary>A real (non-mock) request-type/subtype table edited via <c>RequestTypeEditorService</c>.</summary>
    RealCatalog,
}
