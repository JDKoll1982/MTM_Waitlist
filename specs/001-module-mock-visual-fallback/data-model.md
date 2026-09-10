# Phase 1 Data Model — Module_Mock: Automatic Infor Visual Read Fallback

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Sources**: `spec.md` (FR-002…FR-027, Key Entities), `WeekendProject/Module_Mock/Spec.md` §4/§6, `Discovery/02`, and
`.github/instructions/database-schema-rules.instructions.md` (locked rules).
**Scope**: data entities only. Interface/endpoint contracts live in `contracts/`.

---

## 1. Store inventory and mode (FR-001, FR-018, FR-027)

| Store | Kind | Engine / location | Read mode | Write mode | Backed up | Cached |
|---|---|---|---|---|---|---|
| `mtm_waitlist` | Internal — own | MySQL 5.7, shared host | **Always live** | **Always live** | Yes (FR-009) | No |
| `mtm_wip_application_winforms` | Internal — floor/WIP | MySQL 5.7, shared host | **Always live** | Always live | Yes (FR-009) | **No** (FR-018) |
| `mtm_receiving_application` | Internal — receiving | MySQL 5.7, shared host | **Always live** | Always live | Yes (FR-009) | **No** (FR-018) |
| `mtm_mock` | Internal — **cache only** | MySQL 5.7, shared host (**new DB**) | Live reads of mirrors | Written only by refresh | Yes (FR-009) | n/a (it *is* the cache) |
| Infor Visual (`VISUAL` / `MTMFG`) | **External, read-only** | SQL Server | Live, with cached fallback | **Never written** | **No** (spec non-goal NG3) | n/a |

`mtm_mock` is never authoritative for internal application data (FR-027) and holds no entity that is not a cached copy
of an external read shape.

---

## 2. Entity: `VisualReadShape` (definition, not table — the unit of extensibility, FR-016)

| Field | Type | Notes |
|---|---|---|
| `Key` | string | Stable shape identifier, e.g. `work_order_lookup`; drives every derived name |
| `Module` | enum | `Module_Setup` \| `Module_Waitlist` — selects the source script folder |
| `SourceScriptRelativePath` | string | e.g. `Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql` |
| `InputParameters` | ordered list of `(Name, Type, IsRequired)` | The Visual read's parameter set |
| `OutputColumns` | ordered list of `(Name, Type)` | The Visual read's exact returned column set |
| `MirrorTableName` | string | `visual_<key>_result` |
| `StageTableName` | string | `visual_<key>_result_stage` |
| `GetProcedureName` | string | `sp_visual_<key>_get` |
| `RefreshProcedureName` | string | `sp_visual_<key>_refresh` |
| `RefreshInterval` | `TimeSpan` (default from service config) | Per-shape override allowed |
| `IsEnabled` | bool | Lets an operator park a shape without deleting artifacts |

**Validation rules** (from FR-016 and the playbook):
- `MirrorTableName`, `StageTableName`, `GetProcedureName`, and `RefreshProcedureName` are **derived** from `Key`; a
  shape whose artifacts do not exist under the derived names is invalid and must be reported at startup.
- `OutputColumns` is the contract with live reads: it must match the live Visual projection exactly, or the shape's
  refresh is marked failed and the last good snapshot is retained.
- Adding a shape must not change any other shape's row or artifact.
- Startup artifact-existence validation is performed by the dedicated `mtm_mock` procedure
  `sp_visual_read_shape_metadata_get` (§12) — never by inline SQL or a schema-API query in C# (constitution III).

---

## 3. Cache tables — the five read shapes (FR-002, FR-004, FR-006, FR-017)

> **Authoritative physical column list.** This section is the single authoritative definition of the mirror tables'
> physical columns, types, and indexes. `contracts/visual-read-fallback.md` §3 (result column names) and
> `contracts/mock-service-configuration.md` §3 (catalog projection) reference these definitions rather than restating
> them.

**Common structure for every mirror table `<shape>_result`** (identical for its `_stage` twin):

