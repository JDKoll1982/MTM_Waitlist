namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// Accumulates the user's choices as they move through the New Request wizard.
/// A single instance is created when the green + button is tapped and is threaded
/// through the wizard pages as the navigation parameter so every step shares the
/// same in-progress request.
/// </summary>
public sealed class NewRequestFlowState
{
    public string Building { get; set; } = string.Empty;

    public string WorkCenter { get; set; } = string.Empty;

    public NewRequestTypeDefinition? RequestType { get; set; }

    public NewRequestSubtypeDefinition? Subtype { get; set; }

    public string? InputValue { get; set; }

    public string RequesterEmployeeNumber { get; set; } = string.Empty;

    public string RequesterEmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Builds a final <see cref="WaitlistRequestDraft"/> from the accumulated wizard state.
    /// </summary>
    public WaitlistRequestDraft ToDraft() => new()
    {
        Building = Building.Trim(),
        WorkCenter = WorkCenter.Trim(),
        RequestType = RequestType?.RequestType.Trim() ?? string.Empty,
        Subtype = Subtype?.Name,
        InputValue = InputValue,
        ActiveSetupJobId = WorkCenter.Trim(),
        WorkstationName = WorkCenter.Trim(),
        RequesterEmployeeNumber = RequesterEmployeeNumber.Trim(),
        RequesterEmployeeName = RequesterEmployeeName.Trim(),
        RequestedUtc = DateTimeOffset.UtcNow,
    };
}
