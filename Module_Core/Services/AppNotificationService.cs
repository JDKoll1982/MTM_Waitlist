using System.Collections.Specialized;
using System.Web;
using Microsoft.Windows.AppNotifications;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.ViewModels;

namespace MTM_Waitlist.Notifications;

public class AppNotificationService : IAppNotificationService
{
    private readonly INavigationService _navigationService;

    public AppNotificationService(INavigationService navigationService)
    {
        _navigationService = navigationService;
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
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            App.MainWindow.ShowMessageDialogAsync("TODO: Handle notification invocations when your app is already running.", "Notification Invoked");
            App.MainWindow.BringToFront();
        });
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
