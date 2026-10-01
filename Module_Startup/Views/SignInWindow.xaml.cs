using Microsoft.UI.Xaml;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The sign-in window: the surface a launch ends at when nobody is signed in yet. It takes a name and a
/// credential, states what is missing and what was refused, opens the forced password change, and carries the
/// launch on once the sign-in has been accepted (`contracts/launch-step-contract.md` §5, §6; FR-012, FR-013,
/// FR-014).
/// </summary>
/// <remarks>
/// <para>
/// <b>It carries the launch on; it does not decide where the launch ends.</b> An accepted sign-in resumes the
/// sequence at the identity step, and the launch then decides whether the person reaches the main screens,
/// stops with a cause, or is sent to a computer that needs setting up (FR-001).
/// </para>
/// <para>
/// <b>Closing it before anybody signed in ends the process, and states why first.</b> A window that vanishes
/// silently reads as a crash, so the reason is stated through the launch and only then does the process go
/// (FR-004, FR-008). When the host closes this window because the launch has reached another surface, that is a
/// hand-over and not an abandonment, which is what <see cref="IsHandingOver"/> records.
/// </para>
/// <para>
/// <b>Both panels are the same window.</b> The forced password change replaces the form in place rather than
/// opening a second window, because the person is in the middle of one act and a second window would have its
/// own close box and its own way to be lost behind the first.
/// </para>
/// </remarks>
public sealed partial class SignInWindow : WindowEx
{
    private readonly ILaunchPipeline _pipeline;

    public SignInWindow()
    {
        // Both states are resolved before the markup loads, so every binding sees its source on the first pass.
        ViewModel = App.GetService<SignInViewModel>();
        PasswordChange = App.GetService<PasswordChangeViewModel>();
        _pipeline = App.GetService<ILaunchPipeline>();

        InitializeComponent();

        Title = ViewModel.HeadingText;

        // The surface opens in the middle of the display it is on, so a person looking at either screen finds it
        // where they are looking.
        WindowStartupPlacement.CentreOnScreen(this, "SignIn");

        ViewModel.SignInAccepted += OnAccepted;
        PasswordChange.PasswordChosen += OnAccepted;

        Closed += OnSignInWindowClosed;
    }

    /// <summary>The sign-in screen's state, which owns the check and the hint.</summary>
    internal SignInViewModel ViewModel { get; }

    /// <summary>The forced password change's state, which owns the new password.</summary>
    internal PasswordChangeViewModel PasswordChange { get; }

    /// <summary>
    /// Whether the host is closing this window because the launch has reached another surface. The host sets it
    /// immediately before closing, and a close that carries it is a hand-over rather than an abandonment.
    /// </summary>
    public bool IsHandingOver { get; set; }

    /// <summary>Confirms the name and the credential the person gave.</summary>
    private async void OnSignInClick(object sender, RoutedEventArgs e)
        => await ViewModel.SignInCommand.ExecuteAsync(new SignInCredentials(ViewModel.SignInName, CredentialBox.Password));

    /// <summary>Signs the person in with the sign-in this machine remembers for them, where it can read it.</summary>
    private async void OnUseRememberedSignInClick(object sender, RoutedEventArgs e)
        => await ViewModel.UseRememberedSignInCommand.ExecuteAsync(null);

    /// <summary>Hands the new password the person chose to the launch, which writes it.</summary>
    private async void OnSubmitPasswordClick(object sender, RoutedEventArgs e)
        => await PasswordChange.SubmitCommand.ExecuteAsync(
            new NewPasswordDraft(NewPasswordBox.Password, ConfirmPasswordBox.Password));

    /// <summary>
    /// Carries the launch on from the identity step, which is the one thing this window asks the launch to do.
    /// </summary>
    /// <remarks>
    /// The hand-over flag is set before the sequence is resumed and left set: the host will close this window itself
    /// when the launch reaches its next surface, and that close must not be mistaken for an abandonment. A resume
    /// that throws clears the flag again, because the window is then still the surface the person is looking at.
    /// </remarks>
    private void OnAccepted(object? sender, EventArgs args) => _ = ResumeLaunchAsync();

    /// <summary>Resumes the launch at the identity step and reports a resume that could not be started.</summary>
    private async Task ResumeLaunchAsync()
    {
        IsHandingOver = true;

        try
        {
            await _pipeline.RetryFromAsync(SignInViewModel.ResumeAtStepId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            IsHandingOver = false;

            AppLog.Error("SignIn", exception, "The launch could not be carried on after the sign-in was accepted.");
        }
    }

    /// <summary>
    /// States why the process is ending when the person closes the sign-in window, and stays out of the way when
    /// the host closes it to hand over to another surface.
    /// </summary>
    private void OnSignInWindowClosed(object sender, WindowEventArgs args)
    {
        if (IsHandingOver)
        {
            return;
        }

        // The reason is stated before the process goes, so a closed sign-in window does not read as a crash
        // (FR-004, FR-008).
        _pipeline.End("The sign-in window was closed before anybody signed in.");
    }
}
