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
/// The die answer: one Infor Visual read over the die itself, the part's recorded alternates and the part's
/// work-order history, served live while the plant system answers and from the <c>mtm_mock</c> mirror while it
/// does not.
/// </summary>
/// <remarks>
/// <para>
/// A generic die request has no job to read a die from, so the answer comes from Infor Visual alone (020 FR-001).
/// The term is normalised by the same rule the incorrect-part search applies, so a die number typed without its
/// prefix or in lower case still matches (FR-004).
/// </para>
/// <para>
/// <b>The search travels read shape 12, exactly as the work-order flow's searches travel their shapes.</b> A live
/// answer is returned as it stands, including an empty one; a plant system that cannot be reached is answered from
/// the mirror, which holds the die itself and every (part, die) association the plant records; and a read that
/// fails outright answers <see cref="GenericRequestPresentation.Unavailable"/>, because a failed read proves
/// nothing about the die (FR-005).
/// </para>
/// </remarks>
public sealed class GenericRequestDieData : IGenericRequestDieData
{
    private const string LogCategory = "GenericRequestDie";

    /// <summary>The prefix every die's part number carries.</summary>
    public const string DiePrefix = "FGT";

    private readonly IVisualReadFallback<VisualDieSearchRequest, VisualDieSearchRow> _dies;

    /// <summary>Creates the search.</summary>
    /// <param name="dies">Shape 12: the dies a die number or a part number matches.</param>
    public GenericRequestDieData(IVisualReadFallback<VisualDieSearchRequest, VisualDieSearchRow> dies)
    {
        ArgumentNullException.ThrowIfNull(dies);

        _dies = dies;
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestDieOption>> ReadDiesAsync(
        string searchText,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;
        var typed = (searchText ?? string.Empty).Trim();

        if (typed.Length == 0)
        {
            return GenericRequestStepResult<GenericRequestDieOption>.Empty(attemptedAtUtc);
        }

        // A half-typed term is answered as typed, upper-cased but not padded: the read matches by substring, so
        // zero-padding a number the person is still typing would turn a term that does match into one that does
        // not ("602" would become a die number nothing contains).
        var term = typed.ToUpperInvariant();

        var rows = await TryReadAsync(term, cancellationToken).ConfigureAwait(false);
        if (rows is null)
        {
            return GenericRequestStepResult<GenericRequestDieOption>.Unavailable(attemptedAtUtc);
        }

        // A bare number could be a die whose prefix was left off. The search asks once more as a die number rather
        // than refusing a die the plant records, and it asks with the typed term's own answer already exhausted
        // above, so a part number never loses its dies to the retry (research R2).
        if (rows.Count == 0 && DieNumberCandidate(term) is { } candidate)
        {
            rows = await TryReadAsync(candidate, cancellationToken).ConfigureAwait(false);
            if (rows is null)
            {
                return GenericRequestStepResult<GenericRequestDieOption>.Unavailable(attemptedAtUtc);
            }
        }

        var options = new List<GenericRequestDieOption>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (row.DieNumber.Length > 0 && seen.Add(row.DieNumber))
            {
                options.Add(new GenericRequestDieOption(row.DieNumber, row.Description, row.Location));
            }
        }

        AppLog.Info(LogCategory, $"The die search for '{term}' answered {options.Count} die(s).");

        return options.Count == 0
            ? GenericRequestStepResult<GenericRequestDieOption>.Empty(attemptedAtUtc)
            : GenericRequestStepResult<GenericRequestDieOption>.Populated(options, attemptedAtUtc);
    }

    /// <summary>
    /// Runs the die search's read shape, answering its rows or <see langword="null"/> when it could not be answered.
    /// </summary>
    /// <remarks>
    /// <b>The shape owns the live-then-cached decision.</b> It attempts the plant system, serves the mirror when the
    /// probe has settled on unreachability or the attempt finds the source unreachable, and returns a successful
    /// live answer including an empty one. A shape that could not be answered at all throws, which is reported here
    /// as unavailable rather than as "no die matches" — a failed read is not a verdict about the die (FR-005).
    /// </remarks>
    private async Task<IReadOnlyList<VisualDieSearchRow>?> TryReadAsync(string term, CancellationToken cancellationToken)
    {
        try
        {
            return await _dies.ReadAsync(new VisualDieSearchRequest(term), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogCategory, exception, $"The die search for '{term}' could not be answered.");
            return null;
        }
    }

    /// <summary>
    /// The die number a bare term could be, or <see langword="null"/> when the term already names a family or is not
    /// a number the plant's die numbering could produce.
    /// </summary>
    private static string? DieNumberCandidate(string term)
    {
        foreach (var prefix in PartPictureLayout.KnownPartNumberPrefixes)
        {
            if (term.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }
        }

        return term.All(char.IsAsciiDigit)
            ? DiePrefix + term.PadLeft(PartMatchSearchInput.PartNumberWidth - DiePrefix.Length, '0')
            : null;
    }
}
