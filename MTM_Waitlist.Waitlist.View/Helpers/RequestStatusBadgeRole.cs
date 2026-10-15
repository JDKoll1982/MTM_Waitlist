namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The states a request's status column can read: the four lifecycle words, the overdue marking, and the four
/// conditions a material handler can report. One role per state, so the colour a viewer sees is decided in one
/// place rather than in markup.
/// </summary>
/// <remarks>
/// The vocabulary is the eight words FR-102 settles plus the overdue marking FR-014 adds, which is why the four
/// conditions carry roles of their own rather than sharing one: the column must be able to tell which condition
/// was reported, and a role is how it does that.
/// </remarks>
public enum RequestStatusBadgeRole
{
    /// <summary>The status is missing or unrecognised, so no word and no colour are warranted.</summary>
    None,

    /// <summary>Waiting — queued, nobody has claimed it yet.</summary>
    Waiting,

    /// <summary>In Progress — a handler has claimed it.</summary>
    InProgress,

    /// <summary>Done — finished.</summary>
    Done,

    /// <summary>Cancelled — withdrawn or refused, and kept rather than deleted.</summary>
    Cancelled,

    /// <summary>Overdue — past its time and carrying no reported condition (FR-014).</summary>
    Overdue,

    /// <summary>The handler reported <c>Product Buried</c> (FR-102).</summary>
    ProductBuried,

    /// <summary>The handler reported <c>Cant Find Product</c> (FR-102).</summary>
    CantFindProduct,

    /// <summary>The handler reported <c>Changing Forklift</c> (FR-102).</summary>
    ChangingForklift,

    /// <summary>The handler reported <c>Changing Propane</c> (FR-102).</summary>
    ChangingPropane,
}
