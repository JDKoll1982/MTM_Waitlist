using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Core.Services;

[TestClass]
public sealed class UrgencyCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Compute_DueIsCreatedPlusAllotted_RemainingAndOverdue()
    {
        var created = Now.AddMinutes(-30);
        var state = UrgencyCalculator.Compute(created, TimeSpan.FromMinutes(60), Now);

        Assert.AreEqual(created + TimeSpan.FromMinutes(60), state.DueUtc);
        Assert.AreEqual(TimeSpan.FromMinutes(30), state.Remaining);
        Assert.IsFalse(state.IsOverdue);
    }

    [TestMethod]
    public void Compute_Overdue_WhenPastDue()
    {
        var created = Now.AddMinutes(-120);
        var state = UrgencyCalculator.Compute(created, TimeSpan.FromMinutes(60), Now);

        Assert.IsTrue(state.IsOverdue);
        Assert.IsTrue(state.Remaining < TimeSpan.Zero);
    }

    [TestMethod]
    public void OrderMostUrgentFirst_OverdueFirst_ThenLeastRemaining()
    {
        // a: overdue, b: 10 min left, c: 5 min left.
        var a = UrgencyCalculator.Compute(Now.AddMinutes(-70), TimeSpan.FromMinutes(60), Now);
        var b = UrgencyCalculator.Compute(Now.AddMinutes(-50), TimeSpan.FromMinutes(60), Now);
        var c = UrgencyCalculator.Compute(Now.AddMinutes(-55), TimeSpan.FromMinutes(60), Now);

        var ordered = UrgencyCalculator.OrderMostUrgentFirst(new[] { "b", "a", "c" }, s => s switch
        {
            "a" => a,
            "b" => b,
            _ => c,
        }).ToArray();

        CollectionAssert.AreEqual(new[] { "a", "c", "b" }, ordered);
    }

    [TestMethod]
    public void RemainingLabel_OverdueVsMinutes()
    {
        var overdue = UrgencyCalculator.Compute(Now.AddMinutes(-70), TimeSpan.FromMinutes(60), Now);
        var ok = UrgencyCalculator.Compute(Now.AddMinutes(-30), TimeSpan.FromMinutes(60), Now);

        Assert.AreEqual("Overdue", overdue.RemainingLabel);
        Assert.AreEqual("30 min", ok.RemainingLabel);
    }
}
