using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Contracts.Services;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.Tests.Module_Waitlist.Services;

/// <summary>
/// The notice seam the waitlist holds: the session watermark, the poll above it, and the dismissal. The cases
/// below are the contract's own sentences — nothing at or below the watermark is ever shown (FR-037), each change
/// becomes one notice in plain words naming the lugger, the scrap type and both values where one changed (FR-035),
/// and a dismissal holds for the session while any other way of closing a bar does not (FR-036).
/// </summary>
[TestClass]
public sealed class LuggerChangeNoticeServiceTests
{
    private const string Lugger = "Lugger A";
    private const string RenamedLugger = "Lugger B";
    private const string ScrapType = "PET";

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_OpenSession_TakesTheWatermarkInOneNullCursorReadAndShowsNothing()
    {
        var arrangement = new FakeArrangementService { Watermark = 8 };
        arrangement.Changes.AddRange(
        [
            Change(6, ArrangementChange.Kinds.LuggerAdded),
            Change(7, ArrangementChange.Kinds.LuggerAdded),
            Change(8, ArrangementChange.Kinds.LuggerAdded),
        ]);

        var service = new LuggerChangeNoticeService(arrangement);

        var notices = await service.OpenSessionAsync().ConfigureAwait(false);

        Assert.AreEqual(1, arrangement.WatermarkReads, "Opening the list asks for the watermark exactly once.");
        Assert.AreEqual(0, arrangement.ChangeReads, "Opening the list reads no change row.");
        Assert.AreEqual(0, notices.Count, "A change at or below the watermark is never shown.");
    }

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_Poll_ReturnsOnlyTheChangesAboveTheWatermarkInIdOrder()
    {
        var arrangement = new FakeArrangementService { Watermark = 8 };
        arrangement.Changes.AddRange(
        [
            Change(6, ArrangementChange.Kinds.LuggerAdded),
            Change(10, ArrangementChange.Kinds.AssignmentAdded),
            Change(8, ArrangementChange.Kinds.LuggerRemoved),
            Change(9, ArrangementChange.Kinds.StatusChanged),
        ]);

        var service = new LuggerChangeNoticeService(arrangement);
        await service.OpenSessionAsync().ConfigureAwait(false);

        var notices = await service.PollAsync().ConfigureAwait(false);

        CollectionAssert.AreEqual(
            new long[] { 9, 10 },
            notices.Select(notice => notice.Id).ToArray(),
            "Only the changes above the watermark are shown, one notice each, in id order.");
        Assert.AreEqual(8, arrangement.LastAfterId, "The poll asked for the changes above the session watermark.");
    }

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_Poll_BeforeTheListOpenedNeverReplaysAnOlderChange()
    {
        var arrangement = new FakeArrangementService { Watermark = 4 };
        arrangement.Changes.AddRange(
        [
            Change(1, ArrangementChange.Kinds.LuggerAdded),
            Change(4, ArrangementChange.Kinds.LuggerRenamed),
        ]);

        var service = new LuggerChangeNoticeService(arrangement);

        var notices = await service.PollAsync().ConfigureAwait(false);

        Assert.AreEqual(0, notices.Count, "A poll on a session that never opened establishes the watermark and shows nothing.");
        Assert.AreEqual(1, arrangement.WatermarkReads, "The first poll starts above the current highest change.");
        Assert.AreEqual(0, arrangement.ChangeReads, "And does not even ask for the rows below it.");
    }

