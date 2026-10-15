using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

using Windows.System;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The sign-in stage: it takes an identifier first — a user name, or exactly four digits for an employee number —
/// then the PIN, states what is missing and what was refused, opens the set-up panel for a person the plant
/// system knows and the store does not, and carries the launch on once the sign-in has been accepted
/// (`contracts/identifier-signin-contract.md` §§1-5, `contracts/window-stage-contract.md` §2; FR-001 to FR-015).
/// </summary>
/// <remarks>
/// <para>
/// <b>It carries the launch on; it does not decide where the launch ends.</b> An accepted sign-in resumes the
/// sequence at the identity step, and the launch then decides whether the person reaches the main screens, stops
/// with a cause, or is sent to a computer that needs setting up (FR-001). The credential machinery behind this
/// stage is unchanged: the attempt count, the session check, the machine gate and the temporary-credential
/// handling all behave exactly as they do today (FR-005).
/// </para>
/// <para>
/// <b>It is a stage of the one window, not a window.</b> There is one window for the whole life of the
/// application, so this surface is content the host swaps in and out rather than a window it opens and closes.
/// Closing that window while this stage is showing is answered by <see cref="RequestCloseAsync"/>, which states
/// why the process is ending before it goes (FR-008).
/// </para>
/// <para>
/// <b>The panel takes the form's place, and the two stage flags live in the view models.</b> Which of the
/// identifier stage, the credential stage and the set-up panel is shown is decided from the view models' own
/// flags, and this page only draws the answer — so the routing is provable without a window.
/// </para>
/// </remarks>
internal sealed partial class SignInPage : Page, ILaunchStageSurface
{
    private readonly ILaunchPipeline _pipeline;

    public SignInPage()
    {
        // Every state is resolved before the markup loads, so each binding sees its source on the first pass.
        ViewModel = App.GetService<SignInViewModel>();
        PasswordChange = App.GetService<PasswordChangeViewModel>();
        Panel = App.GetService<SetUpPanelViewModel>();
        _pipeline = App.GetService<ILaunchPipeline>();

        InitializeComponent();

        ViewModel.SignInAccepted += OnAccepted;
        PasswordChange.PasswordChosen += OnAccepted;
        ViewModel.SetUpRequested += OnSetUpRequested;
        Panel.Cancelled += OnSetUpCancelled;

        // Which stage is shown follows the two view models rather than the markup, so the same state drives the
        // keyboard and the surface.
        ViewModel.PropertyChanged += OnStateChanged;
        Panel.PropertyChanged += OnStateChanged;

        DrawStages();

        // The people who signed in here before are read for the stage that shows them, and a store that cannot
        // answer simply leaves the form on its own (FR-048, FR-015).
        _ = ViewModel.LoadPreviousPeopleAsync();

        Unloaded += OnUnloaded;
    }

    /// <summary>The sign-in stage's state, which owns the two stages, the check and the hint.</summary>
    internal SignInViewModel ViewModel { get; }

    /// <summary>The forced password change's state, which owns the new PIN.</summary>
    internal PasswordChangeViewModel PasswordChange { get; }

    /// <summary>The set-up panel's state, which owns the creation and the chosen PIN.</summary>
    internal SetUpPanelViewModel Panel { get; }

    /// <summary>The label over the first field, which takes a user name or an employee number (FR-002).</summary>
    public string IdentifierLabelText => "Startup_SignIn.IdentifierLabel".GetLocalized();

    /// <summary>What the first field asks for, said before the person types rather than after a refusal.</summary>
    public string IdentifierHintText => "Startup_SignIn.IdentifierHint".GetLocalized();

    /// <summary>Confirms the identifier, which is exactly what Enter in the first field does (FR-014).</summary>
    private async void OnContinueClick(object sender, RoutedEventArgs e)
        => await ViewModel.EnterAsync(SignInField.Identifier, IdentifierBox.Text).ConfigureAwait(true);

    /// <summary>Confirms the PIN, which is exactly what Enter in the PIN field does (FR-014).</summary>
    private async void OnSignInClick(object sender, RoutedEventArgs e)
        => await ViewModel.EnterAsync(SignInField.Credential, CredentialBox.Password).ConfigureAwait(true);

