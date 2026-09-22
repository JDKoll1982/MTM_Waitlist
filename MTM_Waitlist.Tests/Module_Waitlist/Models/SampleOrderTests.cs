using System.Collections.Generic;
using System.Windows.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
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
            Status = "Ready"
        };

        order.Fields.Add(new WaitlistField { Label = "Request type", Value = "Coil" });

        Assert.AreEqual("Material request", order.Title);
        Assert.AreEqual("Expo Drive", order.Subtitle);
        Assert.AreEqual("Ready", order.Status);
        Assert.AreEqual(1, order.Fields.Count);
        Assert.AreEqual("Request type", order.Fields[0].Label);
        Assert.AreEqual("Coil", order.Fields[0].Value);
    }

    /// <summary>
    /// The one card row model (<c>data-model.md</c> §7). Every Item's row carries the same members, because
    /// the card is one shape for every Item (FR-006).
    /// </summary>
    [TestMethod]
    public void SampleOrder_CarriesTheTwoLinesThePictureAndTheFourMetadataRows()
    {
        var order = new SampleOrder
        {
            Title = "Pickup",
            Subtitle = "MMC0001000",
            ResolvedImagePath = @"X:\Software Development\Live Applications\MTM_Waitlist\Images\request_item_pickup-coil.png",
            ItemCode = "pickup-coil",
            RequestedByName = "Ada Lovelace",
            RequestedPressName = "Press 4",
            RemainingTimeText = "01:20",
            WaitingForText = "Waiting 35m",
        };

        Assert.AreEqual("Pickup", order.Title, "Line 1 is the Item's umbrella phrase.");
        Assert.AreEqual("MMC0001000", order.Subtitle, "Line 2 is the Item's identifier.");
        Assert.AreEqual("pickup-coil", order.ItemCode, "The row's identity is the Item code the request was raised with.");
        Assert.AreEqual(
            @"X:\Software Development\Live Applications\MTM_Waitlist\Images\request_item_pickup-coil.png",
            order.EffectiveImagePath,
            "The picture configured for the Item is what the card draws.");

        Assert.AreEqual("Ada Lovelace", order.RequestedByName, "The Requested by row.");
        Assert.AreEqual("Press 4", order.RequestedPressName, "The Press row.");
        Assert.AreEqual("01:20", order.RemainingTimeText, "The Remaining time row.");
        Assert.AreEqual("Waiting 35m", order.WaitingForText, "The Waiting row.");
        Assert.IsTrue(order.HasWaitingFor, "A row that carries a waiting age shows the Waiting row.");
    }

    /// <summary>
    /// An unresolvable identifier is reported on the row rather than hidden, so the card can say why it has no
    /// second line instead of showing a blank (FR-026).
    /// </summary>
    [TestMethod]
    public void SampleOrder_ReportsAnUnresolvableIdentifierInsteadOfLeavingTheLineBlank()
    {
        var order = new SampleOrder { Title = "Pickup", Subtitle = "Pickup Coil", Line2Problem = "Its configuration names a value it doesn't have." };

        Assert.IsTrue(order.HasLine2Problem);
        Assert.AreEqual("Its configuration names a value it doesn't have.", order.Line2Problem);
        Assert.IsFalse(string.IsNullOrWhiteSpace(order.Subtitle), "The identifier falls back to the Item's display name, never a blank.");

        var resolved = new SampleOrder { Subtitle = "MMC0001000" };
        Assert.IsFalse(resolved.HasLine2Problem, "A row whose identifier resolved reports no problem.");
    }

    /// <summary>
    /// The action area is unchanged by the Item: the same four affordances for the same stored status
    /// (FR-032, <c>specs/003</c> C7). A row carries the gates, the localized labels and the commands the card
    /// draws, so the card needs no other source to decide what to offer.
    /// </summary>
    [TestMethod]
    public void SampleOrder_ExposesTheSameActionAffordancesForEveryItem()
    {
        var accept = new NoOpCommand();
        var complete = new NoOpCommand();
        var release = new NoOpCommand();
        var cancel = new NoOpCommand();
        var order = new SampleOrder
        {
            Title = "Other",
            CanAccept = true,
            CanCompleteOrRelease = false,
            CanCancelRequest = true,
            AcceptActionText = "Accept",
            CompleteActionText = "Complete",
            ReleaseActionText = "Give back",
            CancelActionText = "Cancel",
            AcceptCommand = accept,
            CompleteCommand = complete,
            ReleaseCommand = release,
            CancelCommand = cancel,
        };

        Assert.IsTrue(order.CanAccept);
        Assert.IsFalse(order.CanCompleteOrRelease);
        Assert.IsTrue(order.CanCancelRequest);

        Assert.AreEqual("Accept", order.AcceptActionText);
        Assert.AreEqual("Complete", order.CompleteActionText);
        Assert.AreEqual("Give back", order.ReleaseActionText);
        Assert.AreEqual("Cancel", order.CancelActionText);

        Assert.IsNotNull(order.AcceptCommand);
        Assert.IsNotNull(order.CompleteCommand);
        Assert.IsNotNull(order.ReleaseCommand);
        Assert.IsNotNull(order.CancelCommand);
    }

    /// <summary>
    /// FR-006 is absolute: no member of the row selects a layout. The card used to pick one of fifteen
    /// per-type views from the row, so the honest check is that no such member exists any more — and that no
    /// member is even shaped like one (a template, a selector, a variant, or a pre-built details control).
    /// </summary>
    [TestMethod]
    public void SampleOrder_ExposesNoMemberThatSelectsALayoutByItem()
    {
        var layoutShapedNames = new[] { "Template", "Selector", "Layout", "Variant", "DetailsContent", "CardView", "LineView" };

        var offenders = typeof(SampleOrder)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => layoutShapedNames.Any(name => property.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                || property.PropertyType.FullName?.StartsWith("Microsoft.UI.Xaml.DataTemplate", StringComparison.Ordinal) == true)
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            Array.Empty<string>(),
            offenders,
            $"The row exposes {string.Join(", ", offenders)}, which is a layout selected by the Item (FR-006).");
    }

    /// <summary>
    /// An Item nobody has given a picture to says so.
    /// </summary>
    /// <remarks>
    /// The row used to fall back to built-in artwork chosen from a table of Item codes, which drew a picture of a
    /// box labelled "WIP" on a request for a coil — a picture of a different thing standing in for an absence.
    /// A picture is a setting now, so no setting means the application's one no-image picture.
    /// </remarks>
    [TestMethod]
    public void SampleOrder_WithNothingConfigured_DrawsTheNoImagePlaceholder()
    {
        var order = new SampleOrder { Title = "Pickup", ItemCode = "pickup-wip" };

        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            order.EffectiveImagePath,
            "A row with nothing configured must say there is no picture rather than borrow one.");
    }

    /// <summary>
    /// The absence is the point, so it is stated as an absence: the row has no member holding an asset file name
    /// for the card to draw, which is the door built-in per-Item artwork came back through last time. The
    /// resolved pictures the row does carry — <c>ResolvedImagePath</c> and <c>WorkCenterImagePath</c> — are
    /// paths a person configured, and they exist for the card to prefer.
    /// </summary>
    [TestMethod]
    public void SampleOrder_CarriesNoBuiltInPictureMember()
    {
        const string removedMember = "ImagePath";

        Assert.IsNull(
            typeof(SampleOrder).GetProperty(
                removedMember,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
            $"The row exposes '{removedMember}' again, which is where built-in per-Item artwork comes back in.");
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

    /// <summary>
    /// A command that does nothing, so the row's affordance check is about the row carrying a command at all
    /// rather than about what the command does — which the list's own action tests cover.
    /// </summary>
    private sealed class NoOpCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