    [DataTestMethod]
    [DataRow(ArrangementChange.Kinds.LuggerAdded, null, null, null, "Lugger_Notice.LuggerAdded", "Lugger A", DisplayName = "an added lugger is named")]
    [DataRow(ArrangementChange.Kinds.LuggerAdded, ScrapType, null, null, "Lugger_Notice.LuggerAddedForScrap", "Lugger A|PET", DisplayName = "an added lugger names its scrap type")]
    [DataRow(ArrangementChange.Kinds.LuggerRenamed, null, "Lugger A", "Lugger B", "Lugger_Notice.LuggerRenamed", "Lugger A|Lugger B", DisplayName = "a rename names both names")]
    [DataRow(ArrangementChange.Kinds.LuggerRenamed, ScrapType, "Lugger A", "Lugger B", "Lugger_Notice.LuggerRenamedForScrap", "Lugger A|Lugger B|PET", DisplayName = "a rename names both names and the scrap type")]
    [DataRow(ArrangementChange.Kinds.LuggerRemoved, null, null, null, "Lugger_Notice.LuggerRemoved", "Lugger A", DisplayName = "a removed lugger is named")]
    [DataRow(ArrangementChange.Kinds.LuggerRemoved, ScrapType, null, null, "Lugger_Notice.LuggerRemovedForScrap", "Lugger A|PET", DisplayName = "a removal names the scrap type it left")]
    [DataRow(ArrangementChange.Kinds.StatusChanged, ScrapType, "Full", "Available", "Lugger_Notice.StatusChanged", "Lugger A|Full|Available|PET", DisplayName = "a status change names the previous and the new status")]
    [DataRow(ArrangementChange.Kinds.DumpPointPromoted, ScrapType, "Lugger A", "Lugger B", "Lugger_Notice.DumpPointPromoted", "Lugger A|Lugger B|PET", DisplayName = "a promoted dump point names where it moved from and to")]
    [DataRow(ArrangementChange.Kinds.AssignmentAdded, ScrapType, null, null, "Lugger_Notice.AssignmentAdded", "Lugger A|PET", DisplayName = "an assignment names the lugger and the scrap type")]
    [DataRow(ArrangementChange.Kinds.AssignmentRemoved, ScrapType, null, null, "Lugger_Notice.AssignmentRemoved", "Lugger A|PET", DisplayName = "an unassignment names the lugger and the scrap type")]
    [DataRow(ArrangementChange.Kinds.AssignmentReordered, ScrapType, "1", "3", "Lugger_Notice.AssignmentReordered", "Lugger A|1|3|PET", DisplayName = "a reorder names both positions")]
    [DataRow("something_else", ScrapType, null, null, "Lugger_Notice.Fallback", "", DisplayName = "a kind the notice has no wording for is still a notice")]
    public async Task Service_LuggerChangeNoticeServiceTests_Poll_ReadsTheKindAndBothValuesIntoOnePlainSentence(
        string changeKind,
        string? scrapType,
        string? previousValue,
        string? newValue,
        string expectedTemplate,
        string expectedArguments)
    {
        var arrangement = new FakeArrangementService { Watermark = 0 };
        arrangement.Changes.Add(new ArrangementChange
        {
            Id = 1,
            ChangeKind = changeKind,
            LuggerName = changeKind == ArrangementChange.Kinds.LuggerRenamed ? RenamedLugger : Lugger,
            ScrapType = scrapType,
            PreviousValue = previousValue,
            NewValue = newValue,
            ChangedByEmployeeName = "Morgan Reyes",
            ChangedUtc = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc),
        });

        var service = new LuggerChangeNoticeService(arrangement);
        await service.OpenSessionAsync().ConfigureAwait(false);

        var notice = (await service.PollAsync().ConfigureAwait(false)).Single();
        var arguments = string.Join('|', notice.MessageArguments);
        var expectedText = string.Format(
            CultureInfo.CurrentCulture,
            expectedTemplate.GetLocalized(),
            [.. notice.MessageArguments]);

