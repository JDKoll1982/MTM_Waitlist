using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable Category card on the New Request Category step — the umbrella word a person reads, how many
/// Items it would offer to this job, and the Category it stands for. The Items themselves are their own step and
/// their own tile (<see cref="NewRequestItemOption"/>).
/// </summary>
/// <remarks>
/// A card carries whether it is the chosen one, so the accent outline follows the card rather than the grid's
/// selection — the same arrangement the generic flow's category and work center cards use.
/// </remarks>
public sealed partial class NewRequestPickerTile : ObservableObject
{
    /// <summary>Display name shown on the card.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Secondary line under the name — how many Items the Category would offer.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The Category this card selects.</summary>
    public RequestCategory? Category { get; init; }

    /// <summary>Whether this card is the category the requester chose.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

