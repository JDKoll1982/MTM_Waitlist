using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// Nothing is kept on a computer except what it needs to reach the store and the local copy of the pictures
/// (FR-025, FR-042; SC-016).
/// </summary>
/// <remarks>
/// <para>
/// This is asserted by reading the tree rather than by running the application, because the failure mode is a
/// writer that only fires on a fault: a fault that ends the process on a machine nobody is watching is exactly
/// when a stray local file is created and never noticed. The check is therefore on the call sites that could do
/// it, not on a file that happens to exist today.
/// </para>
/// <para>
/// <b>The one legitimate writer is named.</b> The picture cache owns a folder under the application's local data
/// and is allowed to, because FR-042 makes the local copy of the pictures part of what a computer may keep. Every
/// other writer under that root is the defect this class exists to catch.
/// </para>
/// </remarks>
[TestClass]
public sealed class LocalStateFreeTests
{
    /// <summary>The one file allowed to write under the application's own local folder.</summary>
    private const string PictureCacheAuthority = "ImageCachePaths.cs";

    /// <summary>Ways a file gets written, which is what this check looks for.</summary>
    private static readonly Regex s_fileWriter = new(
        @"File\.(WriteAllText|WriteAllLines|AppendAllText|AppendAllLines|Create|OpenWrite)|new\s+StreamWriter|Directory\.CreateDirectory",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string Root => RepositoryPatternScan.FindRepositoryRoot();

    /// <summary>
    /// No production file writes to the application's own local folder except the picture cache. A file kept on a
    /// computer is what FR-025 removes, and a fault written to one is what SC-016 forbids outright.
    /// </summary>
    [TestMethod]
    public void Service_LocalStateFreeTests_NothingButThePictureCache_WritesUnderTheApplicationsLocalFolder()
    {
        var offenders = new List<string>();

        foreach (var file in ProductionFiles())
        {
            var text = File.ReadAllText(file);

            if (!text.Contains("LocalApplicationData", StringComparison.Ordinal))
            {
                continue;
            }

            var name = Path.GetFileName(file);

            if (name == PictureCacheAuthority)
            {
                continue;
            }

            if (s_fileWriter.IsMatch(text))
            {
                offenders.Add(Relative(file));
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "These files write under the application's own local folder, so something is kept on the machine that is not the picture copy (FR-025, SC-016):"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>
    /// The application's unhandled-fault routes end at the store rather than at a file: each raises its diagnostic
    /// through the seam, and the only wait on the way out is the seam's own bounded flush (FR-021, FR-025,
    /// FR-037).
    /// </summary>
    /// <remarks>
    /// A fault record written to the machine's disk is the specific defect this guards. It was once done that way,
    /// to name a crash whose cause was unknown, and it was wrong even then: it kept a file on the machine
    /// (FR-025), and it wrote the exception unredacted, so a connection string or a statement's parameter value
    /// would have reached the disk where the seam would have dropped it (FR-036).
    /// </remarks>
    [TestMethod]
    public void Service_LocalStateFreeTests_TheUnhandledFaultRoutes_RecordToTheStoreAndWriteNoFile()
    {
        var app = File.ReadAllText(Path.Combine(Root, "App.xaml.cs"));

        foreach (var handler in new[] { "App_UnhandledException", "CurrentDomain_UnhandledException" })
        {
            // The declaration, not the first mention: each handler is named in the other's documentation.
            var start = app.IndexOf($"void {handler}(", StringComparison.Ordinal);

            Assert.IsTrue(start >= 0, $"{handler} is gone, so a fault that ends the process would leave no account of itself.");

            var body = app[start..];

            var end = body.IndexOf("\n    private", StringComparison.Ordinal);

            body = end > 0 ? body[..end] : body;

            StringAssert.Contains(
                body,
                "AppLog.Error",
                $"{handler} no longer records the fault through the logging seam, so the store would hold no account of it (FR-021).");

            Assert.IsFalse(
                s_fileWriter.IsMatch(body),
                $"{handler} writes a file, so the fault is kept on the machine as well as in the store (FR-025, SC-016).");
        }
    }

    /// <summary>
    /// The flush the fault routes use is bounded, so a store that will not answer cannot hold a process open
    /// against FR-003.
    /// </summary>
    [TestMethod]
    public void Service_LocalStateFreeTests_TheFaultRoutesWait_IsBounded()
    {
        var app = File.ReadAllText(Path.Combine(Root, "App.xaml.cs"));

        var start = app.IndexOf("private static void TryFlushLogStore", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, "The bounded flush the fault routes rely on is gone.");

        var body = app[start..];
        var end = body.IndexOf("\n    ///", StringComparison.Ordinal);

        body = end > 0 ? body[..end] : body;

        StringAssert.Contains(
            body,
            "TimeSpan.FromSeconds",
            "The flush that runs while the process is ending is no longer bounded, so a store that will not answer could hold it open (FR-003).");
    }

    private static IEnumerable<string> ProductionFiles()
    {
        string[] projectRoots =
        [
            Root,
            Path.Combine(Root, "MTM_Waitlist.Startup"),
            Path.Combine(Root, "MTM_Waitlist.Logging"),
            Path.Combine(Root, "MTM_Waitlist.Core"),
        ];

        foreach (var root in projectRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}MTM_Waitlist.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    private static string Relative(string path) => path
        .Replace(Root + Path.DirectorySeparatorChar, string.Empty, StringComparison.Ordinal);
}
