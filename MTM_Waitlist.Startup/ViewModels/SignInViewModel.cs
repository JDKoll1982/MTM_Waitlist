using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The sign-in screen's state: it takes the name and the credential, confirms them, and tells the person what is
/// missing rather than leaving them to guess (`contracts/launch-step-contract.md`; FR-012, FR-014).
/// </summary>
/// <remarks>
/// <para>
/// <b>It makes the check, because that is where a refusal can be said out loud.</b> A refusal has to leave the
/// person on this form so they can try again, and the five-attempt limit is judged on the store's count, so the
/// attempt is spent where it is typed. The launch's own <c>check-credential</c> step then reports the answer
/// instead of asking a second time, which is what keeps one typing equal to one attempt (FR-012).
/// </para>
/// <para>
/// <b>The credential is an argument, never a property.</b> It arrives with the command and is handed straight to
/// the check, so there is no field to bind, no field to log and nothing left behind after the attempt (FR-025,
/// logging contract section 6).
/// </para>
/// <para>
/// <b>The hint answers "what do I still have to do", not "what went wrong".</b> The person is told which of the
/// two values is still missing before they press anything, and a refusal is said separately once it has
/// happened.
/// </para>
/// <para>
/// <b>It routes nothing.</b> This screen says that the sign-in was accepted and the window carries the launch on
/// from the identity step; the launch decides where it ends (FR-001).
/// </para>
/// </remarks>
internal sealed partial class SignInViewModel : ObservableObject
{
    /// <summary>The catalogue entry the launch resumes at once a sign-in has been accepted.</summary>
    internal const string ResumeAtStepId = "resolve-person";

    private readonly CredentialCheckService _credentialCheck;
    private readonly RememberedSignInService _rememberedSignIn;
    private readonly IPendingSignIn _pendingSignIn;
    private readonly ISignInOutcome _outcome;
    private readonly IMachineFacts _machine;

    private readonly string _nameHint;
    private readonly string _credentialHint;

