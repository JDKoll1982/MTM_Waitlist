namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// The outcome of writing this machine's configuration (FR-007, FR-018).
/// </summary>
/// <param name="Succeeded">
/// True only when the display name, the description and all three picture sources were written.
/// </param>
/// <param name="RefusalReason">
/// One of <see cref="MachineConfigurationRefusals"/>, or <c>null</c> when the save succeeded. A refusal is an
/// answer the screen acts on — most often asking for a different display name — so it is returned rather than
/// raised.
/// </param>
/// <param name="ComputerId">
/// The machine's <c>core_computers_registry.id</c> once it exists, or <c>null</c> when the save was refused
/// before any row was written. A first save creates the row; a later one updates the row the machine already has.
/// </param>
/// <remarks>
/// <b>Only what this service can explain is reported here.</b> A store that cannot be reached throws, because an
/// outage is not a refusal: the launch pipeline has its own unavailable state for that, and turning an outage
/// into a refusal would tell the operator to change their answer when the answer was never the problem.
/// </remarks>
public sealed record MachineConfigurationSaveResult(
    bool Succeeded,
    string? RefusalReason,
    long? ComputerId);
