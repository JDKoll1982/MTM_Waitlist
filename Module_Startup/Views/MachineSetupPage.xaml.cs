using System.Threading;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The machine-setup stage: shown when this computer has no configuration and before any operator signs in, it
/// takes the identity of whoever may configure it, and it ends the process on every route out that is not
/// completion (`contracts/machine-configuration-contract.md` §§2-3; FR-006, FR-007, FR-008).
/// </summary>
/// <remarks>
/// <para>
/// <b>Six routes out, one ending.</b> The one window's close box, Escape, Alt+F4, the screen's own cancel
/// control, declining the sign-in and the setup screen's own close all arrive at <see cref="AbortAsync"/>, which
/// states the route's reason before the process goes. The window's close box reaches it through
/// <see cref="RequestCloseAsync"/>, and the rest are wired here, so a route that was wired to something else
/// would be a route that leaves the stage standing (FR-008).
/// </para>
/// <para>
/// <b>The reason is stated, not logged.</b> The statement is shown in a dialog the person dismisses, and only
/// then is the ending declared through the launch. An operator who abandons setup therefore reads why, rather
/// than watching the surface vanish and reporting a crash (FR-008, S15).
/// </para>
/// <para>
/// <b>Completing setup is not an abort.</b> The save raises
/// <see cref="MachineSetupViewModel.ConfigurationSaved"/> and the launch carries on from the save step, which is
/// the one route that does not end the process (S10.1: the step has exactly two outcomes). Nothing closes: the
/// host swaps the stage when the launch reaches its next surface, so the application never has a moment with no
/// window at all.
/// </para>
/// <para>
/// <b>The stage never refuses the launch by itself.</b> A refused sign-in is the view model's to state and the
/// stage stays put; what the pipeline does about a machine that is still unconfigured is the pipeline's, so
/// dismissing this stage by any route still cannot reach the main screens (FR-006).
/// </para>
/// </remarks>
internal sealed partial class MachineSetupPage : Page, ILaunchStageSurface
{
    /// <summary>
    /// The step the launch resumes at once the configuration has been saved. It is the save itself, so the step
    /// reports that this computer is already saved and the launch carries on into identity resolution (FR-020).
    /// </summary>
    private const string SaveStepId = "save-machine-configuration";

    /// <summary>Guards the ending, so two routes cannot both run it.</summary>
    private int _isEnding;

    public MachineSetupPage()
    {
        // Resolved before the markup loads, so every binding sees its source on the first pass.
        ViewModel = App.GetService<MachineSetupViewModel>();

        InitializeComponent();

        ViewModel.ConfigurationSaved += OnConfigurationSaved;
    }

    /// <summary>The setup screen's state, which owns the capture and the save.</summary>
    internal MachineSetupViewModel ViewModel { get; }

    /// <summary>Unlocks setup when the sign-in the person gave is authorised to configure a computer.</summary>
    private async void OnSignInClick(object sender, RoutedEventArgs e)
        => await ViewModel.SignInCommand.ExecuteAsync(
            new MachineSetupCredentials(ViewModel.SignInName, CredentialBox.Password)).ConfigureAwait(true);

    /// <summary>Saves this computer's identity and its note.</summary>
    private async void OnSaveClick(object sender, RoutedEventArgs e)
        => await ViewModel.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);

    /// <summary>
    /// The screen's own route out. With nobody authorised it is the person declining the sign-in, which has its
    /// own reason to state; with somebody authorised it is an abandoned setup.
    /// </summary>
    private async void OnCancelClick(object sender, RoutedEventArgs e)
        => await AbortAsync(ViewModel.IsAuthorized
            ? MachineSetupAbortRoute.CancelControl
            : MachineSetupAbortRoute.SignInDeclined).ConfigureAwait(true);

    /// <summary>Escape is a route out of setup, so it is answered rather than left to the framework.</summary>
    private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        _ = AbortAsync(MachineSetupAbortRoute.Escape);
    }

    /// <summary>Alt+F4 is a route out of setup, and it is stated as the window keys rather than as the close box.</summary>
    private void OnAltF4Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        _ = AbortAsync(MachineSetupAbortRoute.AltF4);
    }

    /// <summary>
    /// Closing the one window while this stage is showing is a route out of setup like any other, so it is
    /// stated as the close box rather than as an ending the host invented.
    /// </summary>
    public Task RequestCloseAsync() => AbortAsync(MachineSetupAbortRoute.WindowClosed);

    /// <summary>
    /// Carries the launch on once this computer's configuration has been saved. The stage is not closed here:
    /// the launch reaches its next surface and the host swaps the content, so there is never a moment with no
    /// window at all.
    /// </summary>
    private async void OnConfigurationSaved(object? sender, EventArgs e)
        => await App.GetService<ILaunchPipeline>()
            .RetryFromAsync(SaveStepId, CancellationToken.None)
            .ConfigureAwait(true);

    /// <summary>
    /// States why the process is ending, then ends it through the launch, which is the only thing that may end it
    /// (FR-008).
    /// </summary>
    /// <param name="route">The route that is ending setup, which is what the statement names.</param>
    private async Task AbortAsync(MachineSetupAbortRoute route)
    {
        if (Interlocked.Exchange(ref _isEnding, 1) == 1)
        {
            return;
        }

        var statement = MachineSetupAbortRoutes.StatementFor(route);

        try
        {
            if (App.MainWindow is { } owner)
            {
                await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
                {
                    Kind = MessageKind.Acknowledge,
                    Title = ViewModel.TitleText,
                    Message = statement,
                    CancelLabel = ViewModel.DismissActionText,
                });
            }
        }
        catch (Exception exception)
        {
            // The statement is not a prerequisite of the ending: if it could not be shown, the ending still has to
            // be stated through the launch rather than left unstated because a dialog failed.
            AppLog.Error("MachineSetup", exception, "The reason setup was ending could not be shown on the stage.");
        }

        App.GetService<ILaunchPipeline>().End(statement);
    }
}
