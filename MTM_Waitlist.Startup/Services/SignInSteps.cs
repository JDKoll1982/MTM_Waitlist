using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Startup.Models;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// What the sign-in produced, held between the surface that collected it and the steps that record it.
/// </summary>
/// <remarks>
/// <para>
/// <b>One attempt, one check.</b> The credential check is the only place an attempt is counted, and a temporary
/// credential's limit is judged on the store's own count (FR-012). A second check made later in the launch would
/// spend a second attempt on the same typing, so the check is made once, where a refusal can be stated to the
/// person looking at the form, and the step that follows reports what it found rather than repeating it.
/// </para>
/// <para>
/// <b>The new password is held here for the length of one launch and nowhere else.</b> It is written to the
/// store as a salted hash the moment the step runs and cleared from here immediately after, so nothing
/// recoverable outlives the operation (FR-013, FR-025).
/// </para>
/// </remarks>
internal interface ISignInOutcome
{
    /// <summary>The check that has been made for the sign-in the launch holds, or <c>null</c> when none has.</summary>
    CredentialCheckResult? Check { get; }

    /// <summary>Whether the accepted account is still on a temporary credential (FR-013).</summary>
    bool RequiresNewPassword { get; }

    /// <summary>The new password the person chose, or <c>null</c> when none has been given.</summary>
    string? NewPassword { get; }

    /// <summary>Records the check that was made, so the steps that follow report it rather than repeat it.</summary>
    /// <param name="check">What the credential check answered.</param>
    void RecordCheck(CredentialCheckResult check);

    /// <summary>Keeps the new password the person chose, ready for the step that writes it.</summary>
    /// <param name="newPassword">The password the person chose.</param>
    void HoldNewPassword(string newPassword);

    /// <summary>Discards everything held, once the launch has used it or the person has started again.</summary>
    void Clear();
}

/// <inheritdoc />
internal sealed class SignInOutcome : ISignInOutcome
{
    private readonly object _gate = new();

    private CredentialCheckResult? _check;
    private string? _newPassword;

    /// <inheritdoc />
    public CredentialCheckResult? Check
    {
        get
        {
            lock (_gate)
            {
                return _check;
            }
        }
    }

    /// <inheritdoc />
    public bool RequiresNewPassword => Check?.RequiresNewPassword == true;

    /// <inheritdoc />
    public string? NewPassword
    {
        get
        {
            lock (_gate)
            {
                return _newPassword;
            }
        }
    }

    /// <inheritdoc />
    public void RecordCheck(CredentialCheckResult check)
    {
        ArgumentNullException.ThrowIfNull(check);

        lock (_gate)
        {
            _check = check;
        }
    }

    /// <inheritdoc />
    public void HoldNewPassword(string newPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        lock (_gate)
        {
            _newPassword = newPassword;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            _check = null;
            _newPassword = null;
        }
    }
}

