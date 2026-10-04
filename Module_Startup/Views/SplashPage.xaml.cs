using System.ComponentModel;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Startup.ViewModels;

namespace MTM_Waitlist.Module_Startup.Views;

/// <summary>
/// The launch surface: it states the work under way, the count it has reached, and the cause of a stop in a
/// strip along the bottom (`contracts/launch-step-contract.md` §3, §4; FR-002, FR-004, FR-005, FR-039).
/// </summary>
/// <remarks>
/// <para>
/// <b>The strip is a row of the layout rather than something laid over the line above it.</b> The line under way
/// takes its own row and the strip its own automatic row beneath it, so the two never share any pixels and a
/// message cannot cover what the person is reading. That is the whole of FR-005.
/// </para>
/// <para>
/// <b>The page owns the thread affinity; the view model stays a plain object.</b> A launch step's continuation
/// runs off the surface's thread, so the view model hands every change to whatever the surface gives it and this
/// page gives it the dispatcher. That is what keeps the view model drivable by a headless test while the window
/// still updates safely.
/// </para>
/// <para>
/// <b>One surface, both paths.</b> The launch window shows this page, and the single-window path navigates to it,
/// so there is one account of a launch rather than a window's copy and a page's copy that can disagree.
/// </para>
/// </remarks>
public sealed partial class SplashPage : Page
{
    /// <summary>
    /// The launch window's state. It is resolved from the host rather than taken as a constructor argument,
    /// because <c>Frame.Navigate</c> activates a page through its XAML type and cannot pass one.
    /// </summary>
    public SplashViewModel ViewModel { get; }

    public SplashPage()
    {
        ViewModel = App.GetService<SplashViewModel>();

        // Set before anything is drawn, so a line written by a launch that is already running is marshalled too.
        ViewModel.UiThreadMarshaller = SendToThisThread;

        InitializeComponent();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += OnUnloaded;

        DrawState();
    }

    /// <summary>
    /// Draws the state the page was constructed with, for the case where the launch began before it appeared.
    /// </summary>
    private void DrawState()
    {
        ProgressTextBlock.Text = ViewModel.ProgressText;
        ErrorTextBlock.Text = ViewModel.Diagnosis ?? string.Empty;

        ErrorStrip.Visibility = string.IsNullOrWhiteSpace(ViewModel.Diagnosis)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    /// <summary>Redraws the parts that are not the bound collection, when either of them changes.</summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(SplashViewModel.ProgressText) or nameof(SplashViewModel.Diagnosis))
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
    /// Ends the process from the surface's own button bar.
    /// </summary>
    /// <remarks>
    /// The window draws no title bar and therefore carries no system buttons, so this is the only visible way out
    /// of the surface. It runs the same ending the window's own close runs, so the two routes cannot state
    /// different reasons or leave different things running (FR-008).
    /// </remarks>
    private async void OnCloseClick(object sender, RoutedEventArgs e)
        => await SplashWindow.EndProcessAsync("Startup_Launch.EndedByCloseButton".GetLocalized());
}
