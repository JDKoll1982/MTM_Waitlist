using System.Security.Cryptography;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Models.UserManagement;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="IUserManagementService"/>
/// <remarks>
/// <para>
/// This is where the identity rules are applied, so a screen cannot be the only guard: a sign-in name, a first
/// name and a last name are each between 1 and 128 characters, the display name derived from the two name parts
/// is bounded to 256, and an employee number is exactly four digits (FR-004, FR-005). The display name is derived
/// here rather than typed, and it is bounded here rather than only at the parts, because two maximum-length parts
/// derive a 257-character name that the column would refuse.
/// </para>
/// <para>
/// The actor is read from the signed-in state rather than taken from a caller, so no caller can attribute a change
/// to the wrong person, and the store re-reads the actor's rung itself (FR-025).
/// </para>
/// </remarks>
public sealed class UserManagementService : IUserManagementService
{
    /// <summary>The longest sign-in name, first name or last name the store holds.</summary>
    public const int MaxNameLength = 128;

    /// <summary>The longest display name the store holds.</summary>
    public const int MaxDisplayNameLength = 256;

    /// <summary>An employee number is exactly this many characters, all digits (FR-004).</summary>
    public const int EmployeeNumberLength = 4;

    /// <summary>The number of digits in a temporary credential.</summary>
    private const int TemporaryCredentialDigits = 4;

    private readonly IUserManagementRepository _repository;
    private readonly StartupState _startupState;

    public UserManagementService(IUserManagementRepository repository, StartupState startupState)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _startupState = startupState ?? throw new ArgumentNullException(nameof(startupState));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<UserRosterRow>> SearchAsync(
        string? searchText,
        string? roleCode,
        CancellationToken cancellationToken = default) =>
        _repository.ListAsync(searchText, roleCode, cancellationToken);

