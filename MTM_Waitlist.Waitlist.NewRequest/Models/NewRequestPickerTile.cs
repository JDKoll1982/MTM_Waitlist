using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable Category card on the New Request Category step — the umbrella word a person reads, how many
/// Items it would offer to this job, and the Category it stands for. The Items themselves are their own step and
/// their own tile (<see cref="NewRequestItemOption"/>).
/// </summary>
public sealed class NewRequestPickerTile
{
    /// <summary>Display name shown on the card.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Secondary line under the name — how many Items the Category would offer.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Resolved image path for the card; empty for Category tiles.</summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>The Category this card selects.</summary>
    public RequestCategory? Category { get; init; }
}

