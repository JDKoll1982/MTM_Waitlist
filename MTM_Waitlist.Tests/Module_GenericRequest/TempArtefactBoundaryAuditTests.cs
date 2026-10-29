using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The boundary that keeps the preview out of the product: no `Temp`-named artefact may exist inside the shipping
/// application or anywhere in its reference graph, and the preview application may not be referenced from it
/// (015-generic-wc-ui-shell, FR-019, FR-021, FR-023, FR-024, FR-025, FR-026).
/// </summary>
/// <remarks>
/// <para>
/// The check reads the project graph from the application's own project file and follows every project reference
/// transitively, so a library that gains a `Temp` type later is caught even though nobody edited the application.
/// It looks for declaration shapes rather than for the three letters, so an unrelated constant such as
/// <c>TempFolderName</c> is not an offence while a type, namespace, resource key, file, folder or assembly named
/// with the convention is.
/// </para>
/// <para>
/// Nothing here relies on the identifiers the retired-symbol audit forbids. The temporary material is identified by
/// its `Temp` naming alone, which is why one search finds all of it.
/// </para>
/// </remarks>
[TestClass]
public sealed class TempArtefactBoundaryAuditTests
{
    private const string ApplicationProject = "MTM_Waitlist.csproj";

    private const string PreviewProject = "TempGenericRequest.Preview";

    /// <summary>
    /// Artefacts allowed to carry the naming even though they are outside the preview application. Each entry is an
    /// exact repository path with the reason it is allowed, so an exemption can never widen by pattern.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> s_allowedOutsideThePreview = new Dictionary<string, string>();

