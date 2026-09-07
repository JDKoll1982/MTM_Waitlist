using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Mock coil-availability source for the Waitlist/New Request path (mock mode only).
/// Keyed by the active job's work order so a job either has a real coil (with populated
/// fields) or has no coil at all — which is what drives hiding the Coil request type.
/// In real mode this is replaced by the live Infor Visual job/coil lookup (to be wired).
/// </summary>
public static class SampleJobCoilCatalog
{
    public static WaitlistCoilInfo GetCoilForJob(string? workOrderOrWorkCenter)
    {
        var key = string.IsNullOrWhiteSpace(workOrderOrWorkCenter) ? string.Empty : workOrderOrWorkCenter.Trim();

        // Work orders / work centers that carry a coil.
        if (key == "WO-076951" || key == "100-3" || key == "100-6" || key == "100-8")
        {
            // Quantity in house is the pooled on-hand WEIGHT (lb) across the part's locations
            // (excluding ignored locations), matching Infor Visual's per-location pooling.
            return new WaitlistCoilInfo
            {
                HasCoil = true,
                CoilNumber = "COIL-204",
                QuantityOnHand = "46,000 lb",
                Description = "0.060 x 48 in galvanized coil",
                AverageWeight = "5,000 lb",
            };
        }

        // Everything else is treated as having no coil (so the Coil tile is hidden).
        return new WaitlistCoilInfo { HasCoil = false };
    }
}
