# Contract: the logging seam

Source: brief S9, S9.1 to S9.4, and `MTM_Waitlist_Exception_Logging_Reference.md` reconciled with the repository
on 2026-09-26. Requirements: FR-021, FR-032 to FR-037, SC-003, SC-014 to SC-016. The table is
`ops_startup_logs`, pinned verbatim.

Logging is its own module, `MTM_Waitlist.Logging`. It is not part of startup, so the shell, Settings and the
module libraries can use it without referencing the launch pipeline.

---

## 1. The seam

```csharp
public interface ILogService
{
    void Write(LogEntry entry);
    void Info(string module, string message, string? target = null);
    void Warn(string module, string message, string? errorType = null);
    void Error(string module, string message, Exception? exception = null);
    void Critical(string module, string message, Exception? exception = null);
    Task FlushAsync(CancellationToken cancellationToken);
}

public enum LogSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public sealed record LogEntry(
    LogSeverity Severity,
    string Module,
    string Message,
    string Outcome,
    string? Action,
    string? ErrorType,
    string? ExceptionDetail,
    string? ExceptionFingerprint,
    string? Target,
    string? CorrelationId);
```

`LogSeverity` is the single severity vocabulary. The launch surface's `LaunchDiagnostic` uses the same type, so a
fault shown on the splash and the entry it wrote are filterable together and cannot drift into two spellings.

Rules drawn from requirements:

- **The seam is unconditional.** It carries no `[Conditional]` attribute and no `#if DEBUG`. The type it
  replaces, `StartupDebugLog`, is marked `[Conditional("DEBUG")]`, which is why a Release build currently
  records nothing and loses the arguments along with the calls (SC-003). A replacement that kept the attribute
  would leave the table empty on a shop-floor machine and the panel showing nothing.
- **Severity, module, machine, person, error type and exception detail are captured by the seam, not supplied
  by the call site.** A call site that has to remember to pass the machine will eventually not (FR-021).
- Machine and person come from `IMachineFacts` and `IPersonIdentity` at write time. Both are read-only
  contracts, so the seam only reads them.
- The severity vocabulary is the one the panel filters on. It matches the table's `level` column.
- `Action` becomes the entry's `event_action`, which the table declares `NOT NULL`. A caller is asked for the
  operation under way; when it does not name one the seam uses the module, because an entry no one can filter by
  action is an entry no one can find (FR-035).
- `Outcome` becomes the table's `outcome`, also `NOT NULL`: `Success`, `Failure`, `Blocked` or `Retried`.
- `Target` is the UI target the caller can name — the window, screen, control or command — and lands in
  `payload_json` under `ui`.
- `ExceptionDetail` and `ExceptionFingerprint` are built by the seam; no call site serializes an exception or
  hashes anything.

### 1.1 The exception chain (FR-032)

The `Error`, `Critical` and `Write` members take the exception and serialize its complete chain into
`exception_detail` as a JSON array, one node per exception, outermost first. Each node carries `level`,
`index`, `type`, `message`, `stackTrace`, `source`, `hresult`, `helpLink`, `targetSite` and `data`. An
`AggregateException` contributes one node for each of its independent failures at the next level; the chain is
not reduced to the first. `ToString()` is stored once as the `full` member of the same object, so a reader gets
the familiar representation and the store keeps the searchable parts separately.

Rules:

- A node's `data` is written through an allowlist. `Exception.Data` is open to any code, so it is never written
  as it stands.
- The serialized chain is capped. Truncation happens at a node boundary and sets `truncated: true`, so a short
  fault is never mistaken for a cut-off one.
- Serializing is defensive. A member that throws on read, or a `Data` value that refers to itself, is replaced
  with a marker rather than being allowed to lose the entry.
- The chain is held in one column, not in child rows. The entry's hash covers one row, and splitting it would
  break the chain the store exists to keep.

### 1.2 The fingerprint (FR-033, SC-014)

