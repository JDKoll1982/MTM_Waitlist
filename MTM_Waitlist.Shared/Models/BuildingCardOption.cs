using MTM_Waitlist.Module_Shared.Helpers;

namespace MTM_Waitlist.Module_Shared.Models;

/// <summary>
/// One building, shaped for the shared part-picture card: the shared card needs a title, a picture to draw, a
/// chosen flag and two automation ids, and a building has no picture of its own.
/// </summary>
/// <remarks>
/// <para>
/// Two screens offer a building choice — the shell's facility flyout and the wizard's work-center step — and both
/// are the same list drawn the same way (FR-021, FR-022), so the card shape lives here beside the other catalogue
/// options rather than being written twice.
/// </para>
/// <para>
/// <see cref="IsSelected"/> is always false because the building in effect is not offered at all: a card that can
/// never be the chosen one has nothing to draw an outline for, and saying so here is clearer than leaving the
/// card template to bind a flag nothing ever sets.
/// </para>
/// </remarks>
public sealed class BuildingCardOption
{
    public BuildingCardOption(string building, string automationIdPrefix)
    {
        Building = building ?? string.Empty;
        AutomationId = $"{automationIdPrefix}_{Building}";
        ImageAutomationId = $"{automationIdPrefix}Image_{Building}";
    }

    /// <summary>The building this card stands for, which is what choosing it selects.</summary>
    public string Building { get; }

    /// <summary>The card's label: the building's name.</summary>
    public string Title => Building;

    /// <summary>The picture the card draws: the one shared no-image picture, never a blank space.</summary>
    public string ImagePath => ImagePicturePolicy.NoImagePath;

    /// <summary>Never the chosen entry: the building in effect is not offered at all.</summary>
    public bool IsSelected => false;

    /// <summary>The card's automation id.</summary>
    public string AutomationId { get; }

    /// <summary>The card's picture box automation id.</summary>
    public string ImageAutomationId { get; }
}
