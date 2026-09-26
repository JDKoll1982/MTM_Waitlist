namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// The process-lifecycle seam a class library can reach.
/// </summary>
/// <remarks>
/// It replaces the retired app-lifecycle service. Signing out has to end the process from a type that
/// cannot reference the application project, and ending it correctly means marshalling onto the UI thread, which
/// only the host can do — so the host installs the delegate and the library calls this instead of reaching for a
/// window it cannot see. A run with no host (a unit test) simply does nothing.
/// </remarks>
public static class AppLifecycleHost
{
    /// <summary>Ends the process. Installed by the host as it starts.</summary>
    public static Action? Exit { get; set; }

    /// <summary>Ends the process, or does nothing when no host installed the seam.</summary>
    public static void ExitApplication() => Exit?.Invoke();
}
