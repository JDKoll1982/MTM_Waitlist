using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// Defines default image paths for all image location scopes.
/// </summary>
/// <remarks>
/// <para>
/// Every scope shares <b>one</b> default: <see cref="ImagePicturePolicy.NoImagePath"/>. That is deliberate. These
/// paths are what a surface draws when nothing is configured for the scope, which is exactly the case the
/// application's image rule calls "no image" — so the per-scope artwork that used to answer here (a workstation
/// photo, a request-item tile) has been retired in favour of the one placeholder a person learns to read as
/// "no picture". A scope with genuinely configured artwork gets it from the override, never from here.
/// </para>
/// <para>
/// The paths stay relative to the application root, and the file exists (or the surface falls back to the
/// packaged copy of the same asset through <see cref="ImagePicturePolicy.NoImagePackUri"/>.
/// </para>
/// </remarks>
public static class ImageLocationDefaults
{
    /// <summary>
    /// The one default image for every scope. Image dimensions: square, at least
    /// <see cref="ImagePicturePolicy.MinimumPixels"/> pixels a side (the shape the application accepts).
    /// Format: PNG, JPG, JPEG.
    /// </summary>
    public const string NoImagePath = ImagePicturePolicy.NoImagePath;

    /// <summary>
    /// Default image for work centers. Used when no override exists.
    /// Note: Work centers have no JSON config; only database override or default.
    /// </summary>
    public const string WorkCenterDefaultPath = NoImagePath;

    /// <summary>
    /// Default image for request Items. Used when neither an Item override nor the Item's Category family
    /// has an image. See specs/004-unified-card-item-picker/contracts/card-and-identifier.md §4.
    /// </summary>
    public const string RequestItemDefaultPath = NoImagePath;

    /// <summary>
    /// Default image for a request Category family (Pickup / Deliver / Assist / Other).
    /// </summary>
    public const string RequestCategoryDefaultPath = NoImagePath;

    /// <summary>
    /// Gets the default image path for a given image location scope.
    /// </summary>
    /// <param name="scope">The scope type: work_center, request_item, or request_category</param>
    /// <returns>The relative path to the default image file</returns>
    /// <exception cref="ArgumentException">Thrown if scope is not recognized</exception>
    public static string GetDefaultPathByScope(ImageLocationScope scope) => scope switch
    {
        ImageLocationScope.WorkCenter => WorkCenterDefaultPath,
        ImageLocationScope.RequestItem => RequestItemDefaultPath,
        ImageLocationScope.RequestCategory => RequestCategoryDefaultPath,
        _ => throw new ArgumentException($"Unknown scope: {scope}", nameof(scope))
    };

    /// <summary>
    /// Gets the default image path for a given scope string.
    /// </summary>
    /// <param name="scopeString">The scope as a string: work_center, request_item, or request_category</param>
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
            "work_center" => WorkCenterDefaultPath,
            "request_item" => RequestItemDefaultPath,
            "request_category" => RequestCategoryDefaultPath,
            _ => throw new ArgumentException($"Unknown scope: {scopeString}", nameof(scopeString))
        };
    }
}
