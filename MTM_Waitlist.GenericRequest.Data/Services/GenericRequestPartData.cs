using System.Globalization;

using MTM_Waitlist.GenericRequest.Contracts;
using MTM_Waitlist.GenericRequest.Models;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Module_Core.Helpers;
using MTM_Waitlist.Module_Core.Services;
using MTM_Waitlist.Module_Settings.Services;

namespace MTM_Waitlist.GenericRequest.Data.Services;

/// <inheritdoc cref="IGenericRequestPartData" />
/// <summary>
/// The part answer: a typed term completed from Infor Visual and the WIP floor, the sequences those systems can
/// attribute to the accepted part, and the quantity owned by the chosen sequence's operation state.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every Infor Visual read travels a read shape, exactly as the work-order flow's do.</b> The completion list,
/// the sequence list, the operation state and the part-wide quantity are shapes 9, 10, 11 and 7, so each one is
/// answered by the live plant system while it answers and from the <c>mtm_mock</c> mirror while it does not
/// (FR-002/FR-004). That is what makes the generic request usable on a workstation off the plant network, and
/// what the die request alongside it does too.
/// </para>
/// <para>
/// <b>The floor is live, always.</b> <c>WipFloorInventoryService</c> reads an internal store, and internal stores
/// are never served from the mirror (FR-027). A source that could not be asked still makes the read
/// <see cref="GenericRequestPresentation.Unavailable"/> even when the other answered, because a part list missing
/// everything one system knows would let the requester accept a number that is not the one they meant. "Neither
/// system knows this number" is only sayable once both have answered (FR-002).
/// </para>
/// <para>
/// <b>A failed read is not a verdict about the part.</b> When a shape cannot be answered — the plant system
/// failed rather than being unreachable, or there is no mirror to fall back to — the read reports unavailable
/// rather than an empty list, so nothing claims a part has no sequences when the question was never answered.
/// </para>
/// </remarks>
public sealed class GenericRequestPartData : IGenericRequestPartData
{
    private const string LogCategory = "GenericRequestPart";

    private readonly IVisualReadFallback<VisualPartMatchesRequest, VisualPartMatchesRow> _partMatches;
    private readonly IVisualReadFallback<VisualPartSequencesRequest, VisualPartSequencesRow> _partSequences;
    private readonly IVisualReadFallback<VisualPartSequenceStateRequest, VisualPartSequenceStateRow> _sequenceState;
    private readonly IVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow> _quantities;
    private readonly WipFloorInventoryService _floor;

    /// <summary>Creates the part answer.</summary>
    /// <param name="partMatches">Shape 9: the parts a typed term matches.</param>
    /// <param name="partSequences">Shape 10: every sequence the plant attributes to one part.</param>
    /// <param name="sequenceState">Shape 11: one part's operation state at one sequence.</param>
    /// <param name="quantities">Shape 7: the part's part-wide on-hand total.</param>
    /// <param name="floor">Runs the live WIP floor reads.</param>
    public GenericRequestPartData(
        IVisualReadFallback<VisualPartMatchesRequest, VisualPartMatchesRow> partMatches,
        IVisualReadFallback<VisualPartSequencesRequest, VisualPartSequencesRow> partSequences,
        IVisualReadFallback<VisualPartSequenceStateRequest, VisualPartSequenceStateRow> sequenceState,
        IVisualReadFallback<VisualPartWarehouseQuantitiesRequest, VisualPartWarehouseQuantitiesRow> quantities,
        WipFloorInventoryService floor)
    {
        ArgumentNullException.ThrowIfNull(partMatches);
        ArgumentNullException.ThrowIfNull(partSequences);
        ArgumentNullException.ThrowIfNull(sequenceState);
        ArgumentNullException.ThrowIfNull(quantities);
        ArgumentNullException.ThrowIfNull(floor);

        _partMatches = partMatches;
        _partSequences = partSequences;
        _sequenceState = sequenceState;
        _quantities = quantities;
        _floor = floor;
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestPartOption>> ReadCompletionsAsync(
        string term,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;
        var normalized = (term ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return GenericRequestStepResult<GenericRequestPartOption>.Empty(attemptedAtUtc);
        }

        var visual = await TryReadAsync(
            _partMatches,
            new VisualPartMatchesRequest(normalized),
            "the completions for '" + normalized + "'",
            cancellationToken).ConfigureAwait(false);

        var floor = await _floor.FindPartMatchesAsync(normalized, cancellationToken).ConfigureAwait(false);

        if (visual is null || !floor.Succeeded)
        {
            AppLog.Info(
                LogCategory,
                $"The completions for '{normalized}' reported unavailable: Visual "
                + $"{(visual is null ? "unavailable" : "answered")}, floor {(floor.Succeeded ? "answered" : "unavailable")}.");
            return GenericRequestStepResult<GenericRequestPartOption>.Unavailable(attemptedAtUtc);
        }

        var options = new List<GenericRequestPartOption>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in visual)
        {
            var partNumber = row.PartNumber;
            if (partNumber.Length > 0 && seen.Add(partNumber))
            {
                options.Add(new GenericRequestPartOption(
                    partNumber,
                    row.Description,
                    SequenceSource.VisualOwnPart));
            }
        }

        // A number both systems know is already offered, with the description only Visual holds, so the floor's
        // duplicate adds nothing a person would read.
        foreach (var partNumber in floor.Rows)
        {
            if (seen.Add(partNumber))
            {
                options.Add(new GenericRequestPartOption(partNumber, string.Empty, SequenceSource.WipFloor));
            }
        }

        return options.Count == 0
            ? GenericRequestStepResult<GenericRequestPartOption>.Empty(attemptedAtUtc)
            : GenericRequestStepResult<GenericRequestPartOption>.Populated(options, attemptedAtUtc);
    }

