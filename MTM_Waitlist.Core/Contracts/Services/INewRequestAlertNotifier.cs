namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// Shows the file-09 "new request" toast when a request is created and the per-user alert toggle is ON and the app
/// is packaged. The payload embeds a <see cref="MTM_Waitlist.Module_Core.Services.WaitlistRequestLink"/> deep-link
/// argument so tapping the toast opens that request. Callers supply the created request's id + display text; the
/// decision (signal × toggle × packaged) is delegated to <see cref="INewRequestAlertService"/>.
/// </summary>
public interface INewRequestAlertNotifier
{
    /// <summary>
    /// Decides whether a new-request toast should show and, when it should, shows it via
    /// <see cref="IAppNotificationService"/>. Returns true when a toast was actually shown.
    /// </summary>
    Task<bool> NotifyNewRequestAsync(Guid requestId, string title, string body, bool isPackaged, CancellationToken cancellationToken = default);
}
