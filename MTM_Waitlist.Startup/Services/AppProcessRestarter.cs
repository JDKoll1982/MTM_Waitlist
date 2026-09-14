using System.Diagnostics;
using System.Reflection;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;

namespace MTM_Waitlist.Module_Startup.Services;

/// <summary>
/// Starts a fresh copy of this application through the same two steps the corrupt-settings recovery path uses:
/// <see cref="Environment.ProcessPath"/>, with the entry assembly passed as an argument in the
/// <c>dotnet</c>-hosted case.
/// <para>
/// This is the seam that makes signing out provable — a process cannot be restarted from inside a test, so the
/// restart is an interface and this is its one production implementation.
/// </para>
/// </summary>
public sealed class AppProcessRestarter : IAppProcessRestarter
{
    /// <inheritdoc />
    public bool Restart()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            StartupDebugLog.Error(
                "AppProcessRestarter",
                new InvalidOperationException("The current process path is unavailable."),
                "Signing out could not relaunch the application: the current process path is unavailable.");
            return false;
        }

        var processStartInfo = new ProcessStartInfo(processPath)
        {
            UseShellExecute = true
        };

        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrWhiteSpace(entryAssemblyPath))
            {
                StartupDebugLog.Error(
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
