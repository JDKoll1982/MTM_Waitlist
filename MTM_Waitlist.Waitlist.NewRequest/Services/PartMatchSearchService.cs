using System.Globalization;

using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Waitlist.Models;

namespace MTM_Waitlist.Module_Waitlist.Services;

/// <inheritdoc cref="IPartMatchSearchService" />
/// <summary>
/// The live-only fuzzy part search: one Infor Visual read, no mirror declaration and no cached shape, because a
/// contains-search over every part is not a mirrorable shape (FR-084, research R7).
/// </summary>
/// <remarks>
/// <para>
/// The read runs off the thread that asked for it. That matters here more than elsewhere: the operator is typing a
/// correction and waiting for a list, so the read is dispatched to the thread pool — the same way the reachability
/// probe host runs its loop — rather than occupying the caller while a plant-system connection is established.
/// </para>
/// <para>
/// Nothing is cached and nothing is substituted. An unreachable plant system answers
/// <see cref="PartMatchSearchStatus.Unavailable"/> rather than reaching for the mirror, which is the designed
/// behaviour of this step rather than an error path (FR-085).
/// </para>
/// </remarks>
public sealed class PartMatchSearchService : IPartMatchSearchService
{
    /// <summary>
    /// The checked-in queue script this search runs, loaded from disk through the same script store every other
    /// Infor Visual read uses. The filename appears here and nowhere else in the code.
    /// </summary>
    public const string ScriptRelativePath =
        "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql";

    /// <summary>The read's own name for the term it matches part numbers against.</summary>
    public const string TermParameterName = "Term";

    /// <summary>The column the read answers each part number under.</summary>
    private const string PartNumberColumn = "PartNumber";

    private readonly IVisualQueryExecutor _executor;

    /// <summary>Creates the search.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    public PartMatchSearchService(IVisualQueryExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        _executor = executor;
    }

    /// <inheritdoc />
    public async Task<PartMatchSearchResult> FindMatchesAsync(string? term, CancellationToken cancellationToken = default)
    {
        var normalized = PartMatchSearchInput.Normalize(term);
        if (normalized.Length == 0)
        {
            return PartMatchSearchResult.NothingToSearch(normalized);
        }

        var parameters = new Dictionary<string, object?>
        {
            [TermParameterName] = normalized,
        };

        VisualQueryOutcome outcome;
        try
        {
            outcome = await Task
                .Run(
                    async () => await _executor.ExecuteAsync(ScriptRelativePath, parameters, cancellationToken).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "NewRequestPartSearch",
                ex,
                $"The live part search for '{normalized}' threw before answering; the step will use the typed value.");
            return PartMatchSearchResult.Failed(normalized, ex.Message);
        }

        switch (outcome.Status)
        {
            case VisualQueryStatus.Ok:
                var partNumbers = ReadPartNumbers(outcome.Rows);

                AppLog.Info(
                    "NewRequestPartSearch",
                    $"The live part search for '{normalized}' answered {partNumbers.Count} part(s).");

                var exact = partNumbers.FirstOrDefault(
                    partNumber => string.Equals(partNumber, normalized, StringComparison.OrdinalIgnoreCase));

                return exact is not null
                    ? PartMatchSearchResult.Resolved(normalized, exact)
                    : PartMatchSearchResult.Contains(normalized, partNumbers);

            case VisualQueryStatus.Unreachable:
                AppLog.Info(
                    "NewRequestPartSearch",
                    $"The plant system could not be reached for the part search '{normalized}': {outcome.Reason ?? "no reason reported"}. "
                    + "The operator is told the search is unavailable and their typed value is used.");
                return PartMatchSearchResult.Unavailable(normalized, outcome.Reason);

            default:
                AppLog.Warn(
                    "NewRequestPartSearch",
                    $"The live part search for '{normalized}' failed: {outcome.Reason ?? "no reason reported"}. "
                    + "The operator is told and their typed value is used.");
                return PartMatchSearchResult.Failed(normalized, outcome.Reason);
        }
    }

    /// <summary>
    /// The part numbers the read answered, in the read's own order, with a repeated number counted once.
    /// </summary>
    private static IReadOnlyList<string> ReadPartNumbers(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var partNumbers = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!row.TryGetValue(PartNumberColumn, out var value))
            {
                continue;
            }

            var partNumber = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            if (partNumber.Length > 0 && seen.Add(partNumber))
            {
                partNumbers.Add(partNumber);
            }
        }

        return partNumbers;
    }
}
