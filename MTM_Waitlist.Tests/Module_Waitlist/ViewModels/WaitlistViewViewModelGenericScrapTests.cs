using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.Services;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// The stated type survives the arrangement changing: the wording the requester gave lives on the request's own
/// answers, while the lugger is a live read, so neither half can be copied onto the other (FR-011, FR-012, FR-013).
/// </summary>
/// <remarks>
/// Every case goes through the board's own load, so a change that starts rewriting the stored answer, or starts
/// caching the lugger on the request, fails here rather than in the field.
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelGenericScrapTests
{
    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelGenericScrapTests_ATypeRemovedFromTheArrangementKeepsItsWordingAndReportsNotAssigned()
    {
        var board = Board("Copper", _ => new StatedScrapLugger(null, string.Empty, ArrangementRead: true));

        await board.RefreshAsync().ConfigureAwait(false);

        var card = Row(board);

        Assert.AreEqual("Copper", Cell(card, "Scrap Type").Value, "The type the requester stated is what the card names.");
        AssertWording("WaitlistRequest_Scrap_LuggerNotAssigned.Text", Cell(card, "Lugger Location").Value);
        CollectionAssert.AreEqual(
            new[] { "Copper" },
            board.Provider.StatedReads,
            "The stored wording is what was resolved, not a spelling the arrangement now prefers.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelGenericScrapTests_AnOldSpellingStillShowsAfterTheArrangementRenamesTheType()
    {
        var board = Board("5052 aluminum", _ => new StatedScrapLugger(null, string.Empty, ArrangementRead: true));

        await board.RefreshAsync().ConfigureAwait(false);

        var card = Row(board);

        Assert.AreEqual(
            "5052 aluminum",
            Cell(card, "Scrap Type").Value,
            "A rename in the arrangement leaves what the plant was told exactly as it was stated (FR-011).");
        Assert.AreNotEqual(
            "5052 Aluminium",
            Cell(card, "Scrap Type").Value,
            "The card does not adopt the arrangement's new spelling on the request's behalf.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelGenericScrapTests_AReassignedTypeDrawsTheLuggerAssignedNowNotTheOneBefore()
    {
        var board = Board("Steel", _ => new StatedScrapLugger("Door 6 Lugger", string.Empty, ArrangementRead: true));
        await board.RefreshAsync().ConfigureAwait(false);

        Assert.AreEqual("Door 6 Lugger", Cell(Row(board), "Lugger Location").Value, "The first load names the lugger as it stood.");

        // The dump point moves. Nothing about the request changes; only the arrangement did.
        board.Provider.Answer = _ => new StatedScrapLugger("Door 7 Lugger", string.Empty, ArrangementRead: true);
        await board.RefreshAsync().ConfigureAwait(false);

        Assert.AreEqual(
            "Door 7 Lugger",
            Cell(Row(board), "Lugger Location").Value,
            "The dump point is a live fact, so the next load names the lugger assigned now (FR-012).");
        Assert.AreEqual(2, board.Provider.StatedReads.Count, "The lugger is read on every load, never kept on the request.");
    }

    [TestMethod]
    public async Task ViewModel_WaitlistViewViewModelGenericScrapTests_ATypeOutsideTheSharedListStillDrawsWithItsTypeNamed()
    {
        var board = Board("Swarf", _ => new StatedScrapLugger("Scrap Trailer", string.Empty, ArrangementRead: true));

        await board.RefreshAsync().ConfigureAwait(false);

        var card = Row(board);

        Assert.AreEqual("Swarf", Cell(card, "Scrap Type").Value, "A type the shared list does not hold is still the type stated (FR-013).");
        Assert.AreEqual("Scrap Trailer", Cell(card, "Lugger Location").Value, "Its lugger is resolved like any other type's.");
    }

    /// <summary>A board showing one generic scrap request, and the arrangement the stated read is answered from.</summary>
    private static BoardState Board(string statedType, Func<string, StatedScrapLugger> answer) => new(statedType, answer);

    private static SampleOrder Row(BoardState board) => board.ViewModel.Source.Single();

    private static WaitlistLineCardCell Cell(SampleOrder row, string label) =>
        row.Cells.Single(cell => string.Equals(cell.Label, label, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Asserts a card value is a key's wording. The test host has no resource index, so the key resolves to itself
    /// there; the resw is checked for the key as well, which is what makes a missing resource fail here.
    /// </summary>
    private static void AssertWording(string key, string value)
    {
        Assert.AreEqual(key.GetLocalized(), value, $"The cell reads '{key}', so what it draws is that key's wording.");
        StringAssert.Contains(
            File.ReadAllText(Path.Combine(
                RepositoryPatternScan.FindRepositoryRoot(),
                "Strings", "en-us", "Resources.resw")),
            $"name=\"{key}\"",
            $"The application's resources declare no '{key}', so the card would draw a bare key.");
    }

    private sealed class BoardState
    {
        public BoardState(string statedType, Func<string, StatedScrapLugger> answer)
        {
            Provider = new ScrapProvider(answer);
            ViewModel = new WaitlistViewViewModel(
                new WaitlistTestNavigationService(),
                new WaitlistTestBuildingSelectionService(),
                new OneRequestService(GenericScrap(statedType)),
                jobAvailabilityProvider: Provider,
                itemConfigurationService: new ScrapDeclarationService());
        }

        public ScrapProvider Provider { get; }

        public WaitlistViewViewModel ViewModel { get; }

        public Task RefreshAsync() => ViewModel.RefreshAsync();
    }

    /// <summary>The scrap Item's two declared cells, which is where the type and the lugger are drawn.</summary>
    private sealed class ScrapDeclarationService : IRequestItemConfigurationService
    {
        private static readonly RequestItemConfiguration s_declaration = new()
        {
            Item = "pickup-scrap",
            DetailFields =
            [
                new RequestItemFieldDefinition { Label = "Scrap Type", Source = RequestItemFieldDefinition.Sources.Job, Order = 1 },
                new RequestItemFieldDefinition { Label = "Lugger Location", Source = RequestItemFieldDefinition.Sources.Job, Order = 2 },
            ],
        };

        public Task<RequestItemConfigurationSet> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(RequestItemConfigurationSet.From([s_declaration]));

        public Task<RequestItemConfiguration> GetConfigurationAsync(string itemCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(s_declaration);
    }

    /// <summary>The stated read, answering whatever the test currently says the arrangement holds.</summary>
    private sealed class ScrapProvider : IRequestJobPartAvailabilityProvider
    {
        public ScrapProvider(Func<string, StatedScrapLugger> answer) => Answer = answer;

        public Func<string, StatedScrapLugger> Answer { get; set; }

        public List<string> StatedReads { get; } = [];

        public Task<RequestJobPartAvailability> GetAvailabilityAsync(string workCenter, CancellationToken cancellationToken = default) =>
            Task.FromResult(RequestJobPartAvailability.None);

        public Task<StatedScrapLugger> GetStatedScrapLuggerAsync(string scrapType, CancellationToken cancellationToken = default)
        {
            StatedReads.Add(scrapType);

            return Task.FromResult(Answer(scrapType));
        }
    }

    /// <summary>The queue, holding the one request under test.</summary>
    private sealed class OneRequestService(WaitlistRequest request) : IWaitlistRequestService
    {
        public event EventHandler? RequestsChanged
        {
            add { }
            remove { }
        }

        public IReadOnlyList<WaitlistRequest> GetActiveRequests(string? building = null) => [request];

        public WaitlistRequest? GetRequest(Guid requestId) => request.Id == requestId ? request : null;

        public IReadOnlyList<WaitlistRequest> GetMyRequests(string requesterEmployeeNumber, string? building = null) => [];

        public IReadOnlyList<WaitlistRequestAuditEntry> GetAuditTrail(Guid requestId) => [];

        public Task<IReadOnlyList<WaitlistRequestAuditEntry>> LoadAuditTrailAsync(
            Guid requestId,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<WaitlistRequestAuditEntry>>([]);

        public void Reset()
        {
        }

        public Task<int> RefreshFromDatabaseAsync(string? building = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<WaitlistRequestSubmitResult> SubmitAsync(
            WaitlistRequestDraft draft,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This test only reads the board.");

        public Task<bool> TransitionStatusAsync(
            Guid requestId,
            string status,
            string? cancellationReason = null,
            string? canceledByEmployeeNumber = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<WaitlistRequestCancelResult> CancelOwnRequestAsync(
            Guid requestId,
            string requesterEmployeeNumber,
            string? reason = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("This test only reads the board.");

        public Task<WaitlistRequest?> UpdateNoteAsync(
            Guid requestId,
            string? note,
            string? actorEmployeeNumber,
            string? actorEmployeeName,
            CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> AcceptAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> CompleteAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<WaitlistRequest?> ReleaseAsync(
            Guid requestId,
            string handlerEmployeeNumber,
            string? handlerEmployeeName = null,
            CancellationToken cancellationToken = default) => Task.FromResult<WaitlistRequest?>(null);

        public Task<int> SetHandlerStatusAsync(
            Guid requestId,
            string? handlerStatus,
            string expectedMaterialHandler,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    /// <summary>A generic scrap request carrying a stated type in its own answers, as a raise records it.</summary>
    private static WaitlistRequest GenericScrap(string statedType) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        Category = RequestItemCatalog.FindById("pickup-scrap")!.Category.ToString(),
        Item = "pickup-scrap",
        IsGeneric = true,
        Answers = new GenericRequestScrapAnswer(statedType).ToAnswers(),
        Status = "Pending",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };
}
