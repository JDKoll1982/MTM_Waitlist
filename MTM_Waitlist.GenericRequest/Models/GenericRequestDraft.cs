using MTM_Waitlist.GenericRequest.Contracts;

namespace MTM_Waitlist.GenericRequest.Models;

/// <summary>
/// The answers given so far. One step writes its own answer and no step clears another's, so going back keeps
/// everything already chosen.
/// </summary>
public sealed class GenericRequestDraft
{
    public string WorkCenter { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string PartNumber { get; set; } = string.Empty;

    public GenericRequestPartStage PartStage { get; set; } = GenericRequestPartStage.Wip;

    public string Sequence { get; set; } = string.Empty;

    public string DunnageType { get; set; } = string.Empty;

    public string DunnagePart { get; set; } = string.Empty;

    public string ScrapType { get; set; } = string.Empty;

    public string DieNumber { get; set; } = string.Empty;

    public double Quantity { get; set; } = 1;

    public DateTimeOffset? NeededBy { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Whether the chosen item asks for the dunnage page.</summary>
    public bool RequiresDunnage { get; set; }

    /// <summary>Whether the chosen item asks for the scrap page.</summary>
    public bool RequiresScrap { get; set; }

    /// <summary>Whether the chosen item asks for the die page.</summary>
    public bool RequiresDie { get; set; }

    /// <summary>The request number the result step shows once the request has been raised.</summary>
    public string RaisedRequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// What the result step shows. The raise attempt decides it, so an unraised request reads empty and a failed one
    /// reads unavailable. This chunk records the outcome of reaching the end of the flow; the submission that will
    /// decide it for real arrives in a later chunk.
    /// </summary>
    public GenericRequestPresentation ResultPresentation { get; set; } = GenericRequestPresentation.Empty;
}