`error_fingerprint` is SHA-256, lowercase hex, over the fault's type, the module, the action and a normalized
shape of the message — digits, identifiers and quoted values replaced by placeholders — so the same fault hashes
the same on any machine and any run while a different fault does not. The seam computes it, because normalizing
the message needs the message in hand; the procedure stores and indexes it. It is `NULL` for an entry raised
without an exception.

### 1.3 The gathered context (FR-034, FR-035, FR-036)

The seam gathers these itself. A call site never supplies them, for the reason FR-021 gives: a call site that
has to remember them eventually will not.

| Where | What is gathered |
|---|---|
| `payload_json.runtime` | Application name, version and informational version; OS description and architecture; process architecture; whether the process and the OS are 64-bit; the runtime description; the Windows App SDK assembly version; whether the build is packaged (`RuntimeHelper.IsMSIX`); process id; thread id; application uptime in seconds; current culture and UI culture |
| `payload_json.ui` | Window, screen and control names; control type; event name; command name; whether the call was on the UI thread |
| `payload_json.database` | Provider name and version; error code; SQL state; the server's error number; the database and its configured alias; the operation; the stored procedure; the retry attempt; whether a transaction was active and at what isolation; the connection state; the configured command and connection timeouts; affected rows; elapsed milliseconds; and the statement's fingerprint, which contains no values |

- `event_action` carries the action, `correlation_id` the caller's correlation, and `actor_kind`/`actor_id` and
  `host_id`/`mac_address` the person and the machine.
- Nothing listed in §6 is ever gathered. `EnvironmentName` is not gathered at all: FR-025 leaves no local
  configuration to read it from, so a deployment environment would have to come from the store first.
- The UI members are gathered only where they can be read without touching the dispatcher during shutdown. A
  fault raised after the queue is gone must not raise a second one while trying to describe itself.

### 1.4 When recording itself fails (FR-037, SC-016)

The write path never throws to its caller and never re-enters itself.

- A full queue drops its oldest entry rather than blocking or growing.
- A store that refuses the write drops the entry; nothing is written to the machine in its place.
- The writer absorbs its own failures and records nothing about them, so one broken write cannot produce a fault
  that produces a fault.
- The level helpers return; they do not await the store. `FlushAsync` is the only awaiting member, and it is
  bounded.

## 2. Delivery behaviour

| Behaviour | Rule |
|---|---|
| Call cost | `Write` and the level helpers return without waiting on the store. A slow store never delays a caller (S13, "never waits") |
| Queue full | The oldest entry is dropped. The newest faults are the ones worth keeping |
| Flush on shutdown | Bounded. Queued entries are written before the process ends, within a stated maximum |
| Store unreachable | The entry is dropped. Nothing is written to the machine as a substitute record, and no local log file is produced (FR-025, SC-003) |
| Recorder fails | The failure is absorbed. No second diagnostic is raised, the original fault is what the caller sees, and the caller waits no longer than it would have (FR-037, SC-016) |
| Ordering | Entries are written in the order they were raised, so the hash chain reads as a history |

## 3. The store write

One procedure, `sp_ops_startup_logs_insert`, carries the chain. It reads `previous_hash`, computes
`entry_hash` over the entry plus that link, and inserts, all inside one transaction, so concurrent writers
cannot fork the chain. The application never computes or supplies the chain itself.

Columns written, matching the table's existing shape plus the four new fields: `public_id`, `correlation_id`,
`created_utc`, `level`, `event_action`, `outcome`, `actor_kind`, `actor_id`, `host_id`, `mac_address`,
`message`, `payload_json`, `previous_hash`, `entry_hash`, and the new `module`, `error_type`, `exception_detail`
and `error_fingerprint`.

## 4. The `ILogger` surface

The application already makes 226 `ILogger` call sites across 26 files — 219 `_logger.` sites plus 7 standalone
`logger.`/`Logger.` call sites — and 31 files import `Microsoft.Extensions.Logging`. That figure is tree-wide:
64 of the 226 sit in `MTM_Waitlist.Mock.Service`, a separate host with its own file logger that this provider
does not serve (58 of the 219 `_logger.` sites, and 6 of the 7 standalone sites), so the application's own
surface is 162 call sites. **No `ILogger` call site is edited.** One provider writes those calls to the
same store through the same `ILogService`.

