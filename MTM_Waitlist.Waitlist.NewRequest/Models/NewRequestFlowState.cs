namespace MTM_Waitlist.Module_Waitlist.Models;

using MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Accumulates the user's choices as they move through the New Request wizard.
/// A single instance is created when the green + button is tapped and is threaded
/// through the wizard pages as the navigation parameter so every step shares the
/// same in-progress request.
/// </summary>
/// <remarks>
/// Re-laid for specs/004-unified-card-item-picker (US1): the wizard asks for a work centre, a Category and an
/// Item, then at most the one answer the Item's stored configuration requires — so the accumulated choice is the
/// Category/Item pair rather than a request type and a subtype (FR-003, FR-004).
/// </remarks>
public sealed class NewRequestFlowState
{
    public string Building { get; set; } = string.Empty;

    public string WorkCenter { get; set; } = string.Empty;

    /// <summary>The umbrella Category the requester chose.</summary>
    public RequestCategory? Category { get; set; }

    /// <summary>The Item the requester chose under <see cref="Category"/>.</summary>
    public RequestItemDefinition? Item { get; set; }

    /// <summary>
    /// The chosen Item's stored behaviour, read once as the Item step was entered (FR-013, FR-024). It is what
    /// the Details step renders from — never the Item's identity (FR-013).
    /// </summary>
    public RequestItemConfiguration? ItemConfiguration { get; set; }

    /// <summary>
    /// What the requesting work centre's active setup job actually has, resolved <b>between</b> the Category and
    /// the Item step so an unsupported Item is never built, bound or offered (FR-002, contract §3).
    /// </summary>
    public RequestJobPartAvailability Availability { get; set; } = RequestJobPartAvailability.None;

    public string? InputValue { get; set; }

    /// <summary>
    /// The dies the operator chose at the die step, in the job's own order, when the Item they picked asks which
    /// die the request is for (FR-054). Empty for every other Item, and for a request raised before the step
    /// existed — so the ordinary one-request wizard is untouched.
    /// </summary>
    /// <remarks>
    /// The request's one value column holds one die, so several dies can never be folded into one entry: this list
    /// is what the confirmation step reads to raise one request per die (D22).
    /// </remarks>
    public IReadOnlyList<RequestDiePart> SelectedDies { get; set; } = Array.Empty<RequestDiePart>();

    /// <summary>
    /// The dunnage part the operator ended on at the dunnage step, when it is <b>not</b> one the job carries —
    /// their substitute (FR-049). It is kept so returning to the step still shows the card they chose; the value
    /// the request stores is <see cref="InputValue"/>, assigned or substituted alike.
    /// </summary>
    public RequestDunnagePart? SelectedDunnagePart { get; set; }

    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    public string RequesterEmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Builds the <see cref="WaitlistRequestDraft"/>s the accumulated wizard state produces: <b>one per die</b>
    /// the operator chose, and exactly one for every other Item — so the change adds requests rather than
    /// replacing the single-request path the wizard always had (FR-054).
    /// </summary>
    public IReadOnlyList<WaitlistRequestDraft> ToDrafts()
    {
        var dies = SelectedDies ?? Array.Empty<RequestDiePart>();

        return dies.Count == 0
            ? [ToDraftFor(InputValue)]
            : dies.Select(die => ToDraftFor(die.Label)).ToArray();
    }

    /// <summary>
    /// The single draft the rest of the wizard reads. It is the first of <see cref="ToDrafts"/> so the two entry
    /// points can never disagree about what the request carries.
    /// </summary>
    public WaitlistRequestDraft ToDraft() => ToDrafts()[0];

    /// <summary>
    /// What the wizard will raise, one line per entry, in the order it will be raised — the value each request
    /// carries (FR-054).
    /// </summary>
    /// <remarks>
    /// Both review steps render <b>this</b> rather than the request's single <see cref="InputValue"/>, which is what
    /// they did until 2026-09-20: with two dies chosen the preview and the confirmation each showed one die, so both
    /// understated a run that was about to raise two requests. Reading the same list the submission reads is what
    /// stops the two from drifting apart again.
    /// </remarks>
    public IReadOnlyList<string> DetailLines() =>
        ToDrafts()
            .Select(draft => draft.InputValue)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

    /// <summary>
    /// Builds one final draft carrying the Category and the Item the requester chose (FR-004) and the one value
    /// that entry is for — the die it is about, or the answer the Item's configuration required. The two legacy
    /// members are left empty because no wizard step produces them any more.
    /// </summary>
    private WaitlistRequestDraft ToDraftFor(string? inputValue) => new()
    {
        Building = Building.Trim(),
        WorkCenter = WorkCenter.Trim(),
        Category = Category?.ToString() ?? string.Empty,
        Item = Item?.Id ?? string.Empty,
        InputValue = inputValue,
        ActiveSetupJobId = WorkCenter.Trim(),
        WorkCenterName = WorkCenter.Trim(),
        RequesterEmployeeNumber = RequesterEmployeeNumber.Trim(),
        RequesterEmployeeName = RequesterEmployeeName.Trim(),
        RequestedUtc = DateTimeOffset.UtcNow,
    };
}
