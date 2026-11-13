using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.GenericRequest.Services;
using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Core.Services;

namespace MTM_Waitlist.Tests.Module_GenericRequest;

/// <summary>
/// The scrap types a generic work center's requester is offered.
/// </summary>
/// <remarks>
/// The list is the plant's one shared list in that list's own order, with any type the arrangement records but the
/// list does not hold appended behind it. The two answers that say <i>whether</i> there is scrap are not
/// destinations, so they are never offered (FR-002, FR-003). A choice is the type's name and nothing else: the lugger
/// it dumps into belongs on the waitlist card, not on the picker (FR-006).
/// </remarks>
[TestClass]
public sealed class GenericRequestScrapOptionsTests
{
    [TestMethod]
    public async Task Service_GenericRequestScrapOptionsTests_ReadScrapTypesAsync_TheChoicesAreTheSharedListInItsOrderWithArrangementTypesAfterIt()
    {
        var data = CreateLive(new FakeLuggerArrangementService
        {
            Snapshot = LuggerArrangementSnapshot.Create(
            [
                FakeScrapAssignment.Of("Aardvark Scrap", "Door 1 Lugger"),
                FakeScrapAssignment.Of("Steel", "Door 2 Lugger"),
            ]),
        });

        var result = await data.ReadScrapTypesAsync();

        var offered = result.Items.Select(option => option.ScrapType).ToArray();
        var expected = ScrapTypeCatalog.All.Append("Aardvark Scrap").ToArray();

        CollectionAssert.AreEqual(
            expected,
            offered,
            "The shared list comes first in its own order, and only a type it does not hold is appended (FR-002).");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapOptionsTests_ReadScrapTypesAsync_TheTwoNamesThatMeanNoScrapAreNeverOffered()
    {
        var data = CreateLive(new FakeLuggerArrangementService
        {
            Snapshot = LuggerArrangementSnapshot.Create(
            [
                FakeScrapAssignment.Of(ScrapDecisionRules.NoScrap, "Door 1 Lugger"),
                FakeScrapAssignment.Of(ScrapDecisionRules.RequiredPlaceholder, "Door 2 Lugger"),
            ]),
        });

        var result = await data.ReadScrapTypesAsync();

        var offered = result.Items.Select(option => option.ScrapType).ToArray();

        // The arrangement in this test records nothing but the two forbidden spellings, so the whole shared list
        // must still be offered. Without this, a read that answered nothing at all would pass both checks below.
        CollectionAssert.AreEqual(
            ScrapTypeCatalog.All.ToArray(),
            offered,
            "The plant's own list is what the step offers when the arrangement adds nothing usable (FR-002).");

        CollectionAssert.DoesNotContain(
            offered,
            ScrapDecisionRules.NoScrap,
            "No Scrap answers whether there is scrap, not where it goes (FR-003).");
        CollectionAssert.DoesNotContain(
            offered,
            ScrapDecisionRules.RequiredPlaceholder,
            "The required placeholder is not a decision, so it is not a destination either (FR-003).");
    }

    [TestMethod]
    public async Task Service_GenericRequestScrapOptionsTests_ReadScrapTypesAsync_AChoiceIsTheTypeNameAndNothingElse()
    {
        var data = CreateLive(new FakeLuggerArrangementService
        {
            Snapshot = LuggerArrangementSnapshot.Create(
            [
                FakeScrapAssignment.Of("Steel", "Door 1 Lugger", LuggerStatus.Available, sortPosition: 1),
                FakeScrapAssignment.Of("Steel", "Door 2 Lugger", LuggerStatus.Full, isDumpPoint: false, sortPosition: 2),
            ]),
        });

        var result = await data.ReadScrapTypesAsync();

        var steel = result.Items.Single(option => option.ScrapType == "Steel");

        Assert.AreEqual(
            new GenericRequestScrapOption("Steel"),
            steel,
            "A choice carries the type and nothing else, so no lugger count reaches the screen (FR-006).");
    }

    private static GenericRequestLiveStepData CreateLive(FakeLuggerArrangementService arrangement) =>
        new(
            new StubWorkCenterCatalogService(),
            new StubNewRequestPickerService(),
            new StubRequestItemConfigurationService(),
            new RecordingWaitlistRequestService(),
            new StubPersonIdentity(),
            new StubEmployeeDirectoryService(),
            new StubGenericRequestPartData(),
            new FakeDunnageCatalogueReader(),
            arrangement);
}