        Assert.AreEqual(expectedTemplate, notice.MessageTemplate, "The notice reads the wording of its own change kind.");
        Assert.AreEqual(expectedArguments, arguments, "The notice names the lugger, the scrap type and both values where one changed.");
        Assert.AreEqual(expectedText, notice.Text, "The shown sentence is the localized template with those values.");
        Assert.AreEqual(changeKind, notice.ChangeKind, "The notice keeps the kind it was raised from.");
        Assert.AreEqual("Morgan Reyes", notice.ChangedByEmployeeName, "The notice keeps who made the change.");
    }

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_Dismiss_ARaisedByTheCloseButtonIsNotOfferedAgain()
    {
        var arrangement = new FakeArrangementService { Watermark = 0 };
        arrangement.Changes.AddRange(
        [
            Change(1, ArrangementChange.Kinds.LuggerAdded),
            Change(2, ArrangementChange.Kinds.AssignmentAdded),
        ]);

        var service = new LuggerChangeNoticeService(arrangement);
        await service.OpenSessionAsync().ConfigureAwait(false);
        var first = await service.PollAsync().ConfigureAwait(false);
        Assert.AreEqual(2, first.Count, "Two changes draw two notices.");

        var remaining = await service.DismissAsync(1, raisedByCloseButton: true).ConfigureAwait(false);
        var afterAnotherTick = await service.PollAsync().ConfigureAwait(false);

        CollectionAssert.AreEqual(new long[] { 2 }, remaining.Select(notice => notice.Id).ToArray(), "Only the closed bar goes.");
        CollectionAssert.AreEqual(new long[] { 2 }, afterAnotherTick.Select(notice => notice.Id).ToArray(), "A dismissed notice is never offered again in the session.");
    }

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_Dismiss_AnyReasonOtherThanTheCloseButtonLeavesTheNoticeInPlace()
    {
        var arrangement = new FakeArrangementService { Watermark = 0 };
        arrangement.Changes.Add(Change(1, ArrangementChange.Kinds.LuggerAdded));

        var service = new LuggerChangeNoticeService(arrangement);
        await service.OpenSessionAsync().ConfigureAwait(false);
        await service.PollAsync().ConfigureAwait(false);

        var remaining = await service.DismissAsync(1, raisedByCloseButton: false).ConfigureAwait(false);
        var afterAnotherTick = await service.PollAsync().ConfigureAwait(false);

        Assert.AreEqual(1, remaining.Count, "A bar closed for any other reason is not a dismissal.");
        Assert.AreEqual(1, afterAnotherTick.Count, "The notice is still offered on the next tick.");
    }

    [TestMethod]
    public async Task Service_LuggerChangeNoticeServiceTests_Poll_AStoreThatCannotBeReachedAnswersNoNoticesAndRaisesNothing()
    {
        var arrangement = new FakeArrangementService { Watermark = 0, ReadFailure = new InvalidOperationException("the store did not answer") };
        var service = new LuggerChangeNoticeService(arrangement);

        await service.OpenSessionAsync().ConfigureAwait(false);

        var notices = await service.PollAsync().ConfigureAwait(false);

        Assert.AreEqual(0, notices.Count, "An unreachable store means no notices rather than invented ones.");
    }

    private static ArrangementChange Change(long id, string changeKind) => new()
    {
        Id = id,
        ChangeKind = changeKind,
        LuggerName = Lugger,
        ScrapType = ScrapType,
        ChangedByEmployeeName = "Morgan Reyes",
        ChangedUtc = new DateTime(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc),
    };

    /// <summary>
    /// The arrangement seam as the notice service meets it: the watermark, the rows above a cursor, and a store that
    /// can be made to fail. Every other member is outside this seam's use and throws rather than answering.
    /// </summary>
    private sealed class FakeArrangementService : ILuggerArrangementService
    {
        public long Watermark { get; set; }

        public List<ArrangementChange> Changes { get; } = [];

        public Exception? ReadFailure { get; set; }

        public int WatermarkReads { get; private set; }

        public int ChangeReads { get; private set; }

        public long LastAfterId { get; private set; } = -1;

        public Task<long> GetChangeWatermarkAsync(CancellationToken cancellationToken = default)
        {
            WatermarkReads++;

            return ReadFailure is not null
                ? Task.FromException<long>(ReadFailure)
                : Task.FromResult(Watermark);
        }

        public Task<IReadOnlyList<ArrangementChange>> GetChangesAfterAsync(
            long afterId,
            CancellationToken cancellationToken = default)
        {
            ChangeReads++;
            LastAfterId = afterId;

            return ReadFailure is not null
                ? Task.FromException<IReadOnlyList<ArrangementChange>>(ReadFailure)
                : Task.FromResult<IReadOnlyList<ArrangementChange>>(
                    Changes.Where(change => change.Id > afterId).ToArray());
        }

        public Task<LuggerArrangementSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the snapshot.");

        public Task<IReadOnlyList<Lugger>> GetLuggersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the lugger list.");

        public Task<IReadOnlyList<ScrapAssignment>> GetAssignmentsAsync(
            string scrapTypeKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam reads changes, not the assignment list.");

        public Task<int> AddLuggerAsync(
            string luggerName,
            int displayPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> RenameLuggerAsync(
            string luggerPublicId,
            string luggerName,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> SetLuggerPositionAsync(
            string luggerPublicId,
            int displayPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> RemoveLuggerAsync(
            string luggerPublicId,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> SetStatusAsync(
            string luggerPublicId,
            string status,
            string? reason,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> PromoteLuggerAsync(
            string luggerPublicId,
            string scrapTypeKey,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> AssignScrapTypeAsync(
            string scrapType,
            string luggerPublicId,
            int sortPosition,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");

        public Task<int> UnassignScrapTypeAsync(
            string scrapTypeKey,
            string luggerPublicId,
            string actorEmployeeNumber,
            string actorEmployeeName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The notice seam never writes the arrangement.");
    }
}
