using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Tests.Fixtures;

/// <summary>
/// A settable person identity for the tests that are about a consumer rather than about the store.
/// </summary>
/// <remarks>
/// <para>
/// The production contract is read-only on purpose (FR-022), so a test that needs a different person — a second
/// sign-in, for instance — cannot assign through the interface. This fake exists for exactly those cases: it
/// carries the same members with setters, so a test that used to build a mutable launch-state object can build a
/// person instead and still move them mid-test.
/// </para>
/// <para>
/// <see cref="Holds"/> answers the way the production implementation does: from the role in force and from the
/// held set, case insensitively, tolerating a <c>role:</c> prefix, so a test that asserts a developer gate is
/// proving the gate rather than the fake's spelling.
/// </para>
/// </remarks>
public sealed class FakePersonIdentity : IPersonIdentity
{
    public long UserId { get; set; }

    public string SignInName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? EmployeeNumber { get; set; }

    public string CurrentRoleCode { get; set; } = string.Empty;

    public IReadOnlyList<string> HeldRoleCodes { get; set; } = Array.Empty<string>();

    public bool IsSignedIn => UserId > 0;

    public bool Holds(string roleCode)
    {
        var normalized = Normalize(roleCode);

        if (normalized.Length == 0)
        {
            return false;
        }

        return HeldRoleCodes.Any(code => string.Equals(Normalize(code), normalized, StringComparison.OrdinalIgnoreCase))
            || string.Equals(Normalize(CurrentRoleCode), normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? roleCode)
    {
        var trimmed = roleCode?.Trim() ?? string.Empty;

        return trimmed.StartsWith("role:", StringComparison.OrdinalIgnoreCase)
            ? trimmed[5..].Trim()
            : trimmed;
    }
}
