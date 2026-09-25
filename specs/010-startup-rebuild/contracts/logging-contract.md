# Contract: the logging seam

Source: brief S9, S9.1 to S9.4. Requirements: FR-021, SC-003. The table is `ops_startup_logs`, pinned verbatim.

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
    string? ErrorType,
    string? ExceptionDetail,
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

## 2. Delivery behaviour

| Behaviour | Rule |
|---|---|
| Call cost | `Write` and the level helpers return without waiting on the store. A slow store never delays a caller (S13, "never waits") |
| Queue full | The oldest entry is dropped. The newest faults are the ones worth keeping |
| Flush on shutdown | Bounded. Queued entries are written before the process ends, within a stated maximum |
| Store unreachable | The entry is dropped. Nothing is written to the machine as a substitute record, and no local log file is produced |
| Ordering | Entries are written in the order they were raised, so the hash chain reads as a history |

## 3. The store write

One procedure, `sp_ops_startup_logs_insert`, carries the chain. It reads `previous_hash`, computes
`entry_hash` over the entry plus that link, and inserts, all inside one transaction, so concurrent writers
cannot fork the chain. The application never computes or supplies the chain itself.

Columns written, matching the table's existing shape plus the three new fields: `public_id`, `correlation_id`,
`created_utc`, `level`, `event_action`, `outcome`, `actor_kind`, `actor_id`, `host_id`, `mac_address`,
`message`, `payload_json`, `previous_hash`, `entry_hash`, and the new `module`, `error_type` and
`exception_detail`.

## 4. The `ILogger` surface

The application already makes 226 `ILogger<T>` calls in 26 files, and 31 files import
`Microsoft.Extensions.Logging`. **No `ILogger` call site is edited.** One provider writes those calls to the
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
error type and a time window.

Rules:

- Newest first.
- Every query is bounded by a time window and a page size, so a large store cannot freeze the screen.
- The entry card shows the full message, the exception detail and the chain link.
- Gated by `permission.settings.log_panel`, enforced in the view model rather than only hidden.
- Held by `Developer` alone, which is what US5 and SC-005 describe.
