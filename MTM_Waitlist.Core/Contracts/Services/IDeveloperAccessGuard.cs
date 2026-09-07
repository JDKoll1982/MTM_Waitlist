namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Centralized, role-based authorization for Developer/Admin features: the Developer Settings page, mock-master
/// data editing, real request-type/subtype catalog editing, and changing the central mock config. The repo uses
/// case-insensitive string roles (e.g. <c>developer</c>, <c>admin</c>/<c>administrator</c>); this guard is the
/// single source of truth so page visibility and write-path checks stay consistent. Pure and unit-testable.
/// </summary>
public interface IDeveloperAccessGuard
{
    /// <summary>The roles allowed to access Developer tools (normalized lowercase), for diagnostics.</summary>
    IReadOnlyList<string> AllowedDeveloperRoles { get; }

    /// <summary>True if the role may open the role-gated Developer Settings page (main nav).</summary>
    bool CanAccessDeveloperSettings(string? currentUserRole);

    /// <summary>True if the role may edit the mock master data tables.</summary>
    bool CanEditMockMasterData(string? currentUserRole);

    /// <summary>True if the role may edit the real request-type/subtype catalog.</summary>
    bool CanEditRealCatalog(string? currentUserRole);

    /// <summary>True if the role may change the central (all-clients) mock config.</summary>
    bool CanChangeCentralMockConfig(string? currentUserRole);
}
