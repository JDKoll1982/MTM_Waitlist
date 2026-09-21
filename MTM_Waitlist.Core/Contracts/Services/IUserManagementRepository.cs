using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The store-facing half of user management: the six procedures and nothing else, so no statement text lives in
/// the service above it (constitution III).
/// </summary>
/// <remarks>
/// <para>
/// The writes return a typed <see cref="UserManagementResult"/> rather than throwing, because the store's own
/// answers carry the meaning: a duplicate sign-in name arrives as the provider's <c>1062</c>, and a rule the
/// store enforces arrives as its stable <c>mtm_*</c> refusal token.
/// </para>
/// <para>
/// The reads throw when the store cannot be reached. That is deliberate: an empty roster and an unreachable store
/// must not look alike, or the user list would show "no people" during an outage (FR-096).
/// </para>
/// </remarks>
public interface IUserManagementRepository
{
    /// <summary>
    /// Everyone, or those matching <paramref name="searchText"/> across the sign-in name, the display name, the
    /// employee number and the role name, narrowed by <paramref name="roleCode"/> when one is given (FR-086).
    /// A blank search matches everyone and a blank role filters nothing.
    /// </summary>
    Task<IReadOnlyList<UserRosterRow>> ListAsync(
        string? searchText,
        string? roleCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// One person, or <c>null</c> when the store holds no such account (FR-027).
    /// </summary>
    Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the profile and its role assignment together, writing one audit row per written field (FR-006).
    /// </summary>
    Task<UserManagementResult> CreateAsync(
        string username,
        string firstName,
        string lastName,
        string displayName,
        string employeeIdentifier,
        string roleCode,
        long actorUserId,
        string passwordHash,
        byte[] passwordSalt,
        string changeGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a correction, one audit row per field that actually changed (FR-110).
    /// </summary>
    Task<UserManagementResult> UpdateAsync(
        long userId,
        string username,
        string firstName,
        string lastName,
        string displayName,
        string employeeIdentifier,
        string roleCode,
        bool isActive,
        long actorUserId,
        string changeGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the hash and salt of a fresh temporary credential, forces a change at the next sign-in and clears
    /// the wrong-attempt count, writing one audit row with no value on either side (FR-029, FR-111).
    /// </summary>
    Task<UserManagementResult> ResetPasswordAsync(
        long userId,
        long actorUserId,
        string passwordHash,
        byte[] passwordSalt,
        string changeGroupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one attempt with a temporary credential: a failure adds one, a success clears the count. Nothing
    /// else clears it and nothing expires it (FR-036, FR-037, FR-038).
    /// </summary>
    Task RecordTemporaryCredentialAttemptAsync(
        long userId,
        bool wasSuccessful,
        CancellationToken cancellationToken = default);
}
