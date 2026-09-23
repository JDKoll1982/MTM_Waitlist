using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Settings.Models;
using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable die card on the New Request die step: the die itself, the identifier a person reads — its number
/// and where it lives — its picture, and whether this card is one the operator has chosen.
/// </summary>
/// <remarks>
/// A card is choosable and <b>un</b>choosable rather than being the step's way out, because a job may carry
/// several dies and the operator may need more than one of them: each tap marks the card, and the step moves on
/// only when they say they have finished choosing (FR-054). The identifier comes from
/// <see cref="RequestDiePart.Label"/> so the card and the request page write the same die the same way (FR-056),
/// and the card is no longer text only: it draws the die's own picture, or the one shared placeholder when the die
/// cannot be pictured (FR-018).
/// </remarks>
public sealed partial class NewRequestDieOption : ObservableObject
{
    /// <summary>The prefix every automation name on this step is built from.</summary>
    public const string AutomationIdPrefix = "NewRequestDiePage_Option";

    /// <summary>The die this card offers.</summary>
    public RequestDiePart? Die { get; init; }

    /// <summary>The identifier shown on the card — the die's number and its home location.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The secondary line — where the die is, or what it is.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// The picture the card draws: the die's own picture, and the one shared placeholder when the die cannot be
    /// pictured. Never empty, so an unpicturable die is still offered as a card (FR-014, FR-023).
    /// </summary>
    public string ImagePath { get; init; } = ImagePicturePolicy.NoImagePath;

    /// <summary>The automation name of the picture box inside this card, unique to the die it offers.</summary>
    public string ImageAutomationId => $"{AutomationIdPrefix}Image_{Die?.PartNumber}";

    /// <summary>Whether this card is one of the current choices — drives the card's selection outline.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
