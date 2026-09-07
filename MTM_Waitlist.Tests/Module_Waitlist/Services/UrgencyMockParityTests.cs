using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// Mock-parity coverage for Workflow 08: the sample/mock request rows expose RequestedUtc + TargetTimeUtc, so
/// due/remaining/overdue math and most-urgent-first ordering work with the mock toggle ON (no reliance on a static
/// remaining-time string).
/// </summary>
[TestClass]
public sealed class UrgencyMockParityTests
{
    private static readonly string[] ActiveStatuses = { "Pending", "Accepted" };

    [TestMethod]
    public void SampleRows_ExposeRequestedAndTargetTimes_ForActiveRequests()
    {
        var rows = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");
        var active = rows.Where(row => ActiveStatuses.Contains(row.Status)).ToArray();

        Assert.IsTrue(active.Length >= 2, "Sample catalog should include at least two active requests.");
        foreach (var row in active)
        {
            Assert.AreNotEqual(default, row.RequestedUtc, $"'{row.WorkCenter}' should expose RequestedUtc.");
            Assert.IsNotNull(row.TargetTimeUtc, $"'{row.WorkCenter}' should expose TargetTimeUtc.");
        }
    }

    [TestMethod]
    public void SampleRows_MarkedOverdue_HavePastDueTarget()
    {
        var now = DateTimeOffset.UtcNow;
        var rows = SampleWaitlistRequestCatalog.GetRequests("Expo Drive");

        var overdue = rows.FirstOrDefault(row => row.IsOverdue && ActiveStatuses.Contains(row.Status));
        Assert.IsNotNull(overdue, "Sample catalog should include an active overdue request.");
        Assert.IsNotNull(overdue!.TargetTimeUtc);
        Assert.IsTrue(overdue.TargetTimeUtc < now, "A row flagged overdue should have a target in the past.");
    }

    [TestMethod]
    public void UrgencyOrdering_OrdersOverdueFirst_UsingSampleRows()
    {
        var now = DateTimeOffset.UtcNow;
        var rows = SampleWaitlistRequestCatalog.GetRequests("Expo Drive")
            .Where(row => ActiveStatuses.Contains(row.Status))
            .Select(row => (
                row,
                State: UrgencyCalculator.Compute(
                    row.RequestedUtc,
                    (row.TargetTimeUtc ?? row.RequestedUtc) - row.RequestedUtc,
                    now)))
            .ToArray();

        var ordered = UrgencyCalculator.OrderMostUrgentFirst(rows, item => item.State).ToArray();

        // Nothing on-time may appear before an overdue row.
        var firstOverdueIndex = IndexOfFirst(ordered, item => item.State.IsOverdue);
        var firstOnTimeIndex = IndexOfFirst(ordered, item => !item.State.IsOverdue);
        if (firstOverdueIndex >= 0 && firstOnTimeIndex >= 0)
        {
            Assert.IsTrue(firstOverdueIndex < firstOnTimeIndex, "Overdue requests must sort ahead of on-time ones.");
        }
    }

    private static int IndexOfFirst<T>(IReadOnlyList<T> items, Func<T, bool> predicate)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (predicate(items[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
