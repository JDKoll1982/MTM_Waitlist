using System.Threading;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The machine-setup window: it is shown when this computer has no configuration and before any operator signs in,
/// it takes the identity of whoever may configure it, and it ends the process on every route out that is not
/// completion (`contracts/machine-configuration-contract.md` sections 2 and 3; FR-006, FR-007, FR-008).
/// </summary>
/// <remarks>
/// <para>
/// <b>Five routes out, one ending.</b> The close box, Escape, Alt+F4, the screen's own close control and declining
/// the sign-in all arrive at <see cref="AbortAsync"/>, which states the route's reason before the process goes. A
/// route that was wired to something else would be a route that leaves a window standing, so they all share the
/// one path (FR-008).
/// </para>
/// <para>
/// <b>The reason is stated, not logged.</b> The statement is shown in a dialog the person dismisses, and only then
/// is the ending declared through the launch. An operator who abandons setup therefore reads why, rather than
/// watching a window vanish and reporting a crash (FR-008, S15).
/// </para>
/// <para>
/// <b>Completing setup is not an abort.</b> The save raises <see cref="MachineSetupViewModel.ConfigurationSaved"/>
/// and the launch carries on from the save step, which is the one route that does not end the process
/// (S10.1: the step has exactly two outcomes).
/// </para>
/// <para>
/// <b>The window never refuses the launch by itself.</b> A refused sign-in is the view model's to state and the
/// window stays open; what the pipeline does about a machine that is still unconfigured is the pipeline's, so
/// dismissing this window by any route still cannot reach the main screens (FR-006).
/// </para>
/// </remarks>
public sealed partial class MachineSetupWindow : WindowEx
{
    /// <summary>
    /// The step the launch resumes at once the configuration has been saved. It is the save itself, so the step
    /// reports that this computer is already saved and the launch carries on into identity resolution (FR-020).
    /// </summary>
    private const string SaveStepId = "save-machine-configuration";

    /// <summary>Guards the ending, so the close handler and an accelerator cannot both run it.</summary>
    private int _isEnding;

    /// <summary>Whether the window is closing because setup completed, which is not an abort.</summary>
    private bool _isContinuing;

    public MachineSetupWindow()
    {
        // Resolved before the markup loads, so every binding sees its source on the first pass.
        ViewModel = App.GetService<MachineSetupViewModel>();

        InitializeComponent();

        Title = ViewModel.TitleText;

        // The surface opens in the middle of the display it is on, so a person looking at either screen finds it
        // where they are looking.
        WindowStartupPlacement.CentreOnScreen(this, "MachineSetup");

        // The close box and Alt+F4 raise this one. Alt+F4 also has an accelerator of its own, so a route that
        // arrives here is the window's own close affordance and says so.
        AppWindow.Closing += OnWindowClosing;

        ViewModel.ConfigurationSaved += OnConfigurationSaved;
    }

    /// <summary>The setup screen's state, which owns the capture and the save.</summary>
    internal MachineSetupViewModel ViewModel { get; }

    /// <summary>Unlocks setup when the sign-in the person gave is authorised to configure a computer.</summary>
    private async void OnSignInClick(object sender, RoutedEventArgs e)
        => await ViewModel.SignInCommand.ExecuteAsync(
            new MachineSetupCredentials(ViewModel.SignInName, CredentialBox.Password));

    /// <summary>Saves this computer's identity and its picture sources.</summary>
    private async void OnSaveClick(object sender, RoutedEventArgs e)
        => await ViewModel.SaveCommand.ExecuteAsync(null);

    /// <summary>
    /// The screen's own route out. With nobody authorised it is the person declining the sign-in, which has its
    /// own reason to state; with somebody authorised it is an abandoned setup.
    /// </summary>
    private async void OnCancelClick(object sender, RoutedEventArgs e)
        => await AbortAsync(ViewModel.IsAuthorized
            ? MachineSetupAbortRoute.CancelControl
            : MachineSetupAbortRoute.SignInDeclined);

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
    /// Turns the window's own close affordance into the stated ending, and stays out of the way when the launch
    /// is handing over because setup completed.
    /// </summary>
    private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isContinuing)
        {
            return;
        }

        // The close is cancelled and the ending happens after the reason has been stated, so the person never
        // sees the window disappear without being told why (FR-008).
        args.Cancel = true;

        _ = AbortAsync(MachineSetupAbortRoute.WindowClosed);
    }

    /// <summary>
    /// Carries the launch on once this computer's configuration has been saved, and closes this window only after
    /// the launch has a surface to show, so the application never has a moment with no window at all.
    /// </summary>
    private async void OnConfigurationSaved(object? sender, EventArgs e)
    {
        _isContinuing = true;

        await App.GetService<ILaunchPipeline>()
            .RetryFromAsync(SaveStepId, CancellationToken.None)
            .ConfigureAwait(true);

        Close();
    }

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
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = ViewModel.TitleText,
                Content = statement,
                CloseButtonText = ViewModel.DismissActionText,
                DefaultButton = ContentDialogButton.Close,
            };

            await dialog.ShowAsync();
        }
        catch (Exception exception)
        {
            // The statement is not a prerequisite of the ending: if it could not be shown, the ending still has to
            // be stated through the launch rather than left unstated because a dialog failed.
            AppLog.Error("MachineSetup", exception, "The reason setup was ending could not be shown on the window.");
        }

        App.GetService<ILaunchPipeline>().End(statement);
    }
}
