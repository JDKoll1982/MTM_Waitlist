using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One choice in the user list's role filter: a role the catalogue holds, or the "every role" choice that filters
/// nothing (FR-089).
/// </summary>
/// <remarks>
/// The label is resolved from the resource keyed on the role <b>code</b>, falling back to the name the catalogue
/// returned, so a role's displayed text follows its code rather than being matched as text anywhere.
/// </remarks>
public sealed class UserListFilterOption
{
    private UserListFilterOption(string roleCode, string label, bool isAllRoles)
    {
        RoleCode = roleCode;
        Label = label;
        IsAllRoles = isAllRoles;
    }

    /// <summary>The "every role" choice: the filter with no role narrowing.</summary>
    /// <param name="label">The choice's text, already resolved.</param>
    public static UserListFilterOption AllRoles(string label) => new(string.Empty, label, isAllRoles: true);

    /// <summary>One catalogued role.</summary>
    /// <param name="roleCode">The role's code, which is what the filter asks the store for.</param>
    /// <param name="roleName">The role's name as the catalogue returned it, used when no text is keyed for the code.</param>
    public static UserListFilterOption Role(string roleCode, string roleName) =>
        new(roleCode, LabelFor(roleCode, roleName), isAllRoles: false);

    /// <summary>The role's code, or an empty string for the "every role" choice.</summary>
    public string RoleCode { get; }

    /// <summary>What the reader sees in the picker.</summary>
    public string Label { get; }

    /// <summary>Whether this choice narrows nothing.</summary>
    public bool IsAllRoles { get; }

    /// <summary>What a picker shows for this choice.</summary>
    public override string ToString() => Label;

    private static string LabelFor(string roleCode, string roleName)
    {
        var localized = $"Role_{roleCode}".GetLocalized();

        // The resource mechanism returns the key when it holds no entry, and a key is never shown to a person.
        return string.Equals(localized, $"Role_{roleCode}", StringComparison.Ordinal)
            ? roleName
            : localized;
    }
}
