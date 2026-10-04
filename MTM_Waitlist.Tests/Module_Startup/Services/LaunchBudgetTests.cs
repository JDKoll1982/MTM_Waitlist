using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Startup.Models;
using MTM_Waitlist.Module_Startup.Services;

namespace MTM_Waitlist.Tests.Module_Startup.Services;

/// <summary>
/// The launch-wide budget (`contracts/launch-concurrency-contract.md` §2; FR-024, FR-025): it is approximately
/// 45 seconds, it never replaces a step's own stated maximum, and passing it ends the launch at a stated stop
/// whose cause names the budget rather than a step.
/// </summary>
[TestClass]
public sealed class LaunchBudgetTests
{
    [TestMethod]
    public void TheShippedBudget_IsFortyFiveSeconds()
        => Assert.AreEqual(TimeSpan.FromSeconds(45), LaunchBudget.DefaultDuration, "FR-024 states 45 seconds");

    [TestMethod]
    public async Task Budget_ThatPasses_ReportsItWasReachedAndNamesItself()
    {
        using var budget = new LaunchBudget(CancellationToken.None, TimeSpan.FromMilliseconds(40));

        await Task.Delay(120);

        Assert.IsTrue(budget.IsReached, "the budget's own clock fired, so the launch passed it");
        Assert.IsFalse(budget.IsCompleted);
        Assert.IsTrue(budget.Token.IsCancellationRequested);
    }

    [TestMethod]
    public void Budget_NamesItselfInTheStopItStates()
    {
        // FR-024: the stop's cause names the budget rather than a step, and the shipped budget is 45 seconds.
        using var budget = new LaunchBudget(CancellationToken.None);

        StringAssert.Contains(budget.Overrun.Diagnosis, "45-second budget");
        Assert.AreEqual(LaunchStepStatus.Failed, budget.Overrun.Status);
        Assert.IsTrue(budget.Overrun.Remedies.CanRetry, "the only remedy that could remove it is to try again");
    }

    [TestMethod]
    public void Budget_ThatFinishedFirst_IsNotReportedAsReached()
    {
        using var budget = new LaunchBudget(CancellationToken.None, TimeSpan.FromMilliseconds(40));

        budget.MarkCompleted();

        Assert.IsFalse(budget.IsReached, "a launch that reached an outcome of its own in time is not an overrun");
        Assert.IsTrue(budget.IsCompleted);
    }

    [TestMethod]
    public void Budget_AbandonedByTheCaller_IsNotABudgetOverrun()
    {
        using var abandoned = new CancellationTokenSource();
        using var budget = new LaunchBudget(abandoned.Token, TimeSpan.FromMinutes(5));

        abandoned.Cancel();

        Assert.IsTrue(budget.Token.IsCancellationRequested);
        Assert.IsFalse(budget.IsReached, "the launch being abandoned is not the budget being passed");
    }

    [TestMethod]
    public void Budget_OfNoTime_IsRefused()
        => Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => new LaunchBudget(CancellationToken.None, TimeSpan.Zero));

    [TestMethod]
    public void EveryStep_KeepsItsOwnMaximumUnderTheBudget()
    {
        // FR-025: the budget is additional to each step's stated maximum, never a replacement, so the catalogue's
        // own bounds are untouched by the budget's existence.
        var catalog = new LaunchStepCatalog();

        foreach (var step in catalog.Steps)
        {
            Assert.IsTrue(step.MaximumWait > TimeSpan.Zero, $"'{step.Id}' still states its own maximum");
            Assert.IsTrue(
                step.MaximumWait <= LaunchStepCatalog.Ceiling,
                $"'{step.Id}' is still inside the 30-second ceiling");
        }
    }
}
