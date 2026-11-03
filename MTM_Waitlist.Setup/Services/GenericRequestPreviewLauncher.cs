using System.Diagnostics;

using MTM_Waitlist.Module_Setup.Contracts.Services;
using MTM_Waitlist.Module_Setup.Models;

namespace MTM_Waitlist.Module_Setup.Services;

/// <summary>
/// Finds and starts the generic request preview host (015-generic-wc-ui-shell).
/// </summary>
/// <remarks>
/// <para>
/// <b>Where it looks, in order.</b> An explicit path in <c>MTM_GENERIC_PREVIEW_PATH</c> wins; then the application's
/// own folder; then, on a development machine, the preview's build output under the repository that holds the
/// application project. Each step is a real location rather than a guess, and a machine with none of them is a
/// machine that reports the preview as unavailable.
/// </para>
/// <para>
/// <b>The file name is the only place this program is named.</b> The preview host is a separate project that this
/// application never references, and the published payload does not contain it. Naming it here is what lets a
/// developer open it from the screen that marks the work center; nothing is loaded from it, and no type, resource
/// or assembly of it is reachable from this application.
/// </para>
/// </remarks>
public sealed class GenericRequestPreviewLauncher : IGenericRequestPreviewLauncher
{
    /// <summary>The preview host's executable file name, as its own project builds it.</summary>
    private const string PreviewExecutableName = "TempGenericRequest.Preview.exe";

    /// <summary>The preview host's project folder inside the repository.</summary>
    private const string PreviewProjectFolder = "TempGenericRequest.Preview";

    /// <summary>An explicit override, for a machine that keeps the preview somewhere else.</summary>
    private const string PreviewPathEnvironmentVariable = "MTM_GENERIC_PREVIEW_PATH";

    /// <summary>The application project file, which identifies the repository root from a build folder.</summary>
    private const string ApplicationProjectFileName = "MTM_Waitlist.csproj";

    /// <summary>How far above the application's folder the repository root is looked for.</summary>
    private const int RepositorySearchDepth = 6;

    /// <inheritdoc />
    public string? ResolvePreviewExecutable()
    {
        if (Environment.GetEnvironmentVariable(PreviewPathEnvironmentVariable) is { Length: > 0 } configured
            && File.Exists(configured))
        {
            return configured;
        }

        var beside = Path.Combine(AppContext.BaseDirectory, PreviewExecutableName);
        if (File.Exists(beside))
        {
            return beside;
        }

        // A developer may also drop the whole preview folder next to the application.
        var besideInFolder = Path.Combine(AppContext.BaseDirectory, PreviewProjectFolder, PreviewExecutableName);
        if (File.Exists(besideInFolder))
        {
            return besideInFolder;
        }

        return ResolveFromRepository();
    }

    /// <inheritdoc />
    public Task<SetupSelectionResult> LaunchAsync(string? workCenterName = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var executable = ResolvePreviewExecutable();

        if (executable is null)
        {
            return Task.FromResult(new SetupSelectionResult
            {
                Success = false,
                Message = "The generic request preview is not on this machine. Build "
                    + PreviewProjectFolder
                    + " or set "
                    + PreviewPathEnvironmentVariable
                    + " to its executable.",
            });
        }

        var workCenter = workCenterName?.Trim() ?? string.Empty;

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = workCenter.Length == 0
                ? string.Empty
                : $"--work-center \"{workCenter.Replace("\"", string.Empty)}\"",
            WorkingDirectory = Path.GetDirectoryName(executable) ?? AppContext.BaseDirectory,
            UseShellExecute = true,
        });

        return Task.FromResult(new SetupSelectionResult
        {
            Success = true,
            Message = workCenter.Length == 0
                ? "Previewing the generic request flow."
                : $"Previewing the generic request flow for {workCenter}.",
        });
    }

    /// <summary>
    /// The newest preview build under the repository that holds this application's project, or null.
    /// </summary>
    /// <remarks>
    /// Only a development machine has this layout. Walking up rather than counting folders means a move of the
    /// build output, or a different configuration or platform, does not silently stop the preview from being found.
    /// </remarks>
    private static string? ResolveFromRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var depth = 0; depth < RepositorySearchDepth && directory is not null; depth++)
        {
            if (File.Exists(Path.Combine(directory.FullName, ApplicationProjectFileName)))
            {
                return NewestPreviewBuild(Path.Combine(directory.FullName, PreviewProjectFolder));
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>The newest preview executable under one project folder's build output, or null.</summary>
    private static string? NewestPreviewBuild(string projectFolder)
    {
        var output = Path.Combine(projectFolder, "bin");

        if (!Directory.Exists(output))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(output, PreviewExecutableName, SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