    /// <inheritdoc />
    public Task<UserAccount?> GetAsync(long userId, CancellationToken cancellationToken = default) =>
        _repository.GetAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task<UserManagementResult> CreateAsync(UserAccountEdit edit, CancellationToken cancellationToken = default)
    {
        if (!TryNormalize(edit, out var normalized, out var failure))
        {
            return failure;
        }

        var temporaryPin = NewTemporaryPin();
        var salt = PasswordSecretHasher.NewSalt();
        var hash = PasswordSecretHasher.Hash(temporaryPin, salt);

        var result = await _repository
            .CreateAsync(
                normalized.Username,
                normalized.FirstName,
                normalized.LastName,
                normalized.DisplayName,
                normalized.EmployeeIdentifier,
                normalized.RoleCode,
                _startupState.UserId,
                hash,
                salt,
                NewChangeGroupId(),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? UserManagementResult.Succeeded(temporaryPin) : result;
    }

    /// <inheritdoc />
    public async Task<UserManagementResult> UpdateAsync(
        long userId,
        UserAccountEdit edit,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalize(edit, out var normalized, out var failure))
        {
            return failure;
        }

        return await _repository
            .UpdateAsync(
                userId,
                normalized.Username,
                normalized.FirstName,
                normalized.LastName,
                normalized.DisplayName,
                normalized.EmployeeIdentifier,
                normalized.RoleCode,
                edit.IsActive,
                _startupState.UserId,
                NewChangeGroupId(),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UserManagementResult> ResetPasswordAsync(long userId, CancellationToken cancellationToken = default)
    {
        var temporaryPin = NewTemporaryPin();
        var salt = PasswordSecretHasher.NewSalt();
        var hash = PasswordSecretHasher.Hash(temporaryPin, salt);

        var result = await _repository
            .ResetPasswordAsync(
                userId,
                _startupState.UserId,
                hash,
                salt,
                NewChangeGroupId(),
                cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? UserManagementResult.Succeeded(temporaryPin) : result;
    }

    /// <inheritdoc />
    public Task RecordTemporaryCredentialAttemptAsync(long userId, bool wasSuccessful, CancellationToken cancellationToken = default) =>
        _repository.RecordTemporaryCredentialAttemptAsync(userId, wasSuccessful, cancellationToken);

    /// <summary>
    /// A random credential of exactly four digits. Random rather than derived from anything about the person, and
    /// generated here rather than in the window, so it exists in readable form in one place and for one moment.
    /// </summary>
    private static string NewTemporaryPin() =>
        RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, TemporaryCredentialDigits))
            .ToString($"D{TemporaryCredentialDigits}", System.Globalization.CultureInfo.InvariantCulture);

    private static string NewChangeGroupId() => Guid.NewGuid().ToString();

    /// <summary>
    /// Checks the identity rules and produces the values the store writes, or the one typed refusal that names
    /// what is wrong while the reader's typed values stay where they are (FR-005, FR-008).
    /// </summary>
    private static bool TryNormalize(
        UserAccountEdit edit,
        out NormalizedAccount normalized,
        out UserManagementResult failure)
    {
        normalized = default;
        failure = UserManagementResult.Succeeded();

        if (edit is null)
        {
            failure = Invalid(UserManagementMessages.NameRequiredKey, UserManagementMessages.NameRequired);
            return false;
        }

        var username = edit.Username?.Trim() ?? string.Empty;
        var firstName = edit.FirstName?.Trim() ?? string.Empty;
        var lastName = edit.LastName?.Trim() ?? string.Empty;

        if (username.Length == 0 || firstName.Length == 0 || lastName.Length == 0)
        {
            failure = Invalid(UserManagementMessages.NameRequiredKey, UserManagementMessages.NameRequired);
            return false;
        }

        if (username.Length > MaxNameLength || firstName.Length > MaxNameLength || lastName.Length > MaxNameLength)
        {
            failure = Invalid(UserManagementMessages.NameTooLongKey, UserManagementMessages.NameTooLong);
            return false;
        }

        var employeeIdentifier = edit.EmployeeIdentifier?.Trim() ?? string.Empty;
        if (employeeIdentifier.Length != EmployeeNumberLength || !employeeIdentifier.All(char.IsAsciiDigit))
        {
            failure = Invalid(UserManagementMessages.EmployeeNumberInvalidKey, UserManagementMessages.EmployeeNumberInvalid);
            return false;
        }

        var roleCode = edit.RoleCode?.Trim() ?? string.Empty;
        if (roleCode.Length == 0)
        {
            failure = Invalid(UserManagementMessages.RoleUnknownKey, UserManagementMessages.RoleUnknown);
            return false;
        }

        normalized = new NormalizedAccount(
            ToStoredUsername(username),
            firstName,
            lastName,
            DeriveDisplayName(firstName, lastName),
            employeeIdentifier,
            roleCode);

        return true;
    }

    /// <summary>
    /// The stored form of a sign-in name: upper case, because the column is matched in upper case and the store
    /// holds the same form (FR-002).
    /// </summary>
    private static string ToStoredUsername(string username) => username.ToUpperInvariant();

    /// <summary>
    /// The display name, derived from the two name parts and bounded to the column's length. Two names of the
    /// maximum length derive a longer string, and truncating is the one place that can be honoured without
    /// refusing a name the parts rule allows.
    /// </summary>
    private static string DeriveDisplayName(string firstName, string lastName)
    {
        var displayName = $"{firstName} {lastName}".Trim();
        return displayName.Length <= MaxDisplayNameLength
            ? displayName
            : displayName[..MaxDisplayNameLength];
    }

    private static UserManagementResult Invalid(string messageKey, string message) =>
        UserManagementResult.Failed(UserManagementOutcomeKind.InvalidInput, messageKey, message);

    private readonly record struct NormalizedAccount(
        string Username,
        string FirstName,
        string LastName,
        string DisplayName,
        string EmployeeIdentifier,
        string RoleCode);
}
