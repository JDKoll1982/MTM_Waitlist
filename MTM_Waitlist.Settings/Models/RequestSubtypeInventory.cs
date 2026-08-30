namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Comprehensive inventory of all request subtypes and their stable identifiers.
/// This inventory captures the authoritative mapping between subtype display names
/// and their stable GUIDs, organized by parent request type.
/// 
/// Key Facts:
/// - Subtype display names are NOT globally unique (e.g., "Pickup" and "Bring" appear under multiple parents)
/// - Stable GUIDs ARE globally unique, enabling reliable database references
/// - Subtypes inherit images from parent request types when no override exists
/// 
/// Source: Assets/Config/waitlist-request-types.json (id field added to each subtype object in subtypes array)
/// Last Updated: 2026-08-18
/// Total Count: 24 subtypes across 8 request types
/// </summary>
public static class RequestSubtypeInventory
{
    /// <summary>
    /// Complete inventory of all subtypes organized by parent request type.
    /// </summary>
    public static readonly IReadOnlyList<RequestSubtypeGroup> Groups = new[]
    {
        // Pickup (6 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("7bb056da-2dfd-4da5-824c-cff0973544fb"),
            ParentDisplayName = "Pickup",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("feaa6b5e-13db-4ad8-9c17-188f40ca41ed"), DisplayName = "Pickup Other" },
                new RequestSubtypeItem { StableId = new Guid("1a50ee24-6959-4242-9853-9b0e7ab19074"), DisplayName = "Pickup NCM" },
                new RequestSubtypeItem { StableId = new Guid("7bc50583-6bc1-4ffc-94e3-40f2a21ae6d7"), DisplayName = "Pickup WIP" },
                new RequestSubtypeItem { StableId = new Guid("efb993e9-be75-4059-a619-be67519c9bc5"), DisplayName = "Pickup FG" },
                new RequestSubtypeItem { StableId = new Guid("b46bab54-8067-436f-ac8b-d0531538422e"), DisplayName = "Pickup Coil" },
                new RequestSubtypeItem { StableId = new Guid("f6113875-95ba-4469-959f-c91979266596"), DisplayName = "Pickup Flatstock" },
            }
        },
        // Other (1 subtype)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("8ee9f259-e404-4d4f-8f20-b5dd7c1c220f"),
            ParentDisplayName = "Other",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("3be33961-67ad-4fb8-9da1-937996d8bccb"), DisplayName = "General Text Entry" },
            }
        },
        // Coil (5 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("20f434cb-59f2-4ecb-a623-84ff5fa3bed1"),
            ParentDisplayName = "Coil",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("8c5dce16-b806-4165-b314-2368bb97f6d7"), DisplayName = "Bring" },
                new RequestSubtypeItem { StableId = new Guid("340966b0-d6a3-45d9-890e-21a01a3ad95d"), DisplayName = "Pickup" },
                new RequestSubtypeItem { StableId = new Guid("9499cd7c-a8d0-4f5f-b1b7-8f5c2b03c258"), DisplayName = "Wrong Coil @ press" },
                new RequestSubtypeItem { StableId = new Guid("8048b55c-02e8-4797-8c9e-a35fda586650"), DisplayName = "Need Riser Table" },
                new RequestSubtypeItem { StableId = new Guid("4ad1825a-4fa5-4f9f-8ed3-0cc13f4de1bd"), DisplayName = "Need Coil Turned around" },
            }
        },
        // Scrap (3 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("90dc8c5b-6a66-4cd4-94c1-5fc634363f5d"),
            ParentDisplayName = "Scrap",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("73371828-06ce-49f1-9480-541e2498dc5d"), DisplayName = "Empty" },
                new RequestSubtypeItem { StableId = new Guid("185903aa-6f85-4c90-a31e-30e1fa855d0e"), DisplayName = "Pickup Hopper, do not return" },
                new RequestSubtypeItem { StableId = new Guid("bab039e8-ea41-47af-a332-a2853f2148e0"), DisplayName = "Bring Hopper" },
            }
        },
        // Flatstock (3 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("805c5b0f-815f-46bf-b5a7-73de5d74fa1f"),
            ParentDisplayName = "Flatstock",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("bb93c8a1-3921-4e8b-8874-10b9e5215bb3"), DisplayName = "Bring" },
                new RequestSubtypeItem { StableId = new Guid("ccb5ebd3-df70-4405-992f-4632377059a5"), DisplayName = "Pickup" },
                new RequestSubtypeItem { StableId = new Guid("4d8f6d3b-6116-4d07-bd4a-3cc420b8001f"), DisplayName = "Wrong Flatstock @ Workcenter" },
            }
        },
        // Table Handling (2 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("b0fc9058-6c74-4171-9f46-11d9b4332b51"),
            ParentDisplayName = "Table Handling",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("dcbcc811-b52d-4b2f-810d-cc34200e2825"), DisplayName = "Table Place Parts" },
                new RequestSubtypeItem { StableId = new Guid("358870f2-e866-4e12-bd8a-e42f74b784f4"), DisplayName = "Table Remove Parts" },
            }
        },
        // Die Handling (4 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("be310bec-a74d-4242-a1a8-6220557d8700"),
            ParentDisplayName = "Die Handling",
            Subtypes = new[]
            {
                new RequestSubtypeItem { StableId = new Guid("20108458-386b-4337-a3c8-ba2e0481d930"), DisplayName = "Bring Die" },
                new RequestSubtypeItem { StableId = new Guid("20bc250d-4c54-4afb-a678-7190b919ada3"), DisplayName = "Pull Die and Put Away" },
                new RequestSubtypeItem { StableId = new Guid("36652f24-b2da-47d2-84ca-caaff1a608c4"), DisplayName = "Pull Die and Take to Die Shop" },
                new RequestSubtypeItem { StableId = new Guid("53ec191c-f74c-4aba-9f15-55ef3c9f5cc5"), DisplayName = "Pull Die and Leave @ press" },
            }
        },
        // Forklift Assist (0 subtypes)
        new RequestSubtypeGroup
        {
            ParentRequestTypeId = new Guid("11a182a7-507c-4069-9763-b902bc7fe8a0"),
            ParentDisplayName = "Forklift Assist",
            Subtypes = Array.Empty<RequestSubtypeItem>()
        },
    };

    /// <summary>
    /// Gets a subtype item by its stable identifier.
    /// </summary>
    /// <param name="stableId">The stable GUID identifier</param>
    /// <returns>A tuple with the parent group and subtype item, or (null, null) if not found</returns>
    public static (RequestSubtypeGroup? group, RequestSubtypeItem? item) GetById(Guid stableId)
    {
        foreach (var group in Groups)
        {
            var item = group.Subtypes.FirstOrDefault(s => s.StableId == stableId);
            if (item != null)
            {
                return (group, item);
            }
        }
        return (null, null);
    }

    /// <summary>
    /// Gets all subtypes for a specific parent request type.
    /// </summary>
    /// <param name="parentRequestTypeId">The stable GUID of the parent request type</param>
    /// <returns>The subtype group for this parent, or null if not found</returns>
    public static RequestSubtypeGroup? GetByParentId(Guid parentRequestTypeId) =>
        Groups.FirstOrDefault(group => group.ParentRequestTypeId == parentRequestTypeId);

    /// <summary>
    /// Gets a subtype by parent request type name and subtype name.
    /// Note: Use GetById() when possible, as names can change and are not globally unique.
    /// </summary>
    /// <param name="parentDisplayName">The display name of the parent request type</param>
    /// <param name="subtypeDisplayName">The display name of the subtype</param>
    /// <returns>A tuple with the parent group and subtype item, or (null, null) if not found</returns>
    public static (RequestSubtypeGroup? group, RequestSubtypeItem? item) GetByDisplayNames(
        string parentDisplayName, string subtypeDisplayName)
    {
        var group = Groups.FirstOrDefault(g =>
            string.Equals(g.ParentDisplayName, parentDisplayName, StringComparison.OrdinalIgnoreCase));

        if (group == null)
        {
            return (null, null);
        }

        var item = group.Subtypes.FirstOrDefault(s =>
            string.Equals(s.DisplayName, subtypeDisplayName, StringComparison.OrdinalIgnoreCase));

        return (group, item);
    }

    /// <summary>
    /// Validates that a given GUID exists in the inventory.
    /// Useful for error handling when processing overrides.
    /// </summary>
    /// <param name="stableId">The stable GUID to validate</param>
    /// <returns>True if the GUID is a known subtype ID; false otherwise</returns>
    public static bool IsValidId(Guid stableId) =>
        Groups.Any(group => group.Subtypes.Any(subtype => subtype.StableId == stableId));

    /// <summary>
    /// Gets the total count of all subtypes across all parent groups.
    /// </summary>
    public static int TotalSubtypeCount => Groups.Sum(g => g.Subtypes.Count);
}

