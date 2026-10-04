using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Module_Startup.ViewModels;

/// <summary>
/// One field of the set-up panel, in the order Enter walks them (`contracts/identifier-signin-contract.md` §5;
/// FR-014).
/// </summary>
public enum SetUpField
{
    /// <summary>The first name the plant system supplied, which the person may correct.</summary>
    FirstName,

    /// <summary>The last name the plant system supplied, which the person may correct.</summary>
    LastName,

    /// <summary>The PIN the person chooses.</summary>
    Pin,

    /// <summary>The same PIN typed a second time, where Enter submits.</summary>
    Confirmation,
}

/// <summary>
/// What pressing Enter did: the field focus should move to, and whether the panel was submitted
/// (`contracts/identifier-signin-contract.md` §5; FR-014).
/// </summary>
/// <param name="FocusNext">Where focus should go, or <c>null</c> when the panel was submitted.</param>
/// <param name="WasSubmitted">Whether Enter submitted the panel rather than moving on.</param>
public sealed record SetUpEnterOutcome(SetUpField? FocusNext, bool WasSubmitted);

/// <summary>
/// The set-up panel's state: it states that the person is not set up yet, confirms the plant system's name, takes
/// a chosen PIN twice, and creates the account on it
/// (`contracts/identifier-signin-contract.md` §4; FR-006 to FR-011, FR-014, FR-015, FR-024a).
/// </summary>
/// <remarks>
/// <para>
/// <b>The PIN is an argument, never a property.</b> Both values travel together in the draft the command is given,
/// so there is no field to bind, no field to log and nothing left behind after the attempt (FR-008).
/// </para>
/// <para>
/// <b>Enter moves on, and submits only at the end.</b> On each field Enter validates that field and moves to the
/// next; on the confirmation it submits exactly as the button does. A blank field shows its own hint rather than
/// doing nothing, so a person is told what is missing instead of wondering why nothing happened (FR-014, FR-015).
/// </para>
/// <para>
/// <b>Leaving creates nothing.</b> Cancelling raises <see cref="Cancelled"/> so the sign-in surface returns to the
/// identifier stage with the form still usable, and no account is written (FR-024a).
/// </para>
/// </remarks>
internal sealed partial class SetUpPanelViewModel : ObservableObject
{    private readonly AccountCreationService _creation;
    private readonly SignInViewModel? _signIn;

    /// <summary>Creates the panel over the one writer of a self set-up account.</summary>
    /// <param name="creation">The service that writes the profile, the credential and the rank in one call.</param>
    /// <param name="signIn">
    /// The sign-in stage, so a created account is signed in on the PIN the person chose and never passes through
    /// the temporary-credential state (FR-009, FR-045). It is optional so the panel's own rules stay provable on
    /// their own: with no sign-in stage the panel creates the account and raises
    /// <see cref="AccountCreated"/> exactly as it always did.
    /// </param>
    public SetUpPanelViewModel(AccountCreationService creation, SignInViewModel? signIn = null)
    {
        _creation = creation ?? throw new ArgumentNullException(nameof(creation));
        _signIn = signIn;
    }

    /// <summary>Raised once an account has been created, carrying what the store answered.</summary>
    public event EventHandler<AccountSetupResult>? AccountCreated;

    /// <summary>Raised when the person leaves the panel, which creates nothing (FR-024a).</summary>
    public event EventHandler? Cancelled;

    /// <summary>Whether the panel is shown.</summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    /// <summary>The employee number the plant system confirmed, shown back to the person.</summary>
    [ObservableProperty]
    public partial string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>The first name from the plant system, pre-filled and editable (FR-007).</summary>
    [ObservableProperty]
    public partial string FirstName { get; set; } = string.Empty;

    /// <summary>The last name from the plant system, pre-filled and editable (FR-007).</summary>
    [ObservableProperty]
    public partial string LastName { get; set; } = string.Empty;

    /// <summary>What the panel says about the last refusal or the field still to fill.</summary>
    [ObservableProperty]
    public partial string? Message { get; set; }

    /// <summary>True from the moment the creation starts until it answers, which guards the repeated press.</summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>What the panel says about why it is asking.</summary>
    public string HeadingText => "You are not set up for the waitlist yet";

    /// <summary>What the panel says about the name it is confirming (FR-007).</summary>
    public string NamePromptText => "Your name from the plant system, which you can correct:";

    /// <summary>What the panel says about permission, in the terms FR-011 asks for.</summary>
    public string PermissionNoteText => "A higher permission level must be requested from IT.";

    /// <summary>Whether there is anything for the panel to say.</summary>
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>Whether the panel can be submitted, which is while nothing is running.</summary>
    public bool CanSubmit => !IsBusy;

