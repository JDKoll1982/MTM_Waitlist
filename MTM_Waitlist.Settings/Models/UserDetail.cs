using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One person as the person's own page shows them (FR-097 to FR-101).
/// </summary>
/// <remarks>
/// <para>
/// The type carries no permission and no refusal of its own: reading is never refused, so an account the reader
/// cannot act on is still readable and the page states the refusal at the fields and the actions (FR-027, FR-101).
/// </para>
/// <para>
/// The role's text is resolved from the resource keyed on the role <b>code</b>, falling back to the name the store
/// returned, so a role's displayed text follows its code rather than being matched as text anywhere.
/// </para>
/// </remarks>
public sealed class UserDetail
{
    public UserDetail(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        UserId = account.UserId;
        PublicId = account.PublicId;
        UsernameNormalized = account.UsernameNormalized;
        FirstName = account.FirstName;
        LastName = account.LastName;
        DisplayName = account.DisplayName;
        EmployeeIdentifier = account.EmployeeIdentifier;
        RoleCode = account.RoleCode;
        RoleName = account.RoleName;
        RoleRank = account.RoleRank;
        IsActive = account.IsActive;
        TemporaryCredentialFailedAttempts = account.TemporaryCredentialFailedAttempts;
    }

    /// <summary>The person's id, which is what every write names.</summary>
    public long UserId { get; }

    /// <summary>The person's public identifier, as the store holds it.</summary>
    public string PublicId { get; }

    /// <summary>The sign-in name as stored, which is the upper-case form (FR-002).</summary>
    public string UsernameNormalized { get; }

    /// <summary>The person's first name, as the store holds it.</summary>
    public string FirstName { get; }

    /// <summary>The person's last name, as the store holds it.</summary>
    public string LastName { get; }

    /// <summary>The name shown wherever the person is named.</summary>
    public string DisplayName { get; }

    /// <summary>The employee number. Deliberately not unique, so two people may share one (FR-004).</summary>
    public string EmployeeIdentifier { get; }

    /// <summary>The role's code, which is the identity every rank comparison reads.</summary>
    public string RoleCode { get; }

    /// <summary>The role's name as the store returned it, used when no text is keyed for the code.</summary>
    public string RoleName { get; }

    /// <summary>The rung the person's role stands at, read from the catalogue rather than computed here.</summary>
    public int RoleRank { get; }

    /// <summary>Whether the account may sign in at all.</summary>
    public bool IsActive { get; }

    /// <summary>
    /// Wrong attempts against a temporary credential. An account with a real password is never near the limit, so
    /// the page says nothing about this until there is something to say.
    /// </summary>
    public int TemporaryCredentialFailedAttempts { get; }

    /// <summary>The role's displayed text.</summary>
    public string RoleText => RoleTextFor(RoleCode, RoleName);

    /// <summary>Whether the person is switched off, which is what the page marks them with.</summary>
    public bool IsSwitchedOff => !IsActive;

    /// <summary>The mark a switched-off person carries, and an empty string for everybody else.</summary>
    public string StatusText => IsSwitchedOff ? "UserManagement_Row.SwitchedOff".GetLocalized() : string.Empty;

    /// <summary>The role's text, from the resource keyed on the code, or the catalogue's own name as a fallback.</summary>
    internal static string RoleTextFor(string roleCode, string roleName)
    {
        var localized = $"Role_{roleCode}".GetLocalized();

        // The resource mechanism returns the key when it holds no entry, and a key is never shown to a person.
        return string.Equals(localized, $"Role_{roleCode}", StringComparison.Ordinal)
            ? roleName
            : localized;
    }

    /// <summary>
    /// The role's text when only its code is known, which is the case for a read that carries no role name. The
    /// code is the last resort rather than the first: it is not a display name, but it is the only thing left when
    /// neither the resource map nor the store supplies one.
    /// </summary>
    internal static string RoleTextFor(string roleCode) => RoleTextFor(roleCode, roleCode);
}
