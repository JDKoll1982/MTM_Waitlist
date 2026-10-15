namespace MTM_Waitlist.Module_Settings.Models;

/// <summary>
/// The words a derived <see cref="RequestDisposition"/> is written with on a card.
/// <para>
/// These are the contract words FR-091 names verbatim — <c>Finished Goods</c>, <c>Outside Service</c>, <c>WIP</c>
/// — plus <c>NCM</c>, which is the plant's own name for the non-conforming area and the state FR-091 requires the
/// location to decide. They are the same words the pre-spec's cell table uses (§5.3, D36), so the card reads what
/// the requirement names rather than a near-synonym.
/// </para>
/// <para>
/// <see cref="RequestDisposition.Unknown"/> answers <see langword="null" /> rather than a word: nothing has been
/// derived, so the cell resolves nothing and is left out entirely instead of carrying a guess (FR-089).
/// </para>
/// </summary>
public static class RequestDispositionLabels
{
    /// <summary>The word for a disposition, or <see langword="null" /> when nothing has been derived.</summary>
    public static string? Of(RequestDisposition disposition) => disposition switch
    {
        RequestDisposition.FinishedGoods => "Finished Goods",
        RequestDisposition.OutsideService => "Outside Service",
        RequestDisposition.WorkInProcess => "WIP",
        RequestDisposition.Ncm => "NCM",
        _ => null,
    };
}
