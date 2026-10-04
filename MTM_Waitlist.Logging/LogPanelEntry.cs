using System.Globalization;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// One entry as the developer panel reads it back: the columns of <c>ops_startup_logs</c> the reader procedures
/// return (`contracts/logging-contract.md` §7).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the read shape, and <see cref="StoreLogRecord"/> is the write shape.</b> They are separate types on
/// purpose. The write shape is composed by the seam and carries what a write needs; this one carries what a
/// reader needs on top of that, which is the row's own identity, the store's clock and the two chain columns the
/// write procedure computes for itself and the application never supplies. A single type serving both would have
/// to make the chain and the timestamp nullable for the writer's sake and then re-promise them for the reader's.
/// </para>
/// <para>
/// <b>Both the panel and its copy take this type.</b> The panel draws these members and the formatter writes
/// them, in one order that lives in one place (<see cref="LogExportFormatter"/>), so a column that is added to
/// the store and read here cannot appear on the screen and go missing from a pasted copy.
/// </para>
/// <para>
/// <b>Nothing here interprets a value.</b> The exception chain and the payload stay exactly as the store holds
/// them, because the copy the panel makes has to be the evidence rather than a summary of it (FR-038, SC-017).
/// </para>
/// </remarks>
public sealed record LogPanelEntry
{
    /// <summary>The store's own row identity, used to order two entries written in the same clock second.</summary>
    public required long Id { get; init; }

    /// <summary>The entry's public identifier, which is what a reader quotes when they ask about one entry.</summary>
    public required string PublicId { get; init; }

    /// <summary>The workflow the entry belongs to, so a reader can gather everything one operation did.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>When the entry was written, on the store's clock, in UTC.</summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>The severity, in the one vocabulary the seam writes and the panel filters on.</summary>
    public required string Level { get; init; }

    /// <summary>The operation that was under way.</summary>
    public required string EventAction { get; init; }

    /// <summary>How the operation ended: <c>Success</c>, <c>Failure</c>, <c>Blocked</c> or <c>Retried</c>.</summary>
    public required string Outcome { get; init; }

    /// <summary>Whether a person was acting or the application was, as the seam recorded it.</summary>
    public string? ActorKind { get; init; }

    /// <summary>The internal account identifier of the person, or null when nobody was signed in.</summary>
    public string? ActorId { get; init; }

    /// <summary>This machine's hostname, which is the machine filter the panel offers.</summary>
    public string? HostId { get; init; }

    /// <summary>This machine's hardware address, as the seam normalised it.</summary>
    public string? MacAddress { get; init; }

    /// <summary>The part of the application the entry came from.</summary>
    public string? Module { get; init; }

    /// <summary>The fault's type, or null for an entry raised without a fault.</summary>
    public string? ErrorType { get; init; }

    /// <summary>What the entry says.</summary>
    public required string Message { get; init; }

    /// <summary>The serialized exception chain, exactly as the store holds it.</summary>
    public string? ExceptionDetail { get; init; }

    /// <summary>The grouping hash, which is what makes a repeated fault one signal with a count.</summary>
    public string? ErrorFingerprint { get; init; }

    /// <summary>The previous entry's hash, which is this entry's link to what came before it.</summary>
    public string? PreviousHash { get; init; }

    /// <summary>This entry's own hash, which the next entry links to.</summary>
    public required string EntryHash { get; init; }

    /// <summary>The context the seam gathered, exactly as the store holds it.</summary>
    public string? PayloadJson { get; init; }

    /// <summary>
    /// When the entry was written, as both the panel and the copy show it.
    /// </summary>
    /// <remarks>
    /// The wording lives here rather than in either reader, so the line a developer reads on the screen and the
    /// line that reaches a pasted copy cannot drift apart. The store's clock is authoritative, so the value is
    /// written in UTC with its zone stated rather than converted to the reader's local time.
    /// </remarks>
    public string CreatedText => CreatedUtc == DateTime.MinValue
        ? "none"
        : CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z";

    /// <summary>
    /// This entry's place in the tamper-evident chain, as both the panel and the copy show it.
    /// </summary>
    /// <remarks>
    /// Both halves are written together, because a link is only meaningful beside the hash it points at. This is
    /// the only place the chain is ever checked by eye, so the two values travel as one sentence.
    /// </remarks>
    public string ChainLinkText => string.Format(
        CultureInfo.InvariantCulture,
        "previous={0} entry={1}",
        string.IsNullOrWhiteSpace(PreviousHash) ? "none" : PreviousHash,
        string.IsNullOrWhiteSpace(EntryHash) ? "none" : EntryHash);

    /// <summary>
    /// Reads one row of a reader procedure's result.
    /// </summary>
    /// <param name="row">One row, keyed by the column names the procedures select.</param>
    /// <returns>The entry the row describes.</returns>
    /// <remarks>
    /// A reader procedure's result is a dictionary of columns, not a mapped type, so this is where the store's
    /// column names meet the members above. Reading is defensive for the same reason the seam's own reads are: a
    /// row that carries no <c>payload_json</c> is an old entry, and an old entry must still be readable rather
    /// than costing the whole page (FR-021).
    /// </remarks>
    public static LogPanelEntry FromRow(IReadOnlyDictionary<string, object?> row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new LogPanelEntry
        {
            Id = ReadLong(row, "id"),
            PublicId = ReadText(row, "public_id") ?? string.Empty,
            CorrelationId = ReadText(row, "correlation_id") ?? string.Empty,
            CreatedUtc = ReadTimestamp(row, "created_utc"),
            Level = ReadText(row, "level") ?? string.Empty,
            EventAction = ReadText(row, "event_action") ?? string.Empty,
            Outcome = ReadText(row, "outcome") ?? string.Empty,
            ActorKind = ReadText(row, "actor_kind"),
            ActorId = ReadText(row, "actor_id"),
            HostId = ReadText(row, "host_id"),
            MacAddress = ReadText(row, "mac_address"),
            Module = ReadText(row, "module"),
            ErrorType = ReadText(row, "error_type"),
            Message = ReadText(row, "message") ?? string.Empty,
            ExceptionDetail = ReadText(row, "exception_detail"),
            ErrorFingerprint = ReadText(row, "error_fingerprint"),
            PreviousHash = ReadText(row, "previous_hash"),
            EntryHash = ReadText(row, "entry_hash") ?? string.Empty,
            PayloadJson = ReadText(row, "payload_json"),
        };
    }

    private static object? Value(IReadOnlyDictionary<string, object?> row, string column) =>
        row.TryGetValue(column, out var value) && value is not DBNull ? value : null;

    private static string? ReadText(IReadOnlyDictionary<string, object?> row, string column) =>
        Value(row, column)?.ToString();

    private static long ReadLong(IReadOnlyDictionary<string, object?> row, string column)
    {
        // A store that answered a decimal, a string or a null answers a number nobody can order by, so a row
        // without a usable identity is ordered last rather than throwing and taking the page with it.
        try
        {
            return Convert.ToInt64(Value(row, column) ?? 0L, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return 0L;
        }
    }

    private static DateTime ReadTimestamp(IReadOnlyDictionary<string, object?> row, string column)
    {
        // Every timestamp in this store is UTC by construction, so a value read back as unspecified is treated as
        // UTC rather than being reinterpreted as local time and shifting every entry by the machine's offset.
        try
        {
            var value = Value(row, column);
            return value is null
                ? DateTime.MinValue
                : Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToUniversalTime();
        }
        catch (Exception)
        {
            return DateTime.MinValue;
        }
    }
}
