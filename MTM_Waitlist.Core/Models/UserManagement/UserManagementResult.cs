namespace MTM_Waitlist.Module_Core.Models.UserManagement;

/// <summary>
/// What a caller reads after a create, a correction or a reset: one typed outcome rather than a thrown message
/// (FR-008, FR-026).
/// </summary>
public enum UserManagementOutcomeKind
{
    /// <summary>The write happened.</summary>
    Succeeded,

    /// <summary>The fields as typed break one of the identity rules, and the value is kept for correction.</summary>
    InvalidInput,

    /// <summary>The sign-in name is already taken. Read from the provider's own <c>1062</c>.</summary>
    DuplicateUsername,

    /// <summary>The store refused the role as above the actor's own rung (<c>mtm_rank_denied</c>).</summary>
    RoleDenied,

    /// <summary>The store refused deactivating the actor's own account (<c>mtm_self_deactivate_denied</c>).</summary>
    SelfDeactivateDenied,

    /// <summary>The store refused changing the actor's own sign-in name (<c>mtm_self_rename_denied</c>).</summary>
    SelfRenameDenied,

    /// <summary>The store refused a target who outranks the actor (<c>mtm_target_outranks_actor</c>).</summary>
    TargetOutranksActor,

    /// <summary>The store does not hold the requested role code (<c>mtm_role_unknown</c>).</summary>
    RoleUnknown,

    /// <summary>The store could not be reached, and the reader's work is kept rather than discarded.</summary>
    StoreUnavailable,
}

/// <summary>
/// The typed answer from a user-management write, carrying the resource key and the resolved sentence so every
/// caller reports a refusal in the same words (FR-026, FR-112).
/// </summary>
/// <remarks>
/// <see cref="TemporaryPin"/> is filled only by a create or a reset that succeeded. It is the one time the
/// credential exists in readable form: it travels to the window that reveals it and nowhere else, and it is never
/// stored, logged or written to the audit row (FR-030).
/// </remarks>
public sealed record UserManagementResult(
    UserManagementOutcomeKind Kind,
    string MessageKey,
    string Message,
    string TemporaryPin = "")
{
    /// <summary>Whether the write happened.</summary>
    public bool IsSuccess => Kind == UserManagementOutcomeKind.Succeeded;

    /// <summary>
    /// The answer for a write that happened. <paramref name="temporaryPin"/> is filled only by a create or a
    /// reset, and only here does the credential exist in readable form.
    /// </summary>
    public static UserManagementResult Succeeded(string temporaryPin = "")
    {
        return new UserManagementResult(
            UserManagementOutcomeKind.Succeeded,
            string.IsNullOrEmpty(temporaryPin) ? UserManagementMessages.SucceededKey : UserManagementMessages.CredentialIssuedKey,
            string.IsNullOrEmpty(temporaryPin) ? UserManagementMessages.Succeeded : UserManagementMessages.CredentialIssued,
            temporaryPin);
    }

    /// <summary>The answer for a write that did not happen, in the same words wherever it is shown.</summary>
    public static UserManagementResult Failed(UserManagementOutcomeKind kind, string messageKey, string message)
    {
        return new UserManagementResult(kind, messageKey, message);
    }
}
