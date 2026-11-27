# Contract: the data-access seam

Owners: MOD-003 (narrow the seam), MOD-002 (adopt it), MOD-005 (one Visual read path).
Carried by: W18, W19, W20, and the Phase 9 addition T123.

Consumers of this contract are the five services that adopt the seam, the launch steps that call it, and
the two guardrail tests that read its source. The member names below are the interface; do not rename them.

## Public surface after W18

`MTM_Waitlist.Core/Services/MySqlHelperServer.cs`, implementing `IMySqlHelperServer`.

| Member | Before | After |
|---|---|---|
| `ExecuteStoredProcedureQueryAsync` | present | unchanged |
| `ExecuteStoredProcedureNonQueryAsync` | present | unchanged |
| `ExecuteSqlQueryAsync` | present | **removed** |
| `ExecuteSqlNonQueryAsync` | present | **removed** |
| `CreateTextCommand` | private helper | **removed** |
| `CreateStoredProcedureCommand` | private helper | unchanged |
| `ReadRowsAsync` | unbounded | bounded by a shared row cap |

The rule the contract states, and the one a caller may rely on: every operation this seam performs is a
stored-procedure call. There is no overload, flag or parameter that executes caller-supplied statement text.

## What a caller supplies

- A procedure name, non-blank. A blank name is refused before a connection is opened.
- A `MySqlDatabaseTarget`, which selects the store.
- A parameter dictionary whose keys may be written with or without a leading `@`; the seam normalises them.
- A `CancellationToken`. Caller cancellation propagates, is never retried, and is never recorded as an outage.

## What a caller expects

- An unconfigured store returns the neutral result and records no availability state. This is deliberate and
  is unchanged by MOD-003: a store that was never configured is not an unavailable store.
- An exhausted retry budget records unavailability and returns the neutral result, with the store named in
  the recorded reason.
- A failure to record availability never changes the read's outcome. Availability is a side channel.

## The strict route, added by T123

Four call sites cannot use the two lenient members above: they have to read the provider's own answer rather
than a neutral one, and one of them has to carry two calls as a single unit. Adding that inside each caller
would be a second data-access path; adding it here keeps one seam.

| Member | Purpose |
|---|---|
| `ExecuteStrictAsync<T>(MySqlStrictScopeRequest, Func<IMySqlStrictScope, CancellationToken, Task<T>>, CancellationToken)` | Opens one connection, hands the operation a scope bound to it, and commits or rolls back as one unit when the request asks for a transaction. |
| `MySqlStrictScopeRequest(Target, UseTransaction, ConnectionTimeoutSeconds, CommandTimeoutSeconds)` | Which store; whether the calls are one unit; and the connect and statement timeouts the caller already applied to its own connection. |
| `IMySqlStrictScope.QueryAsync / NonQueryAsync / ScalarAsync` | Stored-procedure calls only — a read, a write's affected rows, and a first-column-of-first-row value. |
| `MySqlStoreNotConfiguredException : InvalidOperationException` | The one condition the lenient route neutralises and the strict route cannot. |

The strict route's guarantees, each of which a caller may rely on:

- **Provider exceptions propagate unchanged.** A `MySqlException` — a duplicate key, a `SIGNAL SQLSTATE '45000'`
  refusal, error 1146 or 1305, a dropped connection — reaches the caller as raised, so each caller keeps its own
  number-based handling and none of it has to be re-derived from a neutral answer.
- **Nothing is retried** — not the statements, and not the connection open. A retried write is how one press
  becomes two rows, and a retried transaction would replay half of a unit that had already partly applied.
- **Nothing is recorded as an outage.** Availability is the lenient route's side channel, and it is what a
  screen reads to show its own `Unavailable` state. A strict caller reports its own failure to its own caller.
- **Nothing is logged on failure.** The caller already logs it; a second entry would report one failure twice.
- **The transaction is the unit.** `UseTransaction: true` begins a transaction, commits when the operation
  returns, and rolls back on any exception before rethrowing, then disposes the connection.
- **No statement text, ever.** The scope's three members name a procedure, exactly as the lenient members do.

The default implementation of `ExecuteStrictAsync` on the interface refuses with `NotSupportedException`, so a
test double handed to a strict caller fails loudly instead of answering a neutral value. `MySqlHelperServer` is
the only type that overrides it.

## Adoption rule for W19

Each of the five services keeps every behaviour the seam does not model. Two of them throw
`InvalidOperationException` when no connection is configured, and that throw stays at the caller after the
call is routed through the seam. The five sites are:

- `MTM_Waitlist.Core/Services/UserManagementRepository.cs` — **moved to the strict route, both sites.** Its
  `1062` and signalled-refusal handling is untouched, and an unconfigured store answers
  `MySqlStoreNotConfiguredException`, whose message is the sentence this repository used to throw itself.
- `MTM_Waitlist.Core/Services/PermissionAdministrationService.cs` — **moved to the strict route.** Its `1644`
  refusal still reaches the caller, and reads and writes still go through one pair of helpers.
- `MTM_Waitlist.Core/Services/LuggerArrangementService.cs` — **moved to the strict route for writes.** The
  missing-procedure and missing-table answers (1305 and 1146) are still distinguished, and the reads stay on
  the lenient route. The write connection's resolution is now the seam's, which forces the same
  `mtm_waitlist` database name and applies the same host fallback.
- `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs` — **moved to the strict route with
  `UseTransaction: true`,** so the clear and the re-assignment stay one atomic unit, with the connect and
  command timeouts it applied to its own connection.
- `MTM_Waitlist.Core/Services/ConnectionHealthService.cs` — **not moved,** recorded at the code with a
  `// simplified:` note. Its probe opens a connection and calls no procedure, it applies its own 5-second
  connect timeout instead of the waitlist options', and one of the two sources it probes is SQL Server rather
  than MySQL. Moving it would change the probe's timeout and its reach, which is a behaviour change rather
  than a refactor.

## The Visual read path after W20

One type: `MTM_Waitlist.Module_Core.Services.InforVisualSqlQueryService`, exposing `ExecuteQueueAsync` over
`IConfiguration`. Resolution order is unchanged: `INFOR_VISUAL_SQL_CONNECTION_STRING` first, then the
per-field environment variables, then the configuration values. The setup module registers this type; its own
copy is deleted.

The fallback contract is not changed by this feature. Every shipped read shape keeps a fallback that carries
the shared algorithm, and every registered fallback keeps deriving from it. That is what
`VisualReadRouteAuditTests` asserts and what FR-034 preserves.

## Guardrails this contract answers to

`InlineSqlAuditTests` has two assertions that bite here: no MySQL statement text remains in application code,
and every raw-SQL seam allowlist entry is still accurate. Removing the text path and moving five services must
shrink that allowlist in the same change, or the second assertion fails on a list describing code that no
longer exists.

`RetiredSymbolAuditTests` must keep passing and must not be weakened while W16 retires the 7.1.2 toolkit
symbols.
