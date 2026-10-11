using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// FR-030: no launch path may trigger a refresh of the cached plant data. Refreshing the mirror belongs to
/// <c>MTM_Waitlist.Mock.Service</c> alone; the application only reads and probes.
/// </summary>
/// <remarks>
/// The guard is a source scan rather than a behaviour check, because what must never exist is the call itself:
/// a launch that happened not to refresh today would still be one edit away from doing so. The scan looks for
/// the refresh client and for the refresh procedures by name in every file the launch is built from.
/// </remarks>
[TestClass]
public sealed class LaunchNoRefreshTests
{
    private static readonly string[] ForbiddenMarkers =
    [
        "IMockServiceRefreshClient",
        "MockServiceRefreshClient",
        "sp_visual_",
        "_refresh",
    ];

    [TestMethod]
    public void Service_LaunchNoRefreshTests_NoLaunchSource_NamesARefreshClientOrARefreshProcedure()
    {
        var root = RepoRoot();
        var scanned = 0;

        foreach (var folder in new[] { "MTM_Waitlist.Startup", "Module_Startup" })
        {
            var directory = Path.Combine(root, folder);

            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                scanned++;

                var text = File.ReadAllText(file);

                foreach (var marker in ForbiddenMarkers)
                {
                    Assert.IsFalse(
                        text.Contains(marker, StringComparison.Ordinal),
                        $"{Path.GetFileName(file)} names '{marker}', and no launch path may refresh the cached "
                        + "plant data (FR-030)");
                }
            }
        }

        Assert.IsTrue(scanned > 10, "the scan found the launch's sources, so an empty pass is not a false pass");
    }

    [TestMethod]
    public void Service_LaunchNoRefreshTests_TheApplicationNeverRefreshesTheMirrorItself()
    {
        // The only refresh client in the tree belongs to the settings surface, where a person asks for one; the
        // launch module must not resolve it, and the composition root must not hand it to a launch service.
        var root = RepoRoot();
        var startupRegistration = File.ReadAllText(Path.Combine(
            root,
            "MTM_Waitlist.Startup",
            "Services",
            "DependencyInjection",
            "ModuleDependencyInjectionExtensions.cs"));

        Assert.IsFalse(startupRegistration.Contains("RefreshClient", StringComparison.Ordinal));
        Assert.IsFalse(startupRegistration.Contains("RefreshAsync", StringComparison.Ordinal));
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "the repository root could not be found from the test output directory");

        return directory!.FullName;
    }
}
