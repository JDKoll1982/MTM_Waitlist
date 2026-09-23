using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Helpers;

/// <summary>
/// The waitlist's name for the application-wide picture rule: whether a file on disk actually carries a picture
/// the application will draw.
/// </summary>
/// <remarks>
/// <para>
/// The rule itself lives in <see cref="ImagePicturePolicy"/> so that every assembly — the waitlist, the setup
/// module, the settings screens — asks one question and gets one answer. It began here, as the waitlist's answer
/// to a narrower problem: the image-location service answers with a <em>path</em>, and a path existing had been
/// taken as proof that there is a picture at the end of it. `Assets/RequestTypes/*.png` ship as 68-byte
/// single-white-pixel stand-ins for artwork that was never delivered, so an override that points at one passes
/// every check the resolver makes and still has nothing to show: the card took it and replaced its own picture
/// with a blank tile. Existence is not the question — "is there a picture here?" is.
/// </para>
/// <para>
/// This type stays because it is the name the waitlist's own call sites and tests use, and because the two
/// questions below are not the same one: <see cref="CarriesPicture(string?)"/> asks only about size, while
/// <see cref="IsUsablePicture"/> asks the whole question the surfaces ask — large enough <em>and</em> square.
/// </para>
/// </remarks>
public static class ImageFileProbe
{
    /// <summary>
    /// The smallest width and height the app accepts as a picture (48 pixels — the app's own preview floor).
    /// </summary>
    public static int MinimumPixels => ImagePicturePolicy.MinimumPixels;

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least <see cref="MinimumPixels"/> pixels
    /// on both sides, whatever its shape.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool CarriesPicture(string? path) => ImagePicturePolicy.CarriesPicture(path);

    /// <summary>
    /// Whether the file at <paramref name="path"/> carries a picture at least
    /// <paramref name="minimumPixels"/> pixels on both sides, whatever its shape.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <param name="minimumPixels">The smallest acceptable width and height, in pixels.</param>
    /// <returns><see langword="true"/> only when a picture was found and measured.</returns>
    public static bool CarriesPicture(string? path, int minimumPixels) =>
        ImagePicturePolicy.CarriesPicture(path, minimumPixels);

    /// <summary>
    /// The whole question the surfaces ask about a configured picture: the file is there, it can be read, it is
    /// square, and it is at least <see cref="MinimumPixels"/> pixels on both sides. A file that fails any part of
    /// that is not drawn — the surface falls back to <see cref="ImagePicturePolicy.NoImagePath"/> instead.
    /// </summary>
    /// <param name="path">A rooted path, or one relative to the application directory.</param>
    /// <returns><see langword="true"/> only for a picture the application will draw.</returns>
    public static bool IsUsablePicture(string? path) => ImagePicturePolicy.IsUsable(path);
}
