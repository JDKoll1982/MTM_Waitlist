namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Defines default image paths for all image location scopes.
/// These paths are used as the final fallback when no override or JSON config exists.
/// All paths are relative to the application root and assume Assets folder deployment.
/// </summary>
public static class ImageLocationDefaults
{
    /// <summary>
    /// Default image for request types. Used when no override or JSON imagePath is set.
    /// Image dimensions: Square (100x100px minimum recommended)
    /// Format: PNG, JPG, JPEG
    /// </summary>
    public const string RequestTypeDefaultPath = "Assets\\Placeholders\\default-request-type.png";

    /// <summary>
    /// Default image for request subtypes. Used when no override, subtype JSON imagePath, or parent request type image is set.
    /// Falls back through cascade: Subtype Override → Subtype JSON → Parent Request Type → Request Type Default
    /// Image dimensions: Square (100x100px minimum recommended)
    /// Format: PNG, JPG, JPEG
    /// </summary>
    public const string RequestSubtypeDefaultPath = "Assets\\Placeholders\\default-request-type.png";

    /// <summary>
    /// Default image for work centers. Used when no override exists.
    /// Note: Work centers have no JSON config; only database override or default.
    /// Image dimensions: Square (100x100px minimum recommended)
    /// Format: PNG, JPG, JPEG
    /// </summary>
    public const string WorkCenterDefaultPath = "Assets\\Placeholders\\default-workstation-image.png";

    /// <summary>
    /// Default image for request Items. Used when neither an Item override nor the Item's Category family
    /// has an image. See specs/004-unified-card-item-picker/contracts/card-and-identifier.md §4.
    /// </summary>
    public const string RequestItemDefaultPath = "Assets\\Placeholders\\default-request-type.png";

    /// <summary>
    /// Default image for a request Category family (Pickup / Deliver / Assist / Other).
    /// </summary>
    public const string RequestCategoryDefaultPath = "Assets\\Placeholders\\default-request-type.png";

    /// <summary>
    /// Gets the default image path for a given image location scope.
    /// </summary>
    /// <param name="scope">The scope type: request_type, request_subtype, work_center, request_item, or request_category</param>
    /// <returns>The relative path to the default image file</returns>
    /// <exception cref="ArgumentException">Thrown if scope is not recognized</exception>
    public static string GetDefaultPathByScope(ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.RequestType => RequestTypeDefaultPath,
        ImageLocationScope.RequestSubtype => RequestSubtypeDefaultPath,
        ImageLocationScope.WorkCenter => WorkCenterDefaultPath,
        ImageLocationScope.RequestItem => RequestItemDefaultPath,
        ImageLocationScope.RequestCategory => RequestCategoryDefaultPath,
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };

    /// <summary>
    /// Gets the default image path for a given scope string.
    /// </summary>
    /// <param name="scopeString">The scope as a string: request_type, request_subtype, work_center, request_item, or request_category</param>
    /// <returns>The relative path to the default image file</returns>
    /// <exception cref="ArgumentException">Thrown if scopeString is not recognized</exception>
    public static string GetDefaultPathByScope(string scopeString)
    {
        if (string.IsNullOrWhiteSpace(scopeString))
        {
            throw new ArgumentException("Scope string cannot be null or empty", nameof(scopeString));
        }

        return scopeString.ToLowerInvariant() switch
        {
            "request_type" => RequestTypeDefaultPath,
            "request_subtype" => RequestSubtypeDefaultPath,
            "work_center" => WorkCenterDefaultPath,
            "request_item" => RequestItemDefaultPath,
            "request_category" => RequestCategoryDefaultPath,
            _ => throw new ArgumentException($"Unknown scope: {scopeString}", nameof(scopeString))
        };
    }
}
