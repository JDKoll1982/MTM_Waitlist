using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MTM_Waitlist.Mock.Contracts;
using MTM_Waitlist.Mock.Models;
using MTM_Waitlist.Mock.Service.Contracts;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Reads a shape's complete result from Infor Visual and serializes it for its refresh procedure.
/// </summary>
/// <remarks>
/// <para>
/// <b>One set-based read per shape.</b> The shape's
/// <see cref="VisualReadShape.PopulationScriptRelativePath"/> enumerates the driver population
/// <i>inside</i> Infor Visual and returns the whole snapshot at once, so the service does not issue one
/// round trip per work order. The population scope is the operator decision recorded in
/// <c>tasks.md</c> (Phase 5 note, 2026-09-10): every open work order
/// (<c>STATUS IN ('R','U','F')</c>) plus its operation sequences, its subordinate parts, its parts'
/// inventory locations, and its disposition input.
/// </para>
/// <para>
/// <b>Classification is preserved, not flattened.</b> A connectivity, timeout, or login failure becomes
/// <see cref="VisualSourceUnreachableException"/> so the engine records
/// <c>skippedSourceUnreachable</c> and leaves the live snapshot untouched (FR-008). A genuine query
/// error surfaces as its own failure. A source that answers with an unexpected column set becomes
/// <see cref="VisualSourceSchemaMismatchException"/> so drift is reported rather than loaded.
/// </para>
/// <para>
/// <b>Payload contract.</b> The JSON is an array of objects keyed by the shape's live projection names —
/// its <see cref="VisualReadShape.InputParameters"/> then its
/// <see cref="VisualReadShape.OutputColumns"/> — exactly the keys <c>sp_visual_&lt;shape&gt;_refresh</c>
/// reads out of <c>p_rows</c>. Flags are emitted as <c>1</c>/<c>0</c> because the procedures cast the
/// extracted value with <c>SIGNED</c>, where a JSON boolean would become 0.
/// </para>
/// </remarks>
public sealed class VisualShapePayloadSource : IVisualShapePayloadSource
{
    private readonly IVisualQueryExecutor _executor;
    private readonly ILogger<VisualShapePayloadSource> _logger;

    /// <summary>Creates the payload source.</summary>
    /// <param name="executor">
    /// The shared classified Visual executor — the same one the in-app fallback uses, so a shape's live
    /// read cannot drift between the app and the service.
    /// </param>
    /// <param name="logger">Logger; no connection material or credential is passed to it.</param>
    public VisualShapePayloadSource(IVisualQueryExecutor executor, ILogger<VisualShapePayloadSource> logger)
    {
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(logger);

        _executor = executor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> BuildRefreshPayloadAsync(
        VisualReadShape shape,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shape);

        if (string.IsNullOrWhiteSpace(shape.PopulationScriptRelativePath))
        {
            throw new VisualSourceSchemaMismatchException(
                $"Shape '{shape.Key}' declares no population read, so the service cannot build its snapshot.");
        }

        var outcome = await _executor
            .ExecuteAsync(shape.PopulationScriptRelativePath, new Dictionary<string, object?>(), cancellationToken)
            .ConfigureAwait(false);

        switch (outcome.Status)
        {
            case VisualQueryStatus.Unreachable:
                throw new VisualSourceUnreachableException(
                    outcome.Reason ?? $"Infor Visual is unreachable while reading shape '{shape.Key}'.");

            case VisualQueryStatus.Failed:
                throw new VisualReadFailedException(
                    shape.Key,
                    outcome.Reason ?? $"The Infor Visual population read for shape '{shape.Key}' failed.");

            case VisualQueryStatus.Ok:
                break;

            default:
                throw new VisualReadFailedException(
                    shape.Key,
                    $"The Infor Visual population read for shape '{shape.Key}' returned an unknown status.");
        }

        _logger.LogInformation(
            "Shape {ShapeKey} population read returned {RowCount} row(s).",
            shape.Key,
            outcome.Rows.Count);

        return Serialize(shape, outcome.Rows);
    }

