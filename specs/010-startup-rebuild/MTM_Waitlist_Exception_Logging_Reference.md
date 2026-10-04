# WinUI 3 Exception Logging & MySQL Diagnostics
## Implementation reference for MTM_Waitlist

**Target repository:** [MTM_Waitlist — branch `010-startup-rebuild`](https://github.com/JDKoll1982/MTM_Waitlist/tree/010-startup-rebuild)

**Purpose:** Reference specification for adding structured exception detection, diagnostic capture, and MySQL persistence to the WinUI 3 application.

> **Verified against the repository on 2026-09-26 (branch `010-startup-rebuild`).** The GitHub URL could not be fetched during preparation, so this document was drafted generically. It has since been reconciled with the checkout, and every name, type and store below is now mapped to what the solution actually has. What that means in practice:
>
> - **There is no new error store and no new service.** The store already exists: `ops_startup_logs` in `mtm_waitlist`, which this feature extends with `module`, `error_type` and `exception_detail`. The seam already exists: `ILogService` and `LogEntry` in the `MTM_Waitlist.Logging` project, with `StoreLoggerProvider` for `ILogger` call sites. This document adds fields to those, not alternatives to them.
> - **Every write goes through a stored procedure.** Inline SQL fails the build (`InlineSqlAuditTests`), so persistence is `sp_ops_startup_logs_insert` for the write and `sp_ops_startup_logs_filter` for the panel's reads, never a statement issued from C#.
> - **Nothing is written to the machine.** There is no durable local queue and no local log file. A diagnostic raised while the store is unreachable is dropped, and the application keeps no substitute record (FR-025, SC-003). The queue is in memory and bounded.
> - **The target store is MySQL 5.7**, so JSON is held in `MEDIUMTEXT` columns, timestamps are `DATETIME` in UTC, and identifiers are `BIGINT` without `UNSIGNED`.
> - **Identifiers are snake_case** with the repository's existing prefixes (`ops_`, `auth_`, `config_`, `core_`, `setup_`, `waitlist_`). The `app_error_*` names and CamelCase columns in the generic draft are superseded by the mapping in §8.
> - **Triage — assignment, resolution and fixed-version tracking — is not wanted at all.** Decided 2026-09-26: the panel's copy (§7.1) hands the fault to whoever is fixing it, so the store never needs to hold an assignment. §7 keeps those fields only as a record of what was considered.

---

## 1. Design goals

The error logging system should:

- Capture enough information to diagnose an issue without reproducing it immediately.
- Preserve the original exception and its complete inner-exception chain.
- Record app version, machine/runtime context, and the operation underway.
- Capture WinUI page/control/event context when the caller can provide it.
- Capture safe MySQL provider diagnostics when the exception comes from database work.
- Avoid blocking the UI thread or masking the original exception if logging fails.
- Queue writes in memory and bound them, so a stalled store never delays a caller and nothing is written to the machine as a substitute record.
- Preserve the entry's place in the store's tamper-evident hash chain, which the insert procedure computes rather than the application.
- Support grouping by a stable fingerprint, so repeated occurrences of one fault collapse into one signal.
- Let a developer take one fault out of the panel as text, so it can be handed to a reader who has no access to the store, without writing anything to the machine.
- *(Later feature, not this one.)* Support triage, assignment, resolution, and fixed-version tracking.

## 2. Base .NET exception properties

Capture these from every `System.Exception`. Most values should be nullable because not every exception supplies them.

| # | Property / field | .NET source / type | What to store and why |
|---:|---|---|---|
| 1 | `ExceptionType` | `exception.GetType().FullName` (`string`) | Fully qualified exception type, e.g. `System.NullReferenceException`. |
| 2 | `Message` | `exception.Message` (`string`) | Human-readable exception description. |
| 3 | `StackTrace` | `exception.StackTrace` (`string`) | Call stack at the throw site, including source lines when symbols are available. |
| 4 | `Source` | `exception.Source` (`string`) | Component or assembly that raised the exception, if supplied. |
| 5 | `HResult` | `exception.HResult` (`int`) | HRESULT useful for Windows, COM, and .NET diagnostics. |
| 6 | `HelpLink` | `exception.HelpLink` (`string`) | Associated help link, if any. |
| 7 | `TargetSite` | `exception.TargetSite` (`MethodBase`) | Capture declaring type, method name, and method signature as strings. |
| 8 | `Data` | `exception.Data` (`IDictionary`) | Custom key/value diagnostic data attached to the exception. Sanitize and serialize defensively. |
| 9 | `FullException` | `exception.ToString()` (`string`) | Supplemental full representation, commonly including type, message, stack, and inner exception details. |
| 10 | `InnerException` | `exception.InnerException` (`Exception?`) | Recursively capture the underlying exception; do not merely save its message. |

**Important:** `ToString()` is supplemental. Store type, message, stack, and inner exception data in separate fields for searching and reporting.

Where these land here: `ExceptionType`→`error_type`; `Message`→`message`, the `TEXT` column the entry already has; `StackTrace`, `Source`, `HResult`, `HelpLink`, `TargetSite`, `Data` and `FullException`→the `exception_detail` JSON described in §3; `InnerException`→the chain in that same JSON. `Data` is the one that needs care: any code can put anything in it, so it is written through an allowlist and never as-is.

## 3. Inner and aggregate exceptions

Capture one node for each exception in the chain, as a JSON array inside the entry's `exception_detail` column — not as child rows in a second table. The log store *is* the tamper-evident chain and its hash is computed over one row, so spreading an entry across tables would break the invariant the store exists to protect. Handle `AggregateException.InnerExceptions` as a collection; it can contain multiple independent failures.

The field names below are JSON keys inside `exception_detail`, not table columns.

| Field | Description |
|---|---|
| `ExceptionLevel` | Nesting depth; 0 is the top-level exception. |
| `ExceptionIndex` | Index within a parent's child exception list. |
| `ExceptionType` | Fully qualified type name. |
| `ExceptionMessage` | Message for this exception node. |
| `ExceptionStackTrace` | Stack trace for this exception node. |
| `ExceptionHResult` | HRESULT for this exception node. |
| `ExceptionSource` | Source property. |
| `ExceptionTargetSite` | Method/type/signature that threw it. |
| `ExceptionData` | Sanitized custom key/value data. |
| `ParentExceptionID` | Optional parent node's index within the same entry; a self-reference, not a foreign key. |
| `IsRootCause` | Whether this is a leaf exception node; useful but not proof of causal root. |

Do not assume the deepest exception is always the true root cause. Preserve the full chain and let the developer interpret it.

## 4. Application and machine context

Capture once per error occurrence, by the logging seam rather than by the call site. The right-hand column says where each value lands: an `ops_startup_logs` column that already exists, or a key inside `payload_json`. The seam already gathers the machine and the person from `IMachineFacts` and `IPersonIdentity`, which is what keeps a call site from having to remember them (FR-021).

| # | Database field | .NET source / value | Purpose |
|---:|---|---|---|
| 1 | `ApplicationName` | Entry assembly name or configured app name | Identifies the app. |
| 2 | `ApplicationVersion` | Entry assembly version | Identifies deployed version. |
| 3 | `ApplicationBuild` | The entry assembly's informational version | Distinguishes builds when the assembly version is unchanged. |
| 4 | `EnvironmentName` | **Dropped here.** FR-025 leaves only the connection strings and `MockServiceClient` on the machine, so there is no deployment-environment setting to read. Add it to the store if it is ever needed | n/a |
| 5 | `MachineName` | `Environment.MachineName`, written to `host_id` | Computer where the failure occurred. |
| 6 | `UserName` | The signed-in person from `IPersonIdentity`, not the Windows username | Associates the error with an account. Written to `actor_kind`/`actor_id` using the internal user id, never the display name. |
| 7 | `OperatingSystem` | Windows version/build from supported OS APIs | Helps isolate OS-specific failures. |
| 8 | `OSArchitecture` | `RuntimeInformation.OSArchitecture` | OS architecture. |
| 9 | `ProcessArchitecture` | `RuntimeInformation.ProcessArchitecture` | Architecture of running process. |
| 10 | `DotNetVersion` | `RuntimeInformation.FrameworkDescription` | Runtime/framework description. |
| 11 | `WindowsAppSdkVersion` | The Windows App SDK assembly version, read from the loaded assembly | Useful for WinUI/Windows App SDK issues. |
| 12 | `DeploymentType` | `RuntimeHelper.IsMSIX` — the existing helper, which stays | Helps diagnose deployment and activation problems. |
| 13 | `AppUptimeSeconds` | Current UTC minus app startup UTC | Identifies errors after long sessions. |
| 14 | `ProcessId` | `Environment.ProcessId` | Correlates events from a process. |
| 15 | `ThreadId` | `Environment.CurrentManagedThreadId` | Helps diagnose concurrency issues. |
| 16 | `Culture` | `CultureInfo.CurrentCulture.Name` | Locale-sensitive formatting context. |
| 17 | `UICulture` | `CultureInfo.CurrentUICulture.Name` | UI language context. |
| 18 | `Is64BitProcess` | `Environment.Is64BitProcess` | Process bitness. |
| 19 | `Is64BitOperatingSystem` | `Environment.Is64BitOperatingSystem` | OS bitness. |

Collect only information needed for support. Machine and user identifiers can be sensitive; restrict access and retention.

## 5. WinUI 3 and operation context

These values generally do not come from `Exception`. Supply them from the page, event handler, command, or service that catches the exception.

| # | Database field | Value to capture | Why |
|---:|---|---|---|
| 1 | `WindowName` | Active window identifier/type | Identifies the window involved. |
| 2 | `PageName` | Page/view class name | Identifies the UI surface. |
| 3 | `ControlName` | XAML control `Name`, if known | Identifies the control involved. |
| 4 | `ControlType` | Button, TextBox, Grid, etc. | Identifies control type. |
| 5 | `EventName` | Click, Loaded, SelectionChanged, etc. | Identifies event handler context. |
| 6 | `CommandName` | ICommand or command action name | Useful for MVVM commands. |
| 7 | `NavigationSource` | Current/previous route or page | Helps diagnose navigation. |
| 8 | `NavigationTarget` | Requested destination | Helps diagnose failed navigation. |
| 9 | `IsUIThread` | Whether the operation is on the UI thread | Diagnoses thread-affinity problems. |
| 10 | `DispatcherQueueAvailable` | Dispatcher availability, if safely obtainable | Helps diagnose shutdown/dispatch issues. |
| 11 | `XamlElementName` | Relevant named element | Helps with XAML/binding errors. |
| 12 | `OperationName` | Stable app operation, e.g. `SubmitWaitlistEntry` | Human-readable action being performed. |
| 13 | `CorrelationID` | GUID for a user action/workflow | Connects related logs across layers. |

Example context:

```text
OperationName: SubmitWaitlistEntry
PageName: WaitlistEntryPage
ControlName: SaveButton
EventName: SaveButton_Click
CorrelationID: <guid>
```

Do not attempt to infer a control or page from the stack trace when the caller can provide the context explicitly.

Where these land: `OperationName` is the entry's `event_action`, which is `NOT NULL`, so every entry carries an action; `CorrelationID` is the entry's `correlation_id`; everything else goes into `payload_json` as a `ui` object. `IsUIThread` and `DispatcherQueueAvailable` are worth capturing only where they can be read without touching the dispatcher during shutdown — a fault raised while the queue is gone must not raise a second one.

## 6. MySQL and data-access diagnostics

Provider-specific properties vary. Inspect the actual exception type and capture provider fields only when available. Examples below are common to MySqlConnector and/or MySQL Connector/NET; verify the installed provider's API.

| # | Database field | Value / source | Purpose |
|---:|---|---|---|
| 1 | `DatabaseProvider` | `MySqlConnector` or `MySql.Data` | Identifies client library. |
| 2 | `DatabaseProviderVersion` | Provider assembly version | Helps identify provider-specific behavior. |
| 3 | `MySqlErrorCode` | Provider-specific `ErrorCode`, when available | Numeric provider/server error category. |
| 4 | `SqlState` | `MySqlException.SqlState`, when available | SQLSTATE diagnostic category. |
| 5 | `Number` | `MySqlException.Number`, if provider exposes it | MySQL server error number. |
| 6 | `DatabaseName` | Configured schema name | Identifies affected database. |
| 7 | `DatabaseServer` | Safe server alias/hostname, subject to policy | Identifies target without credentials. |
| 8 | `DatabaseOperation` | SELECT, INSERT, UPDATE, DELETE, etc. | Operation type. |
| 9 | `StoredProcedureName` | Procedure name, if applicable | Pinpoints stored procedure. |
| 10 | `QueryFingerprint` | Hash/normalized SQL template identifier | Groups similar query failures without storing secrets. |
| 11 | `TransactionActive` | Whether a transaction was active | Diagnoses transaction failures. |
| 12 | `TransactionIsolationLevel` | Isolation level, if known | Helps diagnose locking/concurrency. |
| 13 | `ConnectionState` | Open, Closed, Connecting, etc. | Connection state at failure. |
| 14 | `ConnectionTimeoutSeconds` | Configured timeout | Diagnoses connection timeouts. |
| 15 | `CommandTimeoutSeconds` | Configured command timeout | Diagnoses query timeouts. |
| 16 | `RetryAttempt` | Retry number | Shows transient failure/retry behavior. |
| 17 | `AffectedRows` | Provider result, if available | Helps diagnose unexpected row counts. |
| 18 | `DatabaseOperationDurationMs` | Elapsed operation duration | Identifies slow operations/timeouts. |

Where these land: the provider code set, `DatabaseName`, `DatabaseOperation`, `StoredProcedureName`, `ConnectionState`, `RetryAttempt` and `DatabaseOperationDurationMs` go into `payload_json` as a `database` object; `QueryFingerprint` is additionally promoted to the entry's `error_fingerprint`, because that is what groups repeated store failures. `DatabaseServer` records the configured alias only, never the connection string.

**Security:** Never log a full connection string, password, authentication token, or unredacted SQL parameters. Prefer normalized SQL templates or fingerprints. If parameter diagnostics are necessary, use an explicit allowlist and redact sensitive values. Concretely: parameter *names* and their *count* are safe, parameter *values* are not, and a provider message can echo a value back, so the stored message is truncated at the first `=` that follows a parameter name. The authoritative list of what may never be written is `contracts/logging-contract.md` §6.

## 7. Occurrence tracking, triage, and resolution

| # | Database field | Suggested MySQL type | Purpose |
|---:|---|---|---|
| 1 | `ErrorID` | `BIGINT UNSIGNED` | Primary key for one occurrence. |
| 2 | `ErrorGuid` | `CHAR(36)` | Globally unique occurrence ID. |
| 3 | `OccurredAtUtc` | `DATETIME(3)` | UTC time error occurred. |
| 4 | `LoggedAtUtc` | `DATETIME(3)` | UTC time persisted to database. |
| 5 | `Severity` | `VARCHAR(20)` | Error, Critical, Warning, etc. |
| 6 | `Category` | `VARCHAR(50)` | UI, Database, Network, Validation, Startup, etc. |
| 7 | `ErrorFingerprint` | `CHAR(64)` | Stable hash to group similar occurrences. |
| 8 | `CorrelationID` | `CHAR(36)` | Links related operations/log events. |
| 9 | `SessionID` | `CHAR(36)` | App session identifier. |
| 10 | `ParentErrorID` | `BIGINT UNSIGNED NULL` | Links a secondary logging failure or related error. |
| 11 | `IsHandled` | `TINYINT(1)` | Whether application handled the exception. |
| 12 | `WasUserNotified` | `TINYINT(1)` | Whether user saw an error notification. |
| 13 | `UserDescription` | `TEXT` | Optional user-provided description; sanitize. |
| 14 | `ResolutionStatus` | `VARCHAR(30)` | New, Investigating, Fixed, Won't Fix, etc. |
| 15 | `AssignedTo` | `VARCHAR(100)` | Responsible developer/support person. |
| 16 | `ResolutionNotes` | `TEXT` | Root cause and remediation notes. |
| 17 | `ResolvedAtUtc` | `DATETIME(3)` | Resolution timestamp. |
| 18 | `FixedInVersion` | `VARCHAR(50)` | Release containing the fix. |

Keep each occurrence immutable where possible. Track changing status/assignment in a resolution or audit table rather than overwriting the original diagnostic evidence.

**Which of these exist today.** Fields 1–13 map onto `ops_startup_logs` as it already is and as this feature extends it: `ErrorID`→`id`, `ErrorGuid`→`public_id`, `OccurredAtUtc`→`created_utc`, `Severity`→`level`, `ErrorFingerprint`→the new `error_fingerprint`, `CorrelationID`→`correlation_id`, `SessionID`→a `payload_json` key, `IsHandled` and `WasUserNotified`→`payload_json` keys, and `UserDescription`→`payload_json`. `Category` is derived from `module` rather than stored twice. `LoggedAtUtc` is dropped: the chain timestamps an entry once, and a second timestamp would need a column no reader consults.

Fields 14–18 — `ResolutionStatus`, `AssignedTo`, `ResolutionNotes`, `ResolvedAtUtc`, `FixedInVersion` — are **not wanted**. They describe a triage workflow the specification does not ask for, and the owner's support path does not need one: a developer copies the entry out of the panel (§7.1) and hands it to whoever is fixing it. They are kept here only as a record of what was considered.

### 7.1 Getting one fault out of the panel

The panel's export is a copy to the clipboard — the entry card copies one entry, the header copies the view on screen. It writes no file, and there is no save-to-file button: a saved diagnostic is application behaviour kept on the machine, which FR-025 removes, so a file export would need its own decision rather than arriving as a convenience.

The text is exactly what the store holds for those entries — the columns in the panel's order, the exception chain and the payload as stored, the chain link included — with a separator between entries and a header naming the filter the copy came from. It is cut with an explicit marker at a stated ceiling. Nothing is re-formatted and nothing is redacted further, because §6 has already kept the forbidden values out of the store.

Two clipboard rules, both from the platform documentation:

- Use `Clipboard.SetContentWithOptions`, not `SetContent`. The options overload returns a boolean, where `SetContent` throws when the process is not in the foreground — and a panel must not raise a fault while reporting one.
- Set `IsAllowedInHistory` and `IsRoamable` to `false`. Both default to `true`, and a diagnosis names a machine and a person. Microsoft's clipboard guidance says the clipboard should not be used to transfer sensitive data.

## 8. Suggested relational schema

The generic draft proposed five new tables. **This repository uses one.** The block below is kept only to show what was proposed, so a reader can see what was dropped and why; the authoritative shape follows it.

```text
app_error_logs
  ErrorID (PK)
  ErrorGuid
  OccurredAtUtc
  LoggedAtUtc
  Severity
  Category
  ErrorFingerprint
  CorrelationID
  SessionID
  ApplicationVersion
  MachineName
  UserID (if approved)
  OperationName
  ResolutionStatus

app_error_exceptions
  ExceptionID (PK)
  ErrorID (FK)
  ParentExceptionID (nullable FK)
  ExceptionLevel
  ExceptionIndex
  ExceptionType
  ExceptionMessage
  StackTrace
  Source
  HResult
  HelpLink
  TargetSite
  ExceptionData (JSON/text)

app_error_context
  ErrorID (PK/FK)
  EnvironmentData (JSON/text)
  UIContext (JSON/text)
  FullException (LONGTEXT)

app_error_database
  ErrorID (PK/FK)
  DatabaseProvider
  DatabaseProviderVersion
  SqlState
  MySqlErrorCode
  DatabaseName
  DatabaseServer
  DatabaseOperation
  StoredProcedureName
  QueryFingerprint
  ConnectionState
  RetryAttempt
  DatabaseOperationDurationMs
  SafeDiagnosticData (JSON/text)

app_error_resolutions
  ResolutionID (PK)
  ErrorID (FK)
  ChangedAtUtc
  ChangedBy
  ResolutionStatus
  AssignedTo
  ResolutionNotes
  FixedInVersion
```

**What this feature actually uses.** One table, `ops_startup_logs`, already present at `Database/Tables/10_ops_startup_logs/` with paired `create.sql` and `rollback.sql`. It already carries `id`, `public_id`, `correlation_id`, `created_utc`, `level`, `event_action`, `outcome`, `actor_kind`, `actor_id`, `host_id`, `mac_address`, `message`, `payload_json`, `previous_hash` and `entry_hash`. This feature adds exactly four things:

| Addition | Type | Purpose |
|---|---|---|
| `module` | `VARCHAR(64) NULL` | The area the entry came from; backs the panel's module filter |
| `error_type` | `VARCHAR(128) NULL` | The exception's fully qualified type name, so similar faults group |
| `exception_detail` | `MEDIUMTEXT NULL` | The exception chain and `ToString()`, as the JSON in §3 |
| `error_fingerprint` | `CHAR(64) NULL` | The stable SHA-256 over the type, the message shape, the module and the throw site |

Plus the indexes the panel's filter set needs and one procedure for the write. `app_error_exceptions`, `app_error_context` and `app_error_database` are **not created** — they are columns and JSON on the one entry. `app_error_resolutions` is **not created** — it is the deferred triage workflow.

**Naming and types in this repository.** snake_case identifiers with the existing `ops_` prefix; `BIGINT`, not `UNSIGNED`; `CHAR(36)` for a generated identifier; `DATETIME` in UTC, not `DATETIME(3)`; `MEDIUMTEXT` holding JSON, because MySQL 5.7 is the target and the table already stores JSON that way; `VARCHAR(16)` for `level`, matching the `LogSeverity` vocabulary. One artifact per folder with `create.sql` and `rollback.sql`, both registered in the aggregates.

### Suggested types

| Data | This repository's type |
|---|---|
| Primary ID | `BIGINT NOT NULL AUTO_INCREMENT` |
| Public identifier | `CHAR(36)` |
| Exception type (`error_type`) | `VARCHAR(128)` |
| Message | `TEXT` |
| Exception chain and full representation (`exception_detail`) | `MEDIUMTEXT` |
| Structured context (`payload_json`) | `MEDIUMTEXT` containing JSON |
| Timestamp (`created_utc`) | `DATETIME`, stored in UTC |
| Fingerprint (`error_fingerprint`) | `CHAR(64)`, SHA-256 hex |
| Severity (`level`) | `VARCHAR(16)` |
| Outcome (`outcome`) | `VARCHAR(32)` |

The store here is **MySQL 5.7**, and the table already holds JSON in `MEDIUMTEXT` rather than the native `JSON` type. Keep that: adopting the native type would give the store two ways to hold the same thing, and `payload_json` is the established one. `LONGTEXT` is not used; `MEDIUMTEXT` is the repository's ceiling for a payload.

## 9. C# capture example

This is a starting point for capturing the exception chain and common context. It is not a complete production serializer or database service. In production, handle aggregate exceptions recursively, cap payload sizes, sanitize exception data, and ensure serialization itself cannot hide the original exception.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

public static class ExceptionCapture
{
    public static string Capture(
        Exception exception,
        string? operationName = null,
        string? pageName = null,
        string? controlName = null,
        string? eventName = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var exceptionNodes = new List<object>();
        var pending = new Stack<(Exception Exception, int Level, int Index)>();
        pending.Push((exception, 0, 0));

        while (pending.Count > 0)
        {
            var (current, level, index) = pending.Pop();

            var data = new Dictionary<string, string?>();
            foreach (DictionaryEntry item in current.Data)
            {
                string key = item.Key?.ToString() ?? "";
                // Apply an allowlist/redaction policy here.
                data[key] = item.Value?.ToString();
            }

            exceptionNodes.Add(new
            {
                ExceptionLevel = level,
                ExceptionIndex = index,
                ExceptionType = current.GetType().FullName,
                Message = current.Message,
                StackTrace = current.StackTrace,
                Source = current.Source,
                HResult = current.HResult,
                HelpLink = current.HelpLink,
                TargetSite = current.TargetSite?.ToString(),
                Data = data
            });

            if (current is AggregateException aggregate)
            {
                for (int i = aggregate.InnerExceptions.Count - 1; i >= 0; i--)
                    pending.Push((aggregate.InnerExceptions[i], level + 1, i));
            }
            else if (current.InnerException is not null)
            {
                pending.Push((current.InnerException, level + 1, 0));
            }
        }

        var entryAssembly = Assembly.GetEntryAssembly();

        var payload = new
        {
            ErrorGuid = Guid.NewGuid().ToString(),
            OccurredAtUtc = DateTime.UtcNow,
            ApplicationName = entryAssembly?.GetName().Name,
            ApplicationVersion = entryAssembly?.GetName().Version?.ToString(),
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
            DotNetVersion = RuntimeInformation.FrameworkDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            OperationName = operationName,
            PageName = pageName,
            ControlName = controlName,
            EventName = eventName,
            FullException = exception.ToString(),
            Exceptions = exceptionNodes
        };

        return JsonSerializer.Serialize(payload);
    }
}
```

Example WinUI 3 event-handler use:

```csharp
private async void SaveButton_Click(
    object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    try
    {
        await SaveWaitlistEntryAsync();
    }
    catch (Exception ex)
    {
        // The call site supplies what only it knows: the module. The seam adds the
        // machine, the person, the exception chain, the runtime context and the
        // chain link, because a call site that has to remember them eventually will
        // not (FR-021). Capture therefore lives inside MTM_Waitlist.Logging, not here.
        ErrorLogService.Error(
            module: "Waitlist",
            message: "Saving the waitlist entry failed.",
            exception: ex);

        // The user-facing text comes from the .resw resources, never from the
        // exception, and the logging call cannot suppress or replace the fault.
        await ShowLocalisedFailureAsync();
    }
}
```

Replace example page, operation, and service names with the actual symbols in the target repository. Do not introduce these names blindly. The `ExceptionCapture` helper earlier in this section is the *shape* of the capture the seam performs; in this repository it is an internal part of `MTM_Waitlist.Logging` and no call site serializes anything.

## 10. Production implementation requirements

- [ ] Extend the existing seam in `MTM_Waitlist.Logging`; do not create a second error-logging service.
- [ ] Add global handlers for the unhandled paths the app has — `App.UnhandledException` is already registered — while retaining local `try/catch` around recoverable operations.
- [ ] In WinUI 3, account for UI-thread and non-UI-thread exception paths; global handlers are not a substitute for local handling.
- [ ] Keep error logging from recursively triggering itself. If persistence fails, drop the entry and absorb the failure; never raise a second diagnostic from the logging path, and never fall back to a local file.
- [ ] Queue writes in memory, off the UI thread, bounded, dropping the oldest entry when full, with a bounded flush on shutdown.
- [ ] Route every write through `sp_ops_startup_logs_insert`; never issue a statement from C# — `InlineSqlAuditTests` fails the build otherwise.
- [ ] Apply payload size limits and truncate only with an explicit truncation indicator.
- [ ] Redact credentials, tokens, personal data, connection strings and SQL parameter values, per `contracts/logging-contract.md` §6.
- [ ] Use access controls and retention limits for diagnostic data: retention is a procedure bounded by age and by size, and the panel is gated on `permission.settings.log_panel`.
- [ ] Index for the panel's actual query patterns — `level`, `host_id`, `actor_id`, `module` and `error_type`, each paired with `created_utc`, plus `error_fingerprint` — and not for fields nothing filters on.
- [ ] Test a store outage, malformed exception data, nested and aggregate exceptions, queue saturation, and a failing logging path.
- [ ] Verify that the logging path never suppresses or replaces the original exception.
- [ ] Keep user-facing failure text in `.resw` resources; never show an operator an exception message.

## 11. Implementation priorities

| Priority | Scope | Reason |
|---|---|---|
| P0 | Type, message, stack, inner exceptions, UTC timestamp, app version, fingerprint | Essential for identifying and grouping bugs. |
| P0 | Machine/runtime context and operation/page/control context | Explains where and during what action the error occurred. |
| P0 | Safe asynchronous persistence, queued in memory and bounded, with nothing written locally when the store is unreachable | Prevents logging from freezing the UI without putting a substitute record on the machine (FR-025, SC-003) |
| P0 | The entry's place in the tamper-evident hash chain, computed by the insert procedure | FR-021; the chain is the store's whole reason for existing |
| P1 | MySQL SQLSTATE, provider code, stored procedure, query fingerprint, duration, retry attempt | Diagnoses database-specific failures |
| Not wanted | Resolution status, assignment, notes, fixed version | Decided 2026-09-26: the panel's copy hands the fault to a reader instead (D29) |
| P2 | Dispatcher state, architecture, uptime, retry/transaction context | Helps investigate intermittent or environment-specific problems. |

## 12. Recommended first milestone

For the first implementation pass in `MTM_Waitlist`, the work is:

1. Extend `LogEntry` and `ILogService` in `MTM_Waitlist.Logging` with the capture described in §2 to §6, so the seam builds the exception chain and the context rather than the call site.
2. Add `error_fingerprint` to `ops_startup_logs` and extend `sp_ops_startup_logs_insert` to compute it alongside the existing chain.
3. Fold the database diagnostics into `payload_json` from the provider's exception, with the redaction in §6.
4. Keep the write queue in memory, bounded and off the UI thread, and confirm no local file is produced.
5. Integrate at `App.UnhandledException`, at the localisable failure paths, and in the store-touching services, without adding per-call-site context the seam can gather itself.
6. Add tests for nested and aggregate exceptions, redaction, a store outage, queue saturation, and a logging failure that must not suppress the original fault.
7. Give the panel a copy: one entry, or the view on screen, as clipboard text that writes no file and is excluded from clipboard history and device syncing (D29, FR-038).

**This repository's integration points** — verified 2026-09-26: `MTM_Waitlist.Logging` (`ILogService`, `LogEntry`, `StoreLogWriter`, `StoreLoggerProvider`), `App.xaml.cs` (where the `UnhandledException` handler is registered), `Database/Tables/10_ops_startup_logs/`, `Database/StoredProcedures/` (`sp_ops_startup_logs_insert`, `sp_ops_startup_logs_filter`, `sp_ops_startup_logs_purge`), `Module_Settings/Views/DeveloperLogPanelView.xaml` and `MTM_Waitlist.Settings/ViewModels/DeveloperLogPanelViewModel.cs`, with the copy's text built by `MTM_Waitlist.Logging/LogExportFormatter.cs`. The provider is **MySqlConnector**.

---

**Bottom line:** Give every error occurrence its own `public_id` and a stable `error_fingerprint` for grouping. Preserve the full exception chain in `exception_detail`, put the action in `event_action` and the rest of the context in `payload_json`, and make the logger safe when MySQL itself is unavailable — by dropping the entry, never by writing it to the machine. Then let the panel copy an entry, or the view, as text, so the fault can be handed to a reader without giving them the store.