/// <summary>
/// The sign-in steps: resolving the person and their roles, confirming the credential, judging the session,
/// checking the computer against the store, checking whether a new password is needed and setting it
/// (`contracts/launch-step-contract.md` sections 1 and 2; FR-002, FR-010, FR-012, FR-013, FR-015).
/// </summary>
/// <remarks>
/// <para>
/// <b>A step answers for one catalogue entry and never announces itself.</b> Each type here takes its
/// descriptor from the shipped catalogue, so its name, its category, its stated maximum and its target are the
/// sequence's own values (FR-002, FR-003), and the runner is what puts the line on the feed.
/// </para>
/// <para>
/// <b>A step reports; the launch routes.</b> Nothing here decides whether the launch continues to the shell,
/// stops, or returns to the sign-in form. A person who cannot be resolved is reported as a failed step with a
/// stated cause, and what the launch does about it belongs to the pipeline (FR-001, FR-004).
/// </para>
/// </remarks>
internal sealed class ResolvePersonStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "resolve-person";

    private readonly LaunchStep _descriptor;
    private readonly PersonIdentityService _person;
    private readonly IPendingSignIn _pendingSignIn;

    /// <summary>Creates the step over the one writer of identity and the sign-in the launch holds.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="person">The person identity contract's implementation, and its only writer (FR-022).</param>
    /// <param name="pendingSignIn">The sign-in the launch holds, whether the person typed it or a key unlocked it.</param>
    public ResolvePersonStep(
        LaunchStepCatalog catalog,
        PersonIdentityService person,
        IPendingSignIn pendingSignIn)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _person = person ?? throw new ArgumentNullException(nameof(person));
        _pendingSignIn = pendingSignIn ?? throw new ArgumentNullException(nameof(pendingSignIn));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var signInName = _pendingSignIn.SignInName;

        if (string.IsNullOrWhiteSpace(signInName))
        {
            // The launch reached identity resolution without a sign-in, which the pipeline refuses before it gets
            // here. Reporting it rather than resolving nobody keeps the stop attributable to this line (FR-004).
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "The launch holds no sign-in name to resolve, so nobody could be signed in.",
                LaunchRemedySet.RetryOnly));
        }

        return LaunchStepSupport.RunGuardedAsync(
            "The sign-in name could not be resolved against the store",
            async token =>
            {
                var resolved = await _person.ResolveAsync(signInName, token).ConfigureAwait(false);

                return resolved
                    ? new LaunchStepOutcome(
                        LaunchStepStatus.Succeeded,
                        $"The store holds {_person.SignInName} and the sign-in is now theirs.",
                        LaunchRemedySet.None)
                    : new LaunchStepOutcome(
                        LaunchStepStatus.Failed,
                        "The store holds no active account with that sign-in name.",
                        LaunchRemedySet.RetryOnly);
            },
            cancellationToken);
    }
}

/// <summary>
/// The step that reports the roles the resolved person holds, answering for the catalogue's
/// <c>resolve-roles</c> entry (FR-002, FR-022).
/// </summary>
/// <remarks>
/// The roles are read by the identity resolution rather than separately: the one call that resolves a person
/// reads their assignment rows too, so a second read here would be a second round trip for an answer already in
/// hand. The entry stays in the sequence, and what it reports is the set the launch is about to act on, which is
/// the fact a support call needs when a person is missing an authority they expected.
/// </remarks>
internal sealed class ResolveRolesStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "resolve-roles";

    private readonly LaunchStep _descriptor;
    private readonly PersonIdentityService _person;

    /// <summary>Creates the step over the resolved identity.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="person">The resolved person, whose held roles are what this entry reports.</param>
    public ResolveRolesStep(LaunchStepCatalog catalog, PersonIdentityService person)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _person = person ?? throw new ArgumentNullException(nameof(person));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        var heldRoles = _person.HeldRoleCodes;

        if (!_person.IsSignedIn)
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "Nobody is signed in, so no roles could be reported.",
                LaunchRemedySet.RetryOnly));
        }

        return Task.FromResult(heldRoles.Count switch
        {
            0 => new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The store holds no role assignment beyond the role in force, which is the role being carried.",
                LaunchRemedySet.None),
            _ => new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                $"The person holds {heldRoles.Count} role(s): {string.Join(", ", heldRoles)}.",
                LaunchRemedySet.None),
        });
    }
}

