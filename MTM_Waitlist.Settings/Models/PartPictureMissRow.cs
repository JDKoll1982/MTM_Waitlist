namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One part the application can name that still has no picture.
/// </summary>
/// <remarks>
/// The row carries the folder the picture belongs in and the path the file would be stored at, so the exported
/// list is something a person can work through off the shop floor: they photograph the part, name the file after
/// the part, and drop it in the folder this row names.
/// </remarks>
public sealed class PartPictureMissRow
{
    /// <summary>The system the part belongs to: <c>visual_part</c> or <c>wip_part</c>.</summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>The system in the person's words: <c>Visual</c> or <c>WIP</c>.</summary>
    public string SystemName { get; init; } = string.Empty;

    /// <summary>The part number the application can name.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The family folder the picture would be stored in, decided by the number's prefix.</summary>
    public string FamilyFolder { get; init; } = string.Empty;

    /// <summary>
    /// The path the picture would be stored at, relative to the configured picture root, with the extension the
    /// application's acceptance rule allows standing in for the file not yet placed.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>What the person reads in the list: the part number.</summary>
    public override string ToString() => PartNumber;
}
