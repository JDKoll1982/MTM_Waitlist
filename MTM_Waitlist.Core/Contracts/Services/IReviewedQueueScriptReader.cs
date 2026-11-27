namespace MTM_Waitlist.Module_Core.Contracts.Services;

/// <summary>
/// The one reviewed route that runs a checked-in MTM WIP Application queue script against the WIP store.
/// </summary>
/// <remarks>
/// <para>
/// A caller names a script from the reviewed set. It cannot supply statement text, choose a database target,
/// or pass a name the reviewed set does not hold: an unreviewed name is refused before a connection is opened.
/// That is what makes the stored-procedure rule true rather than nearly true for everything except this route.
/// </para>
/// <para>
/// The route exists because the alternative — turning the checked-in queue scripts into stored procedures —
/// is database authoring, which is outside this feature. It is deliberately not part of
/// <see cref="IMySqlHelperServer"/>: the data seam runs stored procedures only.
/// </para>
/// </remarks>
public interface IReviewedQueueScriptReader
{
    /// <summary>
    /// Runs one reviewed queue script.
    /// </summary>
    /// <param name="reviewedScriptName">The script's name, without the <c>.sql</c> suffix.</param>
    /// <param name="parameters">The script's parameters, which the seam normalises.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>
    /// The rows the script answered, bounded by the seam's shared row cap, or <c>null</c> when the name is not
    /// in the reviewed set or its script is missing from the installation. <c>null</c> is "the script is not
    /// here", which is a different answer from an empty list.
    /// </returns>
    Task<IReadOnlyList<Dictionary<string, object?>>?> ExecuteReviewedScriptQueryAsync(
        string reviewedScriptName,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);
}
