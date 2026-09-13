using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Models;
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

    // ── US3: the five orders the list can be shown in (FR-010, FR-011, FR-012) ─────────────────────────

    [TestMethod]
    public void OrderBy_MostUrgent_IsWhatNothingRememberedAndTheDefaultKeyBothGive()
    {
        var rows = new[]
        {
            RowWith("later", Now.AddMinutes(-5), TimeSpan.FromMinutes(120)),
            RowWith("overdue", Now.AddMinutes(-90), TimeSpan.FromMinutes(60)),
            RowWith("soonest", Now.AddMinutes(-55), TimeSpan.FromMinutes(60)),
        };

        var expected = UrgencyCalculator
            .OrderMostUrgentFirst(rows, row => row.Urgency)
            .Select(row => row.Id)
            .ToArray();

        CollectionAssert.AreEqual(
            expected,
            Order(rows, null).Select(row => row.Id).ToArray(),
            "Nothing remembered for this viewer is the most-urgent order (FR-010).");
        CollectionAssert.AreEqual(
            expected,
            Order(rows, WaitlistSortOrder.MostUrgent).Select(row => row.Id).ToArray(),
            "Naming most urgent explicitly is the same order as the default path.");
    }

    [TestMethod]
    public void OrderBy_LongestWaiting_LeadsWithTheOldestRequest()
    {
        var rows = new[]
        {
            RowWith("newest", Now.AddMinutes(-5), TimeSpan.FromMinutes(600)),
            RowWith("oldest", Now.AddMinutes(-120), TimeSpan.FromMinutes(600)),
            RowWith("middle", Now.AddMinutes(-60), TimeSpan.FromMinutes(600)),
        };

        CollectionAssert.AreEqual(
            new[] { "oldest", "middle", "newest" },
            Order(rows, WaitlistSortOrder.LongestWaiting).Select(row => row.Id).ToArray(),
            "Longest waiting is the oldest request first, whatever its urgency.");
    }

    [TestMethod]
    public void OrderBy_Press_GroupsByWorkCentre_AndLeadsEachGroupWithItsMostUrgentRow()
    {
        var rows = new[]
        {
            RowWith("bravo", Now.AddMinutes(-5), TimeSpan.FromMinutes(15), press: "Bravo Line"),
            RowWith("alpha-calm", Now.AddMinutes(-5), TimeSpan.FromMinutes(15), press: "Alpha Line"),
            RowWith("alpha-overdue", Now.AddMinutes(-60), TimeSpan.FromMinutes(15), press: "Alpha Line"),
        };

        CollectionAssert.AreEqual(
            new[] { "alpha-overdue", "alpha-calm", "bravo" },
            Order(rows, WaitlistSortOrder.Press).Select(row => row.Id).ToArray(),
            "Ordering by work centre groups the requests, and the most urgent leads its own group.");
    }

    [TestMethod]
    public void OrderBy_RequestedBy_GroupsByRequester_AndLeadsEachGroupWithItsMostUrgentRow()
    {
        var rows = new[]
        {
            RowWith("zoe", Now.AddMinutes(-5), TimeSpan.FromMinutes(15), requestedBy: "Zoe Bell"),
            RowWith("abe-calm", Now.AddMinutes(-5), TimeSpan.FromMinutes(30), requestedBy: "Abe Cole"),
            RowWith("abe-overdue", Now.AddMinutes(-60), TimeSpan.FromMinutes(30), requestedBy: "Abe Cole"),
        };

        CollectionAssert.AreEqual(
            new[] { "abe-overdue", "abe-calm", "zoe" },
            Order(rows, WaitlistSortOrder.RequestedBy).Select(row => row.Id).ToArray(),
            "Ordering by requester groups the requests, and the most urgent leads its own group.");
    }

    [TestMethod]
    public void OrderBy_Status_LeadsWithWorkStillWaitingToBeClaimed()
    {
        var rows = new[]
        {
            RowWith("accepted-overdue", Now.AddMinutes(-120), TimeSpan.FromMinutes(30), status: "Accepted"),
            RowWith("pending", Now.AddMinutes(-5), TimeSpan.FromMinutes(30), status: "Pending"),
            RowWith("accepted-calm", Now.AddMinutes(-5), TimeSpan.FromMinutes(30), status: "Accepted"),
        };

        CollectionAssert.AreEqual(
            new[] { "pending", "accepted-overdue", "accepted-calm" },
            Order(rows, WaitlistSortOrder.Status).Select(row => row.Id).ToArray(),
            "Status order is the lifecycle the list works through: waiting work, then claimed work, most urgent first inside each.");
    }

    [TestMethod]
    public void OrderBy_AnUnknownKey_FallsBackToTheDefault()
    {
        // A stored preference can outlive the key it names. The list must still come up ordered (FR-010).
        var rows = new[]
        {
            RowWith("calm", Now.AddMinutes(-5), TimeSpan.FromMinutes(120)),
            RowWith("urgent", Now.AddMinutes(-90), TimeSpan.FromMinutes(60)),
            RowWith("oldest", Now.AddMinutes(-200), TimeSpan.FromMinutes(600)),
        };

        var expected = UrgencyCalculator
            .OrderMostUrgentFirst(rows, row => row.Urgency)
            .Select(row => row.Id)
            .ToArray();

        CollectionAssert.AreEqual(expected, Order(rows, "by-whatever").Select(row => row.Id).ToArray(), "An unknown key must not leave the list unordered.");
        CollectionAssert.AreNotEqual(
            Order(rows, WaitlistSortOrder.LongestWaiting).Select(row => row.Id).ToArray(),
            expected,
            "The fixture must be one where a real key gives a different order, or the fallback check proves nothing.");
    }

    [DataTestMethod]
    [DataRow(WaitlistSortOrder.MostUrgent)]
    [DataRow(WaitlistSortOrder.LongestWaiting)]
    [DataRow(WaitlistSortOrder.Press)]
    [DataRow(WaitlistSortOrder.RequestedBy)]
    [DataRow(WaitlistSortOrder.Status)]
    public void OrderBy_EveryOrder_LeavesAnOverdueRowMarkedAndAheadOfItsPeers(string sortOrder)
    {
        // The two rows carry identical work centre, requester and status, so under every order but the age one
        // they are separated by urgency alone — and an overdue row must lead either way (FR-012).
        var overdue = RowWith("overdue", Now.AddMinutes(-90), TimeSpan.FromMinutes(60), press: "Alpha Line", requestedBy: "Abe Cole");
        var calm = RowWith("calm", Now.AddMinutes(-5), TimeSpan.FromMinutes(60), press: "Alpha Line", requestedBy: "Abe Cole");

        var ordered = Order(new[] { calm, overdue }, sortOrder);

        CollectionAssert.AreEqual(new[] { "overdue", "calm" }, ordered.Select(row => row.Id).ToArray(), $"'{sortOrder}' must leave overdue work ahead of a calmer peer.");
        Assert.IsTrue(ordered[0].Urgency.IsOverdue, $"'{sortOrder}' must not clear the overdue flag the card is marked from.");
        Assert.AreEqual("Overdue", ordered[0].Urgency.RemainingLabel, $"'{sortOrder}' must leave the card reading 'Overdue'.");
    }

    [TestMethod]
    public void OrderBy_LongestWaitingAndMostUrgent_AreDifferentOrders_ForEqualWaitsWithDifferentAllotments()
    {
        // Raised at the same instant, so equally waited, but the shorter window is the more urgent of the two.
        var urgent = RowWith("same-moment-urgent", Now.AddMinutes(-30), TimeSpan.FromMinutes(35));
        var calm = RowWith("same-moment-calm", Now.AddMinutes(-30), TimeSpan.FromMinutes(120));

        // Waited twice as long as either, and is the calmest of the three. This is what separates the two orders.
        var oldestCalmest = RowWith("oldest", Now.AddMinutes(-120), TimeSpan.FromMinutes(300));
        var rows = new[] { urgent, calm, oldestCalmest };

        Assert.AreEqual(urgent.RequestedUtc, calm.RequestedUtc, "The fixture's two same-instant requests must really have been raised together.");
        Assert.AreNotEqual(
            urgent.Urgency.Remaining,
            calm.Urgency.Remaining,
            "Equal waits for Items with different allotted minutes are not equally urgent.");

        CollectionAssert.AreEqual(
            new[] { "same-moment-urgent", "same-moment-calm", "oldest" },
            Order(rows, WaitlistSortOrder.MostUrgent).Select(row => row.Id).ToArray(),
            "Most urgent is least time remaining, not how long the request has waited.");
        CollectionAssert.AreEqual(
            new[] { "oldest", "same-moment-urgent", "same-moment-calm" },
            Order(rows, WaitlistSortOrder.LongestWaiting).Select(row => row.Id).ToArray(),
            "Longest waiting is age, and an equal wait is still settled by urgency.");
    }

    /// <summary>Orders the rows through the production rule under test.</summary>
    private static IReadOnlyList<Row> Order(IEnumerable<Row> rows, string? sortOrder)
        => UrgencyCalculator.OrderBy(rows, sortOrder, row => row.Values).ToList();

    /// <summary>A row as the list holds it: its urgency plus the metadata the card already shows.</summary>
    private sealed record Row(string Id, UrgencyState Urgency, DateTimeOffset RequestedUtc, string Press, string RequestedBy, string Status)
    {
        public UrgencyCalculator.SortValues Values => new(Urgency, RequestedUtc, Press, RequestedBy, Status);
    }

    private static Row RowWith(
        string id,
        DateTimeOffset createdUtc,
        TimeSpan allotted,
        string press = "Press 1",
        string requestedBy = "Dana Whitfield",
        string status = "Pending")
        => new(id, UrgencyCalculator.Compute(createdUtc, allotted, Now), createdUtc, press, requestedBy, status);
}