/// <summary>
/// The step that confirms the credential presented, answering for the catalogue's <c>check-credential</c> entry
/// (FR-012).
/// </summary>
/// <remarks>
/// <para>
/// <b>It makes the check only if one has not been made.</b> The sign-in surface makes the check where the person
/// can be told the credential was refused, and this step reports that answer. When the launch reaches here with
/// no check recorded, which is the remembered-sign-in path where no form was shown, the step makes it. Either
/// way one attempt costs exactly one check, which is what keeps the five-attempt limit counting the person's
/// typing rather than the launch's steps (FR-012).
/// </para>
/// <para>
/// <b>A refusal is not a stop.</b> The launch holds a refused outcome and the pipeline decides what to do with
/// it; this step reports the refusal in plain language so the stop, if there is one, is attributable to this
/// line (FR-004).
/// </para>
/// </remarks>
internal sealed class CheckCredentialStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "check-credential";

    private readonly LaunchStep _descriptor;
    private readonly CredentialCheckService _credentialCheck;
    private readonly ISignInOutcome _outcome;
    private readonly IPendingSignIn _pendingSignIn;

    /// <summary>Creates the step over the check, the outcome held so far, and the sign-in the launch holds.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="credentialCheck">The one place a credential is compared and an attempt is counted.</param>
    /// <param name="outcome">What the surface already recorded, if anything.</param>
    /// <param name="pendingSignIn">The sign-in the launch holds.</param>
    public CheckCredentialStep(
        LaunchStepCatalog catalog,
        CredentialCheckService credentialCheck,
        ISignInOutcome outcome,
        IPendingSignIn pendingSignIn)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _credentialCheck = credentialCheck ?? throw new ArgumentNullException(nameof(credentialCheck));
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        _pendingSignIn = pendingSignIn ?? throw new ArgumentNullException(nameof(pendingSignIn));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public async Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var recorded = _outcome.Check;

        if (recorded is not null)
        {
            return Report(recorded);
        }

        var signInName = _pendingSignIn.SignInName;
        var secret = _pendingSignIn.Secret;

        var outcome = await LaunchStepSupport
            .RunGuardedAsync(
                "The credential could not be confirmed against the store",
                async token =>
                {
                    var check = await _credentialCheck
                        .CheckAsync(signInName, secret, token)
                        .ConfigureAwait(false);

                    _outcome.RecordCheck(check);

                    // The credential has been compared, so it is dropped rather than left in memory for the
                    // remaining steps, none of which needs it (FR-025).
                    _pendingSignIn.Clear();

                    return Report(check);
                },
                cancellationToken)
            .ConfigureAwait(false);

        return outcome;
    }

    /// <summary>Turns the check's answer into the plain-language line this step reports.</summary>
    private static LaunchStepOutcome Report(CredentialCheckResult check)
    {
        if (check.IsAccepted)
        {
            return new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                check.RequiresNewPassword
                    ? "The credential was accepted, and the account is still on a temporary credential so a new password is needed."
                    : "The credential was accepted against the store.",
                LaunchRemedySet.None);
        }

        return new LaunchStepOutcome(
            LaunchStepStatus.Failed,
            check.RefusalReason switch
            {
                CredentialCheckRefusals.SignInRequired =>
                    "No sign-in name or no credential was given, so nothing could be confirmed.",
                CredentialCheckRefusals.AttemptsExhausted =>
                    $"This account's temporary credential has already been refused {CredentialCheckService.MaximumTemporaryCredentialAttempts} times, "
                    + "so it is refused until a new password is set.",
                CredentialCheckRefusals.StoreUnreadable =>
                    "The store could not be asked, so the credential was not confirmed.",
                _ => "The sign-in name and the credential were not a pair the store holds.",
            },
            LaunchRemedySet.RetryOnly);
    }
}

/// <summary>
/// The step that judges the person's session on this machine and issues one where none stands, answering for the
/// catalogue's <c>judge-session</c> entry (FR-010, FR-011, SC-011).
/// </summary>
/// <remarks>
/// <para>
/// <b>The store's clock judges it.</b> Nothing here compares a time: the verdict is the one
/// <c>sp_auth_user_active_sessions_get</c> produced from its own clock, so a workstation with a wrong clock
/// cannot extend or deny a session (FR-010).
/// </para>
/// <para>
/// <b>A session is written only where it can be attributed.</b> The row names the person and the machine, so a
/// computer the store holds no record for has nowhere to put one. That case is reported as work left undone
/// rather than as a failure, because FR-015 admits a computer whose hardware identity could not be read and a
/// machine that is admitted must not be stopped for want of a row it cannot have.
/// </para>
/// </remarks>
internal sealed class JudgeSessionStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "judge-session";

    private readonly LaunchStep _descriptor;
    private readonly LaunchSessionService _sessions;
    private readonly ISignInOutcome _outcome;
    private readonly IMachineFacts _machine;

    /// <summary>Creates the step over the session service, the sign-in outcome and this computer's facts.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="sessions">The one place a session is issued, judged and ended.</param>
    /// <param name="outcome">The accepted check, which names the person the session belongs to.</param>
    /// <param name="machine">This computer's facts, which name the machine the session belongs to.</param>
    public JudgeSessionStep(
        LaunchStepCatalog catalog,
        LaunchSessionService sessions,
        ISignInOutcome outcome,
        IMachineFacts machine)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var userId = _outcome.Check?.UserId ?? 0;
        var computerId = _machine.RegisteredComputer?.Id ?? 0;

        if (userId <= 0)
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "No accepted sign-in is held, so there is no session to judge.",
                LaunchRemedySet.RetryOnly));
        }

        if (computerId <= 0)
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                "The store holds no record of this computer, so there is nowhere to record a session for it. "
                + "The person carries on, because an unrecorded computer is not a refused one (FR-015).",
                LaunchRemedySet.None));
        }

        return LaunchStepSupport.RunGuardedAsync(
            "The session could not be judged",
            async token =>
            {
                var state = await _sessions.ValidateAsync(userId, computerId, token).ConfigureAwait(false);

                if (state is { HasStoredSession: true, IsValid: true })
                {
                    return new LaunchStepOutcome(
                        LaunchStepStatus.Succeeded,
                        $"The session stands until {Format(state.ExpiresUtc)}, judged on the store's clock.",
                        LaunchRemedySet.None);
                }

                var issued = await _sessions.IssueAsync(userId, computerId, token).ConfigureAwait(false);

                return new LaunchStepOutcome(
                    LaunchStepStatus.Succeeded,
                    issued.IsValid
                        ? $"A session was written for this person on this machine, expiring {Format(issued.ExpiresUtc)} by the store's clock."
                        : "A session was written for this person on this machine.",
                    LaunchRemedySet.None);
            },
            cancellationToken);
    }

    /// <summary>One store-clock timestamp as a line a person can read, or a plain statement when there is none.</summary>
    private static string Format(DateTime? expiresUtc)
        => expiresUtc is { } value
            ? $"{value:yyyy-MM-dd HH:mm} UTC"
            : "the store's own expiry";
}

