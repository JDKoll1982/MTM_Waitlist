using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The request page: it takes a note, shows the request's history from the store, and keeps itself current
/// while it is open. The accept / complete / cancel actions deliberately live on the list card, so this page
/// must offer none of them.
/// </summary>
[TestClass]
public sealed class WaitlistViewDetailActionTests
{
    private const string RequesterEmployeeNumber = "6331";
    private const string HandlerRole = "Material Handler";

    private static readonly XNamespace s_presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static readonly DateTimeOffset s_now = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Every file that can carry one of this feature's resource keys.</summary>
    private static readonly string[] s_keyBearingFiles =
    [
        Path.Combine("MTM_Waitlist.Waitlist.View", "ViewModels", "WaitlistViewViewModel.cs"),
        Path.Combine("MTM_Waitlist.Waitlist.View", "ViewModels", "WaitlistViewDetailViewModel.cs"),
        Path.Combine("MTM_Waitlist.Waitlist.View", "Models", "WaitlistRequestAuditEntry.cs"),
        Path.Combine("MTM_Waitlist.Waitlist.View", "Services", "WaitlistRequestService.cs"),
        Path.Combine("Module_Waitlist", "Controls", "WaitlistLineCardView.xaml"),
        Path.Combine("Module_Waitlist", "Views", "WaitlistViewDetailPage.xaml"),
        Path.Combine("Services", "WaitlistRequestActionPrompt.cs"),
    ];

    /// <summary>The resource keys this feature introduces, each of which must be referenced where it is shown.</summary>
    private static readonly string[] s_featureKeys =
    [
        "Waitlist_Action.AcceptRequest",
        "Waitlist_Action.CompleteRequest",
        "Waitlist_Action.CancelRequest",
        "Waitlist_Action.AcceptRefusedTitle",
        "Waitlist_Action.Dismiss",
        "Waitlist_Action_Cancel.DialogTitle",
        "Waitlist_Action_Cancel.DialogMessage",
        "Waitlist_Action_Cancel.Confirm",
        "Waitlist_Action_Cancel.Dismiss",
        "Waitlist_Action_Cancel.ReasonPlaceholder",
        "Waitlist_Action_Refused.AcceptTaken",
        "Waitlist_Action_Refused.AcceptGone",
        "Waitlist_Action_Refused.CompleteOrRelease",
        "Waitlist_Action_Refused.Cancel",
        "Waitlist_Action_Refused.Unexpected",
        "Waitlist_Note.Label",
        "Waitlist_Note.Placeholder",
        "Waitlist_Note.Save",
        "Waitlist_Note.Saved",
        "Waitlist_Note.Empty",
        "Waitlist_Note.Unchanged",
        "Waitlist_Note.Refused",
        "Waitlist_History.SystemActor",
        "Waitlist_History_Event.Accepted",
        "Waitlist_History_Event.Completed",
        "Waitlist_History_Event.Released",
        "Waitlist_History.Title",
        "Waitlist_History.Summary",
        "Waitlist_History.Empty",
        "Waitlist_NewMessages.Tooltip",
    ];

