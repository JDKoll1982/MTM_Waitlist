using System.Globalization;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Shared.Helpers;
using MTM_Waitlist.Module_Waitlist.Services;

namespace MTM_Waitlist.GenericRequest.Data.Services;

/// <inheritdoc cref="IGenericRequestDieData" />
/// <summary>
/// The live die search: one Infor Visual read over the die itself, the part's recorded alternates and the part's
/// work-order history, with no mirror declaration and no cached shape.
/// </summary>
/// <remarks>
/// <para>
/// A generic die request has no job to read a die from, so the answer comes from Infor Visual alone (020 FR-001).
/// The term is normalised by the same rule the incorrect-part search applies, so a die number typed without its
/// prefix or in lower case still matches (FR-004).
/// </para>
/// <para>
/// Nothing is cached and nothing is substituted. An unreachable plant system answers
/// <see cref="GenericRequestPresentation.Unavailable"/> rather than reaching for the mirror, and a read that failed
/// answers the same thing, because a failed read proves nothing about the die (FR-005).
/// </para>
/// </remarks>
public sealed class GenericRequestDieData : IGenericRequestDieData
{
    private const string LogCategory = "GenericRequestDie";

    /// <summary>
    /// The checked-in queue script this search runs, loaded from disk through the same executor every other Infor
    /// Visual read uses.
    /// </summary>
    public const string ScriptRelativePath = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetDieSearch.sql";

    /// <summary>The read's own name for the term it matches dies against.</summary>
    public const string TermParameterName = "Term";

    /// <summary>The prefix every die's part number carries.</summary>
    public const string DiePrefix = "FGT";

    private readonly IVisualQueryExecutor _executor;

    /// <summary>Creates the search.</summary>
    /// <param name="executor">Runs the live read against Infor Visual.</param>
    public GenericRequestDieData(IVisualQueryExecutor executor)
    {
        ArgumentNullException.ThrowIfNull(executor);

        _executor = executor;
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;
        var normalized = PartMatchSearchInput.Normalize(searchText);

        if (normalized.Length == 0)
        {
            return GenericRequestStepResult<GenericRequestDieOption>.Empty(attemptedAtUtc);
        }

        var outcome = await ExecuteAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (outcome.Status != VisualQueryStatus.Ok)
        {
            return NotAnswered(outcome, normalized);
        }

        var rows = outcome.Rows;

        // A bare number could be a die whose prefix was left off. The search asks once more as a die number rather
        // than refusing a die the plant records, and it asks with the part's own answer already exhausted above, so
        // a part number never loses its dies to the retry (research R2).
        if (rows.Count == 0 && DieNumberCandidate(normalized) is { } candidate)
        {
            var retry = await ExecuteAsync(candidate, cancellationToken).ConfigureAwait(false);
            if (retry.Status != VisualQueryStatus.Ok)
            {
                return NotAnswered(retry, candidate);
            }

            rows = retry.Rows;
        }

        var options = new List<GenericRequestDieOption>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var dieNumber = GetString(row, "DieNumber");
            if (dieNumber.Length > 0 && seen.Add(dieNumber))
            {
                options.Add(new GenericRequestDieOption(
                    dieNumber,
                    GetString(row, "Description"),
                    GetString(row, "Location")));
            }
        }

        AppLog.Info(LogCategory, $"The live die search for '{normalized}' answered {options.Count} die(s).");

        return options.Count == 0
            ? GenericRequestStepResult<GenericRequestDieOption>.Empty(attemptedAtUtc)
            : GenericRequestStepResult<GenericRequestDieOption>.Populated(options, attemptedAtUtc);
    }

    private async Task<VisualQueryOutcome> ExecuteAsync(string term, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?> { [TermParameterName] = term };

        try
        {
            return await _executor.ExecuteAsync(ScriptRelativePath, parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, $"The live die search for '{term}' threw before answering.");

            return VisualQueryOutcome.Failed(ex.Message);
        }
    }

    private static GenericRequestStepResult<GenericRequestDieOption> NotAnswered(VisualQueryOutcome outcome, string term)
    {
        if (outcome.Status == VisualQueryStatus.Unreachable)
        {
            AppLog.Info(
                LogCategory,
                $"The plant system could not be reached for the die search '{term}': {outcome.Reason ?? "no reason reported"}.");
        }
        else
        {
            AppLog.Warn(
                LogCategory,
                $"The live die search for '{term}' failed: {outcome.Reason ?? "no reason reported"}.");
        }

        return GenericRequestStepResult<GenericRequestDieOption>.Unavailable(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// The die number a bare term could be, or <see langword="null"/> when the term already names a family or is not
    /// a number the plant's die numbering could produce.
    /// </summary>
    private static string? DieNumberCandidate(string normalized)
    {
        foreach (var prefix in PartPictureLayout.KnownPartNumberPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return normalized.All(char.IsAsciiDigit)
            ? DiePrefix + normalized.PadLeft(PartMatchSearchInput.PartNumberWidth - DiePrefix.Length, '0')
            : null;
    }

    private static string GetString(IReadOnlyDictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value)
            ? Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
}