```csharp
public sealed class StoreLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName);
    public void Dispose();
}
```

Rules:

- Registered in the generic host in one place.
- `categoryName` becomes the entry's `Module`, so the panel's module filter works for both surfaces without
  either surface supplying it.
- The structured properties `ILogger` already carries go into `payload_json` rather than being flattened into
  the message.
- The file-based provider is removed in the same phase, so no local log file is produced afterwards.

## 5. The static call-site migration

489 references of `StartupDebugLog` across 83 files (335 `Info`, 147 `Error`, 7 `Configure`) are repointed by
`tools/migrate-logging-callsites.ps1`, dry run first, `-Apply` second, then the 7 `Configure` calls by hand,
then the old type is deleted.

Rules:

- The script touches only the type token. Arguments, line structure and multi-line formatting are untouched.
- The script does not rewrite using directives. `MTM_Waitlist.Module_Core.Helpers` holds ten other types and is
  imported by 134 files, so the new type is made reachable with one global using.
- The one fully-qualified production call site is handled by consuming the qualifier rather than leaving it
  dangling in front of the new name.
- The deletion of `StartupDebugLog` happens only after the script reports zero remaining references, and the
  solution is built immediately afterwards to prove no call site was left behind.

## 6. Never logged

- Credentials, in any form, including temporary credentials and the one-time reset PIN.
- Session tokens, in plaintext or as a hash, and the salt.
- The remembered sign-in payload, its initialisation vector, or its plaintext.
- Key material. The shared key file's **path** is the only secret-adjacent value ever written, and that is
  deliberate so a support reader can see which key a decryption failure used.
- Connection strings with credentials in them.

## 7. The developer panel

Reads through the reader procedures with the filter set the store indexes: criticality, machine, user, module,
error type and a time window, plus grouping by `error_fingerprint` so a repeated fault reads as one signal with
a count rather than as a hundred rows.

Rules:

- Newest first.
- Every query is bounded by a time window and a page size, so a large store cannot freeze the screen.
- The entry card shows the full message, the exception detail and the chain link.
- Gated by `permission.settings.log_panel`, enforced in the view model rather than only hidden.
- Held by `Developer` alone, which is what US5 and SC-005 describe.

### 7.1 The panel's copy (FR-038, SC-017, D29)

The panel's only export is a copy to the clipboard. It writes no file, and there is no save-to-file affordance.

Two affordances, one formatter:

- The entry card copies that one entry.
- The panel header copies the entries currently listed, which the query has already bounded by its page size.

The text is exactly what the store holds for those entries — the columns in the panel's order, the exception chain
and the payload as they were stored, the chain link included — with an entry separator and a header naming the
filter the copy came from. The formatter re-formats nothing and redacts nothing beyond what §6 already forbade the
store to hold, because the copy has to be the evidence rather than a summary of it. When the text would exceed the
stated ceiling it is cut with an explicit marker, never silently. **The ceiling's value is not yet fixed** — T183
records it here as it writes the formatter, so the cut is verifiable against a number rather than a phrase.

Rules for the clipboard call itself:

- `Clipboard.SetContentWithOptions` with `IsAllowedInHistory` and `IsRoamable` both `false`. Both default to
  `true`, and a diagnosis names a machine and a person.
- A `false` return is reported to the reader as a refusal. That overload returns a boolean where
  `Clipboard.SetContent` throws when the process is not in the foreground, and a panel must not raise a fault
  while reporting one.
- Copying is a read: it is not itself recorded, and it changes no row.

Grounded in `Clipboard.SetContentWithOptions` (Windows 10 1809, UniversalApiContract v7),
`ClipboardContentOptions.IsAllowedInHistory` and `IsRoamable`, and the remarks on `Clipboard.SetContent`.
