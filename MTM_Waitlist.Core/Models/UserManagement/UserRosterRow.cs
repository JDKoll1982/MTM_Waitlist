namespace MTM_Waitlist.Module_Core.Models.UserManagement;

/// <summary>
/// One row of the user list: the five facts a row always shows, plus the identifiers a caller needs to open the
/// person or to name their role (FR-086, FR-087).
/// </summary>
/// <remarks>
/// The list shows everyone, switched-off people included, and marks them (FR-088). A row keeps all five facts at
/// every width, which is a rendering rule rather than a rule about this type.
/// </remarks>
public sealed record UserRosterRow(
    long UserId,
    string PublicId,
    string UsernameNormalized,
    string DisplayName,
    string EmployeeIdentifier,
    string RoleCode,
    string RoleName,
    int RoleRank,
    bool IsActive);