    // ── The page offers no actions ───────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_RequestPage_DrawsNoActionButton()
    {
        // The actions moved to the list card. A button left behind here would be a second place that decides
        // who may do what, which is exactly what the two screens must never do.
        var page = LoadPage();
        var forbidden = new[] { "AcceptRequestCommand", "CompleteRequestCommand", "ReleaseRequestCommand", "CancelRequestCommand" };

        var offenders = page
            .Descendants()
            .SelectMany(element => element.Attributes())
            .Where(attribute => forbidden.Contains(attribute.Value, StringComparer.Ordinal))
            .Select(attribute => attribute.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.AreEqual(
            0,
            offenders.Count,
            $"The request page still binds {string.Join(", ", offenders)}; those actions belong on the list card.");
    }

    // ── The note ─────────────────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_SavingANoteStoresItAndRecordsItInTheHistory()
    {
        var (viewModel, service) = BuildLoadedViewModelWithService();

        viewModel.NoteDraft = "Waiting on the die change";
        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);

        var requestId = viewModel.Item!.RequestId!.Value;
        var history = service.GetAuditTrail(requestId);

        Assert.AreEqual(
            "Waiting on the die change",
            service.GetRequest(requestId)!.Note,
            "The note must be stored on the request.");
        Assert.IsTrue(
            history.Any(entry => entry.EventType == "NoteUpdated"),
            "A note must leave a NoteUpdated entry in the request's history (FR-013).");

        var noteEntry = history.Last(entry => entry.EventType == "NoteUpdated");

        Assert.AreEqual(
            "Waiting on the die change",
            noteEntry.Details,
            "The history row must carry the note itself, not just the fact that a note changed.");
        Assert.AreEqual(
            RequesterEmployeeNumber,
            noteEntry.EmployeeNumber,
            "The history row must say who wrote the note (FR-017).");
        Assert.IsTrue(
            viewModel.HistoryRows.Any(entry => entry.EventType == "NoteUpdated"),
            "The note's history entry must be visible on the page.");
        Assert.AreEqual(
            "Waitlist_Note.Saved".GetLocalized(),
            viewModel.NoteMessage,
            "A saved note must be reported through the resource mechanism.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_SendingAnEmptyMessageSaysThereIsNothingToSend()
    {
        // A click must always answer. The old behaviour returned in silence, which is what made the first click
        // on the page look like a broken button.
        var (viewModel, service) = BuildLoadedViewModelWithService();

        viewModel.NoteDraft = "   ";
        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);

        var requestId = viewModel.Item!.RequestId!.Value;

        Assert.IsNull(service.GetRequest(requestId)!.Note, "A blank draft must not be stored as an empty message.");
        Assert.IsFalse(
            service.GetAuditTrail(requestId).Any(entry => entry.EventType == "NoteUpdated"),
            "A send that changed nothing must not claim a history entry.");
        Assert.AreEqual(
            "Waitlist_Note.Empty".GetLocalized(),
            viewModel.NoteMessage,
            "An empty message box must say there is nothing to send rather than doing nothing visibly.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_SendingTheSameMessageTwiceReportsThatItIsAlreadyThere()
    {
        // The service treats an identical message as no change and writes nothing, so reporting a send here
        // would promise a history row that never appears — the exact confusion this page was reported for.
        var (viewModel, service) = BuildLoadedViewModelWithService();

        viewModel.NoteDraft = "Staged coil is the wrong one";
        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);
        var rowsAfterFirstSend = viewModel.HistoryRows.Count(entry => entry.EventType == "NoteUpdated");

        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.AreEqual(
            "Waitlist_Note.Unchanged".GetLocalized(),
            viewModel.NoteMessage,
            "Re-sending the message already on the request must say so.");
        Assert.AreEqual(
            rowsAfterFirstSend,
            viewModel.HistoryRows.Count(entry => entry.EventType == "NoteUpdated"),
            "Re-sending the same message must not add a second history row.");
        Assert.AreEqual(
            1,
            service.GetAuditTrail(viewModel.Item!.RequestId!.Value).Count(entry => entry.EventType == "NoteUpdated"),
            "The store must hold exactly one entry for the one message that was actually sent.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_ARefreshKeepsAnUnsavedMessageOnScreen()
    {
        // The page reloads every 30 seconds. Seeding the box from the store unconditionally meant a tick could
        // erase what the viewer was typing — which is how a message came to be lost before it was ever sent.
        var (viewModel, _) = BuildLoadedViewModelWithService();

        viewModel.NoteDraft = "Half-written message";
        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual(
            "Half-written message",
            viewModel.NoteDraft,
            "A refresh must not overwrite a message the viewer has not sent yet.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_ARefreshAfterSendingLeavesTheSentMessageInPlace()
    {
        var (viewModel, _) = BuildLoadedViewModelWithService();

        viewModel.NoteDraft = "Sent message";
        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);
        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual("Sent message", viewModel.NoteDraft, "The sent message stays in the box after a refresh.");
    }

    // ── The stream is a conversation: sides, a small-print sender, and no log columns ────────────────

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_RequestPage_AMessageIsDrawnOnItsSendersSide()
    {
        var mine = WaitlistMessageRow.From(
            new WaitlistRequestAuditEntry { EventType = "NoteUpdated", Details = "mine", EmployeeNumber = "1234", EmployeeName = "J. Koll" },
            "1234");
        var theirs = WaitlistMessageRow.From(
            new WaitlistRequestAuditEntry { EventType = "NoteUpdated", Details = "theirs", EmployeeNumber = "9999", EmployeeName = "Hana Handler" },
            "1234");

        Assert.AreEqual(HorizontalAlignment.Right, mine.Alignment, "The signed-in person's own message sits on the right.");
        Assert.AreEqual(HorizontalAlignment.Left, theirs.Alignment, "Everyone else's message sits on the left.");
        Assert.IsTrue(mine.IsMine);
        Assert.IsFalse(theirs.IsMine);
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_RequestPage_TheSenderIsNamedInSmallPrintUnderTheBubble()
    {
        var bubbles = BubbleTemplates().ToArray();

        Assert.AreEqual(2, bubbles.Length, "Each side draws the sender beneath its bubble, so both bubble templates carry the name.");

        foreach (var template in bubbles)
        {
            var name = template
                .Descendants()
                .First(element => element.Attribute("Text")?.Value.Contains("SenderDisplayName", StringComparison.Ordinal) == true);

            Assert.AreEqual("11", name.Attribute("FontSize")?.Value, "The sender is small print, not a second heading.");
        }
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_RequestPage_ABubbleCarriesNeitherATimeNorAnEventType()
    {
        // The thread reads as a conversation. The time and the event type belong to the audit view behind the card's
        // own toggle, so a bubble showing either would turn the conversation back into a log.
        var bubbles = BubbleTemplates().ToArray();

        Assert.AreEqual(2, bubbles.Length, "The bubble templates were not found.");

        foreach (var template in bubbles)
        {
            Assert.IsFalse(
                template.Descendants().Any(element => element.Attribute("Text")?.Value.Contains("OccurredLocalText", StringComparison.Ordinal) == true),
                "A bubble does not carry a timestamp.");
            Assert.IsFalse(
                template.Descendants().Any(element => element.Attribute("Text")?.Value.Contains("EventType", StringComparison.Ordinal) == true),
                "A bubble does not name the event that recorded it.");
        }
    }

    /// <summary>
    /// The two bubble templates, told apart from the audit row by the small print that names the sender. The audit
    /// view names its sender as a bold column, so it is not one of these.
    /// </summary>
    private static IEnumerable<XElement> BubbleTemplates() =>
        LoadPage()
            .Descendants(s_presentation + "DataTemplate")
            .Where(template => template.Descendants().Any(element =>
                element.Attribute("Text")?.Value.Contains("SenderDisplayName", StringComparison.Ordinal) == true
                && element.Attribute("FontSize")?.Value == "11"));

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_TheStreamCarriesOnlyEntriesThatHaveWords()
    {
        // Creating, accepting, completing and cancelling a request are events with nothing to say. With no author or
        // time drawn, such an entry would be an empty box in the thread, so it never joins the stream.
        var service = new StubRequestService(BuildRequest())
        {
            AuditTrail =
            [
                new WaitlistRequestAuditEntry { EventType = "Created" },
                new WaitlistRequestAuditEntry { EventType = "NoteUpdated", Details = "first", EmployeeName = "Hana Handler" },
                new WaitlistRequestAuditEntry { EventType = "Accepted" },
                new WaitlistRequestAuditEntry { EventType = "NoteUpdated", Details = "second", EmployeeName = "Hana Handler" },
            ],
        };
        var viewModel = BuildViewModel(service);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        CollectionAssert.AreEqual(
            new[] { "first", "second" },
            viewModel.MessageStream.Select(row => row.Text).ToArray(),
            "The stream carries the words, oldest first, and nothing else.");
        Assert.AreEqual(4, viewModel.HistoryRows.Count, "The audit trail itself still holds every event.");
        Assert.IsTrue(viewModel.HasMessageStream);
        Assert.IsFalse(viewModel.IsMessageStreamEmpty);
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_TheAuditViewKeepsTheEventsThatCarryNoWords()
    {
        // Accepting, completing and cancelling a request happen without the message box, so they never appear in the
        // thread. They must still be readable, which is what the card's toggle is for.
        var service = new StubRequestService(BuildRequest())
        {
            AuditTrail =
            [
                new WaitlistRequestAuditEntry { EventType = "Created" },
                new WaitlistRequestAuditEntry { EventType = "NoteUpdated", Details = "first", EmployeeNumber = "1234", EmployeeName = "J. Koll" },
                new WaitlistRequestAuditEntry { EventType = "Accepted" },
            ],
        };
        var viewModel = BuildViewModel(service);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual(1, viewModel.MessageStream.Count, "Only the entry with words is a message.");
        Assert.AreEqual(3, viewModel.AuditRows.Count, "Every change is in the audit view, with or without words.");
        Assert.AreEqual("Created", viewModel.AuditRows[0].Text, "An event with no words is named, never left blank.");
        Assert.IsTrue(viewModel.AuditRows[0].IsSystem, "A change nobody made by hand is marked as the system's.");
        Assert.AreEqual("first", viewModel.AuditRows[1].Text, "A note keeps its own words in the audit view too.");
        Assert.IsFalse(viewModel.AuditRows[1].IsSystem);
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_TheToggleSwapsTheThreadForTheAuditAndBack()
    {
        var service = new StubRequestService(BuildRequest())
        {
            AuditTrail = [new WaitlistRequestAuditEntry { EventType = "Created" }],
        };
        var viewModel = BuildViewModel(service);
        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.IsFalse(viewModel.IsAuditVisible, "The card opens on the conversation.");
        Assert.IsTrue(viewModel.CanShowAudit, "The toggle offers the audit view.");
        Assert.IsFalse(viewModel.CanShowMessages, "The toggle back is not drawn while the conversation is showing.");
        Assert.AreEqual("Waitlist_Note.Label".GetLocalized(), viewModel.StreamTitle, "The header names the conversation.");

        viewModel.ShowAuditCommand.Execute(null);

        Assert.IsTrue(viewModel.IsAuditVisible);
        Assert.IsTrue(viewModel.CanShowMessages, "The toggle now offers the conversation.");
        Assert.IsFalse(viewModel.CanShowAudit);
        Assert.AreEqual("Waitlist_Note.AuditLabel".GetLocalized(), viewModel.StreamTitle, "The header names the audit view.");
        Assert.IsTrue(viewModel.HasAuditRows, "The audit view has the event to show.");

        viewModel.ShowMessagesCommand.Execute(null);

        Assert.IsFalse(viewModel.IsAuditVisible, "The toggle returns to the conversation.");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_WaitlistMessageRow_DescribesAnEventInPlainWords()
    {
        Assert.AreEqual("Note updated", WaitlistMessageRow.DescribeEvent("NoteUpdated"));
        Assert.AreEqual("Created", WaitlistMessageRow.DescribeEvent("Created"));
        Assert.AreEqual(string.Empty, WaitlistMessageRow.DescribeEvent(null));
        Assert.AreEqual(string.Empty, WaitlistMessageRow.DescribeEvent("   "));
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_ARequestNobodyHasWrittenOnSaysSo()
    {
        var service = new StubRequestService(BuildRequest())
        {
            AuditTrail = [new WaitlistRequestAuditEntry { EventType = "Created" }],
        };
        var viewModel = BuildViewModel(service);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual(0, viewModel.MessageStream.Count, "An event with no words is not a message.");
        Assert.IsTrue(viewModel.IsMessageStreamEmpty, "The stream draws its quiet line rather than an empty thread.");
        Assert.IsFalse(viewModel.HasMessageStream);

        // The test host has no PRI index, so GetLocalized answers the key itself. The wording is proved by the
        // resource declaring it: a key with no entry behind it would show as bare text on the page.
        var resources = File.ReadAllText(Path.Combine(
            RepositoryPatternScan.FindRepositoryRoot(), "Strings", "en-us", "Resources.resw"));
        StringAssert.Contains(
            resources,
            "Waitlist_Note.StreamEmpty",
            "The quiet line is a real resource, not a key with no entry behind it.");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_HistoryEntry_WithNoAuthorIsAttributedToTheSystem()
    {
        // Job created, accepted, completed and canceled are recorded with no actor. A blank where a name belongs
        // reads as missing data; naming the system reads as the truth.
        var systemEntry = new WaitlistRequestAuditEntry { EventType = "Completed" };
        var humanEntry = new WaitlistRequestAuditEntry { EventType = "NoteUpdated", EmployeeName = "Hana Handler" };

        Assert.AreEqual("Waitlist_History.SystemActor".GetLocalized(), systemEntry.ActorDisplayName);
        Assert.AreEqual("Hana Handler", humanEntry.ActorDisplayName);
    }

    // ── Anyone can send a message ───────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_AnyoneSignedInCanSendAMessage()
    {
        // The message box is how the floor talks to whoever picks the request up, so it is not gated on the
        // handler role the way the card's actions are.
        var (viewModel, service) = BuildLoadedViewModelWithService(role: "Production");

        viewModel.NoteDraft = "Front desk is holding the paperwork";
        await viewModel.SaveNoteCommand.ExecuteAsync(null).ConfigureAwait(false);

        Assert.AreEqual(
            "Waitlist_Note.Saved".GetLocalized(),
            viewModel.NoteMessage,
            "A signed-in user who is not a material handler must be able to send a message.");
        Assert.AreEqual(
            "Front desk is holding the paperwork",
            service.GetRequest(viewModel.Item!.RequestId!.Value)!.Note,
            "The message must actually be stored, not merely reported as sent.");
    }

    // ── The history comes from the store ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_ReadsTheHistoryFromTheStoreNotJustThisSession()
    {
        // The store's read is what makes the history cover activity from before the app started. A stub returns
        // an entry this session never created, and the page must render it.
        var entry = new WaitlistRequestAuditEntry
        {
            EventType = "Accepted",
            EmployeeName = "Hana Handler",
            EmployeeNumber = "9001",
            OccurredUtc = s_now.AddHours(-2),
        };

        var service = new StubRequestService(BuildRequest()) { AuditTrail = [entry] };
        var viewModel = BuildViewModel(service);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual(1, viewModel.HistoryRows.Count, "The page must render the history the store returned.");
        Assert.AreEqual("Accepted", viewModel.HistoryRows[0].EventType);
        Assert.IsFalse(viewModel.IsHistoryEmpty, "A history with rows is not empty.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_WithNoHistorySaysSoRatherThanHiding()
    {
        // "Nothing has happened yet" must stay distinguishable from "the history could not be read".
        var service = new StubRequestService(BuildRequest()) { AuditTrail = [] };
        var viewModel = BuildViewModel(service);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.AreEqual(0, viewModel.HistoryRows.Count);
        Assert.IsTrue(viewModel.IsHistoryEmpty, "An empty history must be reported as empty, not hidden.");
        Assert.AreEqual(
            "Waitlist_History.Empty".GetLocalized(),
            viewModel.HistoryEmptyText,
            "The empty line must be the localized copy.");
    }

    // ── Keeping the page current ─────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_RequestPage_RefreshIntervalIsThirtySeconds()
    {
        Assert.AreEqual(
            TimeSpan.FromSeconds(30),
            WaitlistViewDetailViewModel.RefreshInterval,
            "The page refreshes on a 30-second cadence while it is open.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewDetailActionTests_RequestPage_OpeningItMarksTheRequestsActivitySeen()
    {
        // Reading the request's history is what clears its new-message indicator on the card. The marker is the
        // newest entry, so anything that arrives afterwards re-flags the card.
        var newest = s_now.AddMinutes(-5);
        var entry = new WaitlistRequestAuditEntry { EventType = "NoteUpdated", OccurredUtc = newest };
        var seen = new RecordingMessageSeenStore();
        var viewModel = BuildViewModel(new StubRequestService(BuildRequest()) { AuditTrail = [entry] }, seen);

        await viewModel.LoadHistoryAsync().ConfigureAwait(false);

        Assert.IsTrue(seen.Marks.Count > 0, "Opening the request must record that its activity was seen.");
        Assert.IsTrue(
            seen.Marks.All(mark => mark.SeenUtc == newest),
            "The marker must be the newest entry, not the current clock.");
        Assert.IsTrue(
            seen.Marks.All(mark => mark.RequestId == viewModel.Item!.RequestId),
            "The marker must be recorded against the request whose history was read.");
    }

    // ── Localization ─────────────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ViewModel_WaitlistViewDetailActionTests_EveryNewString_ResolvesThroughTheResourceFileAndIsReferencedWhereItIsShown()
    {
        var root = RepositoryPatternScan.FindRepositoryRoot();
        var resourcePath = Path.Combine(root, "Strings", "en-us", "Resources.resw");

        Assert.IsTrue(File.Exists(resourcePath), $"The resource file was not found at '{resourcePath}'.");

        var document = XDocument.Load(resourcePath);
        var values = document
            .Descendants("data")
            .Where(data => data.Attribute("name") is not null)
            .ToDictionary(
                data => (string)data.Attribute("name")!,
                data => (string?)data.Element("value") ?? string.Empty,
                StringComparer.Ordinal);

        var source = string.Join(
            "\n",
            s_keyBearingFiles.Select(file => File.ReadAllText(Path.Combine(root, file))));

        foreach (var key in s_featureKeys)
        {
            Assert.IsTrue(values.ContainsKey(key), $"The resource key '{key}' is missing from Resources.resw.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(values[key]), $"The resource key '{key}' has no copy.");
            Assert.IsTrue(
                source.Contains(key, StringComparison.Ordinal),
                $"The resource key '{key}' is never referenced, so nothing shows it.");
        }
    }

    // ── Fixtures ─────────────────────────────────────────────────────────────────────────────────────

    private static XDocument LoadPage() => XDocument.Load(Path.Combine(
        RepositoryPatternScan.FindRepositoryRoot(),
        "Module_Waitlist",
        "Views",
        "WaitlistViewDetailPage.xaml"));

    private static WaitlistRequest BuildRequest() => new()
    {
        Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        WorkCenterName = "Expo Line 7",
        Category = "Pickup",
        Item = "pickup-ncm",
        InputValue = "Skid 4471 is on the wrong dock",
        ActiveSetupJobId = "JOB-9001",
        RequesterEmployeeNumber = RequesterEmployeeNumber,
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = s_now.AddMinutes(-20),
        TargetTimeUtc = s_now.AddMinutes(40),
        UpdatedUtc = s_now,
    };

    private static WaitlistViewDetailViewModel BuildViewModel(
        IWaitlistRequestService service,
        IWaitlistMessageSeenStore? seenStore = null)
    {
        var viewModel = new WaitlistViewDetailViewModel(
            new WaitlistTestNavigationService(),
            new WaitlistTestBuildingSelectionService(),
            imageLocationService: null,
            requestService: service,
            inventoryService: null,
            personIdentity: new FakePersonIdentity
            {
                EmployeeNumber = RequesterEmployeeNumber,
                DisplayName = "Morgan Reyes",
                CurrentRoleCode = HandlerRole,
            },
            dispatcherQueue: null,
            messageSeenStore: seenStore);

        // Navigate in and straight back out, so the 30-second refresh timer never runs in a headless host.
        viewModel.OnNavigatedTo(WaitlistViewViewModel.CreateSessionOrder(BuildRequest()).Id);
        viewModel.OnNavigatedFrom();
        return viewModel;
    }

    private static (WaitlistViewDetailViewModel ViewModel, WaitlistRequestService Service) BuildLoadedViewModelWithService(string role = HandlerRole)
    {
        var service = new WaitlistRequestService();
        var submit = service
            .SubmitAsync(
                new WaitlistRequestDraft
                {
                    Building = "Expo Drive",
                    WorkCenter = "Expo Line 7",
                    Category = "Pickup",
                    Item = "pickup-ncm",
                    InputValue = "Skid 4471 is on the wrong dock",
                    ActiveSetupJobId = "JOB-9001",
                    WorkCenterName = "Expo Line 7",
                    RequesterEmployeeNumber = RequesterEmployeeNumber,
                    RequesterEmployeeName = "Dana Whitfield",
                })
            .GetAwaiter()
            .GetResult();

        Assert.IsNotNull(submit.Request, "Submitting the fixture request failed.");

        var order = WaitlistViewViewModel.CreateSessionOrder(submit.Request!);
        var viewModel = new WaitlistViewDetailViewModel(
            new WaitlistTestNavigationService(),
            new WaitlistTestBuildingSelectionService(),
            imageLocationService: null,
            requestService: service,
            inventoryService: null,
            personIdentity: new FakePersonIdentity
            {
                EmployeeNumber = RequesterEmployeeNumber,
                DisplayName = "Morgan Reyes",
                CurrentRoleCode = role,
            },
            dispatcherQueue: null,
            messageSeenStore: new RecordingMessageSeenStore());

        viewModel.OnNavigatedTo(order.Id);
        viewModel.OnNavigatedFrom();
        return (viewModel, service);
    }

    /// <summary>A request service whose history read returns exactly what a test hands it.</summary>
    private sealed class StubRequestService : IWaitlistRequestService
    {
        private readonly WaitlistRequest _request;

        public StubRequestService(WaitlistRequest request) => _request = request;

        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        /// <summary>What the store's history read returns.</summary>
        public IReadOnlyList<WaitlistRequestAuditEntry> AuditTrail { get; set; } = [];

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [_request];

        public WaitlistRequest? GetRequest(Guid requestId) => requestId == _request.Id ? _request : null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [_request];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(AuditTrail);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(WaitlistRequestDraft draft, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> TransitionStatusAsync(Guid requestId, string status, string? cancellationReason = null, string? canceledByEmployeeNumber = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(Guid requestId, string requesterEmployeeNumber, string? reason = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<WaitlistRequest?> UpdateNoteAsync(Guid requestId, string? note, string? actorEmployeeNumber = null, string? actorEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> CompleteAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(Guid requestId, string handlerEmployeeNumber, string? handlerEmployeeName = null, CancellationToken cancellationToken = default)
            => Task.FromResult<WaitlistRequest?>(null);

        public Task<int> SetHandlerStatusAsync(Guid requestId, string? handlerStatus, string expectedMaterialHandler, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    /// <summary>Records what the page marked as seen, instead of writing to a settings file.</summary>
    private sealed class RecordingMessageSeenStore : IWaitlistMessageSeenStore
    {
        public List<(Guid RequestId, DateTimeOffset SeenUtc)> Marks { get; } = [];

        public Task<DateTimeOffset?> GetLastSeenUtcAsync(Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task MarkSeenAsync(Guid requestId, DateTimeOffset seenUtc, CancellationToken cancellationToken = default)
        {
            Marks.Add((requestId, seenUtc));
            return Task.CompletedTask;
        }
    }
}
