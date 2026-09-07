namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Coil availability for the active job at a work center (Waitlist/New Request path).
/// Used to decide whether the Coil request type is shown and to populate the coil card
/// fields (requested coil, quantity in house, coil description, average coil weight)
/// when Feature.InforVisualMockData is ON.
/// </summary>
public sealed class WaitlistCoilInfo
{
    public bool HasCoil { get; init; }

    public string CoilNumber { get; init; } = string.Empty;

    public string QuantityOnHand { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string AverageWeight { get; init; } = string.Empty;
}
