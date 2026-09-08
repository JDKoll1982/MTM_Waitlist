namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// Floor-level disposition snapshot for a part from the MTM WIP Application
/// (<c>mtm_wip_application_winforms.inv_inventory</c> + <c>md_locations</c>), produced by the
/// <c>GetWipFloorQuantities</c> queue script. Used as the live floor cross-check next to the Infor
/// Visual <c>WORK_ORDER</c>/<c>OPERATION</c> snapshot when deriving FG / WIP / Outside Service
/// (file 14 Phase 6).
/// </summary>
public sealed record WipFloorQuantitySnapshot
{
    /// <summary>Quantity currently sitting at finished-goods locations (FG / DC-FG / ...FINISHED GOODS).</summary>
    public decimal FinishedGoodsFloorQuantity { get; init; }

    /// <summary>Quantity staged at an outside-service location (e.g. O/S - VITS).</summary>
    public decimal OutsideServiceFloorQuantity { get; init; }

    /// <summary>Quantity staged at a non-conforming location (NCM...).</summary>
    public decimal NonConformingFloorQuantity { get; init; }

    /// <summary>Quantity on the WIP floor / racks (every location that is not FG / O/S / NCM).</summary>
    public decimal WipFloorQuantity { get; init; }

    /// <summary>True when the part has any floor stock at all.</summary>
    public bool HasAnyFloorQuantity =>
        FinishedGoodsFloorQuantity > 0m
        || OutsideServiceFloorQuantity > 0m
        || NonConformingFloorQuantity > 0m
        || WipFloorQuantity > 0m;
}