/// <summary>
/// Represents a group of subtypes organized under a parent request type.
/// </summary>
public sealed class RequestSubtypeGroup
{
    /// <summary>
    /// Stable GUID of the parent request type.
    /// </summary>
    public Guid ParentRequestTypeId { get; init; }

    /// <summary>
    /// Display name of the parent request type.
    /// </summary>
    public string ParentDisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Collection of all subtypes under this parent.
    /// </summary>
    public IReadOnlyList<RequestSubtypeItem> Subtypes { get; init; } = Array.Empty<RequestSubtypeItem>();
}

/// <summary>
/// Represents a single request subtype in the inventory.
/// Immutable record containing stable ID and display name.
/// </summary>
public sealed class RequestSubtypeItem
{
    /// <summary>
    /// Stable globally-unique GUID identifier that never changes, even if display name is renamed.
    /// This is the key used in config_images_locations.scope_item_id for this subtype.
    /// </summary>
    public Guid StableId { get; init; }

    /// <summary>
    /// Display name shown in UI and configuration files.
    /// NOT globally unique; may be shared across subtypes under different parents (e.g., "Pickup" appears twice).
    /// Can change via JSON update without affecting stored overrides.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// The default image path for request subtypes.
    /// Same as request type default, used when no override or JSON config exists.
    /// </summary>
    public string DefaultImagePath => ImageLocationDefaults.RequestSubtypeDefaultPath;
}
