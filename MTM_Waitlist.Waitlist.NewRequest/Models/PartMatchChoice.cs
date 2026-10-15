using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable part on the fuzzy incorrect-part step: the part number the live plant-system read answered with,
/// and whether it is the one currently chosen.
/// </summary>
/// <remarks>
/// A part number is its own name here: the containing read answers part numbers, the operator is correcting a
/// number, and nothing is added to the row that the read did not answer (FR-083, FR-089).
/// </remarks>
public sealed partial class PartMatchChoice : ObservableObject
{
    /// <summary>The prefix every automation name on this step is built from.</summary>
    public const string AutomationIdPrefix = "NewRequestPartSearchPage_Match";

    /// <summary>The part number this row offers — the value the request stores when it is chosen.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>The name a person reads, which is the part number itself.</summary>
    public string Title => PartNumber;

    /// <summary>The picture the row draws: the one shared no-image picture, never a blank space.</summary>
    public string ImagePath => ImagePicturePolicy.NoImagePath;

    /// <summary>This row's automation name, unique to the part it offers.</summary>
    public string AutomationId => $"{AutomationIdPrefix}_{PartNumber}";

    /// <summary>Whether this row is the current choice.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
