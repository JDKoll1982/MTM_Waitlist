using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Fixtures;
using MTM_Waitlist.Tests.Module_GenericRequest;

namespace MTM_Waitlist.Tests.Module_Waitlist;

/// <summary>
/// The two cells a generic scrap request's card draws: the type the requester stated, and where it goes.
/// </summary>
/// <remarks>
/// A generic work center has no job, so neither fact can be read from one. The type is the wording the requester
/// kept on the request and the lugger is resolved from the arrangement read for the load, through the same rule the
/// work-order card uses, so the two cards cannot name different luggers for one type (FR-006, FR-007).
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelScrapCardTests
{
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_AGenericCardNamesTheStatedTypeAndItsLuggerWithNoJob()
    {
        var card = CreateCard(
            "3003 Aluminum",
            Arrangement(
                Row("3003 Aluminum", "Door 6 Lugger", LuggerStatus.Available, sortPosition: 1),
                Row("Steel", "Door 2 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreEqual("3003 Aluminum", Cell(card, "Scrap Type")?.Value);
        Assert.AreEqual(
            "Door 6 Lugger",
            Cell(card, "Lugger Location")?.Value,
            "The card must name the lugger the arrangement assigns to the stated type (FR-006).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_TheLuggerAGenericCardNamesIsTheOneTheSharedRuleNames()
    {
        // Two luggers can take this type, and the dump point is the second of them. A card that named the first
        // lugger would print `Door 5 Lugger`, so this arrangement is what tells the shared rule apart from a guess.
        var arrangement = Arrangement(
            Row("3003 Aluminum", "Door 5 Lugger", LuggerStatus.Available, isDumpPoint: false, sortPosition: 1),
            Row("3003 Aluminum", "Door 6 Lugger", LuggerStatus.Available, sortPosition: 2));

        var generic = CreateCard("3003 Aluminum", arrangement);
        var workOrder = WaitlistViewViewModel.CreateSessionOrder(
            Shape("3003 Aluminum", isGeneric: false),
            RequestJobPartAvailability.All with { ScrapType = "3003 Aluminum", LuggerLocation = "Door 6 Lugger" },
            Declaration());

        Assert.AreEqual(
            "Door 6 Lugger",
            Cell(generic, "Lugger Location")?.Value,
            "The generic card resolves through the arrangement, so it must name the dump point (FR-007, SC-003).");
        Assert.AreEqual(
            Cell(workOrder, "Lugger Location")?.Value,
            Cell(generic, "Lugger Location")?.Value,
            "One scrap type has one lugger, whichever kind of card names it (FR-007, SC-003).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_ATypeWhoseLuggersAreAllBlockedNamesTheFirstWithItsStatus()
    {
        var card = CreateCard(
            "Steel",
            Arrangement(
                Row("Steel", "Door 9 Lugger", LuggerStatus.Full, sortPosition: 1),
                Row("Steel", "Door 10 Lugger", LuggerStatus.DoNotUse, false, sortPosition: 2)));

        Assert.AreEqual("Steel", Cell(card, "Scrap Type")?.Value);
        Assert.AreEqual(
            "Door 9 Lugger (Full)",
            Cell(card, "Lugger Location")?.Value,
            "A blocked type still names a lugger, with its status beside the name, rather than drawing blank (FR-008).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_ATypeWithNoLuggerAssignedRecordsTheGapAndDrawsNoLugger()
    {
        var sink = new RecordingAppLogSink();
        AppLog.Configure(sink);

        try
        {
            var card = CreateCard(
                "Skeleton",
                Arrangement(Row("Steel", "Door 2 Lugger", LuggerStatus.Available, sortPosition: 1)));

            Assert.AreEqual("Skeleton", Cell(card, "Scrap Type")?.Value);
            Assert.IsNull(
                Cell(card, "Lugger Location"),
                "A missing assignment is a configuration gap, never an invented lugger (FR-009).");
            Assert.AreEqual(1, sink.Warnings.Count, "The gap is recorded once, so the arrangement can be corrected.");
            StringAssert.Contains(
                sink.Warnings[0],
                "Skeleton",
                "The record names the type that has no lugger, or nobody can act on it (FR-009).");
        }
        finally
        {
            AppLog.Configure(null);
        }
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_AnUnreadableArrangementStillNamesTheTypeAndReportsNoLugger()
    {
        var card = CreateCard("Galvanized Steel", arrangement: null);

        Assert.AreEqual("Galvanized Steel", Cell(card, "Scrap Type")?.Value);
        var lugger = Cell(card, "Lugger Location");
        Assert.IsNotNull(lugger, "The card must still report the lugger it could not resolve (FR-010).");
        StringAssert.Contains(
            lugger!.Value,
            "Waitlist_Card_LuggerUnavailable",
            "A lugger nobody could read is reported as unavailable, never as a stale or guessed name (FR-010).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapCardTests_CreateSessionOrder_AGenericCardNeverBorrowsTheJobsLuggerArtwork()
    {
        // The work centre happens to be running a job whose scrap has its own lugger picture. That picture belongs
        // to the job's scrap type, so a generic request must draw the shared placeholder instead of it (FR-006).
        var card = WaitlistViewViewModel.CreateSessionOrder(
            Shape("Galvanized Steel", isGeneric: true),
            RequestJobPartAvailability.All with
            {
                ScrapType = "3003 Aluminum",
                LuggerLocation = "Door 6 Lugger",
                LuggerLocationImagePath = @"C:\pictures\Lugger06.png",
            },
            Declaration(),
            Arrangement(Row("Galvanized Steel", "Door 4 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreNotEqual(
            @"C:\pictures\Lugger06.png",
            card.ResolvedPartImagePath,
            "Another job's lugger artwork would send the handler to the wrong lugger (FR-006).");
    }

    private static WaitlistLineCardCell? Cell(SampleOrder card, string label) =>
        WaitlistLineCardCell.CellsToDraw(card.Cells)
            .SingleOrDefault(cell => string.Equals(cell.Label, label, StringComparison.OrdinalIgnoreCase));

    private static SampleOrder CreateCard(string statedScrapType, LuggerArrangementSnapshot? arrangement) =>
        WaitlistViewViewModel.CreateSessionOrder(
            Shape(statedScrapType, isGeneric: true),
            jobAvailability: null,
            Declaration(),
            arrangement);

    private static WaitlistRequest Shape(string statedScrapType, bool isGeneric) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = "Scrap",
        Item = "pickup-scrap",
        IsGeneric = isGeneric,
        Answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [GenericRequestScrapAnswer.ScrapTypeKey] = statedScrapType,
        },
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };

    private static RequestItemConfiguration Declaration() => new()
    {
        Item = "pickup-scrap",
        DetailFields =
        [
            new RequestItemFieldDefinition
            {
                Label = "Scrap Type",
                Source = RequestItemFieldDefinition.Sources.Job,
                Order = 1,
            },
            new RequestItemFieldDefinition
            {
                Label = "Lugger Location",
                Source = RequestItemFieldDefinition.Sources.Job,
                Order = 2,
            },
        ],
    };

    private static LuggerArrangementSnapshot Arrangement(params ScrapAssignment[] rows) =>
        LuggerArrangementSnapshot.Create(rows);

    /// <summary>
    /// The screen itself, not the resolver: a load with a real arrangement reader must put the lugger on the row.
    /// </summary>
    /// <remarks>
    /// Every other case in this file calls <c>CreateSessionOrder</c> directly, so a load that forgot to read the
    /// arrangement or forgot to pass it on would leave them all green while every generic scrap card on the board
    /// named no lugger (FR-006).
    /// </remarks>
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelScrapCardTests_RefreshAsync_ALoadedGenericScrapRowNamesItsLugger()
    {
        var requests = new WaitlistRequestService();
        var viewModel = new WaitlistViewViewModel(
            new NoOpNavigationService(),
            new StubBuildingSelectionService("Expo Drive"),
            requests,
            personIdentity: new FakePersonIdentity { EmployeeNumber = "6331" },
            itemConfigurationService: new SingleDeclaration(),
            luggerArrangementService: new FakeLuggerArrangementService
            {
                Snapshot = Arrangement(Row("Steel", "Door 6 Lugger", LuggerStatus.Available, sortPosition: 1)),
            });

        viewModel.OnNavigatedTo(null!);
        await viewModel.RefreshAsync();

        await requests.SubmitAsync(GenericScrapDraft("Steel"));
        await Task.Delay(50);

        var row = viewModel.Source.Single(order => string.Equals(order.ItemCode, "pickup-scrap", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(
            "Door 6 Lugger",
            Cell(row, "Lugger Location")?.Value,
            "The load must read the arrangement and hand it to the card, or the screen names no lugger (FR-006).");
    }

    private static WaitlistRequestDraft GenericScrapDraft(string scrapType) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = "Scrap",
        Item = "pickup-scrap",
        IsGeneric = true,
        Answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [GenericRequestScrapAnswer.ScrapTypeKey] = scrapType,
        },
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
    };

    /// <summary>The one Item this file cares about, served to the screen's own declaration read.</summary>
    private sealed class SingleDeclaration : IRequestItemConfigurationService
    {
        public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RequestItemConfigurationSet.From([Declaration()]));

        public Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(Declaration());
    }

    private sealed class NoOpNavigationService : INavigationService
    {
        public event Microsoft.UI.Xaml.Navigation.NavigatedEventHandler? Navigated
        {
            add { }
            remove { }
        }

        public bool CanGoBack => false;

        public Microsoft.UI.Xaml.Controls.Frame? Frame { get; set; }

        public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false) => true;

        public bool GoBack() => true;

        public void SetListDataItemForNextConnectedAnimation(object item)
        {
        }
    }

    private sealed class StubBuildingSelectionService : IBuildingSelectionService
    {
        public StubBuildingSelectionService(string selectedBuilding) => SelectedBuilding = selectedBuilding;

        public event EventHandler? BuildingChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? BuildingsChanged
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<string> Buildings => ["Expo Drive", "VITS"];

        public string SelectedBuilding { get; set; }
    }

    private static ScrapAssignment Row(
        string scrapType,
        string luggerName,
        string luggerStatus,
        bool isDumpPoint = true,
        int sortPosition = 1) =>
        new()
        {
            LuggerPublicId = $"LUG-{luggerName}",
            LuggerName = luggerName,
            LuggerStatus = luggerStatus,
            LuggerDisplayPosition = sortPosition,
            ScrapType = scrapType,
            ScrapTypeKey = scrapType.Trim().ToLowerInvariant(),
            IsDumpPoint = isDumpPoint,
            SortPosition = sortPosition,
        };

    /// <summary>Captures what the facade forwards to the store's log, so a warning can be observed without a store.</summary>
    private sealed class RecordingAppLogSink : IAppLogSink
    {
        public List<string> Warnings { get; } = [];

        public void Info(string module, string message, string? target = null)
        {
        }

        public void Error(string module, string message, Exception? exception = null)
        {
        }

        public void Warn(string module, string message, string? errorType = null) => Warnings.Add(message);
    }
}
