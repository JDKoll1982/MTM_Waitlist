using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Mock.Services;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// The shipping gate for the read shapes' content: every script a shape loads at run time must be copied
/// beside the binary that loads it (`contracts/mock-service-configuration.md`; FR-016/FR-020).
/// </summary>
/// <remarks>
/// <para>
/// <b>This guard exists because a missing script is silent and reads as a plant-system outage.</b> The live
/// read loads its script from the application's base directory, and a script that was never copied there makes
/// the executor answer "not found" — which the identifier stage reports to the person as "the plant system
/// cannot be reached". Feature 011 added shape 6 under a new module folder
/// (<c>Database/InforVisual/Queues/Module_Startup</c>) and copied it in the test and service projects but not
/// in the desktop app's, so set-up told a person the plant system was unreachable while Infor Visual was
/// answering normally. Nothing else in the suite could see it: every parity test reads the script from the
/// repository, which always has it.
/// </para>
/// <para>
/// <b>Coverage is read, not enumerated.</b> Each project is asked whether any of its copied
/// <c>Content</c> patterns covers the folder a shape loads from, so an explicit per-folder entry and a
/// whole-tree wildcard both satisfy it, and a new module folder needs no edit here.
/// </para>
/// </remarks>
[TestClass]
public sealed class ReadShapeContentShippingAuditTests
{
    /// <summary>The binaries that load read scripts, and the script kinds each one loads.</summary>
    private static readonly (string ProjectFile, bool LoadsPopulationScripts)[] s_shipTargets =
    [
        // The desktop app performs live reads only; the mirror answers through its own stored procedures.
        ("MTM_Waitlist.csproj", false),

        // The service performs the live read and runs the population script for every shape it refreshes.
        (Path.Combine("MTM_Waitlist.Mock.Service", "MTM_Waitlist.Mock.Service.csproj"), true),
    ];

