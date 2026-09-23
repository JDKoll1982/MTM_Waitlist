using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Models;

/// <summary>
/// One dunnage type the visibility editor shows or hides, drawn as a card.
/// </summary>
/// <remarks>
/// <para>
/// The card the Settings page uses is <c>PartPictureCardTemplate</c>, whose entry contract is five members: a
/// label, the card's automation name, the picture box's automation name, the picture to draw, and whether this is
/// the entry currently chosen. Four of them are answered here; <see cref="IsSelected"/> stays
/// <see langword="false"/> because showing or hiding a type is an action rather than a choice between types, so no
/// card is ever the selected one and no outline is drawn.
/// </para>
/// <para>
/// <see cref="ImagePath"/> defaults to the application's one shared no-image placeholder rather than an empty
/// string, which is what keeps the card from drawing a blank space. A dunnage type's own picture is unchanged by
/// this feature and keeps its own storage and arrangement (FR-033): this screen's read supplies a type's name and
/// identifier and no picture path, so the placeholder is the honest answer here.
/// </para>
/// </remarks>

public sealed class DunnageTypeVisibilityOption
{
    /// <summary>The prefix every automation name on the two dunnage lists is built from.</summary>
    public const string CardIdPrefix = "SettingsPage_DunnageType";

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>The card's label, which is the type's name.</summary>
    public string Title => Name;

    /// <summary>This card's automation name, unique to the type it offers.</summary>
    public string AutomationId => $"{CardIdPrefix}_{Id}";

    /// <summary>The automation name of the picture box inside this card, unique to the type it offers.</summary>
    public string ImageAutomationId => $"{CardIdPrefix}Image_{Id}";

    /// <summary>
    /// The picture the card draws: the type's own picture when the read supplies one, and the shared no-image
    /// placeholder otherwise.
    /// </summary>
    public string ImagePath { get; init; } = ImagePicturePolicy.NoImagePath;

    /// <summary>
    /// Whether this is the type currently chosen. Always <see langword="false"/> on these lists: a type is shown
    /// or hidden, never selected, so the card's selection outline is never drawn.
    /// </summary>
    public bool IsSelected { get; set; }
}
