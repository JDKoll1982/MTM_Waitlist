namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Comprehensive inventory of all request types and their stable identifiers.
/// This inventory captures the authoritative mapping between request type display names
/// and their stable GUIDs, ensuring that renaming operations never orphan stored image overrides.
/// 
/// Source: Assets/Config/waitlist-request-types.json (id field added to each requestType object)
/// Last Updated: 2026-08-18
/// Total Count: 8 request types
/// </summary>
public static class RequestTypeInventory
{
    /// <summary>
    /// Complete inventory of all active request types with stable identifiers.
    /// </summary>
    public static readonly IReadOnlyList<RequestTypeItem> Items = new[]
    {
        new RequestTypeItem
        {
            StableId = new Guid("7bb056da-2dfd-4da5-824c-cff0973544fb"),
            DisplayName = "Pickup",
            Description = "Move parts/materials from one location to another",
            SubtypeCount = 6,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("8ee9f259-e404-4d4f-8f20-b5dd7c1c220f"),
            DisplayName = "Other",
            Description = "General requests not covered by specific types",
            SubtypeCount = 1,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("20f434cb-59f2-4ecb-a623-84ff5fa3bed1"),
            DisplayName = "Coil",
            Description = "Handle and manage coil inventory",
            SubtypeCount = 5,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("90dc8c5b-6a66-4cd4-94c1-5fc634363f5d"),
            DisplayName = "Scrap",
            Description = "Manage scrap material disposal",
            SubtypeCount = 3,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("805c5b0f-815f-46bf-b5a7-73de5d74fa1f"),
            DisplayName = "Flatstock",
            Description = "Handle flatstock materials",
            SubtypeCount = 3,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("b0fc9058-6c74-4171-9f46-11d9b4332b51"),
            DisplayName = "Table Handling",
            Description = "Manage table work center operations",
            SubtypeCount = 2,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("be310bec-a74d-4242-a1a8-6220557d8700"),
            DisplayName = "Die Handling",
            Description = "Handle die setup and changes",
            SubtypeCount = 4,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        },
        new RequestTypeItem
        {
            StableId = new Guid("11a182a7-507c-4069-9763-b902bc7fe8a0"),
            DisplayName = "Forklift Assist",
            Description = "Request forklift assistance",
            SubtypeCount = 0,
            DefaultImagePath = ImageLocationDefaults.RequestTypeDefaultPath
        }
    };

    /// <summary>
    /// Gets a request type item by its stable identifier.
    /// </summary>
    /// <param name="stableId">The stable GUID identifier</param>
    /// <returns>The matching RequestTypeItem, or null if not found</returns>
    public static RequestTypeItem? GetById(Guid stableId) =>
        Items.FirstOrDefault(item => item.StableId == stableId);

    /// <summary>
    /// Gets a request type item by its display name.
    /// Note: Display names can change, so always prefer using GetById() for persistence.
    /// </summary>
    /// <param name="displayName">The display name of the request type</param>
    /// <returns>The matching RequestTypeItem, or null if not found</returns>
    public static RequestTypeItem? GetByDisplayName(string displayName) =>
        Items.FirstOrDefault(item =>
            string.Equals(item.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Validates that a given GUID exists in the inventory.
    /// Useful for error handling when processing overrides.
    /// </summary>
    /// <param name="stableId">The stable GUID to validate</param>
    /// <returns>True if the GUID is a known request type ID; false otherwise</returns>
    public static bool IsValidId(Guid stableId) =>
        Items.Any(item => item.StableId == stableId);
}

/// <summary>
/// Represents a single request type in the inventory.
/// Immutable record containing stable ID, display name, and metadata.
/// </summary>
public sealed class RequestTypeItem
{
    /// <summary>
    /// Stable GUID identifier that never changes, even if display name is renamed.
    /// This is the key used in config_images_locations.scope_item_id for this request type.
    /// </summary>
    public Guid StableId { get; init; }

    /// <summary>
    /// Display name shown in UI and configuration files.
    /// Can change via JSON update without affecting stored overrides.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of this request type's purpose.
    /// Used for UI tooltips and documentation.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Number of subtypes defined for this request type.
    /// Used for UI validation and inventory audits.
    /// </summary>
    public int SubtypeCount { get; init; }

    /// <summary>
    /// The default image path for this request type.
    /// Returned when no override or JSON config exists.
    /// </summary>
    public string DefaultImagePath { get; init; } = ImageLocationDefaults.RequestTypeDefaultPath;
}