    /// <summary>
    /// Takes one of the people offered beside the form and fills in the user name only, so the PIN is still to be
    /// typed (FR-014, FR-048).
    /// </summary>
    private void OnPreviousPersonClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PreviousSignIn person })
        {
            ViewModel.ChoosePreviousPersonCommand.Execute(person);
        }
    }

    /// <summary>Hands the new PIN the person chose to the launch, which writes it.</summary>
    private async void OnSubmitPasswordClick(object sender, RoutedEventArgs e)
        => await PasswordChange.SubmitCommand
            .ExecuteAsync(new NewPasswordDraft(NewPasswordBox.Password, ConfirmPasswordBox.Password))
            .ConfigureAwait(true);

    /// <summary>Enter in the first field resolves the identifier, exactly as the continue button does (FR-014).</summary>
    private async void OnIdentifierKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;

        await ViewModel.EnterAsync(SignInField.Identifier, IdentifierBox.Text).ConfigureAwait(true);
    }

    /// <summary>Enter in the PIN field submits, exactly as the button does (FR-014).</summary>
    private async void OnCredentialKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is not VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;

        await ViewModel.EnterAsync(SignInField.Credential, CredentialBox.Password).ConfigureAwait(true);
    }

    /// <summary>Opens the set-up panel for the person the plant system confirmed (FR-006, FR-007).</summary>
    private void OnSetUpRequested(object? sender, SetUpRequestedEventArgs args)
        => Panel.OpenFor(args.EmployeeNumber, args.FirstName, args.LastName);

    /// <summary>Returns the person to the identifier stage, with nothing created (FR-024a).</summary>
    private void OnSetUpCancelled(object? sender, EventArgs args) => DrawStages();

    /// <summary>Redraws the stages whenever either view model's own flags change.</summary>
    private void OnStateChanged(object? sender, PropertyChangedEventArgs args) => DrawStages();

    /// <summary>
    /// Shows the stage the view models name: the identifier stage, the PIN stage, or the set-up panel in their
    /// place. The forced password change draws itself from its own flag, so it is not touched here.
    /// </summary>
    private void DrawStages()
    {
        var onPanel = Panel.IsVisible;

        IdentifierPanel.Visibility =
            !onPanel && ViewModel.IsIdentifierStage && ViewModel.ShowSignInForm
                ? Visibility.Visible
                : Visibility.Collapsed;

        var credentialWasVisible = CredentialPanel.Visibility == Visibility.Visible;

        CredentialPanel.Visibility =
            !onPanel && ViewModel.IsCredentialStage && ViewModel.ShowSignInForm
                ? Visibility.Visible
                : Visibility.Collapsed;

        // The PIN box is the only thing this stage asks for, so it takes the keyboard the moment it appears:
        // the person has just said who they are - by typing the user name or by picking one of the people
        // offered beside the form - and there is nothing else on the stage to do (FR-014, FR-048).
        if (!credentialWasVisible && CredentialPanel.Visibility == Visibility.Visible)
        {
            CredentialBox.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>
    /// Carries the launch on from the identity step, which is the one thing this stage asks the launch to do.
    /// </summary>
    /// <remarks>
    /// A resume that could not be started is recorded and the stage stays where it is, so the person is not left
    /// with a surface whose only action did nothing.
    /// </remarks>
    private void OnAccepted(object? sender, EventArgs args) => _ = ResumeLaunchAsync();

    /// <summary>Resumes the launch at the identity step and records a resume that could not be started.</summary>
    private async Task ResumeLaunchAsync()
    {
        try
        {
            await _pipeline.RetryFromAsync(SignInViewModel.ResumeAtStepId, CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            AppLog.Error("SignIn", exception, "The launch could not be carried on after the sign-in was accepted.");
        }
    }

    /// <summary>
    /// States why the process is ending when the one window is closed before anybody signed in, and states it
    /// through the launch so the ending does not read as a crash (FR-004, FR-008).
    /// </summary>
    public Task RequestCloseAsync()
    {
        _pipeline.End("The sign-in screen was closed before anybody signed in.");

        return Task.CompletedTask;
    }

    /// <summary>Stops listening once the stage is gone, so nothing tries to draw into a page that has left.</summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.SignInAccepted -= OnAccepted;
        PasswordChange.PasswordChosen -= OnAccepted;
        ViewModel.SetUpRequested -= OnSetUpRequested;
        Panel.Cancelled -= OnSetUpCancelled;
        ViewModel.PropertyChanged -= OnStateChanged;
        Panel.PropertyChanged -= OnStateChanged;
        Unloaded -= OnUnloaded;
    }
}
