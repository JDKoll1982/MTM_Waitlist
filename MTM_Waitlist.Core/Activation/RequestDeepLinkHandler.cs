using Microsoft.UI.Dispatching;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Activation;

/// <summary>
/// The window side of a deep-link activation: the two things the routing logic needs from the running app.
/// </summary>
/// <remarks>
/// Split out from the handler so the routing can be exercised without a UI thread. The production
/// implementation is <see cref="AppWindowDeepLinkWindow"/>, which wraps the app's main window; a check can
/// supply a stand-in and observe exactly what the handler asked the window to do.
/// </remarks>
public interface IDeepLinkWindow
{
    /// <summary>Runs an action on the UI thread once pending work has drained, so the shell exists first.</summary>
    /// <param name="action">The action to run.</param>
    void RunWhenIdle(Action action);

    /// <summary>Brings the app's main window to the foreground.</summary>
    void BringToFront();
}

/// <summary>The production <see cref="IDeepLinkWindow"/>: the app's main window and its dispatcher.</summary>
public sealed class AppWindowDeepLinkWindow : IDeepLinkWindow
{
    private readonly IAppWindowProvider _appWindowProvider;

    public AppWindowDeepLinkWindow(IAppWindowProvider appWindowProvider)
    {
        ArgumentNullException.ThrowIfNull(appWindowProvider);
        _appWindowProvider = appWindowProvider;
    }

    /// <inheritdoc />
    public void RunWhenIdle(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        // Low priority so the shell and navigation frame have initialized before the deep link is routed.
        _appWindowProvider.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => action());
    }

    /// <inheritdoc />
    public void BringToFront() => _appWindowProvider.MainWindow.BringToFront();
}

/// <summary>
/// The one place a notification activation is turned into navigation.
/// </summary>
/// <remarks>
/// Both entry points — the cold-start activation path
/// (<c>AppNotificationActivationHandler</c>) and the while-running path
/// (<c>AppNotificationService.OnNotificationInvoked</c>) — call this helper, and neither contains its own
/// parse or its own fallback UI. A recognised request link opens that request's detail page; anything else is
/// recorded for diagnosis and changes nothing the user can see. Neither outcome presents a dialog: an
/// activation is not a place to explain the code to its author.
/// </remarks>
public sealed class RequestDeepLinkHandler
{
    /// <summary>
    /// The Waitlist request detail page. Addressed by its view-model full name (the page key) rather than a
    /// typed reference, because this type lives in Core and must not reference the Waitlist module's view model.
    /// </summary>
    public const string WaitlistRequestDetailPageKey = "MTM_Waitlist.Module_Waitlist.ViewModels.WaitlistViewDetailViewModel";

    /// <summary>The diagnostic area used for every activation this helper handles.</summary>
    public const string DiagnosticArea = "AppNotification";

    private readonly INavigationService _navigationService;
    private readonly IDeepLinkWindow _window;

    public RequestDeepLinkHandler(INavigationService navigationService, IDeepLinkWindow window)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(window);

        _navigationService = navigationService;
        _window = window;
    }

    /// <summary>
    /// Routes an activation argument.
    /// </summary>
    /// <param name="arguments">The raw argument the activation carried; may be null or unrecognised.</param>
    /// <returns>
    /// <see langword="true"/> when the argument named a request and its detail page was queued;
    /// <see langword="false"/> when it did not map to anything.
    /// </returns>
    public bool TryHandleRequestDeepLink(string? arguments)
    {
        if (!WaitlistRequestLink.TryParse(arguments, out var requestId))
        {
            // Unrecognised: record it so the argument can be diagnosed from the log, and show nothing. The
            // window still comes forward, because the user did interact with the app.
            StartupDebugLog.Info(
                DiagnosticArea,
                $"Unrecognised activation argument '{arguments ?? "(none)"}'; nothing was shown and no navigation was queued.");

            _window.RunWhenIdle(_window.BringToFront);
            return false;
        }

        StartupDebugLog.Info(DiagnosticArea, $"Notification deep-link to request '{requestId:D}'.");

        // The Waitlist detail page resolves a request by its list id, which is request.Id.GetHashCode().
        var parameter = requestId.GetHashCode();

        _window.RunWhenIdle(() =>
        {
            _navigationService.NavigateTo(WaitlistRequestDetailPageKey, parameter);
            _window.BringToFront();
        });

        return true;
    }
}
