using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable component card on the New Request component step: the part number the requesting job carries as a
/// component, the name a person reads, its picture, and whether this card is the one currently chosen.
/// </summary>
/// <remarks>
/// <para>
/// The card's <see cref="ImagePath"/> is the <b>part's own picture</b>, resolved by the step before the option
/// reached the collection because the step is where the resolver is in hand. A part the application cannot picture
/// draws the one shared no-image placeholder — never a blank space, and never artwork standing in for the part
/// (FR-014, FR-023).
/// </para>
/// <para>
/// <see cref="AutomationId"/> and <see cref="ImageAutomationId"/> name the card and the picture box inside it after
/// the part, so automation can find a specific component's tile and the picture it is drawing.
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

    /// <summary>
    /// The picture the card draws: the part's own picture, and the one shared placeholder when the part cannot be
    /// pictured. Never empty (FR-023).
    /// </summary>
    public string ImagePath { get; init; } = ImagePicturePolicy.NoImagePath;

    /// <summary>This card's automation name, unique to the part it offers.</summary>
    public string AutomationId => $"{AutomationIdPrefix}_{PartNumber}";

    /// <summary>The automation name of the picture box inside this card, unique to the part it offers.</summary>
    public string ImageAutomationId => $"{AutomationIdPrefix}Image_{PartNumber}";

    /// <summary>Whether this card is the current choice — drives the card's selection outline.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
