using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class SampleOrderTests
{
    [TestMethod]
    public void SampleOrder_ExposesGenericBindingProperties()
    {
        var order = new SampleOrder
        {
            Title = "Material request",
            Subtitle = "Expo Drive",
            Status = "Ready",
            ImagePath = "coil.png"
        };

        order.Fields.Add(new WaitlistField { Label = "Request type", Value = "Coil" });

        Assert.AreEqual("Material request", order.Title);
        Assert.AreEqual("Expo Drive", order.Subtitle);
        Assert.AreEqual("Ready", order.Status);
        Assert.AreEqual("coil.png", order.ImagePath);
        Assert.AreEqual(1, order.Fields.Count);
        Assert.AreEqual("Request type", order.Fields[0].Label);
        Assert.AreEqual("Coil", order.Fields[0].Value);
    }

    [TestMethod]
    public void RefreshTimeDerivedText_AdvancesTheWaitingAgeWhenTheMinuteChanges()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder
        {
            RequestedUtc = now.AddMinutes(-35),
            WaitingForText = "Waiting 34m",
        };

        var changed = order.RefreshTimeDerivedText(now);

        Assert.IsTrue(changed, "Crossing a minute boundary must refresh the card.");
        Assert.AreEqual("Waiting 35m", order.WaitingForText);
    }

    [TestMethod]
    public void RefreshTimeDerivedText_IsSilentWhenTheMinuteHasNotChanged()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder { RequestedUtc = now.AddMinutes(-35), TargetTimeUtc = now.AddMinutes(30) };
        order.RefreshTimeDerivedText(now);

        var raised = new List<string?>();
        order.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        var changed = order.RefreshTimeDerivedText(now.AddSeconds(20));

        Assert.IsFalse(changed, "A tick inside the same minute must not report a change.");
        Assert.AreEqual(0, raised.Count, "An idle tick must not repaint the card.");
    }

    [TestMethod]
    public void RefreshTimeDerivedText_NotifiesHasWaitingForAlongsideTheAge()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder { RequestedUtc = now.AddMinutes(-3) };

        var raised = new List<string?>();
        order.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        order.RefreshTimeDerivedText(now);

        CollectionAssert.Contains(raised, nameof(SampleOrder.WaitingForText));
        CollectionAssert.Contains(raised, nameof(SampleOrder.HasWaitingFor));
        Assert.IsTrue(order.HasWaitingFor);
    }

    [TestMethod]
    public void RefreshTimeDerivedText_FlipsTheCountdownToOverdueWhenTheTargetPasses()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder
        {
            TargetTimeUtc = now.AddSeconds(50),
            RemainingTimeText = "00:01",
        };

        Assert.IsFalse(order.RefreshTimeDerivedText(now), "Still within the target, so nothing changes.");

        Assert.IsTrue(order.RefreshTimeDerivedText(now.AddMinutes(1)));
        Assert.AreEqual("Overdue", order.RemainingTimeText);
        Assert.IsTrue(order.IsOverdue);
    }

    [TestMethod]
    public void RefreshTimeDerivedText_KeepsAStoreSetOverdueFlag()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder
        {
            RequestedUtc = now.AddMinutes(-10),
            TargetTimeUtc = now.AddHours(5),
            IsOverdueAtSource = true,
            IsOverdue = true,
            RemainingTimeText = "Overdue",
        };

        order.RefreshTimeDerivedText(now.AddMinutes(1));

        Assert.IsTrue(order.IsOverdue, "A later tick must not clear a flag the store set.");
        Assert.AreEqual("Overdue", order.RemainingTimeText);
    }

    [TestMethod]
    public void RefreshTimeDerivedText_LeavesStaticRowsWithoutTimestampsUntouched()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder { Title = "Static", RemainingTimeText = "New" };

        Assert.IsFalse(order.RefreshTimeDerivedText(now));
        Assert.AreEqual(string.Empty, order.WaitingForText);
        Assert.AreEqual("New", order.RemainingTimeText);
    }

    [TestMethod]
    public void TimeUntilNextMinute_AlignsTheFirstTickToTheMinuteBoundary()
    {
        var fifteenSecondsIn = new DateTimeOffset(2026, 9, 11, 14, 30, 15, TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromSeconds(45), WaitlistViewViewModel.TimeUntilNextMinute(fifteenSecondsIn));

        var onTheBoundary = new DateTimeOffset(2026, 9, 11, 14, 30, 0, TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromMinutes(1), WaitlistViewViewModel.TimeUntilNextMinute(onTheBoundary));
    }
}
