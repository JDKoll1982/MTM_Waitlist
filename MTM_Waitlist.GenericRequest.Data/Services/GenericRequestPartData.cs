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
/// The live part answer: a typed term completed from Infor Visual and the WIP floor, the sequences those systems
/// can attribute to the accepted part, and the quantity owned by the chosen sequence's operation state.
/// </summary>
/// <remarks>
/// <para>
/// Every read is live twice over. Infor Visual is asked through the same executor every other plant read uses and
/// the WIP floor through the same floor service, and neither path consults the <c>mtm_mock</c> mirror: FR-017
/// forbids substituting a cached sequence list or quantity, so no shape exists for these values and none may be
/// added.
/// </para>
/// <para>
/// A source that could not be asked makes the read <see cref="GenericRequestPresentation.Unavailable"/> even when
/// the other source answered, because a part list missing everything one system knows would let the requester
/// accept a number that is not the one they meant. "Neither system knows this number" is only sayable once both
/// have answered (FR-002).
/// </para>
/// </remarks>
public sealed class GenericRequestPartData : IGenericRequestPartData
{
    private const string LogCategory = "GenericRequestPart";

    /// <summary>The checked-in Visual script that completes a typed term into part numbers.</summary>
    public const string PartMatchesScript = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartMatches.sql";

    /// <summary>The checked-in Visual script that lists a part's sequences from both relations.</summary>
    public const string PartSequencesScript = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequences.sql";

    /// <summary>The checked-in Visual script that re-reads one sequence's operation state.</summary>
    public const string PartSequenceStateScript = "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartSequenceState.sql";

    /// <summary>The checked-in Visual script that answers a part's on-hand figure when Visual owns the quantity.</summary>
    public const string PartWarehouseQuantitiesScript =
        "Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql";

    private readonly IVisualQueryExecutor _executor;
    private readonly WipFloorInventoryService _floor;

    /// <summary>Creates the live part answer.</summary>
    /// <param name="executor">Runs the live Infor Visual reads.</param>
    /// <param name="floor">Runs the live WIP floor reads.</param>
    public GenericRequestPartData(IVisualQueryExecutor executor, WipFloorInventoryService floor)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(floor);

        _executor = executor;
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

        var visual = await ReadVisualAsync(
            PartMatchesScript,
            new Dictionary<string, object?> { ["Term"] = normalized },
            cancellationToken).ConfigureAwait(false);

        var floor = await _floor.FindPartMatchesAsync(normalized, cancellationToken).ConfigureAwait(false);

        if (visual.Status != VisualQueryStatus.Ok || !floor.Succeeded)
        {
            AppLog.Info(
                LogCategory,
                $"The completions for '{normalized}' reported unavailable: Visual {visual.Status}, floor "
                + $"{(floor.Succeeded ? "answered" : "unavailable")}.");
            return GenericRequestStepResult<GenericRequestPartOption>.Unavailable(attemptedAtUtc);
        }

