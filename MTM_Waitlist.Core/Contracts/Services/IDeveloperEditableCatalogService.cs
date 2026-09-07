using MTM_Waitlist.Module_Core.Models;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Presents the unified list of Developer-editable tables for the Developer-settings editor dropdown: the
/// <c>mock_*</c> tables (enumerated from <c>mock_master_tables_registry</c> via <see cref="IMockMasterDataService"/>)
/// plus the two real (non-mock) request-type/subtype tables. Each entry carries a <see cref="DeveloperTableSourceKind"/>
/// so the editor routes reads/writes to <c>MockMasterDataService</c> (mock) or <c>RequestTypeEditorService</c> (real).
/// SP-only; the real request tables are intentionally not part of any mock registry.
/// </summary>
public interface IDeveloperEditableCatalogService
{
    /// <summary>Returns mock tables (registry order) followed by the two real request tables, de-duplicated.</summary>
    Task<IReadOnlyList<DeveloperEditableTable>> GetTablesAsync(CancellationToken cancellationToken = default);
}
