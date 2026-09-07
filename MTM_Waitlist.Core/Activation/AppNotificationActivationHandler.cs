using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Activation;

public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;
    private readonly IAppNotificationService _notificationService;
    private readonly IAppWindowProvider _appWindowProvider;

    // The Waitlist request detail page. Navigated by its view-model full name (the page key) rather than a typed
    // reference, because this handler lives in Core and must not reference the Waitlist module's view model type.
    private const string WaitlistRequestDetailPageKey = "MTM_Waitlist.Module_Waitlist.ViewModels.WaitlistViewDetailViewModel";

    public AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService, IAppWindowProvider appWindowProvider)
    {
        _navigationService = navigationService;
        _notificationService = notificationService;
        _appWindowProvider = appWindowProvider;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Deep-link: a new-request alert toast taps through to that request's detail page. The toast payload's
        // <toast launch="..."> argument carries a WaitlistRequestLink (action=openrequest&request=<guid>).
        if (AppInstance.GetCurrent().GetActivatedEventArgs()?.Data is AppNotificationActivatedEventArgs notificationArgs
            && WaitlistRequestLink.TryParse(notificationArgs.Argument, out var requestId))
        {
            StartupDebugLog.Info("AppNotification", $"Notification deep-link to request '{requestId:D}'.");
            // The Waitlist detail page resolves a request by its list id, which is request.Id.GetHashCode(). Queue
            // with low priority so the shell/navigation frame has initialized first.
            var parameter = requestId.GetHashCode();
            _appWindowProvider.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _navigationService.NavigateTo(WaitlistRequestDetailPageKey, parameter);
                _appWindowProvider.MainWindow.BringToFront();
            });
            await Task.CompletedTask;
            return;
        }

        // Unrecognized notification arguments: fall back to the original placeholder behaviour.
        _appWindowProvider.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            _appWindowProvider.MainWindow.ShowMessageDialogAsync("TODO: Handle notification activations.", "Notification Activation");
        });

        await Task.CompletedTask;
    }
}