    /// <summary>Matches one <c>Content</c> item, with its attributes and its element body when it has one.</summary>
    private static readonly Regex s_contentItem = new(
        @"<Content\b(?<attributes>[^>]*?)(?:/>|>(?<body>[\s\S]*?)</Content>)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Captures the <c>Include</c> value of a <c>Content</c> item.</summary>
    private static readonly Regex s_includeAttribute = new(
        @"Include\s*=\s*""(?<path>[^""]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void EveryBinaryThatLoadsReadScripts_CopiesEveryShapeScriptItLoads()
    {
        var repositoryRoot = RepositoryPatternScan.FindRepositoryRoot();
        var shapes = VisualReadShapeCatalog.Create();
        var violations = new List<string>();

        foreach (var (projectFile, loadsPopulationScripts) in s_shipTargets)
        {
            var projectPath = Path.Combine(repositoryRoot, projectFile);
            Assert.IsTrue(File.Exists(projectPath), $"'{projectFile}' was not found in the repository.");

            var projectDirectory = (Path.GetDirectoryName(projectFile) ?? string.Empty).Replace('\\', '/');
            var copiedPatterns = CopiedContentPatterns(File.ReadAllText(projectPath), projectDirectory);

            foreach (var shape in shapes)
            {
                ReportUnshipped(projectFile, shape.Key, shape.SourceScriptRelativePath, copiedPatterns, violations);

                if (!loadsPopulationScripts)
                {
                    continue;
                }

                if (shape.PopulationScriptRelativePath is not { Length: > 0 } populationScript)
                {
                    violations.Add(
                        $"{projectFile} refreshes shape '{shape.Key}', which declares no population script to ship.");
                    continue;
                }

                ReportUnshipped(projectFile, shape.Key, populationScript, copiedPatterns, violations);
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "A read script that is not copied beside the binary that loads it fails at run time as 'script not "
            + "found', which the caller reports as the plant system being unreachable."
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Self-test. A pattern set that covers only another module's queries must not satisfy a script outside
    /// it, or the gate above would pass through its own blind spot rather than through the project.
    /// </summary>
    [TestMethod]
    public void IsShipped_WithAPatternThatCoversAnotherModulesQueries_AnswersFalse()
    {
        string[] moduleSetupOnly = ["Database/InforVisual/Queues/Module_Setup/**/*.sql"];

        Assert.IsFalse(IsShipped(moduleSetupOnly, "Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql"));
        Assert.IsTrue(IsShipped(moduleSetupOnly, "Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql"));
        Assert.IsTrue(IsShipped(["Database/InforVisual/Queues/**/*.sql"], "Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql"));
    }

    /// <summary>Records the violation when the project copies nothing that covers the script's folder.</summary>
    private static void ReportUnshipped(
        string projectFile,
        string shapeKey,
        string scriptRelativePath,
        IReadOnlyList<string> copiedPatterns,
        List<string> violations)
    {
        if (!IsShipped(copiedPatterns, scriptRelativePath))
        {
            violations.Add(
                $"{projectFile} copies nothing covering '{scriptRelativePath}' (shape '{shapeKey}'), so that read "
                + "fails as 'script not found'. Add a Content include for its folder.");
        }
    }

    /// <summary>
    /// Whether any copied pattern covers the folder the script is loaded from.
    /// </summary>
    /// <param name="copiedContentPatterns">The project's copied <c>Content</c> include patterns, slash-separated.</param>
    /// <param name="scriptRelativePath">The script's path from the catalog, slash-separated.</param>
    private static bool IsShipped(IReadOnlyList<string> copiedContentPatterns, string scriptRelativePath)
    {
        var directory = scriptRelativePath[..scriptRelativePath.LastIndexOf('/')];

        foreach (var pattern in copiedContentPatterns)
        {
            var fixedPrefix = FixedPrefix(pattern);

            if (fixedPrefix.Length > 0
                && (string.Equals(directory, fixedPrefix, StringComparison.OrdinalIgnoreCase)
                    || directory.StartsWith(fixedPrefix + "/", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The part of a glob that names real folders: everything before the first wildcard segment. An include
    /// of <c>Database/**/*.sql</c> therefore covers every shape, and one of
    /// <c>Database/InforVisual/Queues/Module_Setup/**/*.sql</c> covers only that module's queries.
    /// </summary>
    private static string FixedPrefix(string includePattern)
    {
        var fixedSegments = new List<string>();

        foreach (var segment in includePattern.Split('/'))
        {
            if (segment.Contains('*') || segment.Contains('?'))
            {
                break;
            }

            fixedSegments.Add(segment);
        }

        return string.Join('/', fixedSegments);
    }

    /// <summary>
    /// The project's <c>Content</c> include patterns that are copied to the output directory, rewritten as
    /// repository-relative paths in the order the project file declares them.
    /// </summary>
    /// <param name="projectSource">The project file's text.</param>
    /// <param name="projectRepositoryRelativeDirectory">
    /// The project's own folder, repository-relative and slash-separated, which its includes are written against.
    /// </param>
    private static IReadOnlyList<string> CopiedContentPatterns(string projectSource, string projectRepositoryRelativeDirectory)
    {
        var patterns = new List<string>();

        foreach (Match item in s_contentItem.Matches(projectSource))
        {
            var attributes = item.Groups["attributes"].Value;
            var copies = attributes.Contains("CopyToOutputDirectory", StringComparison.Ordinal)
                || item.Groups["body"].Value.Contains("CopyToOutputDirectory", StringComparison.Ordinal);

            if (!copies)
            {
                continue;
            }

            var include = s_includeAttribute.Match(attributes);
            if (include.Success)
            {
                patterns.Add(ToRepositoryRelative(include.Groups["path"].Value, projectRepositoryRelativeDirectory));
            }
        }

        return patterns;
    }

    /// <summary>
    /// Resolves an include written against the project's own folder into a repository-relative pattern.
    /// </summary>
    /// <param name="includePattern">The <c>Include</c> value as the project file writes it.</param>
    /// <param name="projectRepositoryRelativeDirectory">The project's own folder.</param>
    /// <remarks>
    /// A project that is not at the repository root writes its includes as <c>..\Database\...</c>, so the
    /// leading parent hops have to be consumed before the pattern can be compared with a script's path.
    /// </remarks>
    private static string ToRepositoryRelative(string includePattern, string projectRepositoryRelativeDirectory)
    {
        var normalized = includePattern.Replace('\\', '/');

        if (normalized.StartsWith('/') || normalized.Contains(':'))
        {
            // Rooted, so it cannot be the repository-relative include a catalog script needs.
            return normalized;
        }

        var segments = new List<string>();
        if (projectRepositoryRelativeDirectory.Length > 0)
        {
            segments.AddRange(projectRepositoryRelativeDirectory.Split('/'));
        }

        foreach (var segment in normalized.Split('/'))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }
}
