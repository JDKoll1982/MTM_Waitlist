using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Lifecycle-complete sample Waitlist requests for mock mode (Feature.InforVisualMockData ON).
/// Unlike the display-only <see cref="SampleOrder"/> rows used by <see cref="SampleDataService"/>,
/// these are full <see cref="WaitlistRequest"/> objects spanning Pending / Accepted / Completed /
/// Canceled with requester, handler, note, and lifecycle timestamps, so the list status badge,
/// wait time, cancel-own, My Requests, and urgency features can be exercised with the mock toggle ON.
/// Cancelled/Completed sample rows are retained (never purged) for analytics/monitoring parity.
/// </summary>
public static class SampleWaitlistRequestCatalog
{
    public static IReadOnlyList<WaitlistRequest> GetRequests(string? building = null)
    {
        var normalizedBuilding = string.IsNullOrWhiteSpace(building) ? "Expo Drive" : building.Trim();
        var now = DateTimeOffset.UtcNow;

        var pending = new WaitlistRequest
        {
            Id = Guid.Parse("a0000000-0001-4000-8000-000000000001"),
            Building = normalizedBuilding,
            WorkCenter = "100-3",
            RequestType = "Coil",
            Subtype = "Pickup",
            ActiveSetupJobId = "100-3",
            WorkCenterName = "100-3",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            Status = "Pending",
            RequestedUtc = now.AddMinutes(-35),
            TargetTimeUtc = now.AddMinutes(-5),
            IsOverdue = false,
        };

        var accepted = new WaitlistRequest
        {
            Id = Guid.Parse("a0000000-0002-4000-8000-000000000002"),
            Building = normalizedBuilding,
            WorkCenter = "100-6",
            RequestType = "Pickup",
            Subtype = "Pickup WIP",
            ActiveSetupJobId = "100-6",
            WorkCenterName = "100-6",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            Status = "Accepted",
            RequestedUtc = now.AddMinutes(-80),
            TargetTimeUtc = now.AddMinutes(-50),
            AcceptedUtc = now.AddMinutes(-75),
            IsOverdue = true,
            AssignedMaterialHandler = "6229",
            Note = "Handler claimed this pickup.",
        };

        var completed = new WaitlistRequest
        {
            Id = Guid.Parse("a0000000-0003-4000-8000-000000000003"),
            Building = normalizedBuilding,
            WorkCenter = "100-8",
            RequestType = "Scrap",
            ActiveSetupJobId = "100-8",
            WorkCenterName = "100-8",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            Status = "Completed",
            RequestedUtc = now.AddHours(-3),
            TargetTimeUtc = now.AddHours(-3).AddMinutes(30),
            AcceptedUtc = now.AddHours(-3).AddMinutes(2),
            CompletedUtc = now.AddHours(-2),
        };

        var canceled = new WaitlistRequest
        {
            Id = Guid.Parse("a0000000-0004-4000-8000-000000000004"),
            Building = normalizedBuilding,
            WorkCenter = "100-12",
            RequestType = "Pickup",
            Subtype = "Outside Service",
            ActiveSetupJobId = "100-12",
            WorkCenterName = "100-12",
            RequesterEmployeeNumber = "6229",
            RequesterEmployeeName = "John Koll",
            Status = "Canceled",
            RequestedUtc = now.AddHours(-2),
            TargetTimeUtc = now.AddHours(-2).AddMinutes(30),
            CanceledUtc = now.AddHours(-1).AddMinutes(50),
            CanceledByEmployeeNumber = "6229",
            CancellationReason = "Operator no longer needs this pickup.",
        };

        // A Pending request raised by a DIFFERENT requester so My Requests excludes it and
        // cancel-own is denied for non-creators even in mock mode.
        var otherPending = new WaitlistRequest
        {
            Id = Guid.Parse("a0000000-0005-4000-8000-000000000005"),
            Building = normalizedBuilding,
            WorkCenter = "100-9",
            RequestType = "Forklift Assist",
            InputValue = "Need a forklift to move a pallet to staging.",
            ActiveSetupJobId = "100-9",
            WorkCenterName = "100-9",
            RequesterEmployeeNumber = "5000",
            RequesterEmployeeName = "Other User",
            Status = "Pending",
            RequestedUtc = now.AddMinutes(-12),
            TargetTimeUtc = now.AddMinutes(20),
            IsOverdue = false,
        };

        return new[] { pending, accepted, completed, canceled, otherPending };
    }
}
