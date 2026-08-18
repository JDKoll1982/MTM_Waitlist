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
    public const string RequestTypeDefaultPath = "Assets\\Images\\default-request-type.png";

    /// <summary>
    /// Default image for request subtypes. Used when no override, subtype JSON imagePath, or parent request type image is set.
    /// Falls back through cascade: Subtype Override → Subtype JSON → Parent Request Type → Request Type Default
    /// Image dimensions: Square (100x100px minimum recommended)
    /// Format: PNG, JPG, JPEG
    /// </summary>
    public const string RequestSubtypeDefaultPath = "Assets\\Images\\default-request-type.png";

    /// <summary>
    /// Default image for work centers. Used when no override exists.
    /// Note: Work centers have no JSON config; only database override or default.
    /// Image dimensions: Square (100x100px minimum recommended)
    /// Format: PNG, JPG, JPEG
    /// </summary>
    public const string WorkCenterDefaultPath = "Assets\\Images\\default-workstation-image.png";

    /// <summary>
    /// Gets the default image path for a given image location scope.
    /// </summary>
    /// <param name="scope">The scope type: request_type, request_subtype, or work_center</param>
    /// <returns>The relative path to the default image file</returns>
    /// <exception cref="ArgumentException">Thrown if scope is not recognized</exception>
    public static string GetDefaultPathByScope(ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.RequestType => RequestTypeDefaultPath,
        ImageLocationScope.RequestSubtype => RequestSubtypeDefaultPath,
        ImageLocationScope.WorkCenter => WorkCenterDefaultPath,
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };

    /// <summary>
    /// Gets the default image path for a given scope string.
    /// </summary>
    /// <param name="scopeString">The scope as a string: request_type, request_subtype, or work_center</param>
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
            _ => throw new ArgumentException($"Unknown scope: {scopeString}", nameof(scopeString))
        };
    }
}
