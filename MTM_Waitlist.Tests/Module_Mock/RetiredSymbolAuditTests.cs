using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// SC-013's retired-symbol gate: fails if any retired sample/demo type, setting key, or database object is
/// still present or referenced in code or database artifacts.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scope.</b> This audit scans production and test source (`.cs`, `.xaml`, `.resw`, `.csproj`, `.json`)
/// and live database artifacts (`.sql`). Design documents under <c>specs/</c> and the workstream notes under
/// <c>WeekendProject/</c> legitimately record the retirement and are therefore excluded here; the
/// documentation sweep is tracked by its own tasks (T102, T122, T125).
/// </para>
/// <para>
/// <b>Two deliberate SQL exemptions.</b> A retired object's <c>rollback.sql</c> is the drop artifact the
/// retirement tasks explicitly retained, and <c>Database/Bootstrap/update_table_descriptions.sql</c>
/// carries the recorded "Retired objects" section. Both must name the retired objects, so neither is a
/// stale reference — they are the record <i>of</i> the removal.
/// </para>
/// </remarks>
[TestClass]
public sealed class RetiredSymbolAuditTests
{
    /// <summary>The retired type, key, and object names, as regular expressions.</summary>
    private static readonly (string Description, Regex Pattern)[] s_retiredSymbols =
    [
        ("sample-data contract", new Regex(@"\bISampleDataService\b", RegexOptions.Compiled)),
        ("sample-data service", new Regex(@"\bSampleDataService\b", RegexOptions.Compiled)),
        ("sample catalog", new Regex(@"\bSample\w*Catalog\b", RegexOptions.Compiled)),
        ("mock-toggle setting key", new Regex(@"Feature\.(InforVisualMockData|RecvMockData)", RegexOptions.Compiled)),
        ("mock-toggle service", new Regex(@"\bI?MockToggleService\b", RegexOptions.Compiled)),
        ("mock routing stack", new Regex(@"\bMockRouting\w*\b", RegexOptions.Compiled)),
        ("mock mode stack", new Regex(@"\bMockMode\w*\b", RegexOptions.Compiled)),
        ("mock configuration service", new Regex(@"\bMockConfigurationService\b", RegexOptions.Compiled)),
        ("mock master-data service", new Regex(@"\bI?MockMasterDataService\b", RegexOptions.Compiled)),
        ("settings mock control", new Regex(@"\b(UseMockData|MockDataExpander)\b", RegexOptions.Compiled)),
        ("retired mock procedure", new Regex(@"\bsp_mock_", RegexOptions.Compiled)),
    ];

    /// <summary>File extensions this audit treats as code or database artifacts.</summary>
    private static readonly string[] s_scannedExtensions = [".cs", ".xaml", ".resw", ".sql", ".csproj", ".json"];

    /// <summary>Directories whose files are documentation or build output, not live code.</summary>
    private static readonly string[] s_excludedDirectories =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}",
    ];

    /// <summary>
    /// Fails when any retired symbol survives in code or in a live database artifact.
    /// </summary>
    [TestMethod]
    public void NoRetiredSymbolRemainsInCodeOrDatabaseArtifacts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var violations = new List<string>();

        foreach (var file in EnumerateScannedFiles(repositoryRoot))
        {
            if (IsExemptFromSqlAudit(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(repositoryRoot, file);

            foreach (var (description, pattern) in s_retiredSymbols)
            {
                if (pattern.IsMatch(content))
                {
                    violations.Add($"{relativePath}: {description} ({pattern})");
                }
            }
        }

        Assert.AreEqual(
            0,
            violations.Count,
            "SC-013 requires no retired sample/demo symbol to survive in code or database artifacts:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Excludes the drop artifacts and the maintenance file that record the retirement itself.
    /// </summary>
    /// <param name="file">The candidate file.</param>
    private static bool IsExemptFromSqlAudit(string file)
    {
        if (!file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(file);

        // The retained rollback artifact IS the drop statement for a retired object.
        if (string.Equals(fileName, "rollback.sql", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // The mandatory maintenance file records what was retired and when.
        return string.Equals(fileName, "update_table_descriptions.sql", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateScannedFiles(string repositoryRoot)
    {
        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            if (!s_scannedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (s_excludedDirectories.Any(excluded => file.Contains(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // This audit necessarily names every retired symbol it forbids.
            if (string.Equals(Path.GetFileName(file), "RetiredSymbolAuditTests.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>
    /// Walks up from the test binaries until the solution file is found.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MTM_Waitlist.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        Assert.Fail($"The repository root could not be located above '{AppContext.BaseDirectory}'.");
        return AppContext.BaseDirectory;
    }
}
