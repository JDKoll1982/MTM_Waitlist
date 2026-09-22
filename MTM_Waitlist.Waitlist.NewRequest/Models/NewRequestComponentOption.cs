using CommunityToolkit.Mvvm.ComponentModel;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable component card on the New Request component step: the part number the requesting job carries as a
/// component, the name a person reads, its picture, and whether this card is the one currently chosen.
/// </summary>
/// <remarks>
/// <para>
/// The card's <see cref="ImagePath"/> is a <b>temporary stand-in</b>. Nothing can resolve a Visual or WIP part
/// number to a picture yet, so every card draws the application's shared no-image placeholder rather than a drawing
/// invented here. When parts can be pictured, the value this property is given changes and the card, its layout and
/// its automation names do not.
/// </para>
/// <para>
/// <see cref="AutomationId"/> and <see cref="ImageAutomationId"/> exist because the stand-in has to be verifiable:
/// they name the card and the picture box inside it after the part, so automation can find a specific component's
/// tile — and prove its placeholder is the one drawn — before any real picture exists.
/// </para>
/// </remarks>
public sealed partial class NewRequestComponentOption : ObservableObject
{
    /// <summary>The prefix every automation name on this step is built from.</summary>
    public const string AutomationIdPrefix = "NewRequestComponentPage_Option";

    /// <summary>The part number this card offers — the value the request stores when the card is clicked.</summary>
    public string PartNumber { get; init; } = string.Empty;

    /// <summary>
    /// The name shown on the card. A component's part number <b>is</b> its name: the job's component rows carry a
    /// description for some parts and none for others, so the number is what every card can be labelled with.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The picture the card draws — the shared no-image placeholder until part pictures exist.</summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>This card's automation name, unique to the part it offers.</summary>
    public string AutomationId => $"{AutomationIdPrefix}_{PartNumber}";

    /// <summary>The automation name of the picture box inside this card, unique to the part it offers.</summary>
    public string ImageAutomationId => $"{AutomationIdPrefix}Image_{PartNumber}";

    /// <summary>Whether this card is the current choice — drives the card's selection outline.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