| Column | Type | Rule | Purpose |
|---|---|---|---|
| `id` | `BIGINT UNSIGNED NOT NULL AUTO_INCREMENT` | PK is always `id` (DB rules) | Surrogate key required by the ruleset |
| *(shape key columns)* | see below | Indexed | The read's input parameters, used as lookup predicates |
| *(shape output columns)* | see below | — | The read's exact returned column set |
| `refreshed_utc` | `DATETIME NOT NULL` | `_utc` suffix (DB rules) | Time of the last successful refresh that produced this row (FR-022 age source) |
| `is_seed_content` | `TINYINT(1) NOT NULL DEFAULT 0` | `is_` prefix (DB rules) | `1` only for baseline seed rows written before the first successful refresh (FR-017); refresh always writes `0` |

Engine/charset for every table: `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`.

Index naming: as listed per table below, the five tables use only `PRIMARY` and their `uq_` unique keys — there is
**no** `idx_`-named index in the initial schema. Any secondary (non-unique) index added later is named
`idx_<table>_<columns>` per the ruleset.

> **Why `is_seed_content` exists**: FR-022 requires the indicator to show *the time of the last successful refresh*,
> and the "cold cache on a fresh install" edge case requires seeded content to be served before any refresh has
> happened. Without a marker, seed rows would display a fabricated age. The spec's own Key Entities allow the mirror
> tables to carry "freshness metadata" alongside the mirrored columns, so this is metadata, not a deviation from
> FR-004's structural-identity rule (which governs the *result* the caller receives).

### 3.1 `visual_work_order_lookup_result` (shape `work_order_lookup`)

Source: `Module_Setup/Queries/LookupWorkOrder.sql` · Caller: `SetupLookupService.LookupWorkOrderFromBackendAsync`

| Column | Type | Role |
|---|---|---|
| `id` | BIGINT UNSIGNED AI | PK |
| `normalized_work_order` | `VARCHAR(64) NOT NULL` | **in** |
| `part_number` | `VARCHAR(64) NOT NULL` | out |
| `description` | `VARCHAR(255) NULL` | out |
| `work_center` | `VARCHAR(32) NULL` | out |
| `refreshed_utc` | DATETIME NOT NULL | metadata |
| `is_seed_content` | TINYINT(1) NOT NULL DEFAULT 0 | metadata |

Indexes: `PRIMARY (id)`; unique `uq_visual_work_order_lookup_result_work_order (normalized_work_order, part_number)`.

> **Rules deviation — documented and approved.** The locked DB ruleset lists `work_order` as a banned word; the frozen
> shape key and the spec's read-shape names use it deliberately (mirroring the existing `22_mock_work_orders`
> precedent). The deviation is recorded as a header comment in this table's and `visual_disposition_input_result`'s
> `create.sql`, cross-referenced from `research.md` R9, and **approved by the Tech Lead (database reviewer)** at the
> Phase 1 DDL review — approval recorded in the Phase 1 pull-request description alongside the created artifacts.

### 3.2 `visual_operation_sequences_result` (shape `operation_sequences`)

Source: `Module_Setup/Queries/GetSequences.sql` · Caller: `SetupLookupService.GetSequencesFromBackendAsync`

| Column | Type | Role |
|---|---|---|
| `id` | BIGINT UNSIGNED AI | PK |
| `normalized_work_order` | `VARCHAR(64) NOT NULL` | **in** |
| `part_number` | `VARCHAR(64) NOT NULL` | **in** |
| `sequence_number` | `INT UNSIGNED NOT NULL` | out |
| `description` | `VARCHAR(255) NULL` | out |
| `refreshed_utc` | DATETIME NOT NULL | metadata |
| `is_seed_content` | TINYINT(1) NOT NULL DEFAULT 0 | metadata |

Indexes: `PRIMARY (id)`; unique `uq_visual_operation_sequences_result_lookup_key
(normalized_work_order, part_number, sequence_number)`.
**Confirmation at Phase 1 exit**: `sequence_number` is typed `INT UNSIGNED` because the source sequence is numeric;
the live refresh verification in Phase 1 confirms this against real Visual data (a mismatch is a *failed refresh*,
never a corrupted snapshot).

