using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IDeveloperAccessGuard"/>
public sealed class DeveloperAccessGuard : IDeveloperAccessGuard
{
    private readonly IReadOnlyCollection<string> _allowed;

    public DeveloperAccessGuard()
        : this(new[] { "developer", "admin", "administrator" })
    {
    }

    /// <summary>
    /// Roles that may access Developer tools. Each is trimmed + lowercased; comparison is
    /// case-insensitive (matching how <c>ShellViewModel</c> normalizes roles).
    /// </summary>
    public DeveloperAccessGuard(IEnumerable<string> allowedRoles)
    {
        _allowed = allowedRoles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant())
            .ToHashSet();
    }

    public IReadOnlyList<string> AllowedDeveloperRoles =>
        _allowed.OrderBy(role => role, StringComparer.Ordinal).ToArray();

    public bool CanAccessDeveloperSettings(string? currentUserRole) => IsAllowed(currentUserRole);

    public bool CanEditMockMasterData(string? currentUserRole) => IsAllowed(currentUserRole);

    public bool CanEditRealCatalog(string? currentUserRole) => IsAllowed(currentUserRole);

    public bool CanChangeCentralMockConfig(string? currentUserRole) => IsAllowed(currentUserRole);

    private bool IsAllowed(string? role)
        => !string.IsNullOrWhiteSpace(role) && _allowed.Contains(role.Trim().ToLowerInvariant());
}
