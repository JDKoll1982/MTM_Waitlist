namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What this machine's configuration is, and — when it is not configured — which of the four reasons applies
/// (FR-004, FR-006, FR-009).
/// </summary>
/// <param name="IsConfigured">
/// True only when the store holds this machine's record and its name. A machine that is not configured does not
/// reach the main screens by any route (FR-006, FR-041).
/// </param>
/// <param name="DisplayName">
/// The machine's <c>core_computers_registry.display_name</c>, or <c>null</c> when no row was read. It is reported
/// even when the verdict is unconfigured, because what was read is a fact about the machine either way.
/// </param>
/// <param name="Description">
/// The machine's <c>core_computers_registry.description</c>, or <c>null</c> when the store holds none or no row
/// was read.
/// </param>
/// <param name="UnconfiguredReason">
/// One of <see cref="MachineConfigurationReasons"/>, or <c>null</c> when <paramref name="IsConfigured"/> is true.
/// The reason is specific to the cause because the remedy is offered only where it could work (FR-017).
/// </param>
/// <remarks>
/// This machine's identity, and nothing about pictures: where it reads its pictures from is held once for the
/// whole plant, so a machine that names no folder of its own is configured rather than half-built (FR-040).
/// </remarks>
public sealed record MachineConfigurationState(
    bool IsConfigured,
    string? DisplayName,
    string? Description,
    string? UnconfiguredReason);