    /// <inheritdoc />
    public async Task<GenericRequestStepResult<GenericRequestSequenceOption>> ReadSequencesAsync(
        string partNumber,
        CancellationToken cancellationToken = default)
    {
        var attemptedAtUtc = DateTimeOffset.UtcNow;
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        if (normalizedPart.Length == 0)
        {
            return GenericRequestStepResult<GenericRequestSequenceOption>.Empty(attemptedAtUtc);
        }

        var visual = await TryReadAsync(
            _partSequences,
            new VisualPartSequencesRequest(normalizedPart),
            "the sequences for '" + normalizedPart + "'",
            cancellationToken).ConfigureAwait(false);

        var floor = await _floor.GetPartOperationsAsync(normalizedPart, cancellationToken).ConfigureAwait(false);

        if (visual is null || !floor.Succeeded)
        {
            AppLog.Info(
                LogCategory,
                $"The sequences for '{normalizedPart}' reported unavailable: Visual "
                + $"{(visual is null ? "unavailable" : "answered")}, floor {(floor.Succeeded ? "answered" : "unavailable")}.");
            return GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(attemptedAtUtc);
        }

        var merged = new Dictionary<string, SequenceAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in visual)
        {
            var sequence = NormalizeSequence(row.Sequence);
            if (sequence.Length == 0)
            {
                continue;
            }

            var cancelledOnly = row.CancelledOnly != 0;
            var source = ReadSource(row.SequenceSource);

            var accumulator = Accumulator(merged, sequence);
            accumulator.AddStatus(row.OperationStatus);
            accumulator.AddNature(row.IsLastOperation != 0, row.NextResource);

            if (cancelledOnly)
            {
                // Every operation behind this number is cancelled, so Visual does not offer it (FR-005). The floor
                // may still hold the part there, and that is a real place the part stands.
                accumulator.VisualCancelledOnly = true;
                continue;
            }

            accumulator.AddSource(source);
            accumulator.AddDescription(row.Description);
        }

        foreach (var sequence in floor.Rows)
        {
            var normalizedSequence = NormalizeSequence(sequence);

            // The floor script already drops 'N/A' and anything else that is not an operation number, and this
            // keeps a second guard in code: a value that is not a sequence number is never offered as one
            // (FR-006).
            if (normalizedSequence.Length == 0 || !normalizedSequence.All(char.IsAsciiDigit))
            {
                continue;
            }

            var accumulator = Accumulator(merged, normalizedSequence);
            accumulator.AddSource(SequenceSource.WipFloor);
        }

        var options = merged
            .Where(pair => pair.Value.Sources.Count > 0)
            .OrderBy(pair => pair.Key, SequenceOrder())
            .Select(pair => pair.Value.ToOption(pair.Key))
            .ToArray();

