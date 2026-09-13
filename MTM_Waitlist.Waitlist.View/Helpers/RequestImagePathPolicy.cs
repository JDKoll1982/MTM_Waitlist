using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// Decides whether a path returned by the image-location service is worth taking.
/// </summary>
/// <remarks>
/// <para>
/// The service always answers with a path: when neither an override nor a catalog <c>default_image_path</c> is
/// configured, it returns <see cref="ImageLocationDefaults.RequestTypeDefaultPath"/> — the "no image available"
/// placeholder — rather than reporting that it had nothing to offer. The card cannot tell that apart from a
/// real answer, and it prefers any non-empty resolved path over the image it already has, so accepting the
/// placeholder replaces a good picture with a placeholder card.
/// </para>
/// <para>
/// That is not hypothetical. Nothing initializes the image service at startup, so the first list load skips
/// resolution entirely and shows each row's own image; opening the New Request workflow initializes the
/// service as a side effect, and the next list load then resolves every row to the placeholder. The card is
/// meant to prefer a configured image, not to prefer a card that says there is none.
/// </para>
/// </remarks>
public static class RequestImagePathPolicy
{
    /// <summary>
    /// Whether a resolved path should be taken as the row's image.
    /// </summary>
    /// <param name="resolvedPath">What the image-location service returned, or null.</param>
    /// <returns>
    /// <see langword="true"/> for a genuine resolved path; <see langword="false"/> for nothing at all or for the
    /// service's own placeholder, both of which leave the row's existing image in place.
    /// </returns>
    public static bool IsUsableResolvedPath(string? resolvedPath) =>
        !string.IsNullOrWhiteSpace(resolvedPath) && !IsServicePlaceholder(resolvedPath);

    /// <summary>
    /// Whether a path is one of the service's own fallbacks.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the path is one of the placeholders the service substitutes for "nothing
    /// configured": the request-type, request-subtype, request-item or request-category default.
    /// </returns>
    public static bool IsServicePlaceholder(string? path)
    {
        var normalized = Normalize(path);
        if (normalized.Length == 0)
        {
            return false;
        }

        // Every scope's placeholder is checked, even though they all resolve to the same file today, so a
        // future split of one of them cannot quietly reintroduce the bug. The two Item scopes (FR-009) matter
        // most: the Item is the card's own picture, so its placeholder is exactly the answer that must never
        // replace an image the request already resolves.
        return string.Equals(normalized, Normalize(ImageLocationDefaults.RequestTypeDefaultPath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Normalize(ImageLocationDefaults.RequestSubtypeDefaultPath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Normalize(ImageLocationDefaults.RequestItemDefaultPath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, Normalize(ImageLocationDefaults.RequestCategoryDefaultPath), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compares paths as paths: separators are unified and a leading <c>./</c> is dropped, because the service
    /// hands back backslashes for its own defaults while resolved catalog paths use forward slashes.
    /// </summary>
    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim().Replace('\\', '/');
        while (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        return normalized;
    }
}
