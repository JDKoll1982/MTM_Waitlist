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
/// Whether a part is a work in progress or a finished good. The part step is the only place that sets it.
/// </summary>
public enum GenericRequestPartStage
{
    Wip,
    FinishedGood,
}
