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

    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    public string RequesterEmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Builds a final <see cref="WaitlistRequestDraft"/> from the accumulated wizard state. The draft carries the
    /// Category and the Item the requester chose (FR-004); the two legacy members are left empty because no
    /// wizard step produces them any more.
    /// </summary>
    public WaitlistRequestDraft ToDraft() => new()
    {
        Building = Building.Trim(),
        WorkCenter = WorkCenter.Trim(),
        Category = Category?.ToString() ?? string.Empty,
        Item = Item?.Id ?? string.Empty,
        InputValue = InputValue,
        ActiveSetupJobId = WorkCenter.Trim(),
        WorkCenterName = WorkCenter.Trim(),
        RequesterEmployeeNumber = RequesterEmployeeNumber.Trim(),
        RequesterEmployeeName = RequesterEmployeeName.Trim(),
        RequestedUtc = DateTimeOffset.UtcNow,
    };
}
