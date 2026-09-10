# Contract: `MTM_Waitlist.Mock.Service` Configuration and Shape Catalog

**Feature**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09
**Implements**: FR-008, FR-009, FR-012, FR-013, FR-016, FR-026
**Verified by**: SC-007, SC-008, SC-010, SC-012

This is the contract an operator edits (through the service settings UI) and the contract a maintainer extends when
adding a read shape (FR-016). It defines *what must be persisted*, not the exact serialization format chosen at
implementation time.

---

## 1. Persistence contract

| Item | Requirement |
|---|---|
| Location | Service-local durable store under the service's own app-data folder (never a client-shared store) |
| Durability | Changes survive a service restart (FR-012); settings apply without restart where feasible (FR-013/FR-012 intent) |
| Credential | Stored **only** as a DPAPI `CurrentUser`-protected blob; never plaintext, never in a file readable as text (FR-026) |
| Validation | Invalid values (bad port, unwritable destination, unknown shape) are rejected at save time with a clear message — never silently accepted and failed later |
| Atomicity | A settings save is all-or-nothing; a partially written configuration must never be loaded |
| Logging | Configuration is never logged wholesale (that would defeat §credential rules); only explicitly safe fields are logged |

**Why not store configuration in a database**: the service must be configurable and reportable while the external
source is unreachable, and a restore replaces an entire store — configuration that lived in a restored store would be
rewound by the operation it needs to describe. (Same rationale as `RefreshRunRecord`; see `data-model.md` §4.)

## 2. Configuration schema

```yaml
refreshIntervalMinutes: 15          # global default; per-shape override allowed
autoStartAtLogon: true              # per-user Run entry (FR-007)
mysqldumpPath: null                 # null = resolve from PATH; absence is reported, never worked around
visualSource:
  server: VISUAL                    # from the existing InforVisualDatabaseOptions
  database: MTMFG
  userId: SHOP2
  # password is NOT stored here: it continues to come from the existing
  # INFOR_VISUAL_SQL_PASSWORD / connection-string environment path
api:
  bindAddress: 0.0.0.0              # operator may choose loopback-only
  port: 5760
  credential: <protected blob>      # DPAPI; generated on first run; never displayed or logged
backupPolicies:
  mtm_waitlist:                 { isEnabled: true, scheduleLocalTime: "01:00", retentionCount: 14, destinationDirectory: "…" }
  mtm_wip_application_winforms: { isEnabled: true, scheduleLocalTime: "01:20", retentionCount: 14, destinationDirectory: "…" }
  mtm_receiving_application:    { isEnabled: true, scheduleLocalTime: "01:40", retentionCount: 14, destinationDirectory: "…" }
  mtm_mock:                     { isEnabled: true, scheduleLocalTime: "02:00", retentionCount: 14, destinationDirectory: "…" }
shapeOverrides:
  work_order_lookup: { isEnabled: true, refreshIntervalMinutes: 15 }
```

**Rules**
- Per-store backup independence is a hard requirement: disabling one store must not change any other store's schedule
  or artifacts (FR-009, US6 acceptance 2).
- The four stores are a **fixed** set; an unknown store key is a configuration error (validation message), not an
  extension point. The extension point is shapes (§3).
- The `api.credential` blob is write-only from the operator's perspective: the UI can generate/rotate it, and can never
  display it (FR-026).
- `autoStartAtLogon` is the *setting*; the effect is the presence/absence of the per-user `Run` entry. A mismatch
  between setting and registry state at startup must be reconciled and reported, never silently ignored.

## 3. Shape catalog entry (the extensibility seam — FR-016, SC-012)

Each enabled shape is described by exactly this record:

| Field | Derivation / requirement |
|---|---|
| `shapeKey` | Stable snake_case identifier, e.g. `work_order_lookup` |
| `module` | `Module_Setup` or `Module_Waitlist` — determines the source-script folder |
| `sourceScriptRelativePath` | Must exist on disk: `Database/InforVisual/Queues/<module>/Queries/<Script>.sql` |
| `inputParameters` | Ordered `(name, type, isRequired)`; must match the script's parameters |
| `outputColumns` | Ordered `(name, type)`; **must match the live read's projection exactly** |
| `mirrorTable` | **Derived**: `visual_<shapeKey>_result` |
| `stageTable` | **Derived**: `visual_<shapeKey>_result_stage` |
| `getProcedure` | **Derived**: `sp_visual_<shapeKey>_get` |
| `refreshProcedure` | **Derived**: `sp_visual_<shapeKey>_refresh` |
| `refreshIntervalMinutes` | Optional override of the global default |
| `isEnabled` | Lets an operator park a shape without deleting artifacts |

