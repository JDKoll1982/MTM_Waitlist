namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Enumeration of all supported image location scopes.
/// Maps to the 'scope' column in config_images_locations database table.
/// </summary>
public enum ImageLocationScope
{
    /// <summary>
    /// Top-level request type image (e.g., Pickup, Coil, Scrap, etc.)
    /// Database value: "request_type"
    /// Default image: Assets\Images\default-request-type.png
    /// Inventory: 8 request types (static, defined in waitlist-request-types.json)
    /// </summary>
    RequestType,

    /// <summary>
    /// Request subtype image (e.g., Pickup Other, Pickup NCM, Pickup WIP, etc.)
    /// Database value: "request_subtype"
    /// Default image: Assets\Images\default-request-type.png (same as parent request type)
    /// Cascade: Subtype Override → Subtype JSON → Parent Request Type → Default
    /// Inventory: 24 subtypes across 8 parents (static, defined in waitlist-request-types.json)
    /// Uniqueness: Subtype names are NOT globally unique; GUIDs in JSON are.
    /// </summary>
    RequestSubtype,

    /// <summary>
    /// Work center image representing each facility work center in selection and detail surfaces.
    /// Database value: "work_center"
    /// Default image: Assets\Images\default-workstation-image.png
    /// Cascade: Work Center Override → Default (no JSON config)
    /// Inventory: Dynamic (from setup_workstations_catalog, live database)
    /// </summary>
    WorkCenter
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
        ImageLocationScope.RequestType => "request_type",
        ImageLocationScope.RequestSubtype => "request_subtype",
        ImageLocationScope.WorkCenter => "work_center",
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
            "request_type" => ImageLocationScope.RequestType,
            "request_subtype" => ImageLocationScope.RequestSubtype,
            "work_center" => ImageLocationScope.WorkCenter,
            _ => throw new ArgumentException($"Unknown scope: {scopeString}", nameof(scopeString))
        };
    }

    /// <summary>
    /// Determines if this scope has JSON configuration support.
    /// </summary>
    /// <param name="scope">The scope enumeration value</param>
    /// <returns>True if this scope can be configured via JSON (request_type, request_subtype); false otherwise (work_center)</returns>
    public static bool HasJsonConfig(this ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.RequestType => true,
        ImageLocationScope.RequestSubtype => true,
        ImageLocationScope.WorkCenter => false, // Work centers are dynamic; no JSON config
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };
}
