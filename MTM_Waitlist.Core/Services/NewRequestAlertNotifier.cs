using System.Security;
using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Core.Services;

/// <inheritdoc cref="INewRequestAlertNotifier"/>
public sealed class NewRequestAlertNotifier : INewRequestAlertNotifier
{
    private readonly IAppNotificationService _notifications;
    private readonly INewRequestAlertService _alertService;

    public NewRequestAlertNotifier(IAppNotificationService notifications, INewRequestAlertService alertService)
    {
        _notifications = notifications;
        _alertService = alertService;
    }

    public async Task<bool> NotifyNewRequestAsync(
        Guid requestId,
        string title,
        string body,
        bool isPackaged,
        CancellationToken cancellationToken = default)
    {
        // requestCreatedSignal is true here because this is only called on a successful request creation.
        var shouldNotify = await _alertService
            .ShouldNotifyOnCreatedAsync(requestCreatedSignal: true, isPackaged, cancellationToken)
            .ConfigureAwait(false);
        if (!shouldNotify)
        {
            StartupDebugLog.Info("NewRequestAlert", "Skipping new-request toast: toggle off or app not packaged.");
            return false;
        }

        var arguments = WaitlistRequestLink.Build(requestId);
        var payload = BuildToastXml(title, body, arguments);
        var shown = _notifications.Show(payload);
        StartupDebugLog.Info("NewRequestAlert", $"New-request toast {(shown ? "shown" : "not shown")}. RequestId='{requestId:D}', Title='{title}'.");
        return shown;
    }

    /// <summary>
    /// Builds the app-notification XML for a two-line toast whose <c>launch</c> attribute carries the deep-link
    /// argument (so <c>AppNotificationActivatedEventArgs.Argument</c> routes the tap back to the request).
    /// </summary>
    public static string BuildToastXml(string title, string body, string launchArguments)
    {
        var titleXml = SecurityElement.Escape(title ?? string.Empty);
        var bodyXml = SecurityElement.Escape(body ?? string.Empty);
        var argumentsXml = SecurityElement.Escape(launchArguments ?? string.Empty);
        return $"<toast launch=\"{argumentsXml}\">"
            + "<visual><binding template=\"ToastGeneric\">"
            + $"<text>{titleXml}</text><text>{bodyXml}</text>"
            + "</binding></visual></toast>";
    }
}
