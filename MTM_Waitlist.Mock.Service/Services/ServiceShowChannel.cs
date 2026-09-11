namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The channel a second launch uses to ask the running service to show one of its surfaces.
/// </summary>
/// <remarks>
/// <para>
/// The activation a second launch hands over does not carry the command line for an unpackaged app
/// (verified against the running service on 2026-09-11: the redirect happened, the single instance held,
/// and no window opened), so the request cannot travel with it. The launching process reads its own
/// command line and signals a named event instead, and the running service waits on those events.
/// </para>
/// <para>
/// The names are session-local, which is right for a service that runs as the signed-in account, and are
/// pinned by a test so a rename cannot silently break the handover.
/// </para>
/// </remarks>
internal static class ServiceShowChannel
{
    /// <summary>The event signalled to ask for the status surface.</summary>
    internal const string StatusEventName = @"Local\MTM_Waitlist.Mock.Service.ShowStatus";

    /// <summary>The event signalled to ask for the settings surface.</summary>
    internal const string SettingsEventName = @"Local\MTM_Waitlist.Mock.Service.ShowSettings";

    /// <summary>
    /// Asks a running service to show a surface.
    /// </summary>
    /// <param name="surface">The surface that was asked for.</param>
    /// <remarks>
    /// Does nothing when nothing was requested, and nothing when no service is listening - which is the
    /// ordinary case for a first launch, where this process becomes the service itself and opens its own
    /// window from the command line.
    /// </remarks>
    internal static void Request(ServiceActivationParser.RequestedSurface surface)
    {
        var eventName = surface switch
        {
            ServiceActivationParser.RequestedSurface.Status => StatusEventName,
            ServiceActivationParser.RequestedSurface.Settings => SettingsEventName,
            _ => null
        };

        if (eventName is null)
        {
            return;
        }

        try
        {
            using var request = EventWaitHandle.OpenExisting(eventName);
            request.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // No service is listening yet.
        }
        catch (UnauthorizedAccessException)
        {
            // A service in another session owns the name; this process cannot reach it and cannot fix that.
        }
    }
}