### 3.3 `visual_subordinate_parts_result` (shape `subordinate_parts`)

Source: `Module_Setup/Queries/GetSubordinateParts.sql` · Caller: `SetupLookupService.GetSubordinatePartsFromBackendAsync`

| Column | Type | Role |
|---|---|---|
| `id` | BIGINT UNSIGNED AI | PK |
| `normalized_work_order` | `VARCHAR(64) NOT NULL` | **in** |
| `parent_part_number` | `VARCHAR(64) NOT NULL` | **in** (renamed, see note) |
| `sequence_number` | `INT UNSIGNED NOT NULL` | **in** |
| `category` | `VARCHAR(64) NULL` | out |
| `part_number` | `VARCHAR(64) NOT NULL` | out — the **subordinate** part |
| `description` | `VARCHAR(255) NULL` | out |
| `location` | `VARCHAR(64) NULL` | out |
| `user8` | `VARCHAR(64) NULL` | out |
| `on_hand_quantity` | `DECIMAL(18,4) NOT NULL DEFAULT 0` | out |
| `refreshed_utc` | DATETIME NOT NULL | metadata |
| `is_seed_content` | TINYINT(1) NOT NULL DEFAULT 0 | metadata |

Indexes: `PRIMARY (id)`; unique `uq_visual_subordinate_parts_result_lookup_key
(normalized_work_order, parent_part_number, sequence_number, part_number)`.

> **Note — the one intentional column rename**: the live read takes the *operation's* part number as an input **and**
> returns each *subordinate* part's number as an output. Both are called `PartNumber` in the source. The input key is
> therefore persisted as `parent_part_number` so that `part_number` in the returned row means exactly what it means in
> the live result. `sp_visual_subordinate_parts_get` exposes the parameter as `p_part_number` (the operation
> context), so callers see no difference — this keeps FR-004's structural identity at the *result* boundary.

### 3.4 `visual_inventory_locations_result` (shape `inventory_locations`)

Source: `Module_Waitlist/Queries/GetInventoryLocations.sql` · Caller:
`WaitlistInventoryService.GetInventoryLocationsFromBackendAsync` (on-hand ≥ 1 and ignored-location filtering remain
applied by the caller, unchanged — spec Assumptions)

