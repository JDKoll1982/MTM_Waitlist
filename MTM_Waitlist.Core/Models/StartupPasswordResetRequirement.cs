namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The answer to "does this account still hold its temporary default password?".
/// </summary>
/// <remarks>
/// <para>
/// Resolved from the database <b>before</b> the sign-in form is shown, so an operator whose password is still
/// the temporary default is put straight onto the set-a-new-password surface instead of having to sign in with
/// that password first.
/// </para>
/// <para>
/// The identity fields mirror the logon read's, so the reset can be completed and the sign-in finished without
/// a second lookup. <see cref="None"/> is the answer for an unknown user, an inactive user, an unreachable
/// store, or an account that has a real password — none of which should produce a prompt.
/// </para>
/// </remarks>
public sealed class StartupPasswordResetRequirement
{
    /// <summary>No prompt: the account has a real password, or could not be resolved at all.</summary>
    public static StartupPasswordResetRequirement None { get; } = new();

    /// <summary>Whether the account must set a new password before it can be used.</summary>
    public bool IsRequired { get; init; }

    /// <summary>The account to update, from <c>core_users_profiles.id</c>.</summary>
    public long UserId { get; init; }

    /// <summary>The account's role name, so the completed sign-in keeps its authorization.</summary>
    public string CurrentRole { get; init; } = string.Empty;

    /// <summary>The account's display name, for attribution on the completed sign-in.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>The account's employee identifier, for attribution on the completed sign-in.</summary>
    public string EmployeeIdentifier { get; init; } = string.Empty;
}
