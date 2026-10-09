using System.Globalization;

using MTM_Waitlist.Module_Core.Contracts.Services;

namespace MTM_Waitlist.Module_Logging;

/// <summary>
/// The logging seam: what a call site writes through, and where everything the call site cannot know is captured
/// (FR-021, FR-032 to FR-035; `contracts/logging-contract.md` §1).
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here is conditional and nothing here is a debug-only path.</b> A Release build records exactly what
/// a Debug build records, which is the whole point of replacing a type whose members carried a conditional
/// attribute and were therefore compiled out of the shipped application (SC-003, plan D6).
/// </para>
/// <para>
/// <b>What this type captures rather than receives:</b> the machine's hostname and hardware address from
/// <see cref="IMachineFacts"/>, the signed-in person's internal identifier from <see cref="IPersonIdentity"/>,
/// the serialized exception chain and its grouping fingerprint from <see cref="ExceptionDetailSerializer"/>, the
/// runtime, view and store context from <see cref="DiagnosticContext"/>, an action when the caller named none,
/// an outcome when the caller named none, and a correlation identifier when the caller has none.
/// </para>
/// <para>
/// <b>Every member returns without awaiting the store.</b> The composed record is queued and the caller
/// continues; <see cref="FlushAsync"/> is the only bounded wait and the only member that can be awaited.
/// </para>
/// </remarks>
public sealed class LogService : ILogService
{
    /// <summary>The action used when a caller names none, so <c>event_action</c> is never blank.</summary>
    private const string UnnamedAction = "Application";

    /// <summary>The module used when a caller names none.</summary>
    private const string UnnamedModule = "Application";

    private readonly IMachineFacts _machineFacts;
    private readonly IPersonIdentity _personIdentity;
    private readonly StoreLogWriter _writer;
    private readonly LogStoreLevel _level;
    private readonly DateTime _processStartedUtc;

    /// <summary>
    /// Creates the seam.
    /// </summary>
    /// <param name="machineFacts">This machine, read at write time so no call site has to pass it.</param>
    /// <param name="personIdentity">The person, read at write time so no call site has to pass it.</param>
    /// <param name="writer">The queue the composed records go to.</param>
    /// <param name="level">
    /// The threshold this seam keeps entries at. The shipped default is
    /// <see cref="LogStoreLevel.ShippedMinimum"/>, so errors and warnings are kept and progress lines are
    /// dropped; the host passes the one instance the logging module registered, so a change made in Settings
    /// takes effect here without a restart (<see cref="LogStoreLevel.SectionName"/>).
    /// </param>
    public LogService(
        IMachineFacts machineFacts,
        IPersonIdentity personIdentity,
        StoreLogWriter writer,
        LogStoreLevel? level = null)
    {
        ArgumentNullException.ThrowIfNull(machineFacts);
        ArgumentNullException.ThrowIfNull(personIdentity);
        ArgumentNullException.ThrowIfNull(writer);

        _machineFacts = machineFacts;
        _personIdentity = personIdentity;
        _writer = writer;
        _level = level ?? new LogStoreLevel();
        _processStartedUtc = DiagnosticContext.ResolveProcessStartedUtc();
    }

    /// <summary>
    /// The threshold this seam keeps entries at, exposed to the <c>ILogger</c> provider beside it.
    /// </summary>
    /// <remarks>
    /// Exposed instead of copied so there is exactly one threshold in the process: two copies could disagree, and
    /// a level one door honoured and the other ignored would let entries of the same severity in through one door
    /// and not the other (FR-032). Internal because it is this module's own wiring and not part of the seam a call
    /// site writes through.
    /// </remarks>
    internal LogStoreLevel Level => _level;

    /// <inheritdoc />
    public void Write(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Capture(entry, exception: null, properties: null);
    }

    /// <inheritdoc />
    public void Info(string module, string message, string? target = null) =>
        Capture(
            new LogEntry(LogSeverity.Info, module, message, "Success", null, null, null, null, target, null),
            exception: null,
            properties: null);

    /// <inheritdoc />
    public void Warn(string module, string message, string? errorType = null) =>
        Capture(
            new LogEntry(LogSeverity.Warning, module, message, "Blocked", null, errorType, null, null, null, null),
            exception: null,
            properties: null);

    /// <inheritdoc />
    public void Error(string module, string message, Exception? exception = null) =>
        Capture(
            new LogEntry(LogSeverity.Error, module, message, "Failure", null, exception?.GetType().FullName, null, null, null, null),
            exception,
            properties: null);

