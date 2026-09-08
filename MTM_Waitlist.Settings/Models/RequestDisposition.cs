namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Material disposition of a part/order on the shop floor, used to drive the Finished-Product
/// Item leaves (FG / NCM / WIP / Outside Service) in the Type/Category/Item model.
/// Type/Category refactor (2026-09-07): the Finished Product grouping node branches into these
/// dispositions. NCM is handled separately by the defect feature (file 14 Phase 5); FG / WIP /
/// Outside Service are derived from the Infor Visual job state (file 14 Phase 6).
/// Source: WeekendProject/Documents/Request-Config-Template.csv (pickup-fg / pickup-wip / pickup-outside-service).
/// </summary>
public enum RequestDisposition
{
    /// <summary>Unknown / not yet determined (no matching rule fired).</summary>
    Unknown,

    /// <summary>Finished Goods — stock on hand at a finished-goods / shipping location (PART.QTY_ON_HAND / PART_LOCATION.QTY &gt; 0).</summary>
    FinishedGoods,

    /// <summary>Work In Process — open quantity still on the work order / MT_WIP_INVENTORY.</summary>
    WorkInProcess,

    /// <summary>Outside Service — an operation is subcontracted to an outside vendor (OPERATION has VENDOR_ID / SERVICE_ID / SERVICE_PART_ID).</summary>
    OutsideService
}
