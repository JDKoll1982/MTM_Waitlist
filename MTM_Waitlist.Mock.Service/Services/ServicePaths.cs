namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// The service's own folders, resolved for the signed-in account.
/// </summary>
/// <remarks>
/// These live here rather than on <see cref="ServiceHostBuilder"/> because the log file needs the app-data root
/// on its very first write, and reaching the host builder for it made the logger and the composition root
/// depend on each other. A path is a fact about the installation, not about the wiring.
/// </remarks>
internal static class ServicePaths
{
    /// <summary>Directory name under the user's local app data that holds the service's state.</summary>
    internal const string AppDataDirectoryName = "MTM_Waitlist.Mock.Service";

    /// <summary>
    /// The default app-data root for the current user.
    /// </summary>
    /// <remarks>
    /// Service-local by design: the configuration and run records must survive a database restore, so they never
    /// live in a store that restore replaces (<c>contracts/mock-service-configuration.md</c> §1).
    /// </remarks>
    internal static string DefaultAppDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataDirectoryName);
}