        return options.Length == 0
            ? GenericRequestStepResult<GenericRequestSequenceOption>.Empty(attemptedAtUtc)
            : GenericRequestStepResult<GenericRequestSequenceOption>.Populated(options, attemptedAtUtc);
    }

    /// <inheritdoc />
    public async Task<GenericRequestQuantityReading> ReadQuantityAsync(
        string partNumber,
        string sequence,
        CancellationToken cancellationToken = default)
    {
        var normalizedPart = (partNumber ?? string.Empty).Trim();
        var normalizedSequence = NormalizeSequence(sequence);

        if (normalizedPart.Length == 0 || normalizedSequence.Length == 0)
        {
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        var state = await TryReadAsync(
            _sequenceState,
            new VisualPartSequenceStateRequest(normalizedPart, normalizedSequence),
            "the state of " + normalizedPart + "/" + normalizedSequence,
            cancellationToken).ConfigureAwait(false);

        if (state is null)
        {
            AppLog.Info(LogCategory, $"The state of {normalizedPart}/{normalizedSequence} was not readable.");
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        var stateRow = state.FirstOrDefault();
        var status = stateRow?.OperationStatus ?? string.Empty;

        // The plant's own typing rule (IT, 2026-11-04): the last operation the work order still has to run
        // produces the finished good, and every other operation is work in progress. "Outside Service" is a
        // next-resource name a WIP tag prints, never a type of its own, so it decides nothing here.
        var stage = stateRow is not null && stateRow.IsLastOperation != 0
            ? GenericRequestPartStage.FinishedGood
            : GenericRequestPartStage.Wip;

        // Who owns the quantity is the operation's state, not its type (FR-009): an operation still open has its
        // material on the WIP floor by part and sequence, and one already closed has it in Infor Visual by part
        // number.
        if (RequestDispositionStatusCodes.OpenStatusCodes.Contains(status))
        {
            return await ReadFromFloorAsync(normalizedPart, normalizedSequence, stage, cancellationToken)
                .ConfigureAwait(false);
        }

        if (RequestDispositionStatusCodes.ClosedStatusCodes.Contains(status))
        {
            return await ReadFromVisualAsync(normalizedPart, stage, cancellationToken)
                .ConfigureAwait(false);
        }

        // Visual records no state the plant relies on, or none at all: the floor is asked first and Visual only
        // when the floor holds the part nowhere at that sequence (FR-011).
        var fromFloor = await _floor
            .GetOperationQuantityAsync(normalizedPart, normalizedSequence, cancellationToken)
            .ConfigureAwait(false);

        if (!fromFloor.Succeeded)
        {
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        if (fromFloor.Rows.Count > 0 && fromFloor.Rows[0].LocationCount > 0)
        {
            var held = fromFloor.Rows[0];
            return GenericRequestQuantityReading.OnHand(held.Quantity, SequenceSource.WipFloor, GenericRequestPartStage.Unknown, DateTimeOffset.UtcNow);
        }

        return await ReadFromVisualAsync(normalizedPart, GenericRequestPartStage.Unknown, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GenericRequestQuantityReading> ReadFromFloorAsync(
        string partNumber,
        string sequence,
        GenericRequestPartStage stage,
        CancellationToken cancellationToken)
    {
        var fromFloor = await _floor
            .GetOperationQuantityAsync(partNumber, sequence, cancellationToken)
            .ConfigureAwait(false);

        if (!fromFloor.Succeeded)
        {
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        // The script always answers one row, and a zero is the floor saying it holds none: a fact, not a gap
        // (FR-013).
        var held = fromFloor.Rows.Count > 0 ? fromFloor.Rows[0] : new WipOperationQuantity(0m, 0);
        return GenericRequestQuantityReading.OnHand(held.Quantity, SequenceSource.WipFloor, stage, DateTimeOffset.UtcNow);
    }

    private async Task<GenericRequestQuantityReading> ReadFromVisualAsync(
        string partNumber,
        GenericRequestPartStage stage,
        CancellationToken cancellationToken)
    {
        var quantities = await TryReadAsync(
            _quantities,
            new VisualPartWarehouseQuantitiesRequest(partNumber),
            "the Visual quantity for '" + partNumber + "'",
            cancellationToken).ConfigureAwait(false);

        // The shape answers one row for a part the plant knows, and no row at all for a part it does not know.
        // Only the first is an answer; the second is reported as unread rather than as a confident zero.
        if (quantities is null || quantities.Count == 0)
        {
            AppLog.Info(
                LogCategory,
                $"The Visual quantity for '{partNumber}' was not answered: {(quantities is null ? 0 : quantities.Count)} row(s).");
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        return GenericRequestQuantityReading.OnHand(
            quantities[0].WarehouseQuantity,
            SequenceSource.VisualOwnPart,
            stage,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Runs one read shape, answering its rows or <see langword="null"/> when the shape could not be answered.
    /// </summary>
    /// <remarks>
    /// <b>The shape owns the live-then-cached decision, not this caller.</b> It attempts the plant system, serves
    /// the mirror when the probe has settled on unreachability or the attempt finds the source unreachable, and
    /// returns a successful live answer exactly as it stands — including an empty one. A shape that could not be
    /// answered at all throws, and that is reported here as "not answered" rather than as an empty answer, so a
    /// failed read never claims the part has nothing.
    /// </remarks>
    private static async Task<IReadOnlyList<TRow>?> TryReadAsync<TRequest, TRow>(
        IVisualReadFallback<TRequest, TRow> shape,
        TRequest request,
        string what,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task
                .Run(async () => await shape.ReadAsync(request, cancellationToken).ConfigureAwait(false), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            AppLog.Error(LogCategory, exception, $"The read of {what} could not be answered.");
            return null;
        }
    }

    private static SequenceAccumulator Accumulator(
        Dictionary<string, SequenceAccumulator> merged,
        string sequence)
    {
        if (!merged.TryGetValue(sequence, out var accumulator))
        {
            accumulator = new SequenceAccumulator();
            merged[sequence] = accumulator;
        }

        return accumulator;
    }

    private static SequenceSource ReadSource(string value) =>
        string.Equals(value, "Requirement", StringComparison.OrdinalIgnoreCase)
            ? SequenceSource.VisualRequirement
            : SequenceSource.VisualOwnPart;

    /// <summary>
    /// The form a sequence number is compared and ordered by: trimmed, with leading zeros removed so a Visual
    /// <c>020</c> and a floor <c>20</c> are one sequence.
    /// </summary>
    private static string NormalizeSequence(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (!trimmed.All(char.IsAsciiDigit))
        {
            return trimmed;
        }

        var withoutZeros = trimmed.TrimStart('0');
        return withoutZeros.Length == 0 ? "0" : withoutZeros;
    }

    /// <summary>Numeric order where both are numbers, so <c>9</c> sorts before <c>10</c>.</summary>
    private static IComparer<string> SequenceOrder() => Comparer<string>.Create((left, right) =>
    {
        var leftIsNumber = long.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
        var rightIsNumber = long.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);

        return leftIsNumber && rightIsNumber
            ? leftNumber.CompareTo(rightNumber)
            : string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    });

    /// <summary>One sequence number while the Visual and floor answers are still being merged.</summary>
    private sealed class SequenceAccumulator
    {
        private readonly HashSet<SequenceSource> _sources = new();
        private readonly List<string> _statuses = new();
        private bool _natureRead;

        public bool VisualCancelledOnly { get; set; }

        public IReadOnlyCollection<SequenceSource> Sources => _sources;

        public string Description { get; private set; } = string.Empty;

        public bool IsLastOperation { get; private set; }

        public string NextResource { get; private set; } = string.Empty;

        public void AddSource(SequenceSource source) => _sources.Add(source);

        /// <summary>
        /// Record what this sequence is, from the first row that says: whether it is the last operation the work
        /// order still has to run, and the resource the part goes to next. The own-part relation is read first, so
        /// when both relations name a number it is the part's own routing that describes it.
        /// </summary>
        public void AddNature(bool isLastOperation, string nextResource)
        {
            if (_natureRead)
            {
                return;
            }

            _natureRead = true;
            IsLastOperation = isLastOperation;
            NextResource = nextResource;
        }

        public void AddDescription(string description)
        {
            if (Description.Length == 0 && description.Length > 0)
            {
                Description = description;
            }
        }

        public void AddStatus(string status)
        {
            if (status.Length > 0)
            {
                _statuses.Add(status);
            }
        }

        public GenericRequestSequenceOption ToOption(string sequence)
        {
            var ordered = Enum
                .GetValues<SequenceSource>()
                .Where(_sources.Contains)
                .ToArray();

            return new GenericRequestSequenceOption(
                sequence,
                Description,
                ordered,
                RepresentativeStatus(),
                IsLastOperation,
                NextResource,
                VisualCancelledOnly && _sources.Count == 0);
        }

        /// <summary>
        /// The code the caller classifies against: an open one where any operation at that sequence is open, then a
        /// closed one, and otherwise whatever was recorded.
        /// </summary>
        private string RepresentativeStatus()
        {
            var open = _statuses.FirstOrDefault(RequestDispositionStatusCodes.OpenStatusCodes.Contains);
            if (open is not null)
            {
                return open;
            }

            var closed = _statuses.FirstOrDefault(RequestDispositionStatusCodes.ClosedStatusCodes.Contains);
            return closed ?? _statuses.FirstOrDefault() ?? string.Empty;
        }
    }
}
