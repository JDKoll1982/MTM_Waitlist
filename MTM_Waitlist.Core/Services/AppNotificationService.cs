using System.Collections.Specialized;
using System.Web;
using Microsoft.Windows.AppNotifications;
using MTM_Waitlist.Activation;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Notifications;

public class AppNotificationService : IAppNotificationService
{
    private readonly INavigationService _navigationService;
    private readonly IAppWindowProvider _appWindowProvider;
    private readonly RequestDeepLinkHandler _requestDeepLinkHandler;

    public AppNotificationService(
        INavigationService navigationService,
        IAppWindowProvider appWindowProvider,
        RequestDeepLinkHandler requestDeepLinkHandler)
    {
        _navigationService = navigationService;
        _appWindowProvider = appWindowProvider;
        _requestDeepLinkHandler = requestDeepLinkHandler;
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        // FIX: Only initialize native WinRT notification subsystem if running as a packaged app
        if (RuntimeHelper.IsMSIX)
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
        }
    }

    public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // The argument the activation carries is the whole point of this handler. It used to be discarded in
        // favour of a placeholder dialog, so a tap-through did nothing; the same helper the cold-start path
        // uses now routes it, and the two paths cannot drift (contract C2/C3).
        _requestDeepLinkHandler.TryHandleRequestDeepLink(args.Argument);
    }

    public bool Show(string payload)
    {
        // FIX: Guard against unpackaged crashes since AppNotification creation requires MSIX identity
        if (!RuntimeHelper.IsMSIX)
        {
            // Unpackaged fallback logic: Log or show a simple native window dialog if desired
            return false;
        }

        try
        {
            var appNotification = new AppNotification(payload);
            AppNotificationManager.Default.Show(appNotification);
            return appNotification.Id != 0;
        }
        catch
        {
            return false;
        }
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        // FIX: Ensure unregistration only runs for packaged contexts to avoid disposal crashes
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                AppNotificationManager.Default.Unregister();
            }
            catch
            {
                // Fail-safe pass-through
            }
        }
    }
}
