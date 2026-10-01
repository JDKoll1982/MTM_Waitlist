using System.Threading;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The blocked-state window: the single surface a stopped launch shows. It states the cause, offers only the
/// actions that could remove it, lists what a reset will touch before anything is reset, and ends the process
/// with its reason stated when the person closes it
/// (`contracts/launch-step-contract.md` §4, §5; FR-004, FR-008, FR-016, FR-017, FR-018, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is shown only when the person is needed.</b> <see cref="StartAsync"/> offers the fault to the repair
/// policy first, and a fault that can be put right without asking is put right and the launch carries on. The
/// window is then never activated, so no prompt is raised rather than one appearing and taking itself away
/// (FR-019).
/// </para>
/// <para>
/// <b>The reset is asked about before it runs.</b> The restore control opens a dialog that lists exactly what
/// would be reset, and the reset itself runs only if the person agrees to that list. The list and the reset come
/// from one table entry, so what they read is what happens (FR-018).
/// </para>
/// <para>
/// <b>Closing it ends the process, and states why first.</b> The close box and the surface's own close control
/// reach the same ending, which states the reason through the launch before the process goes, so a stop that the
/// person closes is never reported as a crash (FR-008).
/// </para>
/// </remarks>
public sealed partial class BlockedStateWindow : WindowEx
{
    /// <summary>Guards the ending, so the close box and the close control cannot both run it.</summary>
    private int _isEnding;

    public BlockedStateWindow()
    {
        // Resolved before the markup loads, so every binding sees its source on the first pass.
        ViewModel = App.GetService<BlockedStateViewModel>();

        InitializeComponent();

        Title = ViewModel.HeadingText;

        // The surface opens in the middle of the display it is on, so a person looking at either screen finds it
        // where they are looking.
        WindowStartupPlacement.CentreOnScreen(this, "BlockedState");

        // The close box and Alt+F4 raise this one, and it funnels into the same stated ending the surface's own
        // close control reaches, so no route out leaves the person without a reason (FR-008).
        AppWindow.Closing += OnWindowClosing;
    }

    /// <summary>The stopped launch's state, which owns the cause, the remedies and the reset.</summary>
    internal BlockedStateViewModel ViewModel { get; }

    /// <summary>
    /// Whether the host is closing this window because the launch has reached another surface. The host sets it
    /// immediately before closing, and a close that carries it is a hand-over rather than the person's own close.
    /// </summary>
    public bool IsHandingOver { get; set; }

    /// <summary>
    /// Offers the fault to the repair policy, shows the window only when the person is needed, and never raises a
    /// prompt for a fault that could be repaired without them (FR-019).
    /// </summary>
    /// <remarks>
    /// A repair that could not be made, and a launch that could not be carried on after one, both leave the stop
    /// on screen rather than leaving the person with no window at all. The surface is where the cause is read, so
    /// it has to survive anything the repair path does.
    /// </remarks>
    internal async Task StartAsync()
    {
        try
        {
            if (!await ViewModel.PrepareAsync(CancellationToken.None).ConfigureAwait(true))
            {
                // The fault was put right without asking and the launch has already been carried on, so this
                // window is never shown. The host closes it when the launch reaches its next surface, which is
                // what the flag is there for.
                IsHandingOver = true;
                return;
            }
        }
        catch (Exception exception)
        {
            AppLog.Error(
                "BlockedState",
                exception,
                "A fault that could be repaired without asking could not be carried on, so the stop is being shown.");
        }

        Activate();
    }

    /// <summary>Starts the application again, which is how the failed work is repeated (FR-016).</summary>
    private void OnRetryClick(object sender, RoutedEventArgs e)
        => ViewModel.RetryCommand.Execute(null);

    /// <summary>
    /// Shows exactly what a reset will touch and resets only if the person agrees to that list (FR-018).
    /// </summary>
    /// <remarks>
    /// The dialog is dismissed on every route, including a cancelled one, and the reset runs only on the primary
    /// answer. A dialog that could not be shown at all leaves everything as it is, because a reset nobody agreed
    /// to is not a reset this surface may run.
    /// </remarks>
    private async void OnRestoreDefaultsClick(object sender, RoutedEventArgs e)
    {
        var agreed = false;

        try
        {
            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = ViewModel.ResetPreviewTitleText,
                Content = ViewModel.RestoreDefaultsPreview,
                PrimaryButtonText = ViewModel.ResetPreviewConfirmText,
                CloseButtonText = ViewModel.ResetPreviewCancelText,
                DefaultButton = ContentDialogButton.Close,
            };

            agreed = await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
        catch (Exception exception)
        {
            AppLog.Error("BlockedState", exception, "What a reset would touch could not be shown, so nothing was reset.");
        }

        if (agreed)
        {
            await ViewModel.RestoreDefaultsCommand.ExecuteAsync(null);
        }
    }

    /// <summary>Ends the process from the surface's own close control, with its reason stated first (FR-008).</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => EndWithReason();

    /// <summary>
    /// Turns the window's own close affordance into the stated ending, and stays out of the way when the host
    /// closes it to hand the launch over to another surface.
    /// </summary>
    private void OnWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (IsHandingOver)
        {
            return;
        }

        // The close is cancelled and the ending happens after the reason has been stated, so the person never sees
        // the window disappear without being told why (FR-008).
        args.Cancel = true;

        EndWithReason();
    }

    /// <summary>
    /// States why the process is ending and ends it through the launch, which is the only thing that may end it
    /// (FR-008).
    /// </summary>
    private void EndWithReason()
    {
        if (Interlocked.Exchange(ref _isEnding, 1) == 1)
        {
            return;
        }

        App.GetService<ILaunchPipeline>().End(ViewModel.CloseReasonText);
    }
}
