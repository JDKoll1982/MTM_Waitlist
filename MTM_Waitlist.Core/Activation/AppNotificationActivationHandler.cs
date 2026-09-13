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
    private readonly IAppNotificationService _notificationService;
    private readonly RequestDeepLinkHandler _requestDeepLinkHandler;

    public AppNotificationActivationHandler(
        IAppNotificationService notificationService,
        RequestDeepLinkHandler requestDeepLinkHandler)
    {
        _notificationService = notificationService;
        _requestDeepLinkHandler = requestDeepLinkHandler;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // Deep-link: a new-request alert toast taps through to that request's detail page. The toast payload's
        // <toast launch="..."> argument carries a WaitlistRequestLink (action=openrequest&request=<guid>).
        // An unrecognised argument is recorded and shows nothing — there is no placeholder path here any more.
        var arguments = (AppInstance.GetCurrent().GetActivatedEventArgs()?.Data as AppNotificationActivatedEventArgs)?.Argument;
        _requestDeepLinkHandler.TryHandleRequestDeepLink(arguments);

        await Task.CompletedTask;
    }
}
