using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One row of the user list: the five facts a row always shows, plus what a caller needs to open the person
/// (FR-086, FR-087).
/// </summary>
/// <remarks>
/// <para>
/// The row shows the sign-in name, the name, the employee number, the role and the active state, at every width.
/// Keeping all five here rather than composing them in the view is what makes the width rule a layout concern:
/// the values do not change with the window, only how they are laid out does.
/// </para>
/// <para>
/// The role's text is resolved from the resource keyed on the role <b>code</b>, falling back to the name the
/// store returned, so a role's displayed name is found by reading the code rather than by matching text.
/// </para>
/// </remarks>
public sealed class UserSummary
{
    public UserSummary(UserRosterRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        UserId = row.UserId;
        PublicId = row.PublicId;
        UsernameNormalized = row.UsernameNormalized;
        DisplayName = row.DisplayName;
        EmployeeIdentifier = row.EmployeeIdentifier;
        RoleCode = row.RoleCode;
        RoleName = row.RoleName;
        IsActive = row.IsActive;
    }

    /// <summary>The person's id, which is what opening their page needs.</summary>
    public long UserId { get; }

    /// <summary>The person's public identifier, as the store holds it.</summary>
    public string PublicId { get; }

    /// <summary>Fact 1: the sign-in name, as stored (upper case).</summary>
    public string UsernameNormalized { get; }

    /// <summary>Fact 2: the person's name.</summary>
    public string DisplayName { get; }

    /// <summary>Fact 3: the employee number. Deliberately not unique, so two people may share one.</summary>
    public string EmployeeIdentifier { get; }

    /// <summary>The role's code, which is the identity every gate compares.</summary>
    public string RoleCode { get; }

    /// <summary>The role's name as the store returned it, used only when the resource holds no text for the code.</summary>
    public string RoleName { get; }

    /// <summary>Fact 4: the role's text.</summary>
    public string RoleText
    {
        get
        {
            var localized = $"Role_{RoleCode}".GetLocalized();

            // The resource mechanism returns the key when it has no entry, which is never shown to a person.
            return string.Equals(localized, $"Role_{RoleCode}", StringComparison.Ordinal)
                ? RoleName
                : localized;
        }
    }

    /// <summary>Fact 5, as the store holds it: whether the account may sign in at all.</summary>
    public bool IsActive { get; }

    /// <summary>
    /// The mark a switched-off person carries, and an empty string for everybody else.
    /// </summary>
    /// <remarks>
    /// Only the people who are switched off are marked, which is what the shipped resource holds a string for: an
    /// active person needs no label to say so, and a second key for it would be a label nobody needs.
    /// </remarks>
    public string StatusText => IsSwitchedOff ? "UserManagement_Row.SwitchedOff".GetLocalized() : string.Empty;

    /// <summary>
    /// Whether the person is switched off, which is what the row marks them with. A switched-off person is listed
    /// like anybody else and is never hidden by this (FR-088).
    /// </summary>
    public bool IsSwitchedOff => !IsActive;

    /// <summary>
    /// What the row announces when it is reached by keyboard: the person, their four other facts, and what
    /// opening the row does.
    /// </summary>
    public string OpenAnnouncement
    {
        get
        {
            var facts = $"{DisplayName}, {UsernameNormalized}, {EmployeeIdentifier}, {RoleText}";
            var status = IsSwitchedOff ? $", {StatusText}" : string.Empty;

            return $"{facts}{status}. {"UserManagement_Row.Open".GetLocalized()}";
        }
    }
}