    private static readonly Regex s_declared = new(
        @"\b(class|interface|record|struct|enum|namespace)\s+(?<name>Temp[A-Z]\w*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_namedSegment = new(
        @"\b(?:using|namespace)\s+(?<name>[\w.]*\.Temp)\s*[;{]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_resourceKey = new(
        @"<data\s+name=""(?<name>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex s_assemblyName = new(
        @"(?:Include|AssemblyName|RootNamespace)\s*=\s*""(?<name>Temp[\w.]*)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Lazy<string> s_root = new(RepositoryPatternScan.FindRepositoryRoot);

    private static readonly Lazy<IReadOnlyList<string>> s_projectDirectories = new(() =>
        Directory
            .EnumerateFiles(s_root.Value, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(s_root.Value, file))
            .Select(file => Path.GetDirectoryName(file)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());

    [TestMethod]
    public void Audit_TempArtefactBoundaryAuditTests_TheShippedApplicationCarriesNoTempArtefact()
    {
        var root = s_root.Value;
        var offenders = new List<string>();

        foreach (var project in ShippedProjects(root))
        {
            foreach (var file in SourceFiles(root, project))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');

                if (s_allowedOutsideThePreview.ContainsKey(relative))
                {
                    continue;
                }

                offenders.AddRange(OffencesIn(relative, File.ReadAllText(file)));
            }

            foreach (var name in NamedArtefacts(root, project))
            {
                var relative = Path.GetRelativePath(root, name).Replace('\\', '/');

                if (s_allowedOutsideThePreview.ContainsKey(relative))
                {
                    continue;
                }

                offenders.Add($"{relative}: a file or folder named with the convention");
            }
        }

        Assert.AreEqual(
            0,
            offenders.Count,
            "temporary material reached the shipped application or its reference graph: " + string.Join("; ", offenders));
    }

    [TestMethod]
    public void Audit_TempArtefactBoundaryAuditTests_TheShippedApplicationDoesNotReferenceThePreview()
    {
        var project = Path.Combine(s_root.Value, ApplicationProject);

        var references = XDocument.Load(project)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToList();

        Assert.IsFalse(
            references.Any(reference => reference.Contains(PreviewProject, StringComparison.OrdinalIgnoreCase)),
            $"the application references {PreviewProject}, so it would build and run with the preview attached");

        Assert.IsFalse(
            ShippedProjects(s_root.Value).Any(project => Path.GetFileNameWithoutExtension(project)
                .Equals(PreviewProject, StringComparison.OrdinalIgnoreCase)),
            $"{PreviewProject} is reachable from the application through a project reference");
    }

    [TestMethod]
    public void Audit_TempArtefactBoundaryAuditTests_TheCheckNamesAnOffence()
    {
        var offences = OffencesIn("Whatever/Offender.cs", "internal sealed class TempOffendingSource { }");

        Assert.AreEqual(1, offences.Count, "the check did not notice a type named with the convention");
        StringAssert.Contains(offences[0], "TempOffendingSource", "the check did not name the offending type");

        var keyOffence = OffencesIn("Whatever/Resources.resw", "<data name=\"TempGenericRequest_Thing.Text\" xml:space=\"preserve\" />");

        Assert.AreEqual(1, keyOffence.Count, "the check did not notice a resource key named with the convention");
        StringAssert.Contains(keyOffence[0], "TempGenericRequest_Thing", "the check did not name the offending key");

        Assert.AreEqual(
            0,
            OffencesIn("Whatever/Harmless.cs", "public const string TempFolderName = \"Temp\";").Count,
            "the check flagged an unrelated constant, which would make the rule unusable");
    }

    /// <summary>What a file's text does that crosses the boundary.</summary>
    private static IReadOnlyList<string> OffencesIn(string relativePath, string text)
    {
        var offences = new List<string>();

        foreach (var pattern in new[] { s_declared, s_namedSegment, s_assemblyName })
        {
            foreach (Match match in pattern.Matches(text))
            {
                offences.Add($"{relativePath}: '{match.Groups["name"].Value}'");
            }
        }

        foreach (Match match in s_resourceKey.Matches(text))
        {
            var key = match.Groups["name"].Value;

            if (key.Split('.').Any(segment => segment.StartsWith("Temp", StringComparison.Ordinal)))
            {
                offences.Add($"{relativePath}: resource key '{key}'");
            }
        }

        return offences;
    }

    /// <summary>The application and every project it reaches through project references.</summary>
    private static IReadOnlyList<string> ShippedProjects(string root)
    {
        var found = new List<string>();
        var pending = new Queue<string>();
        pending.Enqueue(Path.Combine(root, ApplicationProject));

        while (pending.Count > 0)
        {
            var project = pending.Dequeue();

            if (found.Contains(project, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            found.Add(project);

            foreach (var reference in ReferencedProjects(project))
            {
                pending.Enqueue(reference);
            }
        }

        return found;
    }

    /// <summary>The project files a project file references, resolved to absolute paths.</summary>
    private static IEnumerable<string> ReferencedProjects(string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;

        return XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(directory, include!.Replace('\\', Path.DirectorySeparatorChar))))
            .Where(File.Exists);
    }

    /// <summary>Every source file a project's folder holds, outside its build output and other projects.</summary>
    private static IEnumerable<string> SourceFiles(string root, string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;

        return Directory
            .EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".resw", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(file => !IsBuildOutput(root, file))
            .Where(file => !BelongsToAnotherProject(root, directory, file));
    }

    /// <summary>Files and folders a project's folder holds whose own name carries the convention.</summary>
    private static IEnumerable<string> NamedArtefacts(string root, string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;

        return Directory
            .EnumerateFileSystemEntries(directory, "*", SearchOption.AllDirectories)
            .Where(entry => Path.GetFileName(entry).StartsWith("Temp", StringComparison.Ordinal))
            .Where(entry => !IsBuildOutput(root, entry))
            .Where(entry => !BelongsToAnotherProject(root, directory, entry));
    }

    private static bool IsBuildOutput(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');

        return relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
            || relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Whether a path under <paramref name="projectDirectory"/> actually belongs to a different project.
    /// </summary>
    /// <remarks>
    /// The application's project file sits at the repository root, so its directory is the whole tree. Without this
    /// filter the application would appear to contain every other project in the solution, which is how a check like
    /// this reports a boundary breach that is not one.
    /// </remarks>
    private static bool BelongsToAnotherProject(string root, string projectDirectory, string path) =>
        s_projectDirectories.Value
            .Where(other => !string.Equals(other, projectDirectory, StringComparison.OrdinalIgnoreCase))
            .Any(other => string.Equals(path, other, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(other + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
}
