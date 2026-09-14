using CommunityToolkit.Mvvm.ComponentModel;

using MTM_Waitlist.Module_Settings.Models;

namespace MTM_Waitlist.Module_Waitlist.Models;

/// <summary>
/// One selectable dunnage card on the New Request dunnage step: the part itself, the name a person reads, its
/// picture, and whether this card is the one currently chosen.
/// </summary>
/// <remarks>
/// The card model carries no wording of its own beyond the secondary line the part resolves, so the step never
/// invents a label for a part (FR-022). A substituted part uses the same card shape as an assigned one — it is
/// the <see cref="RequestDunnagePart.IsAssignedToJob"/> flag, not a second template, that says where it came from.
/// </remarks>
public sealed partial class NewRequestDunnageOption : ObservableObject
{
    /// <summary>The dunnage part this card offers.</summary>
    public RequestDunnagePart? Part { get; init; }

    /// <summary>The name shown on the card — the part's own name, or its number when it has none.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The secondary line — the part number, marked as a substitute when the job does not carry it.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The part's picture as an absolute path, or empty when it has none.</summary>
    public string ImagePath { get; init; } = string.Empty;

    /// <summary>Whether this card is the current choice — drives the card's selection outline.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