/// <summary>
/// The step that checks this computer against the store, answering for the catalogue's
/// <c>check-computer-against-store</c> entry (FR-015).
/// </summary>
internal sealed class CheckComputerAgainstStoreStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "check-computer-against-store";

    private readonly LaunchStep _descriptor;
    private readonly MachineGateService _gate;

    /// <summary>Creates the step over the gate, which owns the verdict and its four answers.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="gate">The gate that checks this computer against the store.</param>
    public CheckComputerAgainstStoreStep(LaunchStepCatalog catalog, MachineGateService gate)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return LaunchStepSupport.RunGuardedAsync(
            "This computer could not be checked against the store",
            async token =>
            {
                var result = await _gate.CheckAsync(token).ConfigureAwait(false);

                return result.IsAdmitted
                    ? new LaunchStepOutcome(LaunchStepStatus.Succeeded, result.Reason, LaunchRemedySet.None)
                    : new LaunchStepOutcome(
                        LaunchStepStatus.Failed,
                        result.Reason,
                        // A store that could not be asked may answer on the next try; a computer the store does
                        // not hold will not change because this machine's configuration was reset (FR-017).
                        result.Verdict is MachineGateVerdict.StoreUnreadable
                            ? LaunchRemedySet.RetryOnly
                            : LaunchRemedySet.None);
            },
            cancellationToken);
    }
}

/// <summary>
/// The step that reports whether the accepted account still needs a new password, answering for the catalogue's
/// <c>check-temporary-credential</c> entry (FR-013).
/// </summary>
/// <remarks>
/// It reads the check that was made rather than making a second one: the temporary-credential flag and the
/// attempt count came back with the credential read, so asking the store again would be a round trip for an
/// answer already held.
/// </remarks>
internal sealed class CheckTemporaryCredentialStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "check-temporary-credential";

    private readonly LaunchStep _descriptor;
    private readonly ISignInOutcome _outcome;

    /// <summary>Creates the step over the accepted check it reports.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="outcome">The accepted check, which carries the temporary-credential flag.</param>
    public CheckTemporaryCredentialStep(LaunchStepCatalog catalog, ISignInOutcome outcome)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (_outcome.Check is not { IsAccepted: true })
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "No accepted sign-in is held, so nothing could be said about the credential's state.",
                LaunchRemedySet.RetryOnly));
        }

        return Task.FromResult(_outcome.RequiresNewPassword
            ? new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The account is still on a temporary credential, so a new password must be set before anything else (FR-013).",
                LaunchRemedySet.None)
            : new LaunchStepOutcome(
                LaunchStepStatus.Succeeded,
                "The account is on an ordinary credential, so no new password is needed.",
                LaunchRemedySet.None));
    }
}

