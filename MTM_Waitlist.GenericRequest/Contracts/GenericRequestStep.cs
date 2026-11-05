namespace MTM_Waitlist.GenericRequest.Contracts;

/// <summary>
/// One step of a generic request, in the order the flow normally visits them.
/// </summary>
/// <remarks>
/// The order here is the nominal order. <see cref="Services.GenericRequestFlowRules"/> decides which steps a
/// particular request actually visits, because dunnage, scrap and die are only reached when the item needs them.
/// </remarks>
public enum GenericRequestStep
{
    WorkCenter,
    Category,
    Item,
    Part,
    Dunnage,
    Scrap,
    Die,
    Details,
    Summary,
    Result,
}

/// <summary>
/// The three presentations one screen can show. Exactly one is in force at a time.
/// </summary>
public enum GenericRequestPresentation
{
    Populated,
    Empty,
    Unavailable,
}

/// <summary>
/// Whether a sequence is the finished good or work in progress, or is in a state Visual does not record. The part
/// step derives it from the chosen sequence's operation state and never asks the requester for it.
/// </summary>
/// <remarks>
/// There are only two types, and the plant's own rule decides between them: the last operation a work order still
/// has to run produces the finished good, and every other operation is work in progress (IT, 2026-11-04). There is
/// deliberately no outside-service member: subcontracted work is a resource a WIP tag names on its next-resource
/// line, not a type, so a part at outside service is work in progress like any other.
/// </remarks>
public enum GenericRequestPartStage
{
    /// <summary>An operation the order still has to run, and not its last: the part is work in process.</summary>
    Wip,

    /// <summary>The last operation the work order still has to run, which produces the finished good.</summary>
    FinishedGood,

    /// <summary>Visual records no operation state for the sequence, so the floor is read first.</summary>
    Unknown,
}
