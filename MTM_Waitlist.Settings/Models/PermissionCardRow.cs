namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// One person as one card shows them: the person, and that card's own columns of their cells.
/// </summary>
/// <remarks>
/// <para>
/// The card carries only its own area's cells, because a card is one area wide. The person is
/// <see cref="PermissionMatrixRow"/>, which is shared by every card, so what is pending for them and what saving
/// writes for them are one thing on the page rather than one thing per card.
/// </para>
/// <para>
/// There is no separate row for a person per area: this is the same person seen through this card's columns, and
/// the row it wraps is the same object the other cards wrap.
/// </para>
/// </remarks>
public sealed class PermissionCardRow
{
    public PermissionCardRow(PermissionMatrixRow person, IReadOnlyList<PermissionCell> cells)
    {
        Person = person;
        Cells = cells;
    }

    /// <summary>The person, carrying their name, their role, whether they are locked and what is pending.</summary>
    public PermissionMatrixRow Person { get; }

    /// <summary>This card's cells for that person, in the card's own column order.</summary>
    public IReadOnlyList<PermissionCell> Cells { get; }
}
