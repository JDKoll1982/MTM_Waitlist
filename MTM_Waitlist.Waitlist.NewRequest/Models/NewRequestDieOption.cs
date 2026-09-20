using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable die card on the New Request die step: the die itself, the identifier a person reads — its number
/// and where it lives — and whether this card is one the operator has chosen.
/// </summary>
/// <remarks>
/// A card is choosable and <b>un</b>choosable rather than being the step's way out, because a job may carry
/// several dies and the operator may need more than one of them: each tap marks the card, and the step moves on
/// only when they say they have finished choosing (FR-054). The identifier comes from
/// <see cref="RequestDiePart.Label"/> so the card and the request page write the same die the same way (FR-056).
/// </remarks>
public sealed partial class NewRequestDieOption : ObservableObject
{
    /// <summary>The die this card offers.</summary>
    public RequestDiePart? Die { get; init; }

    /// <summary>The identifier shown on the card — the die's number and its home location.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The secondary line — where the die is, or what it is.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Whether this card is one of the current choices — drives the card's selection outline.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
