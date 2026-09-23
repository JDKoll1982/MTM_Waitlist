namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The resource key and the shipped English sentence for every answer a permission change can give.
/// </summary>
/// <remarks>
/// The sentences a person already meets elsewhere are reused rather than restated: the fixed-row sentence is the
/// one the page shows on the locked row, the outranked sentence is the one the page shows above the feature list,
/// and the moved-value sentence is the one the page asks with. No resource key is ever shown to a person: a key
/// that resolves to itself is treated as missing and the shipped sentence is used instead.
/// </remarks>
public static class PermissionAdministrationMessages
{
    /// <summary>The change set landed.</summary>
    public const string SavedKey = "UserManagement_Outcome.Succeeded";

    /// <summary>The person's role is above the reader's own rung.</summary>
    public const string OutrankedKey = "Permissions_Row.UnavailableOutranked";

    /// <summary>The set named the permission that opens the page.</summary>
    public const string GateFixedKey = "Permissions_Row.Fixed";

    /// <summary>A value moved since the caller last saw it.</summary>
    public const string ValueMovedKey = "Permissions_Undo.ValueMoved";

    /// <summary>The set named a key outside the permission namespace.</summary>
    public const string KeyInvalidKey = "Permissions_Outcome.KeyInvalid";

    /// <summary>The store could not be reached.</summary>
    public const string StoreUnavailableKey = "UserManagement_Outcome.StoreUnavailable";

    /// <summary>There is no recorded change to reverse.</summary>
    public const string NothingToReverseKey = "Permissions_Undo.NothingToReverse";

    /// <summary>The shipped sentence for a change set that landed.</summary>
    public const string Saved = "Saved.";

    /// <summary>The shipped sentence for a person who outranks the reader.</summary>
    public const string Outranked = "That person's role is above your own, so their features cannot be changed.";

    /// <summary>The shipped sentence for the fixed row.</summary>
    public const string GateFixed = "This is the permission that opens this page, so it cannot be changed here.";

    /// <summary>The shipped sentence for a value that moved since the caller last saw it.</summary>
    public const string ValueMoved =
        "That value has changed since you last looked, so nothing was written. Check the value shown, then restore it if you still want your change.";

    /// <summary>The shipped sentence for a key outside the namespace.</summary>
    public const string KeyInvalid = "That is not a permission, so it cannot be changed here.";

    /// <summary>The shipped sentence for an unreachable store.</summary>
    public const string StoreUnavailable = "The user store could not be reached. Nothing has been changed. Try again.";

    /// <summary>The shipped sentence for a reversal with nothing behind it.</summary>
    public const string NothingToReverse = "There is no recorded change to undo.";
}
