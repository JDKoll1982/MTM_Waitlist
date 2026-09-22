namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Enumeration of all supported image location scopes.
/// Maps to the 'scope' column in config_images_locations database table.
/// </summary>
public enum ImageLocationScope
{
    /// <summary>
    /// Work center image representing each facility work center in selection and detail surfaces.
    /// Database value: "work_center"
    /// Default image: Assets\Placeholders\default-no-image.png (the application's one no-image placeholder)
    /// Cascade: Work Center Override → Default (no JSON config)
    /// Inventory: Dynamic (from setup_workstations_catalog, live database)
    /// </summary>
    WorkCenter,

    /// <summary>
    /// Request Item image (e.g. pickup-coil, other), keyed by Item code.
    /// Database value: "request_item"
    /// Cascade: Item Override → Category family (<see cref="RequestCategory"/>) → the existing placeholder
    /// Inventory: 23 canonical Items (RequestItemCatalog)
    /// See specs/004-unified-card-item-picker/contracts/card-and-identifier.md §4.
    /// </summary>
    RequestItem,

    /// <summary>
    /// Request Category image family (Pickup / Deliver / Assist / Other), keyed by Category code — the
    /// family an Item falls back to when it has no override of its own.
    /// Database value: "request_category"
    /// Cascade: Category Override → the existing placeholder
    /// Inventory: the four "RequestCategory" members
    /// </summary>
    RequestCategory
}

/// <summary>
/// Extension methods for ImageLocationScope enumeration.
/// </summary>
public static class ImageLocationScopeExtensions
{
    /// <summary>
    /// Converts ImageLocationScope enum to database string representation.
    /// </summary>
    /// <param name="scope">The scope enumeration value</param>
    /// <returns>The string representation used in config_images_locations.scope column</returns>
    public static string ToDatabaseString(this ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.WorkCenter => "work_center",
        ImageLocationScope.RequestItem => "request_item",
        ImageLocationScope.RequestCategory => "request_category",
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };

    /// <summary>
    /// Converts a database string to ImageLocationScope enum.
    /// </summary>
    /// <param name="scopeString">The string from config_images_locations.scope column</param>
    /// <returns>The corresponding ImageLocationScope enumeration value</returns>
    /// <exception cref="ArgumentException">Thrown if scopeString is not recognized</exception>
    public static ImageLocationScope ToScope(this string scopeString)
    {
        if (string.IsNullOrWhiteSpace(scopeString))
        {
            throw new ArgumentException("Scope string cannot be null or empty", nameof(scopeString));
        }

        return scopeString.ToLowerInvariant() switch
        {
            "work_center" => ImageLocationScope.WorkCenter,
            "request_item" => ImageLocationScope.RequestItem,
            "request_category" => ImageLocationScope.RequestCategory,
            _ => throw new ArgumentException($"Unknown scope: {scopeString}", nameof(scopeString))
        };
    }

    /// <summary>
    /// Determines if this scope has JSON configuration support. No live scope does: the request-type and subtype
    /// scopes that read <c>waitlist-request-types.json</c> retired with the vocabulary (FR-023), and the Item,
    /// Category and work-center scopes are configured as database rows.
    /// </summary>
    /// <param name="scope">The scope enumeration value</param>
    /// <returns>Always false for a live scope</returns>
    public static bool HasJsonConfig(this ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.WorkCenter => false, // Work centers are dynamic; no JSON config
        ImageLocationScope.RequestItem => false, // Items are configured as database rows, not JSON
        ImageLocationScope.RequestCategory => false, // Categories are configured as database rows, not JSON
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };
}