/// <summary>
/// The step that writes the new password a person on a temporary credential has chosen, answering for the
/// catalogue's <c>set-new-password</c> entry (FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>It opens nothing.</b> The surface that asks for the new password opens it, and only after the temporary
/// credential has been accepted (FR-013); by the time this step runs, the password has been chosen and is
/// waiting. With nothing waiting and nothing needed, the entry reports its work as left undone rather than
/// inventing a password or treating the sign-in as complete.
/// </para>
/// <para>
/// <b>Only a salted hash is written.</b> The hash and the salt are computed here and the store never sees the
/// plaintext, which is the same rule the sign-in path and the machine-setup gate follow (constitution,
/// Security and Secrets).
/// </para>
/// </remarks>
internal sealed class SetNewPasswordStep : ILaunchStep
{
    /// <summary>The catalogue entry this step answers for.</summary>
    private const string StepId = "set-new-password";

    /// <summary>The write that replaces one account's own password and clears its temporary state.</summary>
    private const string PasswordSetProcedure = "sp_auth_user_password_set";

    private readonly LaunchStep _descriptor;
    private readonly ISignInOutcome _outcome;
    private readonly IMySqlHelperServer _store;

    /// <summary>Creates the step over the new password waiting and the store seam that writes it.</summary>
    /// <param name="catalog">The sequence, which is where the step takes its descriptor from.</param>
    /// <param name="outcome">The accepted check and the password the person chose, if they have.</param>
    /// <param name="store">The stored-procedure seam every write goes through (constitution III).</param>
    public SetNewPasswordStep(
        LaunchStepCatalog catalog,
        ISignInOutcome outcome,
        IMySqlHelperServer store)
    {
        _descriptor = LaunchStepSupport.DescriptorFor(catalog, StepId);
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public LaunchStep Descriptor => _descriptor;

    /// <inheritdoc />
    public Task<LaunchStepOutcome> RunAsync(LaunchStepContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var check = _outcome.Check;

        if (check is not { IsAccepted: true })
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "No accepted sign-in is held, so no password could be set.",
                LaunchRemedySet.RetryOnly));
        }

        if (!check.RequiresNewPassword && _outcome.NewPassword is null)
        {
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Skipped,
                "The account is not on a temporary credential and no new password was chosen, so there is nothing to set.",
                LaunchRemedySet.None));
        }

        var newPassword = _outcome.NewPassword;

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            // The account needs a new password and none has been chosen, which is the surface's job and not
            // something to guess at. Reported as a failed step so the stop names this line (FR-004, FR-013).
            return Task.FromResult(new LaunchStepOutcome(
                LaunchStepStatus.Failed,
                "A new password is required for this account and none has been chosen yet.",
                LaunchRemedySet.RetryOnly));
        }

        return LaunchStepSupport.RunGuardedAsync(
            "The new password could not be set",
            async token =>
            {
                var salt = PasswordSecretHasher.NewSalt();
                var hash = PasswordSecretHasher.Hash(newPassword, salt);

                int affected;

                try
                {
                    affected = await _store
                        .ExecuteStoredProcedureNonQueryAsync(
                            PasswordSetProcedure,
                            new Dictionary<string, object?>
                            {
                                ["p_user_id"] = check.UserId,
                                ["p_password_hash"] = hash,
                                ["p_password_salt"] = salt,
                            },
                            MySqlDatabaseTarget.MtmWaitlist,
                            token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    // The plaintext is dropped as soon as it has been hashed, whether the write landed or threw,
                    // so nothing recoverable survives the step (FR-025).
                    _outcome.Clear();
                }

                if (affected < 1)
                {
                    // The store answered and wrote nothing, so the account is still on its temporary credential.
                    // Saying so is the honest answer; reporting success would let the person carry on believing
                    // they had replaced it.
                    return new LaunchStepOutcome(
                        LaunchStepStatus.Failed,
                        "The store accepted no password change for this account, so it is still on the temporary credential.",
                        LaunchRemedySet.RetryOnly);
                }

                return new LaunchStepOutcome(
                    LaunchStepStatus.Succeeded,
                    "The new password has been set, and the account is no longer on a temporary credential.",
                    LaunchRemedySet.None);
            },
            cancellationToken);
    }
}
