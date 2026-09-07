namespace MTM_Waitlist.Module_Core.Services;

/// <summary>
/// Pure decision logic for the file-09 "new-request alert" feature: a toast on a newly submitted request only
/// when the per-user toggle is ON <em>and</em> the app is packaged (MSIX) so app notifications can be shown.
/// OFF or unpackaged = no-op. Deterministic and unit-testable; the app supplies the packaged/toggle inputs.
/// </summary>
public static class RequestAlertGate
{
    /// <summary>
    /// True when a new-request toast should be shown for the given inputs: the per-user toggle is on and the app
    /// is packaged (so <c>IAppNotificationService</c> can display it).
    /// </summary>
    public static bool ShouldShowToast(bool alertsEnabled, bool isPackaged)
        => alertsEnabled && isPackaged;

    /// <summary>
    /// The decision to notify on a request-created signal. <paramref name="requestCreatedSignal"/> is true when a
    /// new request was actually created; the toast only fires when that is true and <see cref="ShouldShowToast"/>.
    /// </summary>
    public static bool ShouldNotifyOnCreated(bool requestCreatedSignal, bool alertsEnabled, bool isPackaged)
        => requestCreatedSignal && ShouldShowToast(alertsEnabled, isPackaged);
}