**Startup validation (must be reported, not thrown as a crash):**
1. The source script exists.
2. The mirror table, stage table, and both procedures exist in `mtm_mock`.
3. `outputColumns` matches the live projection recorded for that shape.

A shape that fails validation is excluded from refresh cycles and reported in `GET /api/status` with a clear reason.
The service still starts and still serves the other shapes (FR-020 — one bad shape must not take the service down).

**Recorded output-column set for the five initial shapes** — the **authoritative result-column list** (the physical
names and types are the authoritative list in `data-model.md` §3, not restated here):

| `shapeKey` | Live projection |
|---|---|
| `work_order_lookup` | `PartNumber`, `Description`, `WorkCenter` |
| `operation_sequences` | `SequenceNumber`, `Description` |
| `subordinate_parts` | `Category`, `PartNumber`, `Description`, `Location`, `User8`, `OnHandQuantity` |
| `inventory_locations` | `PartNumber`, `Location`, `OnHandQuantity` |
| `disposition_input` | `WorkOrderStatus`, `OpenWorkOrderQuantity`, `FinishedGoodsQuantity`, `HasOutsideVendorOperation` |

## 4. Adding a sixth read shape — ordered procedure (the published playbook, FR-016/FR-028)

Every step produces exactly one artifact. Steps 1–3 are database/design work; 4–6 are code.

| # | Step | Artifact produced |
|---|---|---|
| 1 | **Capture the read.** Add the parameterized Visual query and record its input parameters and output columns. | `Database/InforVisual/Queues/<module>/Queries/<Script>.sql` |
| 2 | **Mirror schema.** Create the mirror table and its `_stage` twin (inputs as indexed keys + outputs + `refreshed_utc` + `is_seed_content`), with create/rollback per the DB rules; register both in `Database/Mock/AllTables.sql` and `Bootstrap/update_table_descriptions.sql`. | `Database/Mock/Tables/visual_<shape>_result/{create,rollback}.sql`, `…_stage/{create,rollback}.sql` |
| 3 | **Procedures.** Create `sp_visual_<shape>_refresh` (truncate stage → load → validate → atomic 3-name `RENAME` swap) and `sp_visual_<shape>_get` (parameterized read of the live mirror); register in `AllSPs.sql`. | `Database/Mock/StoredProcedures/sp_visual_<shape>_{refresh,get}/{create,rollback}.sql` |
| 4 | **Service registration.** Add the shape-catalog entry (§3). No engine code changes — the engine is catalog-driven. | Config/`RefreshShapeCatalogProvider` entry |
| 5 | **In-app fallback.** Add the `IVisualReadFallback<TRequest,TRow>` implementation, route the originating caller through it, and register it in DI. No existing shape's contract changes. | `MTM_Waitlist.Mock/Services/Visual<Shape>Fallback.cs` (+ DI registration) |
| 6 | **Verify.** Add a seed row set, a refresh test (swap is atomic; failure leaves the snapshot intact), a
   fallback-parity test (live vs mirror identical shape, including the empty-live-result case), and a caller test. | `Database/Mock/Seeds/<seed>/{create,rollback}.sql` + tests |

**Half-added detection (edge case "New read shape half-added"):** skipping step 4 leaves a shape with tables and
procedures that are never refreshed; skipping step 5 leaves a caller with no fallback. Both are detected by the
startup validation in §3 (catalog-to-artifact check) and by the parity tests in step 6, and both are called out in the
playbook above so the gap is documented rather than discovered in production.

## 5. Client-side credential installation (operator procedure)

1. In the service UI, generate the credential (it is never displayed afterwards; the UI offers "rotate" only).
2. Install the value into each client's configuration, out of band — never in source control, never in
   `appsettings.json` committed to the repo, never in chat or ticket text (FR-026).
3. Rotation invalidates existing clients until they are updated; the service reports unauthorized attempts in its log
   so a mis-installed client is diagnosable without exposing the secret (SC-010).

If a client has no credential configured, it must treat refresh requests as unavailable and continue operating on
cached data — the service is never a hard dependency (FR-025, SC-011).
