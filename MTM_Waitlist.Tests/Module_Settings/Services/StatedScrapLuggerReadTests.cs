using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Settings.Services;

/// <summary>
/// The lugger resolution on the composition root, audited at the source: one routine serves both the work-order card
/// and a stated scrap type, so the two cannot disagree for one type (FR-007).
/// </summary>
/// <remarks>
/// The provider cannot be built from this project — the test assembly does not reference the application, and the
/// provider's own constructor takes the setup job resolver — so the split is read from its source rather than
/// exercised. That is the honest form of the guarantee here: what is asserted is the shape that makes one resolution
/// and one arrangement read possible, and each failure names the file to open.
/// </remarks>
[TestClass]
public sealed class StatedScrapLuggerReadTests
{
    private const string ProviderPath = "Services/RequestJobAvailabilityProvider.cs";

    [TestMethod]
    public void Service_StatedScrapLuggerReadTests_Provider_DeclaresTheStatedEntryPoint()
    {
        Assert.IsTrue(
            Signature(Source()) is not null,
            $"{ProviderPath} declares no Task<StatedScrapLugger> GetStatedScrapLuggerAsync, so a stated scrap type has no resolution to call.");
    }

    [TestMethod]
    public void Service_StatedScrapLuggerReadTests_StatedEntryPoint_TakesNoWorkCentreAndNoJobSnapshot()
    {
        var signature = Signature(Source());
        Assert.IsNotNull(signature, $"{ProviderPath} declares no stated entry point.");

        var parameters = signature!
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2)[0].Trim())
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "string scrapType", "CancellationToken cancellationToken" },
            parameters,
            $"{ProviderPath}: the stated entry point takes the stated type and a token and nothing else, because a generic request has no job and no work centre to hand it.");
    }

    [TestMethod]
    public void Service_StatedScrapLuggerReadTests_BothEntryPoints_CallTheSamePrivateRoutine()
    {
        var source = Source();
        var routine = PrivateRoutine(source);

        Assert.IsNotNull(
            routine,
            $"{ProviderPath} has no private routine answering StatedScrapLugger, so there is no one resolution to share.");

        var calls = Regex.Matches(source, $@"\b{Regex.Escape(routine!)}\s*\(").Count;

        Assert.AreEqual(
            3,
            calls,
            $"{ProviderPath}: '{routine}' appears {calls} time(s). It has to be declared once and called from both entry points, or a work-order card and a generic card can resolve a type differently (FR-007).");
    }

    [TestMethod]
    public void Service_StatedScrapLuggerReadTests_Provider_ReadsTheArrangementInOnePlace()
    {
        var source = Source();
        var reads = Regex.Matches(source, @"\bGetSnapshotAsync\s*\(").Count;

        Assert.AreEqual(
            1,
            reads,
            $"{ProviderPath} calls GetSnapshotAsync {reads} time(s). One read in the shared routine is what makes both cards read the arrangement the same way (FR-006, FR-007).");
    }

    private static string Source() =>
        File.ReadAllText(Path.Combine(RepositoryPatternScan.FindRepositoryRoot(), ProviderPath));

    /// <summary>The stated entry point's parameter list, or null when the file declares no such method.</summary>
    private static string? Signature(string source)
    {
        var match = Regex.Match(source, @"public\s+Task<StatedScrapLugger>\s+GetStatedScrapLuggerAsync\s*\(([^)]*)\)");

        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>The name of the one private routine that answers a <c>StatedScrapLugger</c>.</summary>
    private static string? PrivateRoutine(string source)
    {
        var match = Regex.Match(source, @"private\s+async\s+Task<StatedScrapLugger>\s+(\w+)\s*\(");

        return match.Success ? match.Groups[1].Value : null;
    }
}
