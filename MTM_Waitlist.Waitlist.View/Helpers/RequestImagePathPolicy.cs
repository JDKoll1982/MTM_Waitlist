using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// Decides whether a path returned by the image-location service is worth taking.
/// </summary>
/// <remarks>
/// <para>
/// The service always answers with a path: when neither an override nor a catalog default is configured, it
/// returns <see cref="ImageLocationDefaults.RequestItemDefaultPath"/> — the "no image available" placeholder —
/// rather than reporting that it had nothing to offer. The card cannot tell that apart from a real answer, and
/// it prefers any non-empty resolved path over the image it already has, so accepting the placeholder replaces a
/// good picture with a placeholder card.
/// </para>
/// <para>
/// That is not hypothetical. Nothing initializes the image service at startup, so the first list load skips
/// resolution entirely and shows each row's own image; opening the New Request workflow initializes the
/// service as a side effect, and the next list load then resolves every row to the placeholder. The card is
/// meant to prefer a configured image, not to prefer a card that says there is none.
/// </para>
/// <para>
/// Answering "is this the resolver's way of saying nothing?" is not the whole question, because a configured
/// path can be just as empty as no path at all: the six <c>Assets/RequestTypes/*.png</c> files ship as 68-byte
/// single-pixel stand-ins for artwork that was never delivered, and a seeded override that points at one of them
/// is a real path to a file with no picture in it. <see cref="IsUsableResolvedPicture"/> is the question the
/// card actually asks — a real picture, and not the resolver's placeholder — while
/// <see cref="IsUsableResolvedPath"/> stays the string-only half of it, so the rule can be stated and tested
/// without a file system.
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
    /// Whether a resolved path is worth taking <em>and</em> the file it names actually carries a picture.
    /// </summary>
    /// <param name="resolvedPath">What the image-location service returned, or null.</param>
    /// <returns>
    /// <see langword="true"/> only for a genuine path to a file the application will draw: readable, square, and
    /// at least the size the application accepts. A placeholder, a missing file, an unreadable file, a
    /// single-pixel stand-in and a picture of the wrong shape all answer <see langword="false"/>, which leaves
    /// the row's existing image in place.
    /// </returns>
    /// <remarks>
    /// This is the rule the card and the request page both apply: a picture that shows nothing must never replace
    /// a picture that shows something, whichever way the empty answer is spelled.
    /// </remarks>
    public static bool IsUsableResolvedPicture(string? resolvedPath) =>
        IsUsableResolvedPath(resolvedPath) && ImageFileProbe.IsUsablePicture(resolvedPath);

    /// <summary>
    /// Whether a path is one of the service's own fallbacks.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>
    /// <see langword="true"/> when the path is one of the placeholders the service substitutes for "nothing
    /// configured": the request-item or request-category default.
    /// </returns>
    public static bool IsServicePlaceholder(string? path)
    {
        var normalized = Normalize(path);
        if (normalized.Length == 0)
        {
            return false;
        }

        // Both live Item scopes are checked, even though they resolve to the same file today, so a future split of
        // one of them cannot quietly reintroduce the bug. The Item is the card's own picture (FR-009), so its
        // placeholder is exactly the answer that must never replace an image the request already resolves.
        return string.Equals(normalized, Normalize(ImageLocationDefaults.RequestItemDefaultPath), StringComparison.OrdinalIgnoreCase)
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
