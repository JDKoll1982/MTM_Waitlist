namespace MTM_Waitlist.Module_Core.Models.UserManagement;

/// <summary>
/// The resource key and the shipped English sentence for every answer a user-management write can give.
/// </summary>
/// <remarks>
/// The sentence is a fallback for a missing resource entry, the same shape the settings screens already use: no
/// resource key is ever shown to a person, and a key that resolves to itself is treated as missing. The keys use
/// this feature's pinned <c>UserManagement_*</c> prefix.
/// </remarks>
public static class UserManagementMessages
{
    /// <summary>The write happened.</summary>
    public const string SucceededKey = "UserManagement_Outcome.Succeeded";

    /// <summary>A created or reset account's credential was issued.</summary>
    public const string CredentialIssuedKey = "UserManagement_Outcome.CredentialIssued";

    /// <summary>The fields as typed break an identity rule.</summary>
    public const string InvalidInputKey = "UserManagement_Outcome.InvalidInput";

    /// <summary>The sign-in name is already taken.</summary>
    public const string DuplicateUsernameKey = "UserManagement_Outcome.DuplicateUsername";

    /// <summary>The role is above the actor's own rung.</summary>
    public const string RoleDeniedKey = "UserManagement_Outcome.RoleDenied";

    /// <summary>Deactivating one's own account is refused.</summary>
    public const string SelfDeactivateDeniedKey = "UserManagement_Outcome.SelfDeactivateDenied";

    /// <summary>Changing one's own sign-in name is refused.</summary>
    public const string SelfRenameDeniedKey = "UserManagement_Outcome.SelfRenameDenied";

    /// <summary>The person outranks the reader.</summary>
    public const string TargetOutranksActorKey = "UserManagement_Outcome.TargetOutranksActor";

    /// <summary>The role code is not in the catalogue.</summary>
    public const string RoleUnknownKey = "UserManagement_Outcome.RoleUnknown";

    /// <summary>The store could not be reached.</summary>
    public const string StoreUnavailableKey = "UserManagement_Outcome.StoreUnavailable";

    /// <summary>A sign-in name, a first name or a last name is empty.</summary>
    public const string NameRequiredKey = "UserManagement_Outcome.NameRequired";

    /// <summary>A sign-in name, a first name or a last name is longer than 128 characters.</summary>
    public const string NameTooLongKey = "UserManagement_Outcome.NameTooLong";

    /// <summary>The employee number is not exactly four digits.</summary>
    public const string EmployeeNumberInvalidKey = "UserManagement_Outcome.EmployeeNumberInvalid";

    /// <summary>The shipped sentence for a completed write.</summary>
    public const string Succeeded = "Saved.";

    /// <summary>The shipped sentence for a create or a reset that issued a credential.</summary>
    public const string CredentialIssued = "Saved. Hand the sign-in details over now: this credential is shown once.";

    /// <summary>The shipped sentence for fields that break a rule.</summary>
    public const string InvalidInput = "Check the details you entered and try again.";

    /// <summary>The shipped sentence for a taken sign-in name.</summary>
    public const string DuplicateUsername = "That sign-in name is already taken. Choose another one.";

    /// <summary>The shipped sentence for a role above the actor's own rung.</summary>
    public const string RoleDenied = "You cannot give somebody a role above your own.";

    /// <summary>The shipped sentence for deactivating one's own account.</summary>
    public const string SelfDeactivateDenied = "You cannot deactivate the account you are signed in as.";

    /// <summary>The shipped sentence for renaming one's own account.</summary>
    public const string SelfRenameDenied = "You cannot change the sign-in name of the account you are signed in as.";

    /// <summary>The shipped sentence for a person who outranks the reader.</summary>
    public const string TargetOutranksActor = "That person's role is above your own, so their account cannot be changed.";

    /// <summary>The shipped sentence for a role the catalogue does not hold.</summary>
    public const string RoleUnknown = "That role is no longer in the role catalogue. Choose another one.";

    /// <summary>The shipped sentence for an unreachable store.</summary>
    public const string StoreUnavailable = "The user store could not be reached. Your work has been kept. Try again.";

    /// <summary>The shipped sentence for an empty name field.</summary>
    public const string NameRequired = "Fill in every name field.";

    /// <summary>The shipped sentence for an over-long name field.</summary>
    public const string NameTooLong = "A sign-in name, a first name and a last name are each 128 characters at most.";

    /// <summary>The shipped sentence for a malformed employee number.</summary>
    public const string EmployeeNumberInvalid = "An employee number is exactly four digits.";
}
