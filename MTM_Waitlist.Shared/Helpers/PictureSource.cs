using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MTM_Waitlist.Module_Shared.Helpers;

/// <summary>
/// Turns an image path into something a control can draw, and never hands back "nothing".
/// </summary>
/// <remarks>
/// <para>
/// This is the last line of the application's image rule: when there is no path, when the path names no file, or
/// when the file holds no picture, the caller draws
/// <see cref="ImagePicturePolicy.NoImagePath"/> instead of a blank tile. The checks live with
/// <see cref="ImagePicturePolicy"/> rather than here, so a surface that only needs the answer — a view model
/// deciding whether to keep a row's own picture — does not have to build a bitmap to ask for it.
/// </para>
/// <para>
/// <b>UI thread only.</b> <see cref="BitmapImage"/> is a XAML dependency object, so this must be called from the
/// thread that owns the target control — which is what every value converter and code-behind call site does.
/// </para>
/// </remarks>
public static class PictureSource
{
    /// <summary>
    /// Builds the source for <paramref name="path"/>: the picture when it is one the application will draw,
    /// otherwise the application's "no image" placeholder.
    /// </summary>
    /// <param name="path">
    /// A rooted or UNC path, or one relative to the application directory. Null, blank and unreadable values are
    /// all the same answer: draw the placeholder.
    /// </param>
    /// <returns>An <see cref="ImageSource"/> that is always safe to assign to a control.</returns>
    public static ImageSource FromPath(string? path)
    {
        var file = ImagePicturePolicy.ResolveUsableFile(path);
        if (file is not null)
        {
            try
            {
                return new BitmapImage(new Uri(file));
            }
            catch (UriFormatException)
            {
                // A file name that cannot be spelled as a URI is a picture the control cannot load, which is the
                // same answer as no picture at all. Thrown inside a value converter it would surface as a binding
                // failure with no fallback, so it is caught here instead.
            }
        }

        return FromPlaceholder();
    }

    /// <summary>
    /// The placeholder source on its own, for a surface that has no path to try — for example a card row that was
    /// never given a picture at all.
    /// </summary>
    /// <returns>An <see cref="ImageSource"/> for <see cref="ImagePicturePolicy.NoImagePath"/>.</returns>
    public static ImageSource FromPlaceholder()
    {
        // The placeholder is resolved by existence, not by the picture rule, so a placeholder that is itself
        // square-but-wrong can never send this into a fallback that has nothing left to fall back to.
        var file = ImagePicturePolicy.ResolveExistingFile(ImagePicturePolicy.NoImagePath);

        return file is not null
            ? new BitmapImage(new Uri(file))
            : new BitmapImage(new Uri(ImagePicturePolicy.NoImagePackUri));
    }
}