    /// <summary>Creates the sign-in screen's state over the check, the remembered sign-in and what the launch holds.</summary>
    /// <param name="credentialCheck">The one place a credential is compared and an attempt is counted.</param>
    /// <param name="rememberedSignIn">The remembered sign-in, read so a person who chose it is not asked again.</param>
    /// <param name="pendingSignIn">What the launch holds, which is what the steps read once the form is done.</param>
    /// <param name="outcome">The accepted check and the new password, held for the launch's own steps.</param>
    /// <param name="machine">This computer's facts, which name the machine a remembered sign-in is keyed to.</param>
    public SignInViewModel(
        CredentialCheckService credentialCheck,
        RememberedSignInService rememberedSignIn,
        IPendingSignIn pendingSignIn,
        ISignInOutcome outcome,
        IMachineFacts machine)
    {
        _credentialCheck = credentialCheck ?? throw new ArgumentNullException(nameof(credentialCheck));
        _rememberedSignIn = rememberedSignIn ?? throw new ArgumentNullException(nameof(rememberedSignIn));
        _pendingSignIn = pendingSignIn ?? throw new ArgumentNullException(nameof(pendingSignIn));
        _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));

        _nameHint = "Startup_SignIn.HintNameRequired".GetLocalized();
        _credentialHint = "Startup_SignIn.HintCredentialRequired".GetLocalized();
    }

    /// <summary>
    /// Raised once a sign-in has been accepted, which is the window's cue to carry the launch on from the
    /// identity step. It is raised only after the store accepted the credential.
    /// </summary>
    internal event EventHandler? SignInAccepted;

    /// <summary>The sign-in name the person is typing. It is not a secret and it is shown back to them.</summary>
    [ObservableProperty]
    public partial string SignInName { get; set; } = string.Empty;

    /// <summary>Whether the person asked this machine to remember them once the sign-in is accepted (FR-014).</summary>
    [ObservableProperty]
    public partial bool RememberMe { get; set; }

    /// <summary>True from the moment a sign-in starts until it answers, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>What the screen says about the last refusal, in the reader's words, or <c>null</c> when there is nothing to say.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>
    /// Whether the account that has signed in is still on a temporary credential, so the new password must be
    /// chosen before anything else (FR-013).
    /// </summary>
    [ObservableProperty]
    public partial bool IsPasswordChangeRequired { get; set; }

    /// <summary>What the person still has to give, said before they press anything rather than after a refusal.</summary>
    public string Hint => string.IsNullOrWhiteSpace(SignInName)
        ? _nameHint
        : _credentialHint;

    /// <summary>Whether the sign-in is offered, which is while nothing is running and no new password is needed.</summary>
    public bool CanSignIn => !IsBusy && !IsPasswordChangeRequired;

    /// <summary>Whether there is anything for the screen to say, which is what shows the message strip.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether the sign-in form is shown, which is until a temporary credential has been accepted.</summary>
    public bool ShowSignInForm => !IsPasswordChangeRequired;

    /// <summary>Whether the form's own route into the remembered sign-in is offered.</summary>
    public bool CanUseRememberedSignIn => CanSignIn && !string.IsNullOrWhiteSpace(SignInName);

    /// <summary>The screen's heading.</summary>
    public string HeadingText => "Startup_SignIn.Heading".GetLocalized();

    /// <summary>What the screen says about why it is asking.</summary>
    public string SubtitleText => "Startup_SignIn.Subtitle".GetLocalized();

    /// <summary>The label over the sign-in name.</summary>
    public string SignInNameLabelText => "Startup_SignIn.NameLabel".GetLocalized();

    /// <summary>The label over the credential.</summary>
    public string CredentialLabelText => "Startup_SignIn.CredentialLabel".GetLocalized();

    /// <summary>The label beside the remember-me choice.</summary>
    public string RememberMeLabelText => "Startup_SignIn.RememberMeLabel".GetLocalized();

    /// <summary>The one action that confirms a sign-in.</summary>
    public string SignInActionText => "Startup_SignIn.SignInAction".GetLocalized();

    /// <summary>The second action, for a person this machine already remembers.</summary>
    public string RememberedActionText => "Startup_SignIn.RememberedAction".GetLocalized();

    /// <summary>A run of derived state follows every change to the name the person has typed.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnSignInNameChanged(string value)
    {
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(CanUseRememberedSignIn));
    }

    /// <summary>A run of derived state follows every change to the busy flag.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSignIn));
        OnPropertyChanged(nameof(CanUseRememberedSignIn));
    }

    /// <summary>Whether the message strip is shown follows every change to the message.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnMessageChanged(string? value) => OnPropertyChanged(nameof(HasMessage));

    /// <summary>The switch between the form and the new-password panel follows every change to that flag.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsPasswordChangeRequiredChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSignIn));
        OnPropertyChanged(nameof(ShowSignInForm));
        OnPropertyChanged(nameof(CanUseRememberedSignIn));
    }

    /// <summary>
    /// Confirms the credential the person gave and, when it is accepted, hands the launch the sign-in it needs
    /// to carry on.
    /// </summary>
    /// <param name="credentials">The name and the credential the person gave.</param>
    /// <remarks>
    /// A refusal is stated and the form stays open: the person is asked again rather than sent away, which is
    /// what makes the five-attempt limit meaningful rather than a single chance (FR-012).
    /// </remarks>
    [RelayCommand]
    private async Task SignInAsync(SignInCredentials? credentials)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            if (credentials is { } given && !string.IsNullOrWhiteSpace(given.SignInName))
            {
                SignInName = given.SignInName;
            }

            var check = await _credentialCheck
                .CheckAsync(SignInName, credentials?.Credential, CancellationToken.None)
                .ConfigureAwait(true);

            if (!check.IsAccepted)
            {
                Message = RefusalText(check.RefusalReason);
                return;
            }

            await AcceptAsync(check, credentials?.Credential, applyRememberMeChoice: true).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Signs the person in with the sign-in this machine remembers for them, so a person who chose to be
    /// recognised is not asked for the credential again (FR-014).
    /// </summary>
    /// <remarks>
    /// The remembered credential is presented to the launch's own check rather than trusted: it is a stored
    /// value, and a stored value that is no longer accepted must be refused like any other. Nothing is read when
    /// the name is blank, because the store keys a remembered sign-in by person and this screen has no person
    /// yet.
    /// </remarks>
    [RelayCommand]
    private async Task UseRememberedSignInAsync()
    {
        if (!CanUseRememberedSignIn)
        {
            return;
        }

        IsBusy = true;
        Message = null;

        try
        {
            var userId = await _credentialCheck
                .FindAccountIdAsync(SignInName, CancellationToken.None)
                .ConfigureAwait(true);

            if (userId <= 0)
            {
                Message = RefusalText(CredentialCheckRefusals.CredentialRefused);
                return;
            }

            var remembered = await _rememberedSignIn
                .TryReadAsync(userId, _machine.RegisteredComputer?.Id ?? 0, CancellationToken.None)
                .ConfigureAwait(true);

            if (remembered.Outcome is not RememberedSignInOutcome.Held
                || remembered.SignInName is null
                || remembered.Secret is null)
            {
                // Every other answer is the fall-back FR-014 asks for, and the person is told to give the
                // credential rather than being left wondering why the button did nothing.
                Message = "Startup_SignIn.RememberedUnavailable".GetLocalized();
                return;
            }

            var check = await _credentialCheck
                .CheckAsync(remembered.SignInName, remembered.Secret, CancellationToken.None)
                .ConfigureAwait(true);

            if (!check.IsAccepted)
            {
                Message = RefusalText(check.RefusalReason);
                return;
            }

            await AcceptAsync(check, remembered.Secret, applyRememberMeChoice: false).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Hands the launch the accepted sign-in, applies the person's remember-me choice, and says whether a new
    /// password must be chosen first.
    /// </summary>
    /// <param name="check">The accepted answer, which names the person the sign-in belongs to.</param>
    /// <param name="credential">The credential that was accepted, held only so the launch's steps can use it.</param>
    /// <param name="applyRememberMeChoice">
    /// Whether this sign-in is the person's own answer to the remember-me choice. It is false on the remembered
    /// path, where the person never saw the choice and nothing they did should change it.
    /// </param>
    private async Task AcceptAsync(CredentialCheckResult check, string? credential, bool applyRememberMeChoice)
    {
        _outcome.RecordCheck(check);

        if (!string.IsNullOrEmpty(credential))
        {
            _pendingSignIn.Hold(check.SignInName, credential);
        }

        if (applyRememberMeChoice)
        {
            // The choice is reversible, and unchecking it is how a person stops being remembered on a shared
            // computer. A failure on either side is not a failure to sign in: the person is on their way in, and
            // what could not be stored or forgotten is reported by the store rather than by refusing them
            // (FR-014).
            await ApplyRememberMeChoiceAsync(check, credential).ConfigureAwait(true);
        }

        if (check.RequiresNewPassword)
        {
            // The new-password panel opens here and nowhere else, which is what FR-013 asks: only once the
            // temporary credential has been accepted. The launch is carried on when the password is chosen.
            IsPasswordChangeRequired = true;
            Message = null;
            return;
        }

        SignInAccepted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Stores or forgets this machine's memory of the person, according to the choice they just made.</summary>
    /// <param name="check">The accepted answer, which names the person the choice belongs to.</param>
    /// <param name="credential">The credential that was accepted, stored only when the person asked to be remembered.</param>
    private async Task ApplyRememberMeChoiceAsync(CredentialCheckResult check, string? credential)
    {
        var computerId = _machine.RegisteredComputer?.Id ?? 0;

        if (RememberMe && !string.IsNullOrEmpty(credential))
        {
            await _rememberedSignIn
                .RememberAsync(check.UserId, computerId, check.SignInName, credential, CancellationToken.None)
                .ConfigureAwait(true);

            return;
        }

        if (!RememberMe)
        {
            // Not asking to be remembered is an answer in its own right: whatever this machine held for the
            // person is forgotten, so a choice made earlier cannot outlive the person changing their mind.
            await _rememberedSignIn
                .ForgetAsync(check.UserId, computerId, CancellationToken.None)
                .ConfigureAwait(true);
        }
    }

    /// <summary>One refusal, said in the reader's language rather than as the token the check produced.</summary>
    /// <param name="refusalReason">One of <see cref="CredentialCheckRefusals"/>, or <c>null</c>.</param>
    private static string RefusalText(string? refusalReason) => refusalReason switch
    {
        CredentialCheckRefusals.SignInRequired => "Startup_SignIn.RefusedSignInRequired".GetLocalized(),
        CredentialCheckRefusals.AttemptsExhausted => "Startup_SignIn.RefusedAttemptsExhausted".GetLocalized(),
        CredentialCheckRefusals.StoreUnreadable => "Startup_SignIn.RefusedStoreUnreadable".GetLocalized(),
        _ => "Startup_SignIn.RefusedCredential".GetLocalized(),
    };
}

/// <summary>
/// What the sign-in form captured, handed to the command so the credential is never a property of the screen.
/// </summary>
/// <param name="SignInName">The name the person typed.</param>
/// <param name="Credential">The credential the person typed, which lives as an argument and nowhere else.</param>
internal sealed record SignInCredentials(string SignInName, string Credential);
