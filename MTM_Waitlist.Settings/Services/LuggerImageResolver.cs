using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;

using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MTM_Waitlist.Module_Settings.Services;

/// <summary>
/// Finds the picture that belongs to one lugger.
/// </summary>
public interface ILuggerImageResolver
{
    /// <summary>
    /// The picture for the lugger in the given arrangement slot, or <see langword="null"/> when it has none.
    /// </summary>
    /// <param name="displayPosition">The lugger's 1-based place in the arrangement, which is what the file is named after.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> ResolveAsync(int displayPosition, CancellationToken cancellationToken = default);
}

/// <summary>
/// Maps a lugger's place in the arrangement to a picture file in the configured lugger folder.
/// </summary>
/// <remarks>
/// <para>
/// The file is named after the lugger's <b>place in the arrangement</b>, not after its name: a lead may rename
/// "Door 6 Lugger" to anything at all, and the picture has to keep following the slot rather than the words. The name
/// is two digits wide, so the first nine files are <c>Lugger01</c> … <c>Lugger09</c> and the rest continue
/// <c>Lugger10</c> … <c>Lugger99</c>.
/// </para>
/// <para>
/// The arrangement holds at most <see cref="MaximumLuggerNumber"/> luggers. A slot past that resolves to no picture
/// rather than to a file that would show another lugger's artwork.
/// </para>
/// <para>
/// A missing folder or a missing file is not a failure: the card falls back to the application's no-picture image.
/// The folder is resolved through <see cref="IImageStorageConfigurationResolver"/>, so a plant that moves its
/// artwork changes the setting rather than the build.
/// </para>
/// </remarks>
public sealed class LuggerImageResolver : ILuggerImageResolver
{
    /// <summary>The highest lugger number a picture may be named after.</summary>
    public const int MaximumLuggerNumber = 99;

    /// <summary>
    /// The picture extensions a lugger file may use, in the order they are looked for.
    /// </summary>
    /// <remarks>
    /// PNG first because it is what the rest of the plant's artwork uses; the raster formats Windows draws without a
    /// codec follow it.
    /// </remarks>
    public static readonly IReadOnlyList<string> SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];

    private readonly IImageStorageConfigurationResolver _storage;

    public LuggerImageResolver(IImageStorageConfigurationResolver storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
    }

    /// <inheritdoc />
    public async Task<string?> ResolveAsync(int displayPosition, CancellationToken cancellationToken = default)
    {
        if (!IsAddressable(displayPosition))
        {
            return null;
        }

        string folder;
        try
        {
            folder = await _storage.GetLuggerFolderPathAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A configuration that cannot be read is an outage, not a missing picture: the card still has to draw,
            // and it draws the no-picture image rather than taking the arrangement down with it.
            return null;
        }

        return ResolveInFolder(folder, displayPosition);
    }

    /// <summary>The file for one slot in one folder, or <see langword="null"/> when no picture sits there.</summary>
    public static string? ResolveInFolder(string? folder, int displayPosition)
    {
        if (string.IsNullOrWhiteSpace(folder) || !IsAddressable(displayPosition))
        {
            return null;
        }

        // Asked once for the folder rather than once per candidate: on an unreachable share every File.Exists costs a
        // timeout, and five of those per lugger is a card that takes seconds to draw.
        if (!Directory.Exists(folder))
        {
            return null;
        }

        var stem = FileNameStem(displayPosition);

        foreach (var extension in SupportedExtensions)
        {
            var candidate = Path.Combine(folder, stem + extension);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>The file name without its extension for a slot: <c>Lugger01</c> … <c>Lugger99</c>.</summary>
    public static string FileNameStem(int displayPosition) => $"Lugger{displayPosition:00}";

    /// <summary>
    /// The width in pixels of the picture at <paramref name="path"/>, read from the file's header, or
    /// <see langword="null"/> when it cannot be read.
    /// </summary>
    /// <remarks>
    /// Only the header is decoded, so asking is cheap even for a large photo. The answer is what lets a card read a
    /// picture at half its own size instead of decoding it whole and scaling the result.
    /// </remarks>
    public static async Task<int?> ReadPixelWidthAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var file = ImagePicturePolicy.ResolveUsableFile(path)
                ?? ImagePicturePolicy.ResolveExistingFile(path);

            if (file is null)
            {
                return null;
            }

            using var stream = await FileRandomAccessStream.OpenAsync(file, FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            return (int)decoder.PixelWidth;
        }
        catch (Exception)
        {
            // An unreadable or unshared file is no width at all: the card then reads the picture at its own size
            // rather than failing to draw it.
            return null;
        }
    }

    private static bool IsAddressable(int displayPosition) =>
        displayPosition >= 1 && displayPosition <= MaximumLuggerNumber;
}