    /// <inheritdoc />
    public void Critical(string module, string message, Exception? exception = null) =>
        Capture(
            new LogEntry(LogSeverity.Critical, module, message, "Failure", null, exception?.GetType().FullName, null, null, null, null),
            exception,
            properties: null);

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken cancellationToken) => _writer.FlushAsync(cancellationToken);

    /// <summary>
    /// Captures and queues an entry on behalf of another part of this module that holds an exception and the
    /// structured properties an <c>ILogger</c> call site carried (contract §4).
    /// </summary>
    /// <param name="entry">The entry as the caller described it.</param>
    /// <param name="exception">The fault, when there is one, so the seam serializes the chain itself.</param>
    /// <param name="properties">The structured properties, written into <c>payload_json</c>.</param>
    internal void Capture(
        LogEntry entry,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        try
        {
            // The store keeps what a reader needs rather than everything the application did, and the threshold
            // is consulted here so that both doors onto the store — this seam and the ILogger provider beside it
            // — drop the same entries (FR-032).
            if (_level.Keeps(entry.Severity) is false)
            {
                return;
            }

            // An entry raised from inside a write to the log store is the recorder describing its own work. It is
            // dropped here rather than queued, because the data-access seam announces every call it makes and the
            // announcement of a log write would itself be a log write (§1.4, FR-037).
            if (LogWriteScope.IsWriting)
            {
                return;
            }

            _writer.Enqueue(Compose(entry, exception, properties));
        }
        catch (Exception)
        {
            // The write path never throws to its caller and records nothing about its own failure, so a fault
            // cannot produce a fault (FR-037, SC-016).
        }
    }

    private StoreLogRecord Compose(
        LogEntry entry,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        var module = Blank(entry.Module) ? UnnamedModule : entry.Module.Trim();
        var action = Blank(entry.Action) ? (Blank(module) ? UnnamedAction : module) : entry.Action!.Trim();
        var outcome = Blank(entry.Outcome) ? "Success" : entry.Outcome.Trim();
        var message = string.IsNullOrWhiteSpace(entry.Message) ? "<no message>" : entry.Message;

        var errorType = Blank(entry.ErrorType) ? exception?.GetType().FullName : entry.ErrorType;
        var exceptionDetail = Blank(entry.ExceptionDetail)
            ? ExceptionDetailSerializer.Serialize(exception)
            : entry.ExceptionDetail;

        var fingerprint = Blank(entry.ExceptionFingerprint)
            ? ExceptionDetailSerializer.ComputeFingerprint(errorType, module, action, message)
            : entry.ExceptionFingerprint;

        var person = ReadPerson();
        var payload = DiagnosticContext.Build(
            entry,
            exception,
            properties,
            StoreLogWriter.DatabaseName,
            StoreLogWriter.InsertProcedureName,
            _processStartedUtc);

        return new StoreLogRecord(
            PublicId: Guid.NewGuid().ToString(),
            CorrelationId: Blank(entry.CorrelationId) ? Guid.NewGuid().ToString() : entry.CorrelationId!.Trim(),
            Level: entry.Severity.ToString().ToLowerInvariant(),
            EventAction: action,
            Outcome: outcome,
            ActorKind: person is null ? "system" : "user",
            ActorId: person,
            HostId: ReadMachineFacts(facts => facts.Hostname),
            MacAddress: ReadMachineFacts(facts => facts.MacAddress),
            Module: module,
            ErrorType: errorType,
            Message: message,
            ExceptionDetail: exceptionDetail,
            ErrorFingerprint: fingerprint,
            PayloadJson: payload);
    }

    /// <summary>
    /// Reads the signed-in person, or null when nobody is signed in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The internal account identifier is written and the display name never is: the store is built to be copied
    /// out of a panel, and a reader who needs the person's name joins the account row by the identifier they
    /// already have.
    /// </para>
    /// <para>
    /// <b>The person is context, not a gate.</b> A sign-in state that cannot be read costs the entry its person
    /// and never the entry, so a machine nobody is signed in on — and a machine whose identity read fails — still
    /// records what happened on it. Recording nothing because nobody was signed in would leave the store holding
    /// one machine's history, which is the opposite of what it is for (FR-021, FR-025).
    /// </para>
    /// </remarks>
    private string? ReadPerson()
    {
        try
        {
            return _personIdentity.IsSignedIn
                ? _personIdentity.UserId.ToString(CultureInfo.InvariantCulture)
                : null;
        }
        catch (Exception)
        {
            // An unreadable identity is recorded as nobody rather than losing the entry.
            return null;
        }
    }

    private string? ReadMachineFacts(Func<IMachineFacts, string?> read)
    {
        try
        {
            var value = read(_machineFacts);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception)
        {
            // An unreadable fact is recorded as absent rather than costing the entry (FR-015).
            return null;
        }
    }

    private static bool Blank(string? value) => string.IsNullOrWhiteSpace(value);
}
