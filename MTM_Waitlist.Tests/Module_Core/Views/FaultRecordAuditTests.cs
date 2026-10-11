using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Core.Views;

/// <summary>
/// The one place a fault that is about to end the process gets recorded before the process goes.
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
/// <b>What the record is, amended 2026-10-10.</b> The first fix wrote each fault to a file under the
/// application's own local folder. That solved the readability problem and broke a MUST: nothing may be kept on a
/// computer (FR-025), and SC-016 says a fault is recorded nowhere on the machine. It was also written unredacted,
/// so an exception whose message carried a connection string or a statement's parameter value would have reached
/// the machine's disk where the logging seam would have dropped it (FR-036). The record is now the store entry,
/// written through the seam and forced out with a bounded flush before the process goes — which is what the
/// original intent asked for, and the deliverable has to reach the deliverable's own store to count.
/// </para>
/// <para>
/// <b>What is accepted.</b> A fault raised while the store is unreachable now leaves no account on the machine at
/// all. That is SC-016 exactly, and the runtime still reports the crash because the handler does not swallow it.
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
    public void Audit_FaultRecordAuditTests_TheUnhandledHandler_RecordsTheFaultWithoutKeepingAFileOnTheMachine()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "AppLog.Error(\"UnhandledException\"",
            "a fault that ends the process has to be recorded through the logging seam, or the store holds no account of the death");

        StringAssert.Contains(
            host,
            "TryFlushLogStore()",
            "the store's queue dies with the process, so the record has to be forced out before the process goes");

        Assert.IsFalse(
            host.Contains("File.WriteAllText(", StringComparison.Ordinal),
            "the fault is written to a file again, which keeps a record on the machine against FR-025 and SC-016");
    }

    [TestMethod]
    public void Audit_FaultRecordAuditTests_TheFaultRecord_IsBestEffortAndBounded()
    {
        var host = Read("App.xaml.cs");

        StringAssert.Contains(
            host,
            "TimeSpan.FromSeconds",
            "the flush that runs while the process is ending must be bounded, so a store that will not answer cannot hold the process open (FR-003)");

        StringAssert.Contains(
            host,
            "Recording the fault is best effort",
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
