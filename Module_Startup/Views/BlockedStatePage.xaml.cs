using System.Threading;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;
using MTM_Waitlist.Module_Shared.Dialogs;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The blocked stage: the single surface a stopped launch shows. It states the cause, offers only the actions
/// that could remove it, lists what a reset will touch before anything is reset, and ends the process with its
/// reason stated when the person closes it
/// (`contracts/launch-step-contract.md` §4, §5; FR-004, FR-008, FR-016, FR-017, FR-018, FR-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is shown only when the person is needed.</b> <see cref="StartAsync"/> offers the fault to the repair
/// policy first, and a fault that can be put right without asking is put right and the launch carries on. The
/// stage stays collapsed — and the host has not swapped to it — so no prompt is raised rather than one appearing
/// and taking itself away (FR-019).
/// </para>
/// <para>
/// <b>The reset is asked about before it runs.</b> The restore control opens a dialog that lists exactly what
/// would be reset, and the reset itself runs only if the person agrees to that list. The list and the reset come
/// from one table entry, so what they read is what happens (FR-018).
/// </para>
/// <para>
/// <b>Closing the one window ends the process with its reason stated first.</b> The stage answers the window's
/// close through <see cref="RequestCloseAsync"/>, and the surface's own close control reaches the same ending, so
/// a stop that the person closes is never reported as a crash (FR-008).
/// </para>
/// </remarks>
internal sealed partial class BlockedStatePage : Page, ILaunchStageSurface
{
    /// <summary>Guards the ending, so the window's close and the close control cannot both run it.</summary>
    private int _isEnding;

    public BlockedStatePage()
    {
        // Resolved before the markup loads, so every binding sees its source on the first pass.
        ViewModel = App.GetService<BlockedStateViewModel>();

        InitializeComponent();
    }

    /// <summary>The stopped launch's state, which owns the cause, the remedies and the reset.</summary>
    internal BlockedStateViewModel ViewModel { get; }

    /// <summary>
    /// Offers the fault to the repair policy, shows the stage only when the person is needed, and never raises a
    /// prompt for a fault that could be repaired without them (FR-019).
    /// </summary>
    /// <remarks>
    /// A repair that could not be made, and a launch that could not be carried on after one, both leave the stop
    /// on screen rather than leaving the person with no surface at all. The stage is where the cause is read, so
    /// it has to survive anything the repair path does.
    /// </remarks>
    internal async Task StartAsync()
    {
        try
        {
            if (!await ViewModel.PrepareAsync(CancellationToken.None).ConfigureAwait(true))
            {
                // The fault was put right without asking and the launch has already been carried on, so this
                // stage is never revealed: the host swaps the content when the launch reaches its next surface.
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

        RootGrid.Visibility = Visibility.Visible;
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
            if (App.MainWindow is { } owner)
            {
                var outcome = await MessageDialogHost.AskAsync(owner, new MessageDialogRequest
                {
                    Kind = MessageKind.DestructiveConfirm,
                    Title = ViewModel.ResetPreviewTitleText,
                    Message = string.IsNullOrWhiteSpace(ViewModel.RestoreDefaultsPreview)
                        ? "Resetting returns every setting on this machine to its shipped value."
                        : ViewModel.RestoreDefaultsPreview,
                    ActLabel = ViewModel.ResetPreviewConfirmText,
                    CancelLabel = ViewModel.ResetPreviewCancelText,
                });

                agreed = outcome.IsAct;
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("BlockedState", exception, "What a reset would touch could not be shown, so nothing was reset.");
        }

        if (agreed)
        {
            await ViewModel.RestoreDefaultsCommand.ExecuteAsync(null).ConfigureAwait(true);
        }
    }

    /// <summary>Ends the process from the surface's own close control, with its reason stated first (FR-008).</summary>
    private void OnCloseClick(object sender, RoutedEventArgs e) => EndWithReason();

    /// <summary>
    /// Ends the process with the stop's own reason when the one window is closed while the stop is showing
    /// (FR-008).
    /// </summary>
    public Task RequestCloseAsync()
    {
        EndWithReason();

        return Task.CompletedTask;
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
