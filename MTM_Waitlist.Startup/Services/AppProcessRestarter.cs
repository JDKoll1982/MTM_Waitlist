using System.Diagnostics;
using System.Reflection;

using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Starts a fresh copy of this application.
/// </summary>
/// <remarks>
/// <para>
/// It exists so the relaunch is a seam: signing out has to relaunch the process, and a process that relaunches
/// itself cannot be observed by a test that is running inside it. The one production implementation starts the
/// executable through <see cref="Environment.ProcessPath"/>, including the <c>dotnet</c>-hosted case.
/// </para>
/// <para>
/// It replaces <c>IAppProcessRestarter</c>, which lived in the core contracts beside the retired sign-out
/// service. The capability is unchanged; what moved is where it is declared, so the launch module owns the
/// restart it performs rather than the rest of the application carrying a contract it had no use for.
/// </para>
/// </remarks>
public interface IProcessRestarter
{
    /// <summary>
    /// Starts a fresh copy of the application and returns whether it was started. <c>false</c>, or a thrown
    /// exception, is reported to the person rather than swallowed (FR-011).
    /// </summary>
    /// <returns>Whether a replacement instance was started.</returns>
    bool Restart();
}

/// <inheritdoc />
/// <remarks>
/// <b>Both ways of being hosted are handled.</b> An unpackaged build runs its own executable, and a
/// <c>dotnet</c>-hosted run has to be given the entry assembly as an argument, which is the same two-step
/// behaviour the corrupt-settings recovery path already relies on.
/// </remarks>
public sealed class AppProcessRestarter : IProcessRestarter
{
    /// <inheritdoc />
    public bool Restart()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            AppLog.Error(
                "AppProcessRestarter",
                new InvalidOperationException("The current process path is unavailable."),
                "Signing out could not relaunch the application: the current process path is unavailable.");
            return false;
        }

        var processStartInfo = new ProcessStartInfo(processPath)
        {
            UseShellExecute = true,
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(entryAssemblyPath))
            {
                AppLog.Error(
                    "AppProcessRestarter",
                    new InvalidOperationException("The application assembly path is unavailable."),
                    "Signing out could not relaunch the application: the application assembly path is unavailable.");
                return false;
            }

            processStartInfo.Arguments = $"\"{entryAssemblyPath}\"";
        }

        Process.Start(processStartInfo);
        return true;
    }
}
