namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The words a derived <see cref="RequestPartType"/> is written with on a card.
/// <para>
/// The spellings are the plant's own commodity names shortened to what fits a card cell: <c>Coil</c>,
/// <c>Flatstock</c>, <c>Hardware</c>, <c>Bar / Round</c>, <c>Raw Material</c>, <c>Customer Material</c>,
/// <c>Stamped Part</c>, <c>Assembly</c>, <c>Machined Part</c>, <c>Tooling</c>, <c>Die Component</c>,
/// <c>Expense</c>.
/// </para>
/// <para>
/// <see cref="RequestPartType.Unknown"/> answers <see langword="null" /> rather than a word: nothing named a kind,
/// so the cell resolves nothing and is left out entirely instead of carrying a guess (FR-089).
/// </para>
/// </summary>
public static class RequestPartTypeLabels
{
    /// <summary>The word for a part type, or <see langword="null" /> when no kind was named.</summary>
    public static string? Of(RequestPartType partType) => partType switch
    {
        RequestPartType.Coil => "Coil",
        RequestPartType.Flatstock => "Flatstock",
        RequestPartType.Hardware => "Hardware",
        RequestPartType.BarRound => "Bar / Round",
        RequestPartType.RawOther => "Raw Material",
        RequestPartType.CustomerSupplied => "Customer Material",
        RequestPartType.StampedPart => "Stamped Part",
        RequestPartType.Assembly => "Assembly",
        RequestPartType.MachinedPart => "Machined Part",
        RequestPartType.Tooling => "Tooling",
        RequestPartType.DieComponent => "Die Component",
        RequestPartType.Expense => "Expense",
        _ => null,
    };
}
