namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Represents a single work center from the setup_workstations_catalog.
/// Immutable record containing ID, name, location, and metadata.
/// </summary>
public sealed class WorkCenterItem
{
    /// <summary>
    /// Numeric primary key from setup_workstations_catalog.id (BIGINT AUTO_INCREMENT).
    /// This is the row key used in config_images_locations.scope_item_id for this work center.
    /// Stable and never changes for a given work center.
    /// </summary>
    public long WorkCenterId { get; init; }

    /// <summary>
    /// Display name of the work center (e.g., "Press 1", "Brake 3").
    /// From setup_workstations_catalog.workstation_name.
    /// Can change without affecting stored image overrides (because we use numeric ID).
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Building or facility location of this work center.
    /// From setup_workstations_catalog.building (e.g., "Expo Drive").
    /// Used for grouping and filtering in the UI.
    /// </summary>
    public string Building { get; init; } = string.Empty;

    /// <summary>
    /// Sort order rank for UI display.
    /// From setup_workstations_catalog.sort_rank.
    /// Lower values appear first in lists.
    /// </summary>
    public int SortRank { get; init; }

    /// <summary>
    /// Indicates whether this work center is active.
    /// From setup_workstations_catalog.is_active.
    /// Inactive work centers should generally be excluded from dropdowns and dialogs.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// The default image path for work centers.
    /// Returned when no override exists in config_images_locations.
    /// </summary>
    public string DefaultImagePath => ImageLocationDefaults.WorkCenterDefaultPath;

    /// <summary>
    /// Gets a human-readable display label for the work center.
    /// Format: "[Building] WorkCenterName" (e.g., "[Expo Drive] Press 1")
    /// </summary>
    public string GetDisplayLabel() => $"[{Building}] {DisplayName}";
}
