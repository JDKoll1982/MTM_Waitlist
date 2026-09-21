using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Everything the user-administration screens do to a person: find them, read one, create one, correct one,
/// deactivate or reactivate one, and reset their password (FR-006, FR-086, FR-029).
/// </summary>
/// <remarks>
/// The rules live below this seam, not in the screens: the identity rules are checked here, and the rank rule,
/// the self-lockout rule and the one-role rule are enforced by the store, which is the only place they can be
/// enforced for a caller that never draws a screen (FR-019, FR-025).
/// </remarks>
public interface IUserManagementService
{
    /// <summary>
    /// Everyone, or those matching a search across the sign-in name, the display name, the employee number and
    /// the role name, narrowed by a role code when one is given (FR-086). Reading is never refused.
    /// </summary>
    Task<IReadOnlyList<UserRosterRow>> SearchAsync(
        string? searchText,
        string? roleCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One person, or <c>null</c> when the store holds no such account.
    /// </summary>
    Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a person: validates the fields, derives the display name, issues a temporary credential and writes
    /// the profile and its role assignment in one act (FR-006, FR-007, FR-008).
    /// </summary>
    Task<UserManagementResult> CreateAsync(UserAccountEdit edit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a correction, one audit row per field that actually changed.
    /// </summary>
    Task<UserManagementResult> UpdateAsync(
        long userId,
        UserAccountEdit edit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a fresh temporary credential, forces a change at the next sign-in and clears the wrong-attempt
    /// count. The credential is returned once, in the result, and is stored only as a salted hash (FR-029, FR-030).
    /// </summary>
    Task<UserManagementResult> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one attempt with a temporary credential, so the count always moves and no screen has to remember
    /// to record a failure (FR-036).
    /// </summary>
    Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default);
}
