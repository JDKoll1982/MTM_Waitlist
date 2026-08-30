namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Represents the work center catalog inventory mapping.
/// Unlike request types and subtypes (which are static, defined in JSON),
/// work centers are dynamic and loaded from the database table setup_workstations_catalog.
/// 
/// This model provides the contract for accessing work center identifiers and their image mappings.
/// The actual inventory is loaded from the database at runtime, not from static configuration.
/// 
/// Source: setup_workstations_catalog database table
/// Row Key: setup_workstations_catalog.id (numeric BIGINT)
/// Image Scope: "work_center" in config_images_locations
/// Default Image: Assets\Images\default-workstation-image.png
/// </summary>
public sealed class WorkCenterInventory
{
    /// <summary>
    /// Collection of all active work centers in the catalog.
    /// Loaded from setup_workstations_catalog at application startup and kept in sync.
    /// </summary>
    private readonly IReadOnlyList<WorkCenterItem> _items;

    /// <summary>
    /// Gets the current work center inventory.
    /// </summary>
    public IReadOnlyList<WorkCenterItem> Items => _items;

    /// <summary>
    /// Initializes a new work center inventory with the given items.
    /// </summary>
    /// <param name="items">The collection of work center items from the database</param>
    /// <exception cref="ArgumentNullException">Thrown if items is null</exception>
    public WorkCenterInventory(IReadOnlyList<WorkCenterItem> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>
    /// Gets a work center item by its numeric ID.
    /// </summary>
    /// <param name="workCenterId">The numeric ID from setup_workstations_catalog.id</param>
    /// <returns>The matching WorkCenterItem, or null if not found</returns>
    public WorkCenterItem? GetById(long workCenterId) =>
        _items.FirstOrDefault(item => item.WorkCenterId == workCenterId);

    /// <summary>
    /// Gets a work center item by its display name.
    /// Note: Work center names can change, so always prefer using GetById() for persistence.
    /// </summary>
    /// <param name="displayName">The display name of the work center</param>
    /// <returns>The matching WorkCenterItem, or null if not found</returns>
    public WorkCenterItem? GetByDisplayName(string displayName) =>
        _items.FirstOrDefault(item =>
            string.Equals(item.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all work centers in a specific building.
    /// </summary>
    /// <param name="building">The building location</param>
    /// <returns>Enumerable of work center items in that building</returns>
    public IEnumerable<WorkCenterItem> GetByBuilding(string building) =>
        _items.Where(item =>
            string.Equals(item.Building, building, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets only the active work centers.
    /// </summary>
    /// <returns>Enumerable of active work center items</returns>
    public IEnumerable<WorkCenterItem> GetActive() =>
        _items.Where(item => item.IsActive);

    /// <summary>
    /// Validates that a given numeric ID exists in the inventory.
    /// Useful for error handling when processing overrides.
    /// </summary>
    /// <param name="workCenterId">The numeric ID to validate</param>
    /// <returns>True if the ID is a known work center ID; false otherwise</returns>
    public bool IsValidId(long workCenterId) =>
        _items.Any(item => item.WorkCenterId == workCenterId);

    /// <summary>
    /// Gets the total count of work centers in the inventory.
    /// </summary>
    public int TotalCount => _items.Count;

    /// <summary>
    /// Gets the count of active work centers only.
    /// </summary>
    public int ActiveCount => _items.Count(item => item.IsActive);
}

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

/// <summary>
/// Interface for accessing and managing the work center inventory.
/// Abstracts the data source (database) from consumers.
/// </summary>
public interface IWorkCenterInventoryService
{
    /// <summary>
    /// Gets the current work center inventory.
    /// The inventory should be loaded at application startup and kept in sync with the database.
    /// </summary>
    /// <returns>The work center inventory</returns>
    WorkCenterInventory GetInventory();

    /// <summary>
    /// Refreshes the work center inventory from the database.
    /// Called periodically or when the database is known to have changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RefreshInventoryAsync();
}
