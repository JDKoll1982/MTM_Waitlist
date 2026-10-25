namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// What kind of material or part a part number is, derived from the plant's own Infor Visual classification
/// (<c>PART.PRODUCT_CODE</c> and <c>PART.COMMODITY_CODE</c>) and, for customer-supplied material, the part number's
/// own prefix and description.
/// </summary>
/// <remarks>
/// This is a different question from <see cref="RequestDisposition"/>, which asks what <b>state</b> the job's
/// product is in (finished goods, work in process, out at a vendor, or sitting in a non-conforming area). The two
/// travel together on the snapshot and answer different cells.
/// <para>
/// The taxonomy is grounded in the live install (VISUAL/MTMFG, 2026-10-23): <c>PRODUCT_CODE</c> carries the business
/// category (FG_PRESS, RM_PRESS, MFG_TOOLING, FG_DIE, RM_DIE, the EXP_* expense families) and
/// <c>COMMODITY_CODE</c> carries the material or part kind (E Stamped Item, M Matl Coil, M Fast Fastener, …).
/// <see cref="Unknown"/> means neither code named a kind, and the cell is then left out rather than guessed.
/// </para>
/// </remarks>
public enum RequestPartType
{
    /// <summary>Neither code named a kind (or neither was read).</summary>
    Unknown,

    /// <summary>Coil stock, customer-supplied coil included — <c>M Matl Coil</c>, <c>S Matl Coil</c>.</summary>
    Coil,

    /// <summary>Sheet and strip stock — <c>M Matl Flat</c>.</summary>
    Flatstock,

    /// <summary>Purchased fasteners and hardware — <c>M Fast Fastener</c>.</summary>
    Hardware,

    /// <summary>Bar and round stock — <c>M Matl Round</c>.</summary>
    BarRound,

    /// <summary>Any other raw material — <c>M Matl Net-Shap</c>, <c>S Matl Net-Shap</c>, <c>M Matl Misc</c>.</summary>
    RawOther,

    /// <summary>Customer-supplied material that is not coil or flatstock — <c>M Matl Cust Sup</c>.</summary>
    CustomerSupplied,

    /// <summary>A stamped production part — <c>E Stamped Item</c>.</summary>
    StampedPart,

    /// <summary>A welded or assembled production part — <c>E Assy Item</c>.</summary>
    Assembly,

    /// <summary>A machined production part — <c>E Mach Item</c>.</summary>
    MachinedPart,

    /// <summary>Tooling and dies — <c>T MFG Use Tools</c> or product code <c>MFG_TOOLING</c>.</summary>
    Tooling,

    /// <summary>Die components — <c>M Die Punch</c>, <c>E Tooling</c>.</summary>
    DieComponent,

    /// <summary>An expense item or consumable — any <c>EXP_*</c> product code.</summary>
    Expense,
}
