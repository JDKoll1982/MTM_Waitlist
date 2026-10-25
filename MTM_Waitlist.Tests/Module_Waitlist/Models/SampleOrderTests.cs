using System.Collections.Generic;
using System.Windows.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist.Models;

[TestClass]
public sealed class SampleOrderTests
{
    [TestMethod]
    public void Model_SampleOrderTests_SampleOrder_ExposesGenericBindingProperties()
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
    public void Model_SampleOrderTests_SampleOrder_CarriesTheTwoLinesThePictureAndTheFourMetadataRows()
    {
        var order = new SampleOrder
        {
            Title = "Pickup",
            Subtitle = "MMC0001000",
            MaterialPartNumber = "MMC0001000",
            MaterialPartScope = "visual_part",
            ResolvedPartImagePath = @"X:\Software Development\Live Applications\MTM_Waitlist\Images\Visual\MMC\MMC0001000.png",
            ItemCode = "pickup-coil",
            RequestedByName = "Ada Lovelace",
            RequestedPressName = "Press 4",
            RemainingTimeText = "01:20",
            WaitingForText = "Waiting 35m",
        };

        Assert.AreEqual("Pickup", order.Title, "Line 1 is the Item's umbrella phrase.");
        Assert.AreEqual("MMC0001000", order.Subtitle, "Line 2 is the Item's identifier.");
        Assert.AreEqual("pickup-coil", order.ItemCode, "The row's identity is the Item code the request was raised with.");
        Assert.AreEqual("MMC0001000", order.MaterialPartNumber, "The row carries the part the request is about.");
        Assert.AreEqual("visual_part", order.MaterialPartScope, "The row carries which system that part belongs to.");
        Assert.AreEqual(
            @"X:\Software Development\Live Applications\MTM_Waitlist\Images\Visual\MMC\MMC0001000.png",
            order.EffectiveImagePath,
            "The material part's resolved picture is what the card draws.");

        Assert.AreEqual("Ada Lovelace", order.RequestedByName, "The Requested by row.");
        Assert.AreEqual("Press 4", order.RequestedPressName, "The Press row.");
        Assert.AreEqual("01:20", order.RemainingTimeText, "The Remaining time row.");
        Assert.AreEqual("Waiting 35m", order.WaitingForText, "The Waiting row.");
        Assert.IsTrue(order.HasWaitingFor, "A row that carries a waiting age shows the Waiting row.");
    }

    /// <summary>
    /// The card draws the <b>part's</b> picture and nothing else. A request whose material cannot be pictured
    /// draws the one shared placeholder; it never borrows the picture for the kind of request, and never the
    /// part's family's artwork (FR-019, FR-020).
    /// </summary>
    /// <remarks>
    /// The row still carries the Item code it was raised with — the request page and the card's lines read it —
    /// but nothing about the row turns that code into a picture: the only picture the row can draw is the one the
    /// list resolved for the material part.
    /// </remarks>
    [TestMethod]
    public void Model_SampleOrderTests_SampleOrder_EffectiveImageIsTheMaterialPartsPictureAndThePlaceholderOtherwise()
    {
        // A request about a material, pictured.
        var pictured = new SampleOrder
        {
            ItemCode = "pickup-coil",
            MaterialPartNumber = "MMC0001000",
            MaterialPartScope = "visual_part",
            ResolvedPartImagePath = @"X:\Shared\Images\Visual\MMC\MMC0001000.png",
        };

        Assert.AreEqual(
            @"X:\Shared\Images\Visual\MMC\MMC0001000.png",
            pictured.EffectiveImagePath,
            "A pictured material part is what the card draws.");

        // The same request, with the material named but nothing resolved for it.
        var unpictured = new SampleOrder
        {
            ItemCode = "pickup-coil",
            MaterialPartNumber = "MMC0001000",
            MaterialPartScope = "visual_part",
        };

        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            unpictured.EffectiveImagePath,
            "A material that cannot be pictured draws the one shared placeholder, not a blank and not artwork.");

