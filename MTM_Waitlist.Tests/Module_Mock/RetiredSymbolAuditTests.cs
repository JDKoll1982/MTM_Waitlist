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
/// documentation sweep is tracked by its own tasks (T102, T122, T125). The CodeGraphy graph cache under
/// <c>.codegraphy/</c> is generated build output, like <c>bin/</c> and <c>obj/</c>: it holds a snapshot of
/// symbol names taken at index time and is rebuilt from the source, not written by hand.
/// </para>
/// <para>
/// <b>Two deliberate SQL exemptions.</b> A retired object's <c>rollback.sql</c> is the drop artifact the
/// retirement tasks explicitly retained, and <c>Database/Bootstrap/update_table_descriptions.sql</c>
/// carries the recorded "Retired objects" section. Both must name the retired objects, so neither is a
/// stale reference — they are the record <i>of</i> the removal.
/// </para>
/// <para>
/// <b>One generated-output exemption.</b> <c>tools/tooltip-inventory/</c> holds the artifacts
/// <c>tools/Generate-TooltipInventory.ps1</c> writes; it is gitignored, it is rebuilt from the XAML rather
/// than written by hand, and the generator already skips that folder when it scans. A snapshot taken before
/// a retirement therefore names symbols that no longer exist in source — a fact about the snapshot's age,
/// not about the code — so it is excluded here for the same reason <c>.codegraphy/</c> is.
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

        // ── feature 004-unified-card-item-picker (FR-023, SC-012): the request type and subtype vocabulary ──
        // Identifier-shaped on purpose. Honest retirement prose — "the type/subtype catalog retired with the
        // vocabulary" — must stay legal; a stray identifier, symbol or scope value must not.
        ("retired catalog table", new Regex(@"\bwaitlist_request_(types|subtypes)\b", RegexOptions.Compiled)),
        ("retired catalog read procedure", new Regex(@"\bsp_waitlist_request_(types|subtypes)_get\b", RegexOptions.Compiled)),
        ("retired catalog seed", new Regex(@"\bseed_waitlist_request_catalog\b", RegexOptions.Compiled)),
        ("retired type inventory", new Regex(@"\bRequest(Type|Subtype)Inventory\b", RegexOptions.Compiled)),
        ("retired display-label service", new Regex(@"\bI?Request(Type|Subtype)DisplayLabelService\b", RegexOptions.Compiled)),
        ("retired subtype name reader", new Regex(@"\bI?RequestSubtypeNameReadService\b", RegexOptions.Compiled)),
        ("retired legacy mapper", new Regex(@"\bRequestItemLegacyMapper\b", RegexOptions.Compiled)),
        ("retired image mapping model", new Regex(@"\bRequest(Type|Subtype)ImageMapping\b", RegexOptions.Compiled)),
        ("retired request-type picture dialog", new Regex(@"\bRequestTypeImagesDialog\w*\b", RegexOptions.Compiled)),
        ("retired subtype picture dialog", new Regex(@"\bRequestSubtypeImagesDialog\w*\b", RegexOptions.Compiled)),
        ("retired image scope value", new Regex(@"\brequest_(type|subtype)\b", RegexOptions.Compiled)),
        ("retired per-subtype local-settings key", new Regex(@"\bMaxAllottedMinutes\b", RegexOptions.Compiled)),
        ("retired user-visible picture wording", new Regex(@"Request Type & Subtype Images|Request Subtype Images|Manage subtypes|subtype images", RegexOptions.Compiled)),

        // ── feature 008-part-pictures (FR-014, FR-015, SC-002): the component card's stand-in claim ──
        // The component card used to say its placeholder stood in "until part pictures exist", which stopped being
        // true the moment every part could carry a picture of its own. A surface that says it has no picture to draw
        // is exactly what FR-014 forbids, so the wording must not come back in code, markup or a resource string.
        ("retired part-picture stand-in wording", new Regex(@"until part pictures exist", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    /// <summary>File extensions this audit treats as code or database artifacts.</summary>
    private static readonly string[] s_scannedExtensions = [".cs", ".xaml", ".resw", ".sql", ".csproj", ".json"];

    /// <summary>Directories whose files are documentation or build output, not live code.</summary>
    private static readonly string[] s_excludedDirectories =
    [
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.codegraphy{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}WeekendProject{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.github{Path.DirectorySeparatorChar}",

        // Generated tooltip-inventory output: gitignored, rebuilt from the XAML, and already skipped by its
        // own generator. See the remarks above.
        $"{Path.DirectorySeparatorChar}tools{Path.DirectorySeparatorChar}tooltip-inventory{Path.DirectorySeparatorChar}",
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
    private static string FindRepositoryRoot() => RepositoryPatternScan.FindRepositoryRoot();

    /// <summary>
    /// T003 self-test. An empty pattern set must find nothing: every later use of the helper then proves
    /// something through its pattern set rather than accidentally through the scanner.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithAnEmptyPatternSet_ReturnsNoHits()
    {
        var hits = RepositoryPatternScan.Scan(Array.Empty<(string Description, string Pattern)>());

        Assert.AreEqual(0, hits.Count, RepositoryPatternScan.Describe(hits));
    }

    /// <summary>
    /// The mirror of the scan, and the reason each retirement guard can be trusted: every pattern must bite on a
    /// representative re-introduction of the symbol it forbids.
    /// </summary>
    /// <remarks>
    /// A pattern that has quietly stopped matching — a widened escape, a renamed symbol, a mistyped group — is a
    /// guard that reports clean forever, which is worse than no guard at all. The sample set is required to cover
    /// every declared pattern, so adding a pattern without proving it bites fails the build.
    /// </remarks>
    [TestMethod]
    public void EveryRetiredPattern_BitesOnAReintroductionOfItsSymbol()
    {
        var samples = ReintroductionSamples();

        var unsampled = s_retiredSymbols
            .Select(entry => entry.Description)
            .Where(description => !samples.ContainsKey(description))
            .ToList();

        Assert.AreEqual(
            0,
            unsampled.Count,
            "These retired symbols have no re-introduction sample, so nothing proves their pattern still bites: "
                + string.Join(", ", unsampled));

        var blind = new List<string>();
        foreach (var (description, pattern) in s_retiredSymbols)
        {
            if (!pattern.IsMatch(samples[description]))
            {
                blind.Add($"{description} ({pattern}) does not match its own re-introduction: {samples[description]}");
            }
        }

        Assert.AreEqual(
            0,
            blind.Count,
            "A retired-symbol pattern that no longer matches its own symbol is a guard that can never fail:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, blind));
    }

    /// <summary>
    /// One representative re-introduction per retired symbol: the smallest piece of text that should make the
    /// scan fail if it were ever put back into the repository.
    /// </summary>
    private static Dictionary<string, string> ReintroductionSamples() => new(StringComparer.Ordinal)
    {
        ["sample-data contract"] = "public interface ISampleDataService { }",
        ["sample-data service"] = "public sealed class SampleDataService { }",
        ["sample catalog"] = "public static class SampleOrderCatalog { }",
        ["mock-toggle setting key"] = "\"Feature.InforVisualMockData\": true",
        ["mock-toggle service"] = "public interface IMockToggleService { }",
        ["mock routing stack"] = "public sealed class MockRoutingResolver { }",
        ["mock mode stack"] = "public enum MockModeState { }",
        ["mock configuration service"] = "public sealed class MockConfigurationService { }",
        ["mock master-data service"] = "public interface IMockMasterDataService { }",
        ["settings mock control"] = "<ToggleSwitch x:Name=\"UseMockData\" />",
        ["retired mock procedure"] = "\"sp_mock_parts_get\"",
        ["retired catalog table"] = "CREATE TABLE waitlist_request_types (",
        ["retired catalog read procedure"] = "CREATE PROCEDURE sp_waitlist_request_subtypes_get()",
        ["retired catalog seed"] = "-- Seed: seed_waitlist_request_catalog",
        ["retired type inventory"] = "RequestSubtypeInventory.Groups",
        ["retired display-label service"] = "public interface IRequestTypeDisplayLabelService { }",
        ["retired subtype name reader"] = "public interface IRequestSubtypeNameReadService { }",
        ["retired legacy mapper"] = "RequestItemLegacyMapper.Map(\"Forklift Assist\", null)",
        ["retired image mapping model"] = "public sealed class RequestSubtypeImageMapping { }",
        ["retired request-type picture dialog"] = "new RequestTypeImagesDialogViewModel(",
        ["retired subtype picture dialog"] = "new RequestSubtypeImagesDialogViewModel(",
        ["retired image scope value"] = "<value>request_subtype</value>",
        ["retired per-subtype local-settings key"] = "SaveSettingAsync(\"Urgency.MaxAllottedMinutes.Pickup Coil\", 45)",
        ["retired user-visible picture wording"] = "<value>Manage subtypes</value>",
        ["retired part-picture stand-in wording"] = "The no-image picture stands in until part pictures exist.",
    };

    /// <summary>
    /// T026's placeholder-resource gate: no shipped resource <i>value</i> is a placeholder address, a sample
    /// organisation, filler text, or a developer marker.
    /// </summary>
    /// <remarks>
    /// Only <c>&lt;value&gt;</c> contents are scanned. Key names are deliberately not: the WinUI convention
    /// names many legitimate keys with the word "Placeholder" (a TextBox's watermark, for instance), so
    /// scanning names would fail on correct markup.
    /// </remarks>
    [TestMethod]
    public void NoPlaceholderResourceValueShips()
    {
        (string Description, Regex Pattern)[] patterns =
        [
            ("privacy placeholder address", new Regex(@"YourPrivacyUrlGoesHere", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("example domain", new Regex(@"\bexample\.(com|org|net)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("sample organisation", new Regex(@"\bContoso\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("filler text", new Regex(@"\blorem ipsum\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
            ("developer marker", new Regex(@"\bTODO\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ];

        var violations = new List<string>();
        var valueCount = 0;

        foreach (var (fileName, value) in EnumerateResourceValues())
        {
            valueCount++;

            foreach (var (description, pattern) in patterns)
            {
                if (pattern.IsMatch(value))
                {
                    violations.Add($"{fileName}: {description} [{pattern}] -> {value.Trim()}");
                }
            }
        }

        Assert.IsTrue(
            valueCount > 100,
            $"The scan reached only {valueCount} resource values, so a clean result would prove nothing.");

        Assert.AreEqual(
            0,
            violations.Count,
            "No shipped resource value may be a placeholder:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// T036's retired-wording gate, and the standing check for FR-030: no demo, sample or mock mode may
    /// return, and no shipped text may present one as current.
    /// </summary>
    [TestMethod]
    public void NoRetiredWordingSurvives()
    {
        (string Description, Regex Pattern)[] retiredWording =
        [
            ("retired mock status key", new Regex(@"\bMockSaved\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("retired receiving mock toggle key", new Regex(@"\bRecvMockData\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
            ("retired Infor Visual mock toggle key", new Regex(@"\bInforVisualMockData\b", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ];

        var hits = RepositoryPatternScan.Scan(retiredWording);

        Assert.AreEqual(
            0,
            hits.Count,
            "No retired mock/sample wording may survive (FR-025/FR-029/FR-030):"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));

        // In a shipped resource value these words read as a product statement rather than as a comment saying
        // the mechanism is gone, so resource values are checked explicitly. Comments that state the absence
        // ("never replaced by sample rows") are the record of the removal, not a stale claim, which is why the
        // narrower scan is applied to values only.
        var sampleWording = new Regex(@"\bsample (data|rows)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var offenders = new List<string>();
        var valueCount = 0;

        foreach (var (fileName, value) in EnumerateResourceValues())
        {
            valueCount++;

            if (sampleWording.IsMatch(value))
            {
                offenders.Add($"{fileName} -> {value.Trim()}");
            }
        }

        Assert.IsTrue(
            valueCount > 100,
            $"The scan reached only {valueCount} resource values, so a clean result would prove nothing.");

        Assert.AreEqual(
            0,
            offenders.Count,
            "No shipped resource value may describe sample data or sample rows:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
    }

    /// <summary>Every <c>&lt;value&gt;</c> shipped in a resource file, with the file it came from.</summary>
    private static IEnumerable<(string FileName, string Value)> EnumerateResourceValues()
    {
        var scope = new RepositoryScanScope { Extensions = [".resw"] };

        foreach (var file in RepositoryPatternScan.EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), scope))
        {
            foreach (var value in System.Xml.Linq.XDocument.Load(file).Descendants("value"))
            {
                yield return (Path.GetFileName(file), value.Value);
            }
        }
    }

    /// <summary>
    /// T003 self-test. The default scope must actually reach production source, so that a clean scan is
    /// evidence of compliance rather than of a silently empty enumeration — the failure mode that makes a
    /// scan check vacuous.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithTheDefaultScope_EnumeratesProductionSource()
    {
        var files = RepositoryPatternScan
            .EnumerateScannedFiles(RepositoryPatternScan.FindRepositoryRoot(), RepositoryScanScope.Default)
            .ToList();

        Assert.IsTrue(files.Count > 100, $"The default scan scope reached only {files.Count} files.");

        Assert.IsTrue(
            files.Any(file => file.EndsWith("WaitlistViewViewModel.cs", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope did not reach MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs.");

        Assert.IsTrue(
            files.Any(file => file.EndsWith("SettingsPage.xaml", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope did not reach Module_Settings/Views/SettingsPage.xaml.");

        Assert.IsFalse(
            files.Any(file => file.Contains($"{Path.DirectorySeparatorChar}specs{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || file.Contains($"{Path.DirectorySeparatorChar}defects{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)),
            "The default scan scope must exclude specs/ and defects/, which quote the removed values as evidence.");
    }

    /// <summary>
    /// T003 self-test. A literal that genuinely exists must be found, so a clean result from a real pattern
    /// set cannot come from a matcher that never matches.
    /// </summary>
    [TestMethod]
    public void PatternScan_WithAKnownPresentLiteral_FindsIt()
    {
        var hits = RepositoryPatternScan.Scan([("waitlist field population", new Regex(@"AddRequestFields", RegexOptions.Compiled))]);

        Assert.IsTrue(hits.Count > 0, "The scan helper failed to find a literal that is present in the repository.");
        Assert.IsTrue(hits.All(hit => hit.LineNumber > 0), "Every hit must report the line it was found on.");
    }
}

/// <summary>One pattern match found by <see cref="RepositoryPatternScan"/>.</summary>
/// <param name="RelativePath">The file, relative to the repository root.</param>
/// <param name="LineNumber">The 1-based line the match was found on.</param>
/// <param name="Description">What the pattern is looking for.</param>
/// <param name="Pattern">The pattern itself, so the failure message is self-explanatory.</param>
/// <param name="Text">The matching line, trimmed.</param>
internal sealed record RepositoryScanHit(string RelativePath, int LineNumber, string Description, string Pattern, string Text);

/// <summary>
/// Scoping rules for <see cref="RepositoryPatternScan"/>. Every caller states the scope it means rather
/// than inheriting another caller's, and <see cref="Default"/> is the repository-wide production scope.
/// </summary>
internal sealed class RepositoryScanScope
{
    /// <summary>
    /// Repository-wide over production source: the build-output, VCS and design-document directories are
    /// skipped, and <c>specs/</c> and <c>defects/</c> are excluded deliberately because those files quote
    /// the removed values as evidence of the removal.
    /// </summary>
    internal static RepositoryScanScope Default { get; } = new();

    /// <summary>Directory names that are never scanned, at any depth.</summary>
    internal string[] ExcludedDirectories { get; init; } = RepositoryPatternScan.DefaultExcludedDirectories;

    /// <summary>File extensions that are scanned.</summary>
    internal string[] Extensions { get; init; } = RepositoryPatternScan.DefaultScannedExtensions;

    /// <summary>When non-empty, only files under one of these repository-relative paths are scanned.</summary>
    internal string[] IncludeUnder { get; init; } = [];

    /// <summary>
    /// File names skipped even when in scope. The defaults are the two files that must name every pattern
    /// they forbid: this audit, and the fabricated-value guard.
    /// </summary>
    internal string[] ExcludedFileNames { get; init; } = RepositoryPatternScan.DefaultExcludedFileNames;

    /// <summary>Returns a copy of this scope with additional file names excluded.</summary>
    /// <param name="fileNames">The additional file names to skip.</param>
    internal RepositoryScanScope ExcludingFiles(params string[] fileNames)
        => new()
        {
            ExcludedDirectories = ExcludedDirectories,
            Extensions = Extensions,
            IncludeUnder = IncludeUnder,
            ExcludedFileNames = [.. ExcludedFileNames, .. fileNames],
        };
}

/// <summary>
/// T003's pattern-set scan: one helper that takes a set of regular expressions and reports every matching
/// line in the repository, so each later check declares a pattern set instead of writing its own traversal.
/// Used by the fabricated-literal, placeholder-resource and retired-wording gates.
/// </summary>
/// <remarks>
/// Matching is performed line by line so a hit can name its file and line. Patterns are therefore expected
/// to describe a single-line shape, which every gate in this feature does.
/// </remarks>
internal static class RepositoryPatternScan
{
    /// <summary>The file types this scan treats as code, resource or database artifacts.</summary>
    internal static readonly string[] DefaultScannedExtensions = [".cs", ".xaml", ".resw", ".sql", ".csproj", ".json"];

    /// <summary>Directories that are build output, VCS metadata, test output, or design record.</summary>
    internal static readonly string[] DefaultExcludedDirectories = ["bin", "obj", ".git", "TestResults", "specs", "defects"];

    /// <summary>The files that declare the patterns and so necessarily contain them.</summary>
    internal static readonly string[] DefaultExcludedFileNames = ["RetiredSymbolAuditTests.cs", "FabricatedValueGuard.cs"];

    /// <summary>Walks up from the test binaries until the solution file is found.</summary>
    /// <returns>The repository root.</returns>
    internal static string FindRepositoryRoot()
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

    /// <summary>Scans the scoped repository for a string-pattern set.</summary>
    /// <param name="patterns">The pattern set, as description plus regular-expression source.</param>
    /// <param name="scope">The scope to scan; the production default when omitted.</param>
    /// <returns>One hit per matching line per pattern.</returns>
    internal static IReadOnlyList<RepositoryScanHit> Scan(
        IReadOnlyList<(string Description, string Pattern)> patterns,
        RepositoryScanScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        return Scan(
            [.. patterns.Select(entry => (entry.Description, new Regex(entry.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)))],
            scope);
    }

    /// <summary>Scans the scoped repository for a regular-expression pattern set.</summary>
    /// <param name="patterns">The pattern set, as description plus compiled expression.</param>
    /// <param name="scope">The scope to scan; the production default when omitted.</param>
    /// <returns>One hit per matching line per pattern.</returns>
    internal static IReadOnlyList<RepositoryScanHit> Scan(
        IReadOnlyList<(string Description, Regex Pattern)> patterns,
        RepositoryScanScope? scope = null)
    {
        ArgumentNullException.ThrowIfNull(patterns);

        var hits = new List<RepositoryScanHit>();
        if (patterns.Count == 0)
        {
            return hits;
        }

        scope ??= RepositoryScanScope.Default;
        var repositoryRoot = FindRepositoryRoot();

        foreach (var file in EnumerateScannedFiles(repositoryRoot, scope))
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, file);
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                foreach (var (description, pattern) in patterns)
                {
                    if (pattern.IsMatch(lines[index]))
                    {
                        hits.Add(new RepositoryScanHit(relativePath, index + 1, description, pattern.ToString(), lines[index].Trim()));
                    }
                }
            }
        }

        return hits;
    }

    /// <summary>Enumerates the files a scope covers.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="scope">The scope to apply.</param>
    /// <returns>The files to scan.</returns>
    internal static IEnumerable<string> EnumerateScannedFiles(string repositoryRoot, RepositoryScanScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        foreach (var file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            if (!scope.Extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(repositoryRoot, file);

            if (scope.IncludeUnder.Length > 0
                && !scope.IncludeUnder.Any(prefix => relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (scope.ExcludedFileNames.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (scope.ExcludedDirectories.Any(excluded => IsUnderDirectory(relativePath, excluded)))
            {
                continue;
            }

            yield return file;
        }
    }

    /// <summary>Renders a hit list for an assertion message.</summary>
    /// <param name="hits">The hits to render.</param>
    /// <returns>A readable multi-line report, or a marker when there are none.</returns>
    internal static string Describe(IReadOnlyList<RepositoryScanHit> hits)
    {
        ArgumentNullException.ThrowIfNull(hits);

        return hits.Count == 0
            ? "(no hits)"
            : string.Join(
                Environment.NewLine,
                hits.Select(hit => $"{hit.RelativePath}:{hit.LineNumber}: {hit.Description} [{hit.Pattern}] -> {hit.Text}"));
    }

    private static bool IsUnderDirectory(string relativePath, string directoryName)
        => relativePath.StartsWith(directoryName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains(Path.DirectorySeparatorChar + directoryName + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