    /// <summary>
    /// Turns the raw rows into the refresh payload, validating the source's column set on the way.
    /// </summary>
    /// <param name="shape">The shape whose projection the payload must match.</param>
    /// <param name="rows">The rows the population read returned.</param>
    private string Serialize(VisualReadShape shape, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var expectedKeys = GetExpectedPayloadKeys(shape);

        if (rows.Count > 0)
        {
            ValidateColumnSet(shape, rows[0], expectedKeys);
        }

        var payload = new List<Dictionary<string, object?>>(rows.Count);

        foreach (var row in rows)
        {
            var item = new Dictionary<string, object?>(expectedKeys.Count, StringComparer.Ordinal);

            foreach (var key in expectedKeys)
            {
                item[key] = ToJsonValue(row, key);
            }

            payload.Add(item);
        }

        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// The projection names the payload must carry: the read's inputs, then its outputs, in catalog
    /// order. This is the same ordering the shape's refresh procedure extracts the columns in.
    /// </summary>
    private static List<string> GetExpectedPayloadKeys(VisualReadShape shape)
    {
        var keys = new List<string>(shape.InputParameters.Count + shape.OutputColumns.Count);

        foreach (var parameter in shape.InputParameters)
        {
            if (!keys.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
            {
                keys.Add(parameter.Name);
            }
        }

        foreach (var column in shape.OutputColumns)
        {
            if (!keys.Contains(column.Name, StringComparer.OrdinalIgnoreCase))
            {
                keys.Add(column.Name);
            }
        }

        return keys;
    }

    /// <summary>
    /// Fails the refresh when the source's columns do not match the catalog exactly (FR-020).
    /// </summary>
    /// <remarks>
    /// Drift in either direction is a fault: a missing column would be loaded as <c>NULL</c>, and an
    /// unexpected column means the query and the catalog have diverged. The previous snapshot stays in
    /// place and the shape is reported, rather than a wrong-shaped snapshot becoming live.
    /// </remarks>
    private static void ValidateColumnSet(
        VisualReadShape shape,
        IReadOnlyDictionary<string, object?> sampleRow,
        IReadOnlyList<string> expectedKeys)
    {
        var actualKeys = sampleRow.Keys.ToList();

        var missing = expectedKeys
            .Where(key => !actualKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var unexpected = actualKeys
            .Where(key => !expectedKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count == 0 && unexpected.Count == 0)
        {
            return;
        }

        throw new VisualSourceSchemaMismatchException(
            $"Shape '{shape.Key}' returned an unexpected column set. " +
            $"Missing [{string.Join(", ", missing)}], unexpected [{string.Join(", ", unexpected)}].");
    }

    /// <summary>
    /// Reads one projection value and normalizes it for JSON.
    /// </summary>
    private static object? ToJsonValue(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null || value is DBNull)
        {
            return null;
        }

        return value switch
        {
            // A flag must reach the procedure as 1/0: it casts the extracted value with SIGNED, and a
            // JSON boolean would be read as the string "true" and cast to 0.
            bool boolValue => boolValue ? 1 : 0,

            // Numbers stay numbers so the procedure's CAST(... AS DECIMAL/SIGNED) reads them directly.
            byte byteValue => byteValue,
            sbyte signedByteValue => signedByteValue,
            short shortValue => shortValue,
            ushort unsignedShortValue => unsignedShortValue,
            int intValue => intValue,
            uint unsignedIntValue => unsignedIntValue,
            long longValue => longValue,
            decimal decimalValue => decimalValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,

            // Dates are sent as ISO-8601 text; the mirrors store DATETIME, and the refresh procedures
            // only cast the columns they declare, never the timestamps (they stamp refreshed_utc).
            DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),

            Guid guidValue => guidValue.ToString(),

            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