        // A request that names no material part and resolved no picture of its own — a table assist, a
        // consumable, a dunnage request whose part nobody pictured.
        var noMaterial = new SampleOrder { ItemCode = "pickup-dunnage" };

        Assert.AreEqual(
            ImagePicturePolicy.NoImagePath,
            noMaterial.EffectiveImagePath,
            "A request that names no part draws the one shared placeholder rather than inventing a picture.");

        Assert.AreEqual(
            string.Empty,
            noMaterial.MaterialPartNumber,
            "A request that names no part says so on the row rather than naming a part it does not have.");
    }

    /// <summary>
    /// An unresolvable identifier is reported on the row rather than hidden, so the card can say why it has no
    /// second line instead of showing a blank (FR-026).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_SampleOrder_ReportsAnUnresolvableIdentifierInsteadOfLeavingTheLineBlank()
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
    public void Model_SampleOrderTests_SampleOrder_ExposesTheSameActionAffordancesForEveryItem()
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
    public void Model_SampleOrderTests_SampleOrder_ExposesNoMemberThatSelectsALayoutByItem()
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
    public void Model_SampleOrderTests_SampleOrder_WithNothingConfiguredDrawsTheNoImagePlaceholder()
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
    public void Model_SampleOrderTests_SampleOrder_CarriesNoBuiltInPictureMember()
    {
        const string removedMember = "ImagePath";

        Assert.IsNull(
            typeof(SampleOrder).GetProperty(
                removedMember,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance),
            $"The row exposes '{removedMember}' again, which is where built-in per-Item artwork comes back in.");
    }

    [TestMethod]
    public void Model_SampleOrderTests_RefreshTimeDerivedText_AdvancesTheWaitingAgeWhenTheMinuteChanges()
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
    public void Model_SampleOrderTests_RefreshTimeDerivedText_IsSilentWhenTheMinuteHasNotChanged()
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
    public void Model_SampleOrderTests_RefreshTimeDerivedText_NotifiesHasWaitingForAlongsideTheAge()
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
    public void Model_SampleOrderTests_RefreshTimeDerivedText_FlipsTheCountdownToOverdueWhenTheTargetPasses()
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
    public void Model_SampleOrderTests_RemainingTimeAndAllotmentText_StatesWhatIsLeftAgainstWhatTheItemAllotted()
    {
        // The countdown on its own says nothing about how close a request is: nine minutes left is most of a
        // fifteen-minute allotment and half of an eighteen-minute one. The row states both, in the one shape the
        // countdown and the ring already speak, so the two figures can be read against each other (FR-017).
        var due = new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder
        {
            TargetTimeUtc = due,
            AllottedMinutes = 15,
        };

        order.RefreshTimeDerivedText(due.AddMinutes(-9));

        Assert.AreEqual(
            "00:09 / 00:15",
            order.RemainingTimeAndAllotmentText,
            "The remaining-time row must state what is left against the minutes the Item allotted.");
    }

    [TestMethod]
    public void Model_SampleOrderTests_RefreshTimeDerivedText_KeepsAStoreSetOverdueFlag()
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
    public void Model_SampleOrderTests_RefreshTimeDerivedText_LeavesStaticRowsWithoutTimestampsUntouched()
    {
        var now = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var order = new SampleOrder { Title = "Static", RemainingTimeText = "New" };

        Assert.IsFalse(order.RefreshTimeDerivedText(now));
        Assert.AreEqual(string.Empty, order.WaitingForText);
        Assert.AreEqual("New", order.RemainingTimeText);
    }

    [TestMethod]
    public void Model_SampleOrderTests_TimeUntilNextMinute_AlignsTheFirstTickToTheMinuteBoundary()
    {
        var fifteenSecondsIn = new DateTimeOffset(2026, 9, 11, 14, 30, 15, TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromSeconds(45), WaitlistViewViewModel.TimeUntilNextMinute(fifteenSecondsIn));

        var onTheBoundary = new DateTimeOffset(2026, 9, 11, 14, 30, 0, TimeSpan.Zero);
        Assert.AreEqual(TimeSpan.FromMinutes(1), WaitlistViewViewModel.TimeUntilNextMinute(onTheBoundary));
    }

    /// <summary>
    /// The status zone's word and theme follow the same precedence: the handler condition when the row carries
    /// one, otherwise the overdue marking, otherwise the lifecycle word (FR-012, FR-023).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_StatusWord_PrefersTheConditionOverTheOverdueMarkingAndTheLifecycleWord()
    {
        var order = new SampleOrder
        {
            Status = "Pending",
            IsOverdue = true,
            HandlerStatus = "Product Buried",
        };

        Assert.AreEqual(
            "Product Buried",
            order.StatusWord,
            "A condition wins over everything, including the overdue marking (FR-012, FR-023).");
        Assert.AreNotEqual("Overdue", order.StatusWord, "A condition and the overdue marking are never both shown.");
        Assert.AreNotEqual("Waiting", order.StatusWord, "The lifecycle word is the last of the three, not the first.");
        Assert.AreEqual(
            StatusColumnTheme.HandlerCondition,
            order.StatusTheme,
            "The condition is themed as a condition, so the word and the theme cannot disagree.");
        Assert.AreNotEqual(
            StatusColumnTheme.Overdue,
            order.StatusTheme,
            "The two reds are apart so an overdue row and a condition-carrying row are never mistaken for one another (FR-072).");
    }

    /// <summary>
    /// With no condition reported, the overdue marking is the next of the three, and nothing but the overdue
    /// marking takes the overdue theme (FR-014).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_StatusWord_ReadsOverdueWhenNoConditionIsSetAndTheRequestIsPastItsTime()
    {
        var order = new SampleOrder { Status = "Pending", IsOverdue = true };

        Assert.AreEqual("Overdue", order.StatusWord, "An overdue row with no condition reads the overdue marking.");
        Assert.AreEqual(StatusColumnTheme.Overdue, order.StatusTheme);

        order.IsOverdue = false;

        Assert.AreEqual("Waiting", order.StatusWord, "Clearing the overdue marking falls through to the lifecycle word.");
        Assert.AreEqual(StatusColumnTheme.Normal, order.StatusTheme, "A lifecycle word is themed normally.");
    }

    /// <summary>
    /// The word and the theme ride the overdue flag, so the card is told to repaint both rather than reading a
    /// stale pair.
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_StatusWord_AnnouncesTheWordAndTheThemeWhenTheOverdueMarkingChanges()
    {
        var order = new SampleOrder { Status = "Pending" };
        var raised = new List<string?>();
        order.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        order.IsOverdue = true;

        CollectionAssert.Contains(raised, nameof(SampleOrder.StatusWord));
        CollectionAssert.Contains(raised, nameof(SampleOrder.StatusTheme));
        Assert.AreEqual("Overdue", order.StatusWord);
        Assert.AreEqual(StatusColumnTheme.Overdue, order.StatusTheme);
    }

    /// <summary>
    /// The lifecycle word comes from the stored status alone: the four mapped statuses read their word, and a
    /// stored value the card does not know yields no word at all (FR-012, FR-018).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_StatusWord_ReadsTheLifecycleWordForTheFourStoredStatusesAndNothingForAnUnknownOne()
    {
        Assert.AreEqual("Waiting", new SampleOrder { Status = "Pending" }.StatusWord);
        Assert.AreEqual("In Progress", new SampleOrder { Status = "Accepted" }.StatusWord);
        Assert.AreEqual("Done", new SampleOrder { Status = "Completed" }.StatusWord);
        Assert.AreEqual("Cancelled", new SampleOrder { Status = "Canceled" }.StatusWord, "The one-l spelling maps like the two-l one.");
        Assert.AreEqual("Cancelled", new SampleOrder { Status = "Cancelled" }.StatusWord);
        Assert.AreEqual(StatusColumnTheme.Normal, new SampleOrder { Status = "Accepted" }.StatusTheme);

        var unknown = new SampleOrder { Status = "Archived" };

        Assert.AreEqual(
            string.Empty,
            unknown.StatusWord,
            "An unrecognised stored status yields no word, and the card then removes the row rather than leaving it blank (FR-018).");
        Assert.IsFalse(unknown.HasStatusBadge);
        Assert.AreEqual(StatusColumnTheme.Normal, unknown.StatusTheme);
    }

    /// <summary>
    /// The vocabulary is closed and verbatim: the four conditions are restored as reported, and anything else —
    /// a fifth word, a differently cased or padded one — is restored as no condition at all and behaves exactly
    /// like a row nobody reported on (FR-071, FR-073).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_HandlerStatus_RestoresOnlyTheFourVerbatimConditionsAndTreatsAnythingElseAsNoCondition()
    {
        foreach (var condition in HandlerStatusCatalog.Conditions)
        {
            var reported = new SampleOrder { Status = "Pending", IsOverdue = true, HandlerStatus = condition };

            Assert.AreEqual(condition, reported.HandlerStatus, $"'{condition}' is one of the four conditions.");
            Assert.AreEqual(condition, reported.StatusWord, $"'{condition}' is what the status row reads.");
            Assert.AreEqual(StatusColumnTheme.HandlerCondition, reported.StatusTheme);
        }

        foreach (var altered in new[] { "Product buried", "ProductBuried", " Product Buried", "Product Buried ", "Product Missing", "Pending", string.Empty, "   " })
        {
            var restored = new SampleOrder { Status = "Pending", IsOverdue = true, HandlerStatus = altered };

            Assert.IsNull(restored.HandlerStatus, $"'{altered}' is not one of the four verbatim conditions.");
            Assert.AreEqual(
                "Overdue",
                restored.StatusWord,
                $"'{altered}' behaves exactly like a row nobody reported on, not like a word of its own.");
            Assert.AreEqual(StatusColumnTheme.Overdue, restored.StatusTheme, $"'{altered}' must not theme the column as a condition.");
        }
    }

    /// <summary>
    /// The ring's scale: the Item's configured allotment, or the labelled fifteen-minute default when the Item
    /// configures none or configures one that cannot be measured (FR-017).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_AllottedMinutes_FallsBackToTheLabelledDefaultWhenTheItemConfiguresNone()
    {
        Assert.AreEqual(
            15,
            SampleOrder.DefaultAllottedMinutes,
            "The default is pinned at a labelled fifteen minutes rather than read from a setting.");

        Assert.AreEqual<int?>(
            SampleOrder.DefaultAllottedMinutes,
            new SampleOrder().AllottedMinutes,
            "An Item that configures no allotment allots the default.");

        Assert.AreEqual<int?>(
            30,
            new SampleOrder { AllottedMinutes = 30 }.AllottedMinutes,
            "A configured allotment is the ring's scale.");

        foreach (var unusable in new int?[] { null, 0, -5 })
        {
            var row = new SampleOrder { AllottedMinutes = unusable };

            Assert.AreEqual<int?>(
                SampleOrder.DefaultAllottedMinutes,
                row.AllottedMinutes,
                $"An allotment of '{unusable?.ToString() ?? "unset"}' cannot be measured, so the ring falls back to the default rather than having no scale.");
        }
    }

    /// <summary>
    /// The ring states which of the two things it is drawing: the time the request still has left while it is
    /// live, and the time it took once it has closed (FR-015, FR-016).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_RingMeasure_ReadsTimeLeftWhileLiveAndTimeTakenOnceClosed()
    {
        var requested = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(
            TimeRingMeasure.TimeLeft,
            new SampleOrder { Status = "Pending", RequestedUtc = requested }.RingMeasure,
            "A live request's ring measures the time it still has left.");

        Assert.AreEqual(
            TimeRingMeasure.TimeTaken,
            new SampleOrder { Status = "Pending", RequestedUtc = requested, ClosedUtc = requested.AddMinutes(8) }.RingMeasure,
            "A close instant closes the request for the ring, whatever the stored status still reads.");

        Assert.AreEqual(
            TimeRingMeasure.TimeTaken,
            new SampleOrder { Status = "Completed", RequestedUtc = requested }.RingMeasure,
            "A finished stored status closes the request for the ring as well (FR-015).");

        Assert.AreEqual(
            TimeRingMeasure.TimeLeft,
            new SampleOrder { Status = "Accepted", RequestedUtc = requested, HandlerStatus = "Changing Forklift" }.RingMeasure,
            "A reported condition does not move the ring's measure.");
    }

    /// <summary>
    /// Once the request closes the ring is held at the moment it closed: the fraction reports the time taken and
    /// does not keep moving afterwards (FR-015).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_RingValue_FreezesOnTheTimeTakenOnceTheRequestCloses()
    {
        var requested = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);
        var closed = requested.AddMinutes(8);
        var order = new SampleOrder
        {
            Status = "Completed",
            RequestedUtc = requested,
            ClosedUtc = closed,
            AllottedMinutes = 15,
        };

        Assert.AreEqual(
            8d / 15d,
            order.RingValue,
            0.000001,
            "The closed ring reports the eight minutes it took out of its fifteen-minute allotment.");

        Assert.AreEqual(
            8d / 15d,
            SampleOrder.RingFraction(requested, closed, 15, closed.AddHours(6)),
            0.000001,
            "Measured six hours later the closed ring still reports the same fraction, because it measures to the close instant.");

        Assert.IsTrue(
            SampleOrder.RingFraction(requested, null, 15, closed.AddHours(6)) > 1d,
            "The same request while live would have grown past its allotment, so the freeze is the close instant and not the clock standing still.");
    }

    /// <summary>
    /// A request that ran past its allotment reads above one rather than being clamped, so the ring can show the
    /// overrun instead of hiding it at full (FR-014).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_RingValue_CarriesAnOverdueRequestsOverrunAboveOneRatherThanClampingIt()
    {
        var requested = new DateTimeOffset(2026, 9, 11, 14, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(
            40d / 15d,
            SampleOrder.RingFraction(requested, null, 15, requested.AddMinutes(40)),
            0.000001,
            "Forty minutes of a fifteen-minute allotment is the whole allotment and the overrun besides.");

        var overdue = new SampleOrder
        {
            Status = "Accepted",
            IsOverdue = true,
            RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-40),
            AllottedMinutes = 15,
        };

        Assert.IsTrue(
            overdue.RingValue > 1d,
            "A live overdue row's fraction reads above one, so the overrun is carried rather than clamped.");
        Assert.AreEqual(
            TimeRingMeasure.TimeLeft,
            overdue.RingMeasure,
            "An overdue but unfinished request is still measuring the time left (FR-014).");
    }

    /// <summary>
    /// Rows 3 and 4 of the status zone: the person row reads the requester while nobody has claimed the request
    /// and the handler once somebody has, and the requester row appears only once the request has closed
    /// (FR-011, FR-022).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_PersonText_ReadsTheRequesterWhileUnclaimedAndBothPeopleOnceClosed()
    {
        var unclaimed = new SampleOrder { Status = "Pending", RequestedByName = "Ada Lovelace" };

        Assert.AreEqual("Ada Lovelace", unclaimed.PersonText, "An unclaimed request's person row reads the requester (data-model.md §5).");
        Assert.IsTrue(unclaimed.HasPerson);
        Assert.AreEqual(string.Empty, unclaimed.RequesterText);
        Assert.IsFalse(
            unclaimed.HasRequester,
            "The requester row is absent rather than blank while the request is open — the requester is already row 3.");

        var claimed = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "6229",
            AssignedMaterialHandlerName = "Grace Hopper",
        };

        Assert.AreEqual(
            "Grace Hopper",
            claimed.PersonText,
            "A claimed request's person row reads its material handler's name, not the employee number beside it.");
        Assert.IsTrue(claimed.HasPerson);
        Assert.IsFalse(claimed.HasRequester);

        var closed = new SampleOrder
        {
            Status = "Completed",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "6229",
            AssignedMaterialHandlerName = "Grace Hopper",
        };

        Assert.AreEqual("Grace Hopper", closed.PersonText, "A closed request keeps its handler on row 3.");
        Assert.IsTrue(closed.HasPerson);
        Assert.AreEqual("Ada Lovelace", closed.RequesterText, "And names the requester on row 4.");
        Assert.IsTrue(closed.HasRequester);

        var handlerWithNoRecordedName = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "6229",
        };

        Assert.AreEqual(
            string.Empty,
            handlerWithNoRecordedName.PersonText,
            "A handler whose name no source records leaves the row empty; the employee number is never printed as a name.");
        Assert.IsFalse(
            handlerWithNoRecordedName.HasPerson,
            "And the card removes the row rather than drawing the number where a person belongs (FR-018).");

        var cancelledWhileWaiting = new SampleOrder { Status = "Canceled", RequestedByName = "Ada Lovelace" };

        Assert.AreEqual(
            "Ada Lovelace",
            cancelledWhileWaiting.PersonText,
            "A request cancelled before anybody claimed it names the only person who could have cancelled it.");
        Assert.IsTrue(cancelledWhileWaiting.HasPerson);
        Assert.AreEqual("Ada Lovelace", cancelledWhileWaiting.RequesterText);
        Assert.IsTrue(cancelledWhileWaiting.HasRequester);

        var blankHandler = new SampleOrder
        {
            Status = "Accepted",
            RequestedByName = "Ada Lovelace",
            AssignedMaterialHandler = "   ",
        };

        Assert.AreEqual(
            "Ada Lovelace",
            blankHandler.PersonText,
            "A handler that is only blank is not a person, so row 3 falls back to the requester rather than drawing whitespace.");
        Assert.IsTrue(blankHandler.HasPerson);

        var closedWithoutNames = new SampleOrder { Status = "Completed" };

        Assert.IsFalse(closedWithoutNames.HasPerson, "A row with nobody to name removes row 3 rather than drawing a blank one (FR-018).");
        Assert.IsFalse(closedWithoutNames.HasRequester, "And removes row 4 as well.");
    }

    /// <summary>
    /// The card's detail cells are the fields the Item declared, drawn in declaration order, with nowhere to put
    /// a field that resolved nothing: one cell per resolved field and no padding (FR-026, <c>data-model.md</c> §7).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_Cells_DrawOneCellPerResolvedFieldInDeclaredOrder()
    {
        var order = new SampleOrder { ItemCode = "pickup-coil" };

        order.Cells.Add(new WaitlistLineCardCell { Label = "CardCellCoilDescription", Value = "H-118", Order = 4 });
        order.Cells.Add(new WaitlistLineCardCell { Value = "Take To Die Shop", Order = 1 });
        order.Cells.Add(new WaitlistLineCardCell { Label = "CardCellQuantityInHouse", Value = "   ", Order = 3 });
        order.Cells.Add(new WaitlistLineCardCell { Label = "CardCellCoilNumber", Value = "MMC0001000", Order = 2 });

        Assert.AreEqual(4, order.Cells.Count, "The row holds every cell it was given, in the order it was given them.");

        var drawn = WaitlistLineCardCell.CellsToDraw(order.Cells);

        Assert.AreEqual(3, drawn.Count, "One cell per declared field that resolved a value; the field that resolved nothing is not drawn at all.");
        CollectionAssert.AreEqual(
            new[] { "Take To Die Shop", "MMC0001000", "H-118" },
            drawn.Select(cell => cell.Value).ToArray(),
            "The cells are drawn in their declared order, not the order the row happened to resolve them in.");
        Assert.IsFalse(
            drawn.Any(cell => cell.Label == "CardCellQuantityInHouse"),
            "A cell that resolved nothing is not among the drawn cells (FR-018).");
    }

    /// <summary>
    /// The ring's own text carries one of two different quantities by state: the time the request still has left
    /// while it is live, and the time it took once it has closed (FR-015). A closed request must not keep showing
    /// the countdown it was frozen at.
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_RingText_ReadsTheTimeLeftWhileLiveAndTheTimeTakenOnceClosed()
    {
        var requested = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);
        var live = new SampleOrder
        {
            RequestedUtc = requested,
            TargetTimeUtc = requested.AddMinutes(90),
        };
        live.RefreshTimeDerivedText(requested.AddMinutes(10));

        Assert.AreEqual(TimeRingMeasure.TimeLeft, live.RingMeasure);
        Assert.AreEqual(live.RemainingTimeText, live.RingText, "A live ring draws the same countdown the row shows.");

        var closed = new SampleOrder
        {
            RequestedUtc = requested,
            TargetTimeUtc = requested.AddMinutes(90),
            ClosedUtc = requested.AddMinutes(80),
        };

        Assert.AreEqual(TimeRingMeasure.TimeTaken, closed.RingMeasure);
        Assert.AreEqual("01:20", closed.RingText, "A closed ring draws the time the request took, not a countdown (FR-015).");
        Assert.AreNotEqual(closed.RemainingTimeText, closed.RingText);
    }

    /// <summary>
    /// The time-taken text has nothing to say about a row that has not closed, or one with no creation instant, and
    /// says so rather than guessing a figure (FR-089).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_FormatTimeTaken_YieldsNothingWithoutBothInstants()
    {
        var requested = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero);

        Assert.AreEqual(string.Empty, SampleOrder.FormatTimeTaken(null, requested));
        Assert.AreEqual(string.Empty, SampleOrder.FormatTimeTaken(requested, null));
        Assert.AreEqual("00:00", SampleOrder.FormatTimeTaken(requested, requested));
    }

    /// <summary>
    /// The four slots the per-Item templates bind take the row's cells in order, and answer nothing at all for
    /// a slot the row did not fill (FR-026).
    /// </summary>
    /// <remarks>
    /// The templates bind <c>Cell0</c> to <c>Cell3</c> rather than indexing the collection, because a slot a
    /// row never filled is the ordinary case and an index into it is a binding failure: what the templates
    /// need is an empty slot, not a failed lookup.
    /// </remarks>
    [TestMethod]
    public void Model_SampleOrderTests_CellSlots_TakeTheRowsCellsInOrderAndAnswerNothingForTheRest()
    {
        var order = new SampleOrder();

        Assert.IsNull(order.Cell0, "A row with no cells still answers a cell for its first slot.");
        Assert.IsNull(order.Cell1, "A row with no cells still answers a cell for its second slot.");
        Assert.IsNull(order.Cell2, "A row with no cells still answers a cell for its third slot.");
        Assert.IsNull(order.Cell3, "A row with no cells still answers a cell for its fourth slot.");

        var first = new WaitlistLineCardCell { Label = "Coil number", Value = "MMC0001000" };
        var second = new WaitlistLineCardCell { Label = "Coil description", Value = "Coil 0.062 x 48 wide" };

        order.Cells.Add(first);
        order.Cells.Add(second);

        Assert.AreSame(first, order.Cell0);
        Assert.AreSame(second, order.Cell1);
        Assert.IsNull(order.Cell2, "A row with two cells answers a cell for its third slot.");
        Assert.IsNull(order.Cell3, "A row with two cells answers a cell for its fourth slot.");

        order.Cells.Add(new WaitlistLineCardCell { Label = "Average coil weight", Value = "5,432 lb" });
        order.Cells.Add(new WaitlistLineCardCell { Label = "Request description", Value = "Changeover review" });

        Assert.AreSame(order.Cells[2], order.Cell2);
        Assert.AreSame(order.Cells[3], order.Cell3);
    }

    /// <summary>
    /// A slot announces itself when the row's cells arrive, so a card bound before its cells are resolved fills
    /// its slots rather than staying empty (FR-025).
    /// </summary>
    [TestMethod]
    public void Model_SampleOrderTests_CellSlots_AnnounceThemselvesWhenTheRowsCellsArrive()
    {
        var order = new SampleOrder();
        var announced = new List<string?>();

        order.PropertyChanged += (_, args) => announced.Add(args.PropertyName);

        order.Cells.Add(new WaitlistLineCardCell { Label = "Coil number", Value = "MMC0001000" });

        CollectionAssert.IsSubsetOf(
            new[] { "Cell0", "Cell1", "Cell2", "Cell3" },
            announced,
            "The row's slots do not announce themselves when its cells arrive, so a card bound first would "
                + "draw no cells at all.");
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
