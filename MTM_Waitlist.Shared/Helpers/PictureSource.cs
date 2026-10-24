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
    /// Builds the source for <paramref name="path"/>, <b>read</b> at most <paramref name="decodePixelWidth"/> pixels
    /// wide.
    /// </summary>
    /// <param name="path">The picture's path, as <see cref="FromPath(string?)"/> takes it.</param>
    /// <param name="decodePixelWidth">
    /// The width the bitmap is decoded at. A picture drawn small is read small, so a 1024-pixel artwork shown at
    /// half size is decoded at 512 pixels rather than decoded whole and scaled to fit — which costs a quarter of the
    /// memory and is what "half the size" means for the image being read rather than for the control drawing it.
    /// Zero or less leaves the picture at its own size.
    /// </param>
    /// <returns>An <see cref="ImageSource"/> that is always safe to assign to a control.</returns>
    public static ImageSource FromPath(string? path, int decodePixelWidth)
    {
        var file = ImagePicturePolicy.ResolveUsableFile(path);
        if (file is not null)
        {
            try
            {
                var bitmap = new BitmapImage();
                if (decodePixelWidth > 0)
                {
                    // Set before the URI: DecodePixelWidth must be in place before the bitmap starts loading, or
                    // it is ignored and the picture is decoded whole.
                    bitmap.DecodePixelWidth = decodePixelWidth;
                }

                bitmap.UriSource = new Uri(file);
                return bitmap;
            }
            catch (UriFormatException)
            {
                // Fall through to the placeholder, as the path-only overload does.
            }
        }

        return FromPlaceholder(decodePixelWidth);
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

    /// <summary>
    /// The placeholder source, read at most <paramref name="decodePixelWidth"/> pixels wide.
    /// </summary>
    /// <param name="decodePixelWidth">The width to decode at; zero or less leaves it at its own size.</param>
    public static ImageSource FromPlaceholder(int decodePixelWidth)
    {
        var file = ImagePicturePolicy.ResolveExistingFile(ImagePicturePolicy.NoImagePath);

        if (file is not null)
        {
            var bitmap = new BitmapImage();
            if (decodePixelWidth > 0)
            {
                bitmap.DecodePixelWidth = decodePixelWidth;
            }

            bitmap.UriSource = new Uri(file);
            return bitmap;
        }

        return new BitmapImage(new Uri(ImagePicturePolicy.NoImagePackUri));
    }
}
