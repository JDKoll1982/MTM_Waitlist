namespace MTM_Waitlist.Module_Core.Models;

/// <summary>
/// What this machine's configuration is, and — when it is not configured — which of the four reasons applies
/// (FR-004, FR-006, FR-009).
/// </summary>
/// <param name="IsConfigured">
/// True only when the machine is registered, holds a display name, and holds all three of its picture sources.
/// A machine that is not configured does not reach the main screens by any route (FR-006).
/// </param>
/// <param name="DisplayName">
/// The machine's <c>core_computers_registry.display_name</c>, or <c>null</c> when no row was read. It is reported
/// even when the verdict is unconfigured, because what was read is a fact about the machine either way.
/// </param>
/// <param name="Description">
/// The machine's <c>core_computers_registry.description</c>, or <c>null</c> when the store holds none or no row
/// was read.
/// </param>
/// <param name="PictureSources">
/// The machine's <b>live</b> picture sources, one per <see cref="MachineConfigurationSourceKinds"/> entry when it
/// is configured and a subset or nothing when it is not. Sources that have been withdrawn are not returned: they
/// are no longer where this machine reads its pictures from, and the setup screen pre-filling a withdrawn folder
/// would invite the same broken answer back.
/// </param>
/// <param name="UnconfiguredReason">
/// One of <see cref="MachineConfigurationReasons"/>, or <c>null</c> when <paramref name="IsConfigured"/> is true.
/// The reason is specific to the cause because the remedy is offered only where it could work (FR-017).
/// </param>
public sealed record MachineConfigurationState(
    bool IsConfigured,
    string? DisplayName,
    string? Description,
    IReadOnlyList<PictureSource> PictureSources,
    string? UnconfiguredReason);