    /// <summary>A run of derived state follows every change to the busy flag.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    /// <summary>Whether the message strip is shown follows every change to the message.</summary>
    /// <param name="value">The new value, which is not read here.</param>
    partial void OnMessageChanged(string? value) => OnPropertyChanged(nameof(HasMessage));

    /// <summary>Opens the panel for the person the plant system confirmed (FR-006, FR-007).</summary>
    /// <param name="employeeNumber">The employee number the person typed.</param>
    /// <param name="firstName">The first name the plant system answered.</param>
    /// <param name="lastName">The last name the plant system answered.</param>
    public void OpenFor(string employeeNumber, string firstName, string lastName)
    {
        EmployeeNumber = employeeNumber?.Trim() ?? string.Empty;
        FirstName = firstName?.Trim() ?? string.Empty;
        LastName = lastName?.Trim() ?? string.Empty;
        Message = null;
        IsVisible = true;
    }

    /// <summary>Leaves the panel, creating nothing and returning the person to the identifier stage (FR-024a).</summary>
    [RelayCommand]
    private void Leave()
    {
        Message = null;
        IsVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Creates the account on the PIN the person chose, exactly as clicking the button does (FR-014).
    /// </summary>
    /// <param name="draft">The confirmed name, the employee number and the two typed PIN values.</param>
    [RelayCommand]
    public async Task CreateAsync(AccountSetupDraft? draft)
    {
        if (IsBusy || draft is null)
        {
            return;
        }

        // The panel's own fields are the confirmed name, so the draft is built from what the person left in them
        // rather than from whatever the plant system first answered (FR-007: the edited name is what is stored).
        var confirmed = draft with
        {
            FirstName = string.IsNullOrWhiteSpace(FirstName) ? draft.FirstName : FirstName,
            LastName = string.IsNullOrWhiteSpace(LastName) ? draft.LastName : LastName,
        };

        IsBusy = true;
        Message = null;

        try
        {
            var result = await _creation.CreateAsync(confirmed, CancellationToken.None).ConfigureAwait(true);

            if (!result.IsCreated)
            {
                Message = result.FailureReason;
                return;
            }

            IsVisible = false;

            // The account exists, on the PIN the person just typed, so the sign-in is completed on that same PIN
            // rather than asking for it a third time. The account was created with no requirement to change it,
            // so this cannot put the person through the temporary-credential state (FR-009, FR-045).
            if (_signIn is not null)
            {
                _signIn.SignInName = result.SignInName ?? confirmed.EmployeeNumber;
                await _signIn.EnterAsync(SignInField.Credential, confirmed.Pin).ConfigureAwait(true);

                if (_signIn.HasMessage)
                {
                    // The account was created but the sign-in was refused, so the panel stays open and says why
                    // rather than leaving the person with a closed panel and no account to sign in with.
                    IsVisible = true;
                    Message = _signIn.Message;
                    return;
                }
            }

            AccountCreated?.Invoke(this, result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// What pressing Enter on a field does: it validates that field and moves to the next, and on the last field
    /// it submits exactly as the button does (FR-014).
    /// </summary>
    /// <param name="current">The field Enter was pressed on.</param>
    /// <param name="draft">The values the form currently holds, passed as one argument.</param>
    /// <returns>Where focus should go, and whether the panel was submitted.</returns>
    public async Task<SetUpEnterOutcome> EnterAsync(SetUpField current, AccountSetupDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        switch (current)
        {
            case SetUpField.FirstName:
                if (IsBlank(draft.FirstName))
                {
                    Message = "Enter your first name before carrying on.";
                    return new SetUpEnterOutcome(SetUpField.FirstName, false);
                }

                Message = null;
                return new SetUpEnterOutcome(SetUpField.LastName, false);

            case SetUpField.LastName:
                if (IsBlank(draft.LastName))
                {
                    Message = "Enter your last name before carrying on.";
                    return new SetUpEnterOutcome(SetUpField.LastName, false);
                }

                Message = null;
                return new SetUpEnterOutcome(SetUpField.Pin, false);

            case SetUpField.Pin:
                if (string.IsNullOrEmpty(draft.Pin))
                {
                    Message = "Choose a PIN before carrying on.";
                    return new SetUpEnterOutcome(SetUpField.Pin, false);
                }

                Message = null;
                return new SetUpEnterOutcome(SetUpField.Confirmation, false);

            default:
                // The last field submits. A blank or mismatched value is refused by the creation call's own rules
                // and the reason is stated, rather than Enter doing nothing at all (FR-015).
                await CreateAsync(draft).ConfigureAwait(true);

                return new SetUpEnterOutcome(null, true);
        }
    }

    private static bool IsBlank(string? value) => string.IsNullOrWhiteSpace(value);
}
