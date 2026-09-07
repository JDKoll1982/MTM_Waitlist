using System.Globalization;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <summary>
/// Sample average-coil-weight values used when <c>Feature.RecvMockData</c> is ON, mirroring the
/// rows seeded by <c>Database/MTMReceivingApp/Seeds/seed_receiving_history_coil_weights</c>.
/// </summary>
public static class SampleAverageCoilWeightCatalog
{
    /// <summary>Mock average skid weight (lb) for the seeded coil part MMC0001000 (4800+5000+5200 / 3 = 5000).</summary>
    public const decimal Mmc0001000AverageWeight = 5000m;

    public static string GetAverageCoilWeightText(string? partId)
    {
        var normalized = partId?.Trim();
        if (string.Equals(normalized, "MMC0001000", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Mmc0001000AverageWeight.ToString("N0", CultureInfo.InvariantCulture)} lb";
        }

        return string.Empty;
    }
}