| Column | Type | Role |
|---|---|---|
| `id` | BIGINT UNSIGNED AI | PK |
| `part_number` | `VARCHAR(64) NOT NULL` | **in** and out (single column serves both — the live read's input and output part number are the same value) |
| `location` | `VARCHAR(64) NOT NULL` | out |
| `on_hand_quantity` | `DECIMAL(18,4) NOT NULL DEFAULT 0` | out |
| `refreshed_utc` | DATETIME NOT NULL | metadata |
| `is_seed_content` | TINYINT(1) NOT NULL DEFAULT 0 | metadata |

Indexes: `PRIMARY (id)`; unique `uq_visual_inventory_locations_result_location (part_number, location)`.

### 3.5 `visual_disposition_input_result` (shape `disposition_input`)

Source: `Module_Waitlist/Queries/GetDispositionInput.sql` · Caller: `RequestDispositionResolver.GetDispositionInputAsync`

| Column | Type | Role |
|---|---|---|
| `id` | BIGINT UNSIGNED AI | PK |
| `work_order` | `VARCHAR(64) NOT NULL` | **in** |
| `part_number` | `VARCHAR(64) NOT NULL` | **in** |
| `work_order_status` | `VARCHAR(8) NULL` | out (status codes remain owned by `RequestDispositionStatusCodes`; Open = {R,U,F}, Closed = {C}, X never FG) |
| `open_work_order_quantity` | `DECIMAL(18,4) NOT NULL DEFAULT 0` | out |
| `finished_goods_quantity` | `DECIMAL(18,4) NOT NULL DEFAULT 0` | out |
| `has_outside_vendor_operation` | `TINYINT(1) NOT NULL DEFAULT 0` | out (`has_` prefix per DB rules) |
| `refreshed_utc` | DATETIME NOT NULL | metadata |
| `is_seed_content` | TINYINT(1) NOT NULL DEFAULT 0 | metadata |

Indexes: `PRIMARY (id)`; unique `uq_visual_disposition_input_result_lookup_key (work_order, part_number)`.

### 3.6 Stage twins

For each of the five tables above, an identical-structure twin `<shape>_result_stage` exists with the same columns and
constraints. The stage twin is **always** `.sql`-created alongside its live table (Phase 1) and is never read by the
application — only by `sp_visual_<shape>_refresh`.

**Refresh-and-swap transition** (FR-006, SC-005), executed inside `sp_visual_<shape>_refresh`:

1. `TRUNCATE TABLE <shape>_result_stage;`
2. Load the stage twin with the complete new result set (`refreshed_utc = UTC_TIMESTAMP()`,
   `is_seed_content = 0`).
3. Validate the load (row count and column mapping); on any error, roll back / abort and leave the live table untouched.
4. Swap atomically in a single statement (the documented left-to-right three-name idiom, because the middle step's
   target name is occupied by the outgoing snapshot):

   ```sql
   RENAME TABLE visual_x_result       TO visual_x_result_prev,
                visual_x_result_stage TO visual_x_result,
                visual_x_result_prev  TO visual_x_result_stage;
   ```

**Invariants**:
- The live table name always resolves to a complete snapshot (never empty, never partial).
- The stage twin after a swap holds the *previous* complete snapshot, which is truncated at the start of the next
  cycle. No snapshot history is retained (spec Assumptions: retention follows the refresh interval).
- If the source is unreachable, the procedure is **not** called at all; the live table is untouched (FR-008).
- If the source returns an unexpected column set, the load fails and the refresh is marked failed; the live table is
  untouched (edge case "Source schema drift").
- `visual_x_result_prev` is a transient name that never persists between cycles; it exists only inside the swap
  statement.

---

## 4. Entity: `RefreshRunRecord` (FR-008, FR-013)

| Field | Type | Notes |
|---|---|---|
| `RunId` | GUID | |
| `ShapeKey` | string | FK → `VisualReadShape.Key` |
| `StartedUtc` / `FinishedUtc` | DateTime (UTC) | `_utc` convention |
| `Outcome` | enum | `Succeeded` \| `SkippedSourceUnreachable` \| `FailedSchemaMismatch` \| `FailedLoad` \| `FailedUnknown` |
| `RowCount` | int? | Rows loaded into the stage twin on success |
| `DurationMs` | long | |
| `ErrorMessage` | string? | Sanitized; never contains a credential (FR-026) |

**State transitions**: `Pending → Running → Succeeded | SkippedSourceUnreachable | Failed*`.
A `SkippedSourceUnreachable` outcome is a *normal* result (US3 acceptance 3): it must be logged, must not alter the
live table, and must not prevent the next scheduled cycle.

**Persistence**: the record store is **service-local and durable** (the service's own app-data folder), deliberately
**not** in MySQL. Rationale: a restore replaces an entire store, so operational history kept inside a store would be
rewound by the very operation it needs to describe; and refresh status must remain reportable when Infor Visual is
down.

---

## 5. Entity: `ServiceConfiguration` (FR-012)

| Field | Type | Default chosen at implementation time | Notes |
|---|---|---|---|
| `RefreshInterval` | `TimeSpan` | 15 minutes | Global default; per-shape override via shape catalog |
| `VisualSource` | object | from existing `InforVisualDatabaseOptions` | Server/database/user; password from the existing env-var path (never plaintext in this file) |
| `ApiSettings.BindAddress` | string | `0.0.0.0` | Configurable (FR-012); loopback-only is a valid operator choice |
| `ApiSettings.Port` | int | 5760 | Configurable |
| `ApiSettings.Credential` | protected blob | generated on first run | DPAPI `CurrentUser`; never displayed or logged (FR-026) |
| `BackupPolicies` | map store → `BackupPolicy` | see §6 | Independent per store (FR-009) |
| `AutoStartAtLogon` | bool | true | Writes/removes the per-user `Run` entry (FR-007) |
| `MysqldumpPath` | string? | resolved from `PATH` | Absence is reported, not worked around (FR-013) |

---

## 6. Entity: `BackupPolicy` and `BackupArtifact` (FR-009, FR-013, SC-008)

**`BackupPolicy`** — one instance per store, independently configurable:

| Field | Type | Default | Notes |
|---|---|---|---|
| `Store` | enum | — | One of the four MySQL stores |
| `IsEnabled` | bool | `mtm_waitlist`/WIP/receiving **true**, `mtm_mock` **true** | Disabling one store must not affect others (US6 acceptance 2) |
| `ScheduleLocalTime` | `TimeOnly` | 01:00 | Local host time |
| `RetentionCount` | int | 14 | Artifacts kept per store |
| `DestinationDirectory` | string | `<service app data>\backups\<store>` | Must be writable; validated at save time |
| `LastRunUtc` / `LastOutcome` | — | — | Surfaced by FR-013 |

**`BackupArtifact`**:

| Field | Type | Notes |
|---|---|---|
| `ArtifactId` | GUID | |
| `Store` | enum | |
| `CreatedUtc` | DateTime (UTC) | |
| `FilePath` | string | `.sql` produced with `--result-file` |
| `SizeBytes` | long | Zero-length ⇒ failure, never a success record |
| `IsRetained` | bool | `false` once pruned by `RetentionCount` |
| `IsSafetySnapshot` | bool | `true` for the pre-restore snapshot taken by `RestoreService` |

**Validation rules**: an artifact is recorded as successful **only** if the `mysqldump` process exited 0 *and* the
file exists with a non-zero size; if the tool is missing, the outcome is `ToolUnavailable` and **no** artifact record
is written (edge case "Backup facility missing", FR-013).

---

## 7. Entity: `RestoreOutcome` (FR-010, FR-023, SC-009)

| Field | Type | Notes |
|---|---|---|
| `Store` | enum | Target store |
| `ArtifactId` | GUID | Chosen backup |
| `SafetySnapshotArtifactId` | GUID? | Taken before the destructive step — the recovery path if the restore fails partway |
| `RequestedUtc` / `ConfirmedUtc` / `FinishedUtc` | DateTime (UTC) | `ConfirmedUtc` is null when the operator did not accept the prompt |
| `Outcome` | enum | `NotConfirmed` \| `Succeeded` \| `FailedDrop` \| `FailedReload` \| `FailedVerification` |
| `VerificationSummary` | string? | Row counts of the store's core tables compared before/after |

**State transitions**: `Requested → (NotConfirmed | Confirmed) → SafetySnapshotTaken → Dropping → Reloading →
Verifying → Succeeded | Failed*`.
`NotConfirmed` must leave the store byte-for-byte unchanged (US6 acceptance 3). `Failed*` must name the
`SafetySnapshotArtifactId` so the operator can recover (edge case "Restore interrupted").
**Reachability**: this entity is only ever produced by a local UI action in the service process; no contract in
`contracts/` exposes it over the network (FR-023).

---

## 8. Entity: `ReadStatusSnapshot` (FR-005, FR-022; in-app, not persisted)

The single status model type is **`ReadStatusSnapshot`** (`MTM_Waitlist.Mock/Models/ReadStatusSnapshot.cs`), and the
state enum it carries is **`VisualReadStatus`** (`MTM_Waitlist.Mock/Models/VisualReadStatus.cs`). These two names are
**authoritative** across `plan.md`, `contracts/visual-read-fallback.md` §4, `research.md` R11/R12, and `tasks.md`.

| Field | Type | Notes |
|---|---|---|
| `Status` | `VisualReadStatus` enum | `Unknown` \| `Live` \| `Cached` — the detector state |
| `IsCachedDataInUse` | bool | `true` iff `Status == Cached`; drives indicator visibility (FR-005) |
| `ChangedUtc` | DateTime (UTC) | When the state last changed |
| `CachedDataAgeUtc` | DateTime? (UTC) | The last successful refresh time across shapes — the FR-022 age source; null when only seed content exists |
| `IsSeedContentOnly` | bool | True when the newest served content still has `is_seed_content = 1` |
| `PerShapeLastRefreshUtc` | map shape → DateTime? | Feeds the status surface (FR-013) |

**State transitions** (see `research.md` R11 for the hysteresis rationale):
```
Unknown --(first probe succeeds)--> Live
Unknown --(first probe fails)-----> Cached
Live    --(2 consecutive failures)-> Cached
Cached  --(1 success)-------------> Live
```
The indicator (FR-005) is visible if and only if `State == Cached`; it renders no control, so no transition can be
triggered by the user (FR-003, US4 acceptance 2).

---

## 9. Entity: `SharedCredential` (FR-026, SC-010)

| Field | Type | Notes |
|---|---|---|
| `ProtectedValue` | byte[] | DPAPI `CurrentUser` ciphertext; the only stored form |
| `CreatedUtc` | DateTime (UTC) | |

**Rules**: never rendered in any UI, never written to any log or status payload, compared in constant time by the API
authentication handler, and rotated only by explicit operator action (which invalidates existing clients until the new
value is installed).

---

## 10. Internal-store availability state (FR-021)

When an **internal** store is unavailable, the affected screen shows an `Unavailable` state carrying:
`Store`, `LastAttemptUtc`, `RetryCount`, `NextRetryUtc`, and an operator-facing message. Retries are bounded: up to
three attempts with delays of approximately 1 s, 2 s, and 4 s; after the third failure the screen shows the
`Unavailable` state for that screen. There is **no** cached counterpart and **no** sample-data substitution for
internal stores (FR-001, FR-018), and this
state is a per-screen condition rather than a persistent banner (FR-021).

---

## 11. Retired entities (deleted by this feature — FR-014)

| Entity | Where | Disposition |
|---|---|---|
| `mock_parts`, `mock_work_orders`, `mock_work_centers`, `mock_locations`, `mock_inventory_locations`, `mock_requesters`, `mock_request_types`, `mock_master_tables_registry` | `mtm_waitlist` tables | Drop with rollback artifact |
| `sp_mock_*` (+ `sp_mock_master_table_columns_get`) | `mtm_waitlist` procedures | Drop with rollback artifact |
| `seed_mock_master_default` | `Database/Seeds/` | Remove artifact + master-list entry |
| `Feature.InforVisualMockData`, `Feature.RecvMockData` | local settings keys | Delete keys; delete `MockSettingKeys` |
| Sample catalog / routing / toggle / toast types | C#, DI, Settings UI, tests | Delete (see `Discovery/01`) |

No schema object from the retired families is migrated into `mtm_mock`; `mtm_mock` contains only the five mirror
tables, their stage twins, the ten `sp_visual_*` procedures, and the metadata procedure
`sp_visual_read_shape_metadata_get` (§12).

---

## 12. Procedure: `sp_visual_read_shape_metadata_get` (FR-016, FR-020; constitution III)

A `mtm_mock` metadata procedure used by the service's shape-catalog startup validation
(`RefreshShapeCatalogProvider`, tasks T029/T086 and T110/T111). For a requested shape (or all shapes), it returns
whether the mirror table, stage twin, `..._get` procedure, and `..._refresh` procedure exist, plus the mirror table's
actual column names derived from `information_schema`.

| Output | Meaning |
|---|---|
| `shape_key` | The requested shape key |
| `mirror_table_exists` | `1` when `visual_<key>_result` exists |
| `stage_table_exists` | `1` when `visual_<key>_result_stage` exists |
| `get_procedure_exists` | `1` when `sp_visual_<key>_get` exists |
| `refresh_procedure_exists` | `1` when `sp_visual_<key>_refresh` exists |
| `mirror_columns` | Comma-joined actual column names of the mirror table |

**Why a procedure rather than inline SQL:** constitution III forbids inline/hard-coded SQL statement text in C#, and
the inline-SQL audit (tasks T098) would otherwise flag the schema/metadata reads. Routing the check through this
procedure keeps the SP-first rule intact with no audit exemption.
