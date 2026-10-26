using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Settings.Services;
using MTM_Waitlist.Module_Settings.ViewModels;
using MTM_Waitlist.Tests.Fixtures;

namespace MTM_Waitlist.Tests.Module_Settings.ViewModels;

/// <summary>
/// The lugger arrangement's editor (FR-001, FR-003, FR-011, FR-019, FR-028 to FR-032). The summary is read once,
/// every action reaches its own service method with the signed-in actor, and the two refusals that never reach the
/// store — a blank reason on <c>Do Not Use</c>, and a person without the key — are made here.
/// </summary>
[TestClass]
public sealed class LuggerArrangementViewModelTests
{
    private const string Door6 = "lugger-door-6";
    private const string Door7 = "lugger-door-7";
    private const string Door9 = "lugger-door-9";
    private const string Extra = "lugger-extra";
    private const string Trailer = "lugger-trailer";

    private const string ActorNumber = "4417";
    private const string ActorName = "Dana Bruck";

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_InitializeAsync_ListsEveryLuggerWithItsStatusAndScrapTypes()
    {
        var (_, viewModel) = await ArrangeAsync();

        Assert.AreEqual(5, viewModel.Luggers.Count, "Every lugger the store holds is listed, including one taking no scrap type.");

        var door6 = viewModel.Luggers.Single(row => row.PublicId == Door6);
        Assert.AreEqual("Door 6 Lugger", door6.Name);
        Assert.AreEqual(LuggerStatus.Available, door6.Status, "The status shown is the stored one.");
        CollectionAssert.AreEqual(new[] { "3003 Aluminum" }, door6.ScrapTypes.ToArray(), "A lugger names the scrap types it takes.");

        var trailer = viewModel.Luggers.Single(row => row.PublicId == Trailer);
        Assert.AreEqual(LuggerStatus.DoNotUse, trailer.Status);
        Assert.AreEqual("Towed away", trailer.StatusReason, "The reason a lugger is out of use is readable from the summary.");
        Assert.IsFalse(trailer.HasScrapTypes, "A lugger taking no scrap type says so rather than being left blank.");
        Assert.AreEqual(viewModel.NoScrapTypesText, trailer.ScrapTypesText, "The empty answer is the shipped sentence.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_InitializeAsync_ShowsWhichLuggerHoldsEachScrapTypeDumpPoint()
    {
        var (_, viewModel) = await ArrangeAsync();

        var steel = viewModel.ScrapTypes.Single(row => row.ScrapTypeKey == "galvanized steel");
        Assert.AreEqual("Door 7 Lugger", steel.DumpPointName, "Each scrap type names the lugger handlers walk to (FR-031).");

        var copper = viewModel.ScrapTypes.Single(row => row.ScrapTypeKey == "copper");
        Assert.AreEqual("Door 9 Lugger (Full)", copper.DumpPointName, "A type whose only lugger cannot receive names it with its status.");
    }

    /// <summary>
    /// A card dragged to another place asks the store for that place, naming the lugger that moved and the
    /// 1-based position it was dropped at (FR-001).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_MoveLuggerAsync_AsksTheStoreForTheDroppedPlace()
    {
        var (service, viewModel) = await ArrangeAsync();

        await viewModel.MoveLuggerAsync((Extra, 1));

        var write = service.Placements.Single();
        Assert.AreEqual(Extra, write.PublicId, "The write names the lugger that was dragged.");
        Assert.AreEqual(1, write.DisplayPosition, "The write asks for the place it was dropped at.");
        Assert.AreEqual(ActorNumber, write.ActorEmployeeNumber);
        Assert.AreEqual(ActorName, write.ActorEmployeeName);
    }

    /// <summary>
    /// A drag that names no lugger, or a reader who may not write, changes nothing: the store is never asked to
    /// renumber the arrangement on the strength of a gesture that did not move anything.
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_MoveLuggerAsync_WithoutALuggerOrKey()
    {
        var (service, viewModel) = await ArrangeAsync();

        await viewModel.MoveLuggerAsync((string.Empty, 2));
        Assert.AreEqual(0, service.Placements.Count, "A drag that names no lugger is not a reorder.");

        viewModel.IsEntitled = false;
        await viewModel.MoveLuggerAsync((Extra, 1));
        Assert.AreEqual(0, service.Placements.Count, "A reader without the key cannot reorder the arrangement.");
    }

    /// <summary>
    /// "Make dump point" is offered only when it can do something: the chosen type must have a backup assigned and
    /// this lugger must not already hold the role (FR-006, FR-018).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_CanPromote_NeedsABackupAndALuggerThatIsNotTheDumpPoint()
    {
        var (_, viewModel) = await ArrangeAsync();

        // 3003 Aluminum has two luggers: Door 6 holds the dump point and Extra is its backup.
        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Extra));
        viewModel.ScrapTypeInput = "3003 Aluminum";
        Assert.IsTrue(viewModel.CanPromote, "A backup beside the dump point can be promoted.");

        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door6));
        viewModel.ScrapTypeInput = "3003 Aluminum";
        Assert.IsFalse(viewModel.CanPromote, "The lugger already holding the dump point has nothing to promote.");

        // Galvanized Steel has one lugger, so there is no backup to promote ahead of.
        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door7));
        viewModel.ScrapTypeInput = "Galvanized Steel";
        Assert.IsFalse(viewModel.CanPromote, "With no backup assigned, the action would offer a move that cannot happen.");

        // And a lugger that does not take the type at all cannot be its dump point.
        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door9));
        viewModel.ScrapTypeInput = "3003 Aluminum";
        Assert.IsFalse(viewModel.CanPromote, "A lugger the type is not assigned to is not its dump point either.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_UnassignScrapTypeAsync_PassesTheNormalisedKeyAndTheActor()
    {
        var (service, viewModel) = await ArrangeAsync();

        viewModel.SelectedLugger = viewModel.Luggers.Single(row => row.PublicId == Extra);
        viewModel.ScrapTypeInput = "  3003 ALUMINUM ";
        await viewModel.UnassignScrapTypeAsync();

        var write = service.Unassignments.Single();
        Assert.AreEqual("3003 aluminum", write.ScrapTypeKey, "The store matches on the trimmed, lower-cased key (FR-009).");
        Assert.AreEqual(Extra, write.PublicId);
        Assert.AreEqual(ActorNumber, write.ActorEmployeeNumber);
        Assert.AreEqual(ActorName, write.ActorEmployeeName);
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_PromoteLuggerAsync_PassesTheNormalisedKeyAndTheActor()
    {
        var (service, viewModel) = await ArrangeAsync();

        viewModel.SelectedLugger = viewModel.Luggers.Single(row => row.PublicId == Extra);
        viewModel.ScrapTypeInput = "Galvanized Steel";
        await viewModel.PromoteLuggerAsync();

        var write = service.Promotions.Single();
        Assert.AreEqual("galvanized steel", write.ScrapTypeKey);
        Assert.AreEqual(Extra, write.PublicId);
        Assert.AreEqual(ActorNumber, write.ActorEmployeeNumber);
        Assert.AreEqual(ActorName, write.ActorEmployeeName);
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_AStoreRefusal_IsShownAsTheStoresOwnSentence()
    {
        var (service, viewModel) = await ArrangeAsync();

        service.WriteFailure = new LuggerArrangementRefusalException("That lugger name is already in use.");
        viewModel.SelectedLugger = viewModel.Luggers.Single(row => row.PublicId == Door6);
        viewModel.LuggerNameInput = "Door 7 Lugger";
        await viewModel.RenameLuggerAsync();

        Assert.AreEqual(
            "That lugger name is already in use.",
            viewModel.MessageText,
            "A refusal the store raised is shown in its words rather than reworded (FR-004).");
        Assert.IsFalse(viewModel.IsStoreUnavailable, "A refusal is an answer from the store, not the store being away.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_AWriteTheStoreDidNotMatch_IsShownAsRefused()
    {
        var (service, viewModel) = await ArrangeAsync();

        service.WriteAnswer = 0;
        viewModel.SelectedLugger = viewModel.Luggers.Single(row => row.PublicId == Door6);
        viewModel.LuggerNameInput = "Recycled Door 6";
        await viewModel.RenameLuggerAsync();

        Assert.AreEqual(
            viewModel.RefusedText,
            viewModel.MessageText,
            "Zero affected rows means nothing matched, which is stated rather than reported as a success.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_AWriteThatFailed_LeavesTheArrangementOnScreen()
    {
        var (service, viewModel) = await ArrangeAsync();

        service.WriteFailure = new InvalidOperationException("The connection was closed.");
        viewModel.SelectedLugger = viewModel.Luggers.Single(row => row.PublicId == Door6);
        viewModel.LuggerNameInput = "Recycled Door 6";
        await viewModel.RenameLuggerAsync();

        Assert.AreEqual(
            viewModel.WriteFailedText,
            viewModel.MessageText,
            "A write that did not land is stated as a change that was not saved, not as a store that cannot be read.");
        Assert.IsFalse(
            viewModel.IsStoreUnavailable,
            "The store still answers the read, so a failed write is not the unavailable state.");
        Assert.AreEqual(5, viewModel.Luggers.Count, "The cards the reader can see were taken away by a failed write.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_AStoreBehindThisBuild_SaysTheStoreNeedsUpdating()
    {
        var (service, viewModel) = await ArrangeAsync();

        service.WriteFailure = new LuggerArrangementStoreOutOfDateException(
            "PROCEDURE mtm_waitlist.sp_waitlist_lugger_locations_position_set does not exist",
            new InvalidOperationException("missing procedure"));

        await viewModel.MoveLuggerAsync((Extra, 1));

        Assert.AreEqual(
            viewModel.StoreOutOfDateText,
            viewModel.MessageText,
            "A missing procedure is a deployment gap, which the page says rather than asking for a retry that cannot work.");
        Assert.AreEqual(5, viewModel.Luggers.Count, "The arrangement is still the one the store answered.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_AnUnreachableStore_ShowsTheUnavailableStateWithoutSampleLuggers()
    {
        var service = new FakeArrangementService();
        var viewModel = BuildViewModel(service);

        service.ReadFailure = new InvalidOperationException("The store is not reachable.");
        await viewModel.InitializeAsync();

        Assert.IsTrue(viewModel.IsStoreUnavailable, "A store that cannot be read is stated as unavailable.");
        Assert.AreEqual(viewModel.UnavailableText, viewModel.MessageText, "The page says what happened, not an empty arrangement.");
        Assert.AreEqual(0, viewModel.Luggers.Count, "No sample lugger stands in for the store (FR-003, constitution II).");
        Assert.AreEqual(0, viewModel.ScrapTypes.Count, "No sample scrap type stands in for the store either.");

        service.ReadFailure = null;
        await viewModel.LoadAsync();

        Assert.IsFalse(viewModel.IsStoreUnavailable, "The reader's retry clears the state once the store answers.");
        Assert.AreEqual(5, viewModel.Luggers.Count, "The retry draws the arrangement the store now answers.");
    }

    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_WithoutThePermission_RefusesTheRouteAndReadsNothing()
    {
        var service = new FakeArrangementService();
        var viewModel = BuildViewModel(service, held: false);

        await viewModel.InitializeAsync();

        Assert.IsFalse(viewModel.IsEntitled, "The arrangement is gated on its own permission (FR-028).");
        Assert.IsTrue(viewModel.NotEntitled, "The page is told to show the refusal rather than the arrangement.");
        Assert.AreEqual(0, viewModel.Luggers.Count, "Nothing is read for a person who may not open it (FR-029).");
        Assert.AreEqual(0, service.LuggersReads, "The store is not even asked.");

        viewModel.SelectedLugger = new LuggerArrangementRow { PublicId = Door6, Name = "Door 6 Lugger" };
        viewModel.StatusInput = LuggerStatus.Full;
        await viewModel.SetStatusAsync();

        Assert.AreEqual(0, service.StatusWrites.Count, "The editor's actions are refused on the same answer.");
    }

    private static async Task<(FakeArrangementService Service, LuggerArrangementViewModel ViewModel)> ArrangeAsync(
        bool held = true)
    {
        var service = new FakeArrangementService();
        var viewModel = BuildViewModel(service, held);
        await viewModel.InitializeAsync();
        return (service, viewModel);
    }

    private static LuggerArrangementViewModel BuildViewModel(
        FakeArrangementService service,
        bool held = true,
        ILuggerBackupPrompt? backupPrompt = null) =>
        new(
            service,
            new FakePermissionService(held),
            new FakePersonIdentity { UserId = 12, SignInName = "DBRUCK", DisplayName = ActorName, EmployeeNumber = ActorNumber },
            backupPrompt: backupPrompt);

    /// <summary>
    /// The question asked when a lugger stops being available. It records what it was asked and answers what the
    /// test stated, so both halves — that it was asked, and what the editor did with the answer — are provable.
    /// </summary>
    private sealed class FakeBackupPrompt : ILuggerBackupPrompt
    {
        internal FakeBackupPrompt(bool answer) => Answer = answer;

        internal bool Answer { get; set; }

        internal List<(string ChangedLugger, string ScrapType, string BackupLugger)> Asked { get; } = [];

        public Task<bool> AskToMakeBackupAvailableAsync(
            string changedLuggerName,
            string scrapType,
            string backupLuggerName,
            CancellationToken cancellationToken = default)
        {
            Asked.Add((changedLuggerName, scrapType, backupLuggerName));
            return Task.FromResult(Answer);
        }
    }

    /// <summary>
    /// A lugger stopped being available while another lugger on the same scrap type is only a backup, so the editor
    /// offers to make that backup available — and does, when the person says yes (owner decision, 2026-10-26).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_SetStatusAsync_LeavingAvailableOffersToMakeTheBackupAvailable()
    {
        // Arrange: Door 6 is the available lugger for 3003 Aluminum, and Extra is its backup.
        var service = new FakeArrangementService();
        var prompt = new FakeBackupPrompt(answer: true);
        var viewModel = BuildViewModel(service, backupPrompt: prompt);
        await viewModel.InitializeAsync();

        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door6));
        viewModel.StatusInput = LuggerStatus.Full;

        // Act
        await viewModel.SetStatusAsync();

        // Assert: the question names the lugger that is leaving, the scrap type, and the backup that could cover it.
        var asked = prompt.Asked.Single();
        Assert.AreEqual("Door 6 Lugger", asked.ChangedLugger);
        Assert.AreEqual("3003 Aluminum", asked.ScrapType);
        Assert.AreEqual("Extra Lugger", asked.BackupLugger);

        Assert.AreEqual(LuggerStatus.Available, service.StoredStatus(Extra), "the backup was made available");
        StringAssert.Contains(viewModel.MessageText, "Extra Lugger", "the editor says what it did");
    }

    /// <summary>
    /// Answering no keeps the backup a backup, and the editor says who is now responsible for the scrap type rather
    /// than leaving the consequence to be discovered (owner decision, 2026-10-26).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_SetStatusAsync_KeepingTheBackupSaysWhoIsResponsible()
    {
        // Arrange
        var service = new FakeArrangementService();
        var prompt = new FakeBackupPrompt(answer: false);
        var viewModel = BuildViewModel(service, backupPrompt: prompt);
        await viewModel.InitializeAsync();

        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door6));
        viewModel.StatusInput = LuggerStatus.DoNotUse;
        viewModel.ReasonInput = "Cracked";

        // Act
        await viewModel.SetStatusAsync();

        // Assert
        Assert.AreEqual(1, prompt.Asked.Count, "the question is asked once for the one backup");
        Assert.AreEqual(LuggerStatus.Backup, service.StoredStatus(Extra), "the backup is left exactly as it was");
        StringAssert.Contains(viewModel.MessageText, "Extra Lugger");
        StringAssert.Contains(viewModel.MessageText, "responsibility", "the consequence has to be stated, not implied");
    }

    /// <summary>
    /// A status change that costs no scrap type its available lugger asks nothing: no other lugger takes the type,
    /// or another available lugger still does (owner decision, 2026-10-26).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_SetStatusAsync_WithNoBackupToOfferAsksNothing()
    {
        // Arrange: Galvanized Steel is held by Door 7 alone, so there is no backup to offer.
        var service = new FakeArrangementService();
        var prompt = new FakeBackupPrompt(answer: true);
        var viewModel = BuildViewModel(service, backupPrompt: prompt);
        await viewModel.InitializeAsync();

        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door7));
        viewModel.StatusInput = LuggerStatus.Full;

        // Act
        await viewModel.SetStatusAsync();

        // Assert
        Assert.AreEqual(0, prompt.Asked.Count, "a type with no backup has nobody to promote");
        Assert.AreEqual(LuggerStatus.Full, service.StoredStatus(Door7));
    }

    /// <summary>
    /// A lugger that is already available, or that stays available, is not a change that costs anything, so the
    /// question is not asked (owner decision, 2026-10-26).
    /// </summary>
    [TestMethod]
    public async Task ViewModel_LuggerArrangementViewModelTests_SetStatusAsync_StayingAvailableAsksNothing()
    {
        // Arrange
        var service = new FakeArrangementService();
        var prompt = new FakeBackupPrompt(answer: true);
        var viewModel = BuildViewModel(service, backupPrompt: prompt);
        await viewModel.InitializeAsync();

        viewModel.SelectLugger(viewModel.Luggers.Single(row => row.PublicId == Door6));
        viewModel.StatusInput = LuggerStatus.Available;

        // Act
        await viewModel.SetStatusAsync();

        // Assert
        Assert.AreEqual(0, prompt.Asked.Count);
    }

    /// <summary>Answers the one gate this screen reads.</summary>
    private sealed class FakePermissionService : IPermissionService
    {
        private readonly bool _held;

        internal FakePermissionService(bool held) => _held = held;

        public Task<bool> HasPermissionAsync(string permissionKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(_held);

        public Task<IReadOnlyDictionary<string, bool>> HasPermissionsAsync(
            IEnumerable<string> permissionKeys,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<string, bool>>(
                permissionKeys.ToDictionary(key => key, _ => _held, StringComparer.Ordinal));

        public void Invalidate()
        {
        }
    }

    /// <summary>
    /// The arrangement seam, recording what each write was asked to do. Its default is the arrangement the shipped
    /// seed writes, so a test asserts against what a lead actually opens.
    /// </summary>
    private sealed class FakeArrangementService : ILuggerArrangementService
    {
        internal FakeArrangementService()
        {
            Luggers.AddRange(SeededLuggers());
            Rows.AddRange(SeededRows());
        }

        internal List<Lugger> Luggers { get; } = [];

        internal List<ScrapAssignment> Rows { get; } = [];

        internal Exception? ReadFailure { get; set; }

        internal Exception? WriteFailure { get; set; }

        internal int WriteAnswer { get; set; } = 1;

        internal int LuggersReads { get; private set; }

        internal List<(string Name, int DisplayPosition, string ActorEmployeeNumber, string ActorEmployeeName)> Adds { get; } = [];

        internal List<(string PublicId, string Name, string ActorEmployeeNumber, string ActorEmployeeName)> Renames { get; } = [];

        internal List<(string PublicId, int DisplayPosition, string ActorEmployeeNumber, string ActorEmployeeName)> Placements { get; } = [];

        internal List<(string PublicId, string ActorEmployeeNumber, string ActorEmployeeName)> Removals { get; } = [];

        internal List<(string PublicId, string Status, string? Reason, string ActorEmployeeNumber, string ActorEmployeeName)> StatusWrites { get; } = [];

        internal List<(string ScrapType, string PublicId, int SortPosition, string ActorEmployeeNumber, string ActorEmployeeName)> Assignments { get; } = [];

        internal List<(string ScrapTypeKey, string PublicId, string ActorEmployeeNumber, string ActorEmployeeName)> Unassignments { get; } = [];

        internal List<(string PublicId, string ScrapTypeKey, string ActorEmployeeNumber, string ActorEmployeeName)> Promotions { get; } = [];

        internal string StoredStatus(string publicId) =>
            Luggers.Single(lugger => lugger.PublicId == publicId).Status;

        public Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            ReadFailure is not null
                ? Task.FromException<LuggerArrangementSnapshot>(ReadFailure)
                : Task.FromResult(LuggerArrangementSnapshot.Create(Rows));

        public Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default)
        {
            LuggersReads++;

            return ReadFailure is not null
                ? Task.FromException<IReadOnlyList<Lugger>>(ReadFailure)
                : Task.FromResult<IReadOnlyList<Lugger>>(Luggers.OrderBy(lugger => lugger.DisplayPosition).ToArray());
        }

        public Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
            string scrapTypeKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ScrapAssignment>>(
                Rows.Where(row => string.Equals(row.ScrapTypeKey, scrapTypeKey, StringComparison.Ordinal))
                    .OrderBy(row => row.SortPosition ?? int.MaxValue)
                    .ToArray());

        public Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default) => Task.FromResult(0L);

        public Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
            long afterId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArrangementChange>>(Array.Empty<ArrangementChange>());

        public Task<int> AddLuggerAsync(
            string luggerName,
            int displayPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Adds.Add((luggerName, displayPosition, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> RenameLuggerAsync(
            string luggerPublicId,
            string luggerName,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Renames.Add((luggerPublicId, luggerName, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> SetLuggerPositionAsync(
            string luggerPublicId,
            int displayPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Placements.Add((luggerPublicId, displayPosition, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> RemoveLuggerAsync(
            string luggerPublicId,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Removals.Add((luggerPublicId, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> SetStatusAsync(
            string luggerPublicId,
            string status,
            string? reason,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            StatusWrites.Add((luggerPublicId, status, reason, actorEmployeeNumber, actorEmployeeName));

            // The store really does keep the new status, so the double does too: a reload after a write must show
            // what the write did, or a test about what happens next would be testing a stale arrangement.
            if (WriteFailure is null && WriteAnswer > 0)
            {
                var lugger = Luggers.FirstOrDefault(candidate => candidate.PublicId == luggerPublicId);

                if (lugger is not null)
                {
                    var index = Luggers.IndexOf(lugger);
                    Luggers[index] = new Lugger
                    {
                        Id = lugger.Id,
                        PublicId = lugger.PublicId,
                        Name = lugger.Name,
                        Status = status,
                        StatusReason = reason,
                        DisplayPosition = lugger.DisplayPosition,
                        CreatedUtc = lugger.CreatedUtc,
                        UpdatedUtc = lugger.UpdatedUtc,
                    };
                }

                for (var index = 0; index < Rows.Count; index++)
                {
                    if (string.Equals(Rows[index].LuggerPublicId, luggerPublicId, StringComparison.Ordinal))
                    {
                        Rows[index] = Rows[index] with { LuggerStatus = status, LuggerStatusReason = reason };
                    }
                }
            }

            return WriteAsync();
        }

        public Task<int> PromoteLuggerAsync(
            string luggerPublicId,
            string scrapTypeKey,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Promotions.Add((luggerPublicId, scrapTypeKey, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> AssignScrapTypeAsync(
            string scrapType,
            string luggerPublicId,
            int sortPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Assignments.Add((scrapType, luggerPublicId, sortPosition, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        public Task<int> UnassignScrapTypeAsync(
            string scrapTypeKey,
            string luggerPublicId,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default)
        {
            Unassignments.Add((scrapTypeKey, luggerPublicId, actorEmployeeNumber, actorEmployeeName));
            return WriteAsync();
        }

        private Task<int> WriteAsync() =>
            WriteFailure is not null
                ? Task.FromException<int>(WriteFailure)
                : Task.FromResult(WriteAnswer);

        private static IReadOnlyList<Lugger> SeededLuggers() =>
        [
            new Lugger { PublicId = Door6, Name = "Door 6 Lugger", Status = LuggerStatus.Available, DisplayPosition = 1 },
            new Lugger { PublicId = Door7, Name = "Door 7 Lugger", Status = LuggerStatus.Available, DisplayPosition = 2 },
            new Lugger { PublicId = Door9, Name = "Door 9 Lugger", Status = LuggerStatus.Full, DisplayPosition = 3 },
            new Lugger { PublicId = Extra, Name = "Extra Lugger", Status = LuggerStatus.Backup, DisplayPosition = 5 },
            new Lugger
            {
                PublicId = Trailer,
                Name = "Scrap Trailer",
                Status = LuggerStatus.DoNotUse,
                StatusReason = "Towed away",
                DisplayPosition = 6,
            },
        ];

        private static IReadOnlyList<ScrapAssignment> SeededRows() =>
        [
            Row(Door6, "Door 6 Lugger", LuggerStatus.Available, "3003 aluminum", "3003 Aluminum", dumpPoint: true, sortPosition: 1),
            Row(Extra, "Extra Lugger", LuggerStatus.Backup, "3003 aluminum", "3003 Aluminum", dumpPoint: false, sortPosition: 2),
            Row(Door7, "Door 7 Lugger", LuggerStatus.Available, "galvanized steel", "Galvanized Steel", dumpPoint: true, sortPosition: 1),
            Row(Door9, "Door 9 Lugger", LuggerStatus.Full, "steel", "Steel", dumpPoint: false, sortPosition: 1),
            Row(Door9, "Door 9 Lugger", LuggerStatus.Full, "copper", "Copper", dumpPoint: true, sortPosition: 1),
            new ScrapAssignment
            {
                LuggerPublicId = Trailer,
                LuggerName = "Scrap Trailer",
                LuggerStatus = LuggerStatus.DoNotUse,
                LuggerStatusReason = "Towed away",
                LuggerDisplayPosition = 6,
            },
        ];

        private static ScrapAssignment Row(
            string publicId,
            string name,
            string status,
            string key,
            string spelling,
            bool dumpPoint,
            int sortPosition) =>
            new()
            {
                LuggerPublicId = publicId,
                LuggerName = name,
                LuggerStatus = status,
                ScrapType = spelling,
                ScrapTypeKey = key,
                IsDumpPoint = dumpPoint,
                SortPosition = sortPosition,
            };
    }
}
