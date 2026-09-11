using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MTM_Waitlist.Mock.Service.Models;

namespace MTM_Waitlist.Mock.Service.Services;

/// <summary>
/// Durable, service-local store of the most recent refresh run per read shape (FR-008, FR-013).
/// </summary>
/// <remarks>
/// <para>
/// Persistence is deliberately <b>not</b> in MySQL (data-model.md §4): a restore replaces an entire
/// store, so operational history kept inside a store would be rewound by the very operation it needs
/// to describe, and refresh status must remain reportable while Infor Visual is down.
/// </para>
/// <para>
/// A save is all-or-nothing: the file is written to a temporary path and swapped into place, so a
/// reader never observes a half-written record set. Error text is sanitized on the way in, so a
/// credential can never reach the file that backs the status surface (FR-026, SC-010).
/// </para>
/// </remarks>
public sealed class RefreshRunRecordStore
{
    /// <summary>File name of the persisted run records under the service app-data root.</summary>
    public const string FileName = "refresh-run-records.json";

    /// <summary>Same credential-shaped pattern the engine redacts, applied defensively on write.</summary>
    private static readonly Regex s_secretPattern = new(
        @"(password|pwd|user\s*id|uid)\s*=\s*[^;]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _appDataRoot;
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, RefreshRunRecord> _lastRuns = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a store rooted at the given service app-data folder.
    /// </summary>
    /// <param name="appDataRoot">Absolute path to the service's own app-data folder.</param>
    public RefreshRunRecordStore(string appDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataRoot);

        _appDataRoot = appDataRoot;
        _filePath = Path.Combine(appDataRoot, FileName);
    }

    /// <summary>Absolute path of the persisted record file.</summary>
    public string FilePath => _filePath;

    /// <summary>
    /// Loads the persisted records. A missing or unreadable file yields an empty set rather than a
    /// failure: the status surface must still work on a fresh install or after a corrupt file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _lastRuns.Clear();

            if (!File.Exists(_filePath))
            {
                return;
            }

            await using var stream = File.OpenRead(_filePath);

            List<RefreshRunRecord>? records;
            try
            {
                records = await JsonSerializer
                    .DeserializeAsync<List<RefreshRunRecord>>(stream, s_jsonOptions, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                // A hand-edited or truncated file must not stop the service from recording new runs.
                return;
            }

            foreach (var record in records ?? [])
            {
                if (!string.IsNullOrWhiteSpace(record.ShapeKey))
                {
                    _lastRuns[record.ShapeKey] = record with { ErrorMessage = Sanitize(record.ErrorMessage) };
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Records one run as the shape's most recent result and persists the whole set atomically.
    /// </summary>
    /// <param name="record">The run to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RecordAsync(RefreshRunRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.ShapeKey))
        {
            throw new ArgumentException("A run record must name its shape.", nameof(record));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _lastRuns[record.ShapeKey] = record with { ErrorMessage = Sanitize(record.ErrorMessage) };
            await SaveCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns the most recent run for one shape, or <see langword="null"/> when it has never run.</summary>
    /// <param name="shapeKey">The shape's catalog key.</param>
    public RefreshRunRecord? GetLastRun(string shapeKey) =>
        shapeKey is not null && _lastRuns.TryGetValue(shapeKey, out var record) ? record : null;

    /// <summary>Returns the most recent run for every shape that has run, ordered by shape key.</summary>
    public IReadOnlyList<RefreshRunRecord> GetAllLastRuns() =>
        _lastRuns.Values.OrderBy(record => record.ShapeKey, StringComparer.Ordinal).ToList();

    /// <summary>
    /// Removes connection-string credential material from error text (FR-026).
    /// </summary>
    /// <param name="message">Candidate error text.</param>
    /// <returns>The text with any credential-shaped segment redacted.</returns>
    public static string? Sanitize(string? message) =>
        string.IsNullOrEmpty(message) ? message : s_secretPattern.Replace(message, "$1=***");

    private async Task SaveCoreAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_appDataRoot);

        var temporaryPath = _filePath + ".tmp";

        await using (var stream = File.Create(temporaryPath))
        {
            await JsonSerializer
                .SerializeAsync(stream, _lastRuns.Values.OrderBy(record => record.ShapeKey, StringComparer.Ordinal).ToList(), s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryPath, _filePath, overwrite: true);
    }
}
