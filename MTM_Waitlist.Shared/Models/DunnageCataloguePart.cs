using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Models;

/// <summary>
/// One dunnage part, as both surfaces that offer the plant's dunnage catalogue draw it.
/// </summary>
/// <remarks>
/// <para>
/// This is the row the generic request's dunnage page and the work-order substitute dialog share, so the two can
/// never disagree about what a dunnage part is. It is a projection of
/// <c>MTM_Waitlist.Module_Setup.Models.SetupDunnagePart</c>, made at the reader seam; the Setup type stays where it
/// is and keeps the fields only its own flow needs.
/// </para>
/// <para>
/// <see cref="ImagePath"/> is the database relative path the catalogue stores, never bytes and never an absolute
/// path. A path whose file is missing resolves to no picture, which the surface draws as the shared placeholder
/// rather than as an unavailable store.
/// </para>
/// </remarks>
public sealed class DunnageCataloguePart
{
    public string Id { get; set; } = string.Empty;

    public string TypeId { get; set; } = string.Empty;

    /// <summary>The part number the filter and the sort use.</summary>
    public string PartNumber { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The dunnage type's display name, populated by the catalogue read.</summary>
    public string DunnageTypeName { get; set; } = string.Empty;

    /// <summary>The home location the one-box filter matches against.</summary>
    public string HomeLocation { get; set; } = string.Empty;

    /// <summary>The database relative path to the part's picture, or blank when it has none.</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Whether the part has its own picture, which is what the images-only choice filters on.</summary>
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath);

    /// <summary>Whether to draw the no-picture placeholder glyph in the part's card.</summary>
    public bool ShowNoImagePlaceholder => !HasImage;

    /// <summary>The picture to draw, falling back to the plant's own no-picture artwork.</summary>
    public string DisplayImagePath => HasImage ? ImagePath : ImagePicturePolicy.NoImagePath;

    /// <summary>The words the placeholder carries, or empty when the part has a picture.</summary>
    public string ImageFallbackText => HasImage ? string.Empty : "No image available";
}