        var options = new List<GenericRequestPartOption>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in visual.Rows)
        {
            var partNumber = GetString(row, "PartNumber");
            if (partNumber.Length > 0 && seen.Add(partNumber))
            {
                options.Add(new GenericRequestPartOption(
                    partNumber,
                    GetString(row, "Description"),
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

        var visual = await ReadVisualAsync(
            PartSequencesScript,
            new Dictionary<string, object?> { ["PartNumber"] = normalizedPart },
            cancellationToken).ConfigureAwait(false);

        var floor = await _floor.GetPartOperationsAsync(normalizedPart, cancellationToken).ConfigureAwait(false);

        if (visual.Status != VisualQueryStatus.Ok || !floor.Succeeded)
        {
            AppLog.Info(
                LogCategory,
                $"The sequences for '{normalizedPart}' reported unavailable: Visual {visual.Status}, floor "
                + $"{(floor.Succeeded ? "answered" : "unavailable")}.");
            return GenericRequestStepResult<GenericRequestSequenceOption>.Unavailable(attemptedAtUtc);
        }

        var merged = new Dictionary<string, SequenceAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in visual.Rows)
        {
            var sequence = NormalizeSequence(GetString(row, "Sequence"));
            if (sequence.Length == 0)
            {
                continue;
            }

            var cancelledOnly = GetFlag(row, "CancelledOnly");
            var source = ReadSource(GetString(row, "SequenceSource"));

            var accumulator = Accumulator(merged, sequence);
            accumulator.AddStatus(GetString(row, "OperationStatus"));
            accumulator.AddNature(GetFlag(row, "IsLastOperation"), GetString(row, "NextResource"));

            if (cancelledOnly)
            {
                // Every operation behind this number is cancelled, so Visual does not offer it (FR-005). The floor
                // may still hold the part there, and that is a real place the part stands.
                accumulator.VisualCancelledOnly = true;
                continue;
            }

            accumulator.AddSource(source);
            accumulator.AddDescription(GetString(row, "Description"));
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

        var state = await ReadVisualAsync(
            PartSequenceStateScript,
            new Dictionary<string, object?>
            {
                ["PartNumber"] = normalizedPart,
                ["Sequence"] = normalizedSequence,
            },
            cancellationToken).ConfigureAwait(false);

        if (state.Status != VisualQueryStatus.Ok)
        {
            AppLog.Info(LogCategory, $"The state of {normalizedPart}/{normalizedSequence} was not readable: {state.Status}.");
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        var stateRow = state.Rows.FirstOrDefault();
        var status = stateRow is null ? string.Empty : GetString(stateRow, "OperationStatus");

        // The plant's own typing rule (IT, 2026-11-04): the last operation the work order still has to run
        // produces the finished good, and every other operation is work in progress. "Outside Service" is a
        // next-resource name a WIP tag prints, never a type of its own, so it decides nothing here.
        var stage = stateRow is not null && GetFlag(stateRow, "IsLastOperation")
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
        var quantities = await ReadVisualAsync(
            PartWarehouseQuantitiesScript,
            new Dictionary<string, object?> { ["PartNumber"] = partNumber },
            cancellationToken).ConfigureAwait(false);

        // The script answers one row of zeros for a part the plant knows and stocks nowhere, and no row at all for
        // a part it does not know. Only the first is an answer; the second is reported as unread rather than as a
        // confident zero.
        if (quantities.Status != VisualQueryStatus.Ok || quantities.Rows.Count == 0)
        {
            AppLog.Info(
                LogCategory,
                $"The Visual quantity for '{partNumber}' was not answered: {quantities.Status}, {quantities.Rows.Count} row(s).");
            return GenericRequestQuantityReading.Unavailable(DateTimeOffset.UtcNow);
        }

        var figure = GetDecimal(quantities.Rows[0], "WarehouseQuantity");
        return GenericRequestQuantityReading.OnHand(figure, SequenceSource.VisualOwnPart, stage, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Run one checked-in Visual script off the calling thread, so a plant-system connection never stalls the screen
    /// that asked for it.
    /// </summary>
    private async Task<VisualQueryOutcome> ReadVisualAsync(
        string scriptRelativePath,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Task
                .Run(
                    async () => await _executor.ExecuteAsync(scriptRelativePath, parameters, cancellationToken).ConfigureAwait(false),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppLog.Error(LogCategory, ex, $"The live read of '{scriptRelativePath}' threw before answering.");
            return VisualQueryOutcome.Failed(ex.Message);
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

    private static string GetString(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;

    private static bool GetFlag(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value) && value is not null && GetDecimal(row, key) != 0m;

    private static decimal GetDecimal(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return 0m;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            double doubleValue => Convert.ToDecimal(doubleValue),
            float floatValue => Convert.ToDecimal(floatValue),
            int intValue => intValue,
            long longValue => longValue,
            bool boolValue => boolValue ? 1m : 0m,
            _ => decimal.TryParse(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed) ? parsed : 0m,
        };
    }

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
