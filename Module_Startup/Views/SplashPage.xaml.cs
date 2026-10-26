using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.Services;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The launch surface, and the stage the one window shows while a launch runs: it states how far the launch has
/// got, every operation still in flight, and the cause of a stop in a strip along the bottom
/// (`contracts/launch-step-contract.md` §§3-4, `contracts/launch-concurrency-contract.md` §3; FR-002, FR-004,
/// FR-005, FR-031, FR-039).
/// </summary>
/// <remarks>
/// <para>
/// <b>Every open line is rendered, not one current step.</b> The rows come from
/// <see cref="LaunchSurfaceViewModel"/>, which keeps one row per operation from the moment it announces itself
/// until the row that ends it arrives, so a launch that runs several operations at once shows several rows
/// (FR-031). The line that a stop carries is the same row that states the cause, which is why the strip at the
/// bottom repeats it rather than summarising it (FR-004).
/// </para>
/// <para>
/// <b>The strip is a row of the layout rather than something laid over the rows above it.</b> The rows take
/// their own row and the strip its own automatic row beneath it, so the two never share any pixels and a message
/// cannot cover what the person is reading. That is the whole of FR-005.
/// </para>
/// <para>
/// <b>The page owns the thread affinity; the view models stay plain objects.</b> A launch step's continuation
/// runs off the surface's thread, so each view model hands every change to whatever the surface gives it and
/// this page gives it the dispatcher. That is what keeps the view models drivable by a headless test while the
/// window still updates safely.
/// </para>
/// <para>
/// <b>It is a stage of the one window, not a window.</b> Closing that window while this stage is showing is
/// answered by <see cref="RequestCloseAsync"/>, which ends the process with its reason stated first (FR-008).
/// </para>
/// </remarks>
internal sealed partial class SplashPage : Page, ILaunchStageSurface
{
    /// <summary>Guards the ending, so the person's close and the hand-over cannot both run it.</summary>
    private static bool s_isEnding;

    public SplashPage()
    {
        // Both states are resolved from the host rather than taken as constructor arguments, because the host
        // builds this page by type.
        ViewModel = App.GetService<SplashViewModel>();
        Surface = App.GetService<LaunchSurfaceViewModel>();

        // Set before anything is drawn, so a line written by a launch that is already running is marshalled too.
        ViewModel.UiThreadMarshaller = SendToThisThread;
        Surface.UiThreadMarshaller = SendToThisThread;

        InitializeComponent();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += OnUnloaded;

        DrawState();
    }

    /// <summary>The launch's own state: how far it has got and the cause of a stop.</summary>
    internal SplashViewModel ViewModel { get; }

    /// <summary>The state with several operations in flight, which is where the rows come from (FR-031).</summary>
    internal LaunchSurfaceViewModel Surface { get; }

    /// <summary>
    /// Draws the state the page was constructed with, for the case where the launch began before it appeared.
    /// </summary>
    private void DrawState()
    {
        ProgressTextBlock.Text = ViewModel.ProgressText;
        ErrorTextBlock.Text = ViewModel.Diagnosis ?? string.Empty;

        // The strip carries either the cause of a stop or the statement that a failed picture refresh is being
        // held, and it is away only when it has neither to say.
        ErrorStrip.Visibility = string.IsNullOrWhiteSpace(ViewModel.Diagnosis) && ViewModel.IsHoldingForPictureCopies is false
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>Redraws the parts that are not the bound collection, when either of them changes.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName
            is nameof(SplashViewModel.ProgressText)
            or nameof(SplashViewModel.Diagnosis)
            or nameof(SplashViewModel.IsHoldingForPictureCopies))
        {
            DrawState();
        }
    }

    /// <summary>Runs a change on the thread that owns this page, which is the only thread that may draw it.</summary>
    private void SendToThisThread(Action work)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            work();
            return;
        }

        _ = DispatcherQueue.TryEnqueue(() => work());
    }

    /// <summary>Stops listening once the page is gone, so nothing tries to draw into a page that has left.</summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Unloaded -= OnUnloaded;
    }

    /// <summary>
    /// Ends the hold on a picture refresh that did not work, which lets the launch carry on to its surface.
    /// </summary>
    /// <remarks>
    /// The hold is the statement's own button rather than a second decision: the refresh has already been given up
    /// on, the copies on this computer are the ones in use, and the person is saying they have read that.
    /// </remarks>
    private void OnContinueClick(object sender, RoutedEventArgs e)
        => App.GetService<IPictureCopyHold>().Acknowledge();

    /// <summary>
    /// Ends the process from the surface's own button bar.
    /// </summary>
    /// <remarks>
    /// The one window carries its own close box, so this is a second way out rather than the only one, and it
    /// runs the same ending the window's own close runs so the two routes cannot state different reasons or
    /// leave different things running (FR-008).
    /// </remarks>
    private async void OnCloseClick(object sender, RoutedEventArgs e)
        => await EndProcessAsync("Startup_Launch.EndedByCloseButton".GetLocalized()).ConfigureAwait(true);

    /// <summary>
    /// Ends the process with its reason stated first when the one window is closed while the launch is running
    /// (FR-008).
    /// </summary>
    public Task RequestCloseAsync() => EndProcessAsync("Startup_Launch.EndedByWindowClose".GetLocalized());

    /// <summary>
    /// Stops the host and ends the process, stating why. Bounded and idempotent: the host stop is awaited so no
    /// background service is still writing when the process goes away, and a second caller is a no-op.
    /// </summary>
    /// <param name="reason">
    /// Why the process is ending, in the reader's words. It is the same ending whichever route reached it, so the
    /// surface's own button and the window's close answer alike.
    /// </param>
    internal static async Task EndProcessAsync(string reason)
    {
        if (s_isEnding)
        {
            return;
        }

        s_isEnding = true;

        AppLog.Info("StartupLaunch", reason);
        await App.ShutdownHostAsync().ConfigureAwait(true);
        App.ExitApplication();
    }
}
