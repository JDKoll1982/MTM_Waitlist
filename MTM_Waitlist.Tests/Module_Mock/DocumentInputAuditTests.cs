using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MTM_Waitlist.Tests.Module_Mock;

/// <summary>
/// FR-027's design-document gate (§D19): the spreadsheet and the workflow document are
/// <b>design-time</b> inputs. They are read once, by a person, when the configuration data is authored —
/// never by the program. This audit fails the build if any source, markup, project or settings file in
/// the tree names either of them, so the documents cannot drift back into being a shipped input.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is scanned.</b> Only the file types that can carry a build dependence: <c>.cs</c>,
/// <c>.xaml</c>, <c>.csproj</c>, <c>.props</c>, <c>.targets</c> and <c>.json</c>. SQL comment text and
/// Markdown are deliberately <i>not</i> scanned: a comment in a database artifact or a design note
/// cannot make the program read the document, and the design record under <c>specs/</c> legitimately
/// quotes both names as the reason this guard exists.
/// </para>
/// <para>
/// <b>Why the fixture self-test exists.</b> A scan that silently reaches no files reports success for
/// the wrong reason. <see cref="DesignDocumentScan_ReachesAFixtureThatReferencesThem"/> walks the same
/// matcher over a directory that <i>does</i> reference both names, so the production assertion below is
/// evidence of compliance rather than of an empty enumeration.
/// </para>
/// </remarks>
[TestClass]
public sealed class DocumentInputAuditTests
{
    /// <summary>The two documents of record, as patterns over the names they are referred to by.</summary>
    private static readonly (string Description, Regex Pattern)[] s_designDocumentReferences =
    [
        ("spreadsheet document of record", new Regex(@"Request-Config-Template", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
        ("workflow document of record", new Regex(@"unified-item-picker-workflows", RegexOptions.Compiled | RegexOptions.CultureInvariant)),
    ];

    /// <summary>The file types that could turn either document into a build input.</summary>
    private static readonly string[] s_scannedExtensions = [".cs", ".xaml", ".csproj", ".props", ".targets", ".json"];

    /// <summary>The directory holding this audit's deliberate fixture.</summary>
    private static readonly string s_fixtureDirectory = Path.Combine("MTM_Waitlist.Tests", "Module_Mock", "Fixtures");

    /// <summary>
    /// Self-test: the matcher must find a reference that is genuinely there, so the production scan below
    /// cannot pass merely because nothing was scanned.
    /// </summary>
    [TestMethod]
    public void DesignDocumentScan_ReachesAFixtureThatReferencesThem()
    {
        var scope = new RepositoryScanScope
        {
            Extensions = s_scannedExtensions,
            IncludeUnder = [s_fixtureDirectory],
            ExcludedDirectories = RepositoryPatternScan.DefaultExcludedDirectories,
            ExcludedFileNames = [],
        };

        var hits = RepositoryPatternScan.Scan(s_designDocumentReferences, scope);

        Assert.IsTrue(
            hits.Count >= 2,
            $"The scan reached only {hits.Count} reference(s) in the deliberate fixture at '{s_fixtureDirectory}', "
                + "so a clean production result would prove nothing:"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));
    }

    /// <summary>
    /// FR-027: no shipped file may name either design document.
    /// </summary>
    [TestMethod]
    public void NoShippedFileReferencesTheDesignDocuments()
    {
        var scope = new RepositoryScanScope
        {
            Extensions = s_scannedExtensions,

            // The default production scope, plus this audit's own fixture directory — the fixture exists
            // to be found by the self-test above, not to be a shipped reference.
            ExcludedDirectories = [.. RepositoryPatternScan.DefaultExcludedDirectories, "Fixtures"],
            ExcludedFileNames = ["DocumentInputAuditTests.cs", .. RepositoryPatternScan.DefaultExcludedFileNames],
        };

        var hits = RepositoryPatternScan.Scan(s_designDocumentReferences, scope);

        Assert.AreEqual(
            0,
            hits.Count,
            "FR-027 makes the spreadsheet and the workflow document design-time inputs: a person reads them once, "
                + "when the configuration data is authored. No source, markup, project or settings file may name "
                + "either of them, because naming one in a build input is how it becomes a shipped dependency. "
                + "Point the reference at specs/004-unified-card-item-picker/contracts/ instead:"
                + Environment.NewLine
                + RepositoryPatternScan.Describe(hits));
    }
}
