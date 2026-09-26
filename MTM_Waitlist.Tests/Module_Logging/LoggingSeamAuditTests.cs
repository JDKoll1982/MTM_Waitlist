using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Logging;

/// <summary>
/// The properties of the logging seam that only its source can be asked about: that it is unconditional, and
/// that the module writes nothing to the machine (SC-003, SC-016, FR-025; plan D6, D27).
/// </summary>
/// <remarks>
/// <para>
/// The seam's whole reason for existing is that the type it replaces was compiled out of a Release build and
/// wrote a local log file, so a behavioural test cannot prove either property: today's build either has the
/// attribute or it does not. These read the logging module's own source, which is the same technique the
/// repository's other policy gates use.
/// </para>
/// <para>
/// The scope is the <c>MTM_Waitlist.Logging</c> project, so the audit stays about the new seam and does not
/// restate the retired path that a later unit deletes.
/// </para>
/// </remarks>
[TestClass]
public sealed class LoggingSeamAuditTests
{
    /// <summary>The logging module, which is the whole scope of both checks.</summary>
    private static RepositoryScanScope LoggingModuleScope => new() { IncludeUnder = ["MTM_Waitlist.Logging"] };

    [TestMethod]
    public void LoggingModuleScope_CoversTheSeamSource()
    {
        // A scan whose scope resolves to nothing finds nothing, so the two checks below would pass without ever
        // reading a file. This proves the scope reaches the seam before either of them is believed.
        var hits = RepositoryPatternScan.Scan(
            [("the seam's own contract", @"public interface ILogService")],
            LoggingModuleScope);

        Assert.AreEqual(
            1,
            hits.Count,
            "the logging module's scope does not cover its own source, so the seam checks are vacuous");
    }

    [TestMethod]
    public void LoggingModule_CarriesNoConditionalAttributeAndNoDebugOnlyGuard()
    {
        // Arrange
        var patterns = new List<(string Description, string Pattern)>
        {
            ("conditional compilation attribute", @"\[Conditional"),
            ("debug-only compilation guard", @"#if\s+DEBUG\b"),
            ("non-debug compilation guard", @"#if\s+!\s*DEBUG\b"),
        };

        // Act
        var hits = RepositoryPatternScan.Scan(patterns, LoggingModuleScope);

        // Assert: a release build records exactly what a debug build records (SC-003, plan D6).
        Assert.AreEqual(
            0,
            hits.Count,
            "the logging seam must be unconditional; a conditional attribute or a debug guard compiles it out of a"
                + " Release build, which is why the retired type recorded nothing on a shop-floor machine:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, hits.Select(hit => $"{hit.RelativePath}:{hit.LineNumber} {hit.Text}")));
    }

    [TestMethod]
    public void LoggingModule_WritesNothingToTheMachine()
    {
        // Arrange
        var patterns = new List<(string Description, string Pattern)>
        {
            ("file write", @"\bFile\.(WriteAll|AppendAll|Create|OpenWrite)"),
            ("stream writer", @"new\s+StreamWriter\s*\("),
            ("file stream", @"new\s+FileStream\s*\("),
            ("directory creation", @"Directory\.CreateDirectory\s*\("),
        };

        // Act
        var hits = RepositoryPatternScan.Scan(patterns, LoggingModuleScope);

        // Assert: the store is the only destination. A diagnostic the store refuses is dropped, and the machine
        // keeps no substitute record of it (FR-025, SC-003).
        Assert.AreEqual(
            0,
            hits.Count,
            "the logging module must write nothing to the machine:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, hits.Select(hit => $"{hit.RelativePath}:{hit.LineNumber} {hit.Text}")));
    }
}
