using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The fields a form holds for one person, together with the person they belong to (FR-097, FR-099).
/// </summary>
/// <remarks>
/// <para>
/// One type serves both halves of an edit. Read off the loaded account it is the baseline the form was opened
/// with, and read off the form it is what the reader has typed, so the difference between the two <i>is</i> the
/// unsaved-change state the page needs before it acts on a half-edited person (FR-100). The same value is what a
/// save submits, which is why the create form and the person's page can share one flow: a create is an edit of a
/// person who does not exist yet and whose id is zero.
/// </para>
/// <para>
/// The display name is deliberately absent: it is derived from the two name parts rather than typed, so one person
/// cannot be shown under two spellings.
/// </para>
/// </remarks>
public sealed record UserEditRequest(
    long UserId,
    string Username,
    string FirstName,
    string LastName,
    string EmployeeIdentifier,
    string RoleCode,
    bool IsActive)
{
    /// <summary>The form as it is opened for a person, so a save that changed nothing can be told from one that did.</summary>
    public static UserEditRequest From(UserDetail person)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new UserEditRequest(
            person.UserId,
            person.UsernameNormalized,
            person.FirstName,
            person.LastName,
            person.EmployeeIdentifier,
            person.RoleCode,
            person.IsActive);
    }

    /// <summary>The same fields as a create with no person behind it yet.</summary>
    public static UserEditRequest Blank() => new(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, IsActive: true);

    /// <summary>The write the user-management service applies.</summary>
    public UserAccountEdit ToAccountEdit() =>
        new(Username, FirstName, LastName, EmployeeIdentifier, RoleCode, IsActive);
}
