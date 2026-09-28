using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// The one place a fault that is about to end the process gets written down somewhere that outlives it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect this pins.</b> Found by hand on 2026-09-28: the application died the moment the main screens
/// loaded, and the store's log held nothing but successful lines — 226 of them, not one an error. Every line
/// written to the store goes through a queue that another thread flushes, so a fault that ends the process takes
/// its own account of itself with it, and the fault was left unreadable. The Windows crash record carried only
/// <c>80131509</c>, a stowed <c>InvalidOperationException</c>, and no stack.
/// </para>
/// <para>
/// <b>Why the check reads the wiring rather than driving it.</b> The handler only runs when a real fault ends a
/// real process, which no unit test can arrange. The wiring is read the way the repository's other source audits
/// read it, and each assertion names the mechanism so a reader who finds this failing knows what was removed.
/// </para>
/// </remarks>
[TestClass]
public sealed class FaultRecordAuditTests
{
    [TestMethod]
    public void TheUnhandledHandler_WritesTheFaultSomewhereTheProcessCannotTakeWithIt()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "RecordFault(\"Unhandled\"",
            "a fault that ends the process has to be written down where the death cannot lose it");

        StringAssert.Contains(
            host,
            "File.WriteAllText(",
            "the record is written to a file directly, because the store log's queue dies with the process");
    }

    [TestMethod]
    public void TheFaultRecord_IsBestEffortAndBounded()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "MaximumRecordedFaults",
            "a fault inside a loop must not fill the disk with one record per occurrence");

        StringAssert.Contains(
            host,
            "Writing the record is best-effort",
            "a record that cannot be written must not become a second fault on top of the one being recorded");
    }

    /// <summary>One repository file, addressed from the repository root.</summary>
    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { RepositoryPatternScan.FindRepositoryRoot() }.Concat(parts).ToArray());

        Assert.IsTrue(File.Exists(path), $"the file this audit reads is not where it was: {path}");

        return File.ReadAllText(path);
    }
}
