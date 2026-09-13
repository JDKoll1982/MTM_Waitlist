using Microsoft.VisualStudio.TestTools.UnitTesting;

using MTM_Waitlist.Module_Waitlist.Models;
using MTM_Waitlist.Module_Waitlist.ViewModels;
using MTM_Waitlist.Tests.Module_Mock;

namespace MTM_Waitlist.Tests.Module_Waitlist.ViewModels;

/// <summary>
/// US1 check 1 (<c>contracts/verification-gates.md</c> G4 #1). A card field value must be traceable to the
/// request on screen; the guard also rejects any value shaped like an invented material attribute, so a
/// substitute re-spelled later still fails.
/// </summary>
/// <remarks>
/// The no-fabrication test is expressed as "every rendered value is a substring of something the request
/// carries". That is the honest form of FR-001/FR-002 for this codebase: <see cref="WaitlistRequest"/> has
/// no material-attribute properties at all, so a card that shows a part, quantity, customer, packlist or
/// destination is showing something no source produced.
/// </remarks>
[TestClass]
public sealed class WaitlistViewViewModelFieldTests
{
    /// <summary>One request per branch of the field-population switch.</summary>
    private static IReadOnlyList<WaitlistRequest> EveryRequestShape { get; } =
    [
        Shape("Coil", "Pickup Coil"),
        Shape("Coil", "Wrong Coil"),
        Shape("Scrap", "Empty Lugger"),
        Shape("Scrap", "Full Lugger"),
        Shape("Pickup", "Pickup FG"),
        Shape("Pickup", "Pickup NCM"),
        Shape("Pickup", "Pickup WIP"),
        Shape("Pickup", "Pickup Coil"),
        Shape("Pickup", "Outside Service"),
        Shape("Pickup", "Other"),
        Shape("Flatstock", null),
        Shape("Table Handling", null),
        Shape("Die Handling", null),
        Shape("Forklift Assist", null),
        Shape("Other", null),
        Shape("Other", "General"),
    ];

    [TestMethod]
    public void CreateSessionOrder_ForEveryRequestShape_CarriesOnlyValuesTheRequestCarries()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            foreach (var field in order.Fields.Where(field => !string.IsNullOrWhiteSpace(field.Value)))
            {
                Assert.IsTrue(
                    IsSourced(field.Value, request),
                    $"{Describe(request)}: field '{field.Label}' renders '{field.Value}', which no source on the request produces.");
            }
        }
    }

    [TestMethod]
    public void CreateSessionOrder_ForEveryRequestShape_RendersNoFabricatedLookingValue()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);
            var offenders = FabricatedValueGuard.FindFabricatedLooking(order.Fields.Select(field => field.Value));

            Assert.AreEqual(
                0,
                offenders.Count,
                $"{Describe(request)}: the card renders fabricated-looking value(s): {string.Join(" | ", offenders)}");
        }
    }

    [TestMethod]
    public void CreateSessionOrder_ForEveryRequestShape_LeavesAnEmptySlotEmpty()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            foreach (var field in order.Fields.Where(field => string.IsNullOrWhiteSpace(field.Value)))
            {
                Assert.AreEqual(
                    string.Empty,
                    field.Label,
                    $"{Describe(request)}: slot '{field.Label}' has no value, so it must carry no label either — an empty labelled shell is not truthful.");
            }
        }
    }

    [TestMethod]
    public void CreateSessionOrder_PadsToTheFiveCardSlotsTheTemplatesBind()
    {
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            Assert.IsTrue(
                order.Fields.Count >= 5,
                $"{Describe(request)}: the per-type templates bind Fields[0]..Fields[4], so a row must expose five slots; it exposed {order.Fields.Count}.");
        }
    }

    [TestMethod]
    public void CreateSessionOrder_SurfacesTheHandlerNeededDataTheRequestCarries()
    {
        // FR-014: a handler must be able to read who asked, where it goes, and how urgent it is off the card
        // itself. Every one of these comes from the request — nothing is derived into a value the request
        // does not carry, which is why the absent ones stay absent rather than being filled in.
        var request = Shape("Pickup", "Pickup NCM");
        var order = WaitlistViewViewModel.CreateSessionOrder(request);

        Assert.AreEqual(request.RequesterEmployeeName, order.RequestedByName, "The handler cannot see who asked.");
        Assert.AreEqual(request.WorkCenter, order.RequestedPressName, "The handler cannot see where the material goes.");
        Assert.IsTrue(order.HasWaitingFor, "The handler cannot see how long the request has been waiting.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(order.RemainingTimeText), "The handler cannot see how urgent the request is.");
        Assert.AreEqual(
            request.RequesterEmployeeNumber,
            order.RequesterEmployeeNumber,
            "The handler cannot see which employee raised the request.");
    }

    [TestMethod]
    public void CreateSessionOrder_AddsNoHandlerSectionToTheCardsFixedLayout()
    {
        // FR-021: handler data is surfaced through the card's existing slots, never by adding a section.
        foreach (var request in EveryRequestShape)
        {
            var order = WaitlistViewViewModel.CreateSessionOrder(request);

            Assert.AreEqual(
                5,
                order.Fields.Count,
                $"{Describe(request)}: the card's fixed layout carries five detail slots; this row exposes {order.Fields.Count}.");
        }
    }

    /// <summary>Builds a request of the given shape carrying distinctive, non-fabricated values.</summary>
    /// <param name="requestType">The request type under test.</param>
    /// <param name="subtype">The subtype, or null for the no-subtype shape.</param>
    /// <returns>The request.</returns>
    private static WaitlistRequest Shape(string requestType, string? subtype) => new()
    {
        Building = "Expo Drive",
        WorkCenter = "Expo Line 7",
        RequestType = requestType,
        Subtype = subtype,
        InputValue = "Skid 4471 is on the wrong dock",
        ActiveSetupJobId = "JOB-9001",
        WorkCenterName = "Expo Line 7",
        RequesterEmployeeNumber = "6331",
        RequesterEmployeeName = "Dana Whitfield",
        Status = "Pending",
        RequestedUtc = DateTimeOffset.UtcNow.AddMinutes(-12),
        TargetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(48),
    };

    /// <summary>
    /// Whether a rendered value is carried by the request. The request id is compared in both its plain and
    /// its dashed rendering because the card shows one of them.
    /// </summary>
    private static bool IsSourced(string value, WaitlistRequest request)
        => SourcedValues(request).Any(source => source.Contains(value, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> SourcedValues(WaitlistRequest request)
    {
        yield return request.Building;
        yield return request.WorkCenter;
        yield return request.WorkCenterName;
        yield return request.RequestType;
        yield return request.Subtype ?? string.Empty;
        yield return request.InputValue ?? string.Empty;
        yield return request.ActiveSetupJobId;
        yield return request.RequesterEmployeeNumber;
        yield return request.RequesterEmployeeName;
        yield return request.Status;
        yield return request.Id.ToString("N");
        yield return request.Id.ToString("D");
    }

    private static string Describe(WaitlistRequest request) => $"{request.RequestType} / {request.Subtype ?? "(no subtype)"}";
}
