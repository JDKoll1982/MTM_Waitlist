using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One part on the picture screen: the part the person looked up, its system, and the picture the application
/// would draw for it right now.
/// </summary>
/// <remarks>
/// <para>
/// The screen resolves a picture exactly as a card does — the same resolver, the same acceptance rule — so what
/// this row shows is what a surface draws, rather than a second opinion about it.
/// </para>
/// <para>
/// <see cref="ImagePath"/> is never empty. A part with no picture answers the one shared placeholder, which is the
/// card template's contract: a model that draws through <c>PartPictureCardTemplate</c> always has something to
/// draw, and never a blank space.
/// </para>
/// </remarks>
public sealed class PartPictureRow
{
    /// <summary>The system the part belongs to: <c>visual_part</c> or <c>wip_part</c>.</summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>The part number, as the store keys it.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>What the person reads: the part number.</summary>
    public string DisplayName => PartNumber;

    /// <summary>
    /// The picture a surface would draw for this part: a usable file, or the shared placeholder.
    /// </summary>
    public string ImagePath { get; init; } = ImagePicturePolicy.NoImagePath;

    /// <summary>The part's family folder, which is decided by the number's prefix rather than by any stored value.</summary>
    public string FamilyFolder => PartPictureLayout.FamilyFolderFor(PartNumber);

    /// <summary>Where the picture would be stored, relative to the configured picture root.</summary>
    /// <remarks>
    /// The extension is not known before a file is chosen, so this answers the folder the file will land in.
    /// </remarks>
    public string RelativeFolderPath =>
        $"{PartPictureLayout.CollectionFolderFor(Scope)}/{FamilyFolder}";

    /// <summary>Whether this part has a picture of its own rather than the placeholder.</summary>
    public bool HasPicture =>
        !string.Equals(ImagePath, ImagePicturePolicy.NoImagePath, StringComparison.OrdinalIgnoreCase);

    /// <summary>The card's automation id, per the shared card template's contract.</summary>
    public string AutomationId => $"PartPicture_{Scope}_{PartNumber}";

    /// <summary>The picture box's automation id, per the shared card template's contract.</summary>
    public string ImageAutomationId => $"PartPictureImage_{Scope}_{PartNumber}";
}
