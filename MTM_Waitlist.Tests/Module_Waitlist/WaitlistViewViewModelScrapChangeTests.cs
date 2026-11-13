using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Core.Models;
using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;

namespace MTM_Waitlist.Tests.Module_Waitlist;

/// <summary>
/// What a generic scrap card keeps, and what it follows, when the arrangement changes after the request was raised.
/// </summary>
/// <remarks>
/// The stated type is a record of what the plant was told, so nothing rewrites it. The lugger is a live fact, so the
/// card follows the arrangement rather than repeating what it said yesterday (FR-011, FR-012).
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelScrapChangeTests
{
    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapChangeTests_CreateSessionOrder_ATypeRemovedFromTheArrangementLeavesTheStatedWordingAlone()
    {
        // The requester stated `Galvanized Steel`; the arrangement no longer knows that type at all.
        var card = CreateCard(
            "Galvanized Steel",
            Arrangement(Row("Steel", "Door 2 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreEqual(
            "Galvanized Steel",
            Cell(card, "Scrap Type")?.Value,
            "A request that quietly rewrites itself is worse than one that says it has a gap (FR-011).");
        Assert.IsNull(
            Cell(card, "Lugger Location"),
            "A type with no assignment reports the gap rather than a lugger that no longer applies (FR-011).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapChangeTests_CreateSessionOrder_ATypeRenamedInTheArrangementKeepsTheStatedWording()
    {
        // The arrangement now spells the type `Galvanized`, while the request kept `Galvanized Steel`.
        var card = CreateCard(
            "Galvanized Steel",
            Arrangement(Row("Galvanized", "Door 4 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreEqual(
            "Galvanized Steel",
            Cell(card, "Scrap Type")?.Value,
            "The request keeps the wording the requester chose, not the arrangement's new spelling (FR-011).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapChangeTests_CreateSessionOrder_ATypeReassignedToAnotherLuggerFollowsTheArrangement()
    {
        var card = CreateCard(
            "Steel",
            Arrangement(Row("Steel", "Door 12 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreEqual(
            "Door 12 Lugger",
            Cell(card, "Lugger Location")?.Value,
            "The dump point is a live fact, so a reassignment is followed rather than stored (FR-012).");
    }

    [TestMethod]
    public void ViewModel_WaitlistViewViewModelScrapChangeTests_CreateSessionOrder_AStoredTypeOutsideTheSharedListIsStillNamed()
    {
        var card = CreateCard(
            "Aardvark Scrap",
            Arrangement(Row("Aardvark Scrap", "Door 1 Lugger", LuggerStatus.Available, sortPosition: 1)));

        Assert.AreEqual(
            "Aardvark Scrap",
            Cell(card, "Scrap Type")?.Value,
            "A type the plant's list no longer carries is still the type the requester stated (FR-013).");
        Assert.AreEqual(
            "Door 1 Lugger",
            Cell(card, "Lugger Location")?.Value,
            "The type's assignment still resolves, because the arrangement is what assigns luggers (FR-013).");
    }

    private static WaitlistLineCardCell? Cell(SampleOrder card, string label) =>
        WaitlistLineCardCell.CellsToDraw(card.Cells)
            .SingleOrDefault(cell => string.Equals(cell.Label, label, StringComparison.OrdinalIgnoreCase));

    private static SampleOrder CreateCard(string statedScrapType, LuggerArrangementSnapshot arrangement) =>
        WaitlistViewViewModel.CreateSessionOrder(
            new WaitlistRequest
            {
                Building = "Expo Drive",
                WorkCenter = "Expo Line 7",
                Category = "Scrap",
                Item = "pickup-scrap",
                IsGeneric = true,
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
            },
            jobAvailability: null,
            new RequestItemConfiguration
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
            },
            arrangement);

    private static LuggerArrangementSnapshot Arrangement(params ScrapAssignment[] rows) =>
        LuggerArrangementSnapshot.Create(rows);

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
}
