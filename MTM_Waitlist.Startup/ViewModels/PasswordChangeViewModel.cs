using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// The forced password change: it takes the new password a person on a temporary credential must choose, checks
/// that it was typed the same way twice, and hands it to the launch to be written (FR-013).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is opened only after the temporary credential has been accepted.</b> The sign-in screen raises the
/// requirement from an accepted check and this panel appears in its place; there is no route here from a refused
/// sign-in, which is what FR-013 asks for in as many words.
/// </para>
/// <para>
/// <b>It writes nothing itself.</b> The password is handed to the launch's own <c>set-new-password</c> step
/// through the sign-in outcome, and that step computes the salted hash and calls the store. A second writer here
/// would be a second place a credential could be changed (constitution III, FR-022).
/// </para>
/// <para>
/// <b>It holds no password of its own.</b> Both values arrive as the command's argument, are compared, and the
/// accepted one is kept only long enough for the step to write it; neither is a property, so neither can be
/// bound, shown back, or logged (FR-025, logging contract section 6).
/// </para>
/// </remarks>
internal sealed partial class PasswordChangeViewModel : ObservableObject
{
    /// <summary>
    /// How few characters a new password may have before this screen refuses it. It is a floor rather than a
    /// policy: the store's own rules, if there are any, are the store's.
    /// </summary>
    internal const int MinimumPasswordLength = 8;

    private readonly ISignInOutcome _outcome;

    /// <summary>Creates the panel over the sign-in outcome it hands the chosen password to.</summary>
    /// <param name="outcome">What the launch holds, including the new password once it is chosen.</param>
    public PasswordChangeViewModel(ISignInOutcome outcome)
        => _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));

    /// <summary>
    /// Raised once a new password has been chosen, which is the window's cue to carry the launch on so the step
    /// that writes it runs.
    /// </summary>
    internal event EventHandler? PasswordChosen;

    /// <summary>True from the moment the password is handed over until the launch answers, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>What the screen says about a refusal, in the reader's words, or <c>null</c> when there is nothing to say.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>Whether there is anything for the screen to say, which is what shows the message strip.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether the password can be handed over, which is while nothing is running.</summary>
    public bool CanSubmit => !IsBusy;

    /// <summary>The panel's heading.</summary>
    public string HeadingText => "Startup_PasswordChange.Heading".GetLocalized();

    /// <summary>What the panel says about why the password is being asked for.</summary>
    public string ExplanationText => "Startup_PasswordChange.Explanation".GetLocalized();

    /// <summary>The label over the new password.</summary>
    public string PasswordLabelText => "Startup_PasswordChange.PasswordLabel".GetLocalized();

    /// <summary>The label over the confirmation.</summary>
    public string ConfirmationLabelText => "Startup_PasswordChange.ConfirmationLabel".GetLocalized();

    /// <summary>The one action the panel offers.</summary>
    public string SubmitActionText => "Startup_PasswordChange.SubmitAction".GetLocalized();

    /// <summary>Whether the message strip is shown follows every change to the message.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnMessageChanged(string? value) => OnPropertyChanged(nameof(HasMessage));

    /// <summary>Whether the action is offered follows every change to the busy flag.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    /// <summary>
    /// Takes the new password the person chose and hands it to the launch to be written.
    /// </summary>
    /// <param name="draft">The new password and the confirmation, which arrived together in one press.</param>
    /// <remarks>
    /// A password that is too short, or that was not typed the same way twice, is stated and the panel stays
    /// open. Neither is handed on, so the launch never sees a value this screen would not have accepted.
    /// </remarks>
    [RelayCommand]
    private Task SubmitAsync(NewPasswordDraft? draft)
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        Message = null;

        if (string.IsNullOrEmpty(draft?.Password))
        {
            Message = "Startup_PasswordChange.PasswordRequired".GetLocalized();
            return Task.CompletedTask;
        }

        if (draft.Password.Length < MinimumPasswordLength)
        {
            Message = "Startup_PasswordChange.PasswordTooShort".GetLocalized();
            return Task.CompletedTask;
        }

        if (!string.Equals(draft.Password, draft.Confirmation, StringComparison.Ordinal))
        {
            Message = "Startup_PasswordChange.ConfirmationMismatch".GetLocalized();
            return Task.CompletedTask;
        }

        IsBusy = true;

        try
        {
            _outcome.HoldNewPassword(draft.Password);
            PasswordChosen?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// What the forced password change captured, handed to the command so neither value is a property of the screen.
/// </summary>
/// <param name="Password">The new password the person chose.</param>
/// <param name="Confirmation">The same password typed a second time.</param>
internal sealed record NewPasswordDraft(string Password, string Confirmation);
