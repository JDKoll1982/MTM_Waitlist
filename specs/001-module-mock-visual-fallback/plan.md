# Implementation Plan: Module_Mock — Automatic Infor Visual Read Fallback

**Branch**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)
**Constitution**: [constitution.md](../../.specify/memory/constitution.md) — **v1.1.1, last amended 2026-09-10** (Phase-gate verdicts below were originally evaluated against v1.0.0; see the note in the Constitution Check)
**Input**: Feature specification from `/specs/001-module-mock-visual-fallback/spec.md`
**Grounding**: `WeekendProject/Module_Mock/Spec.md`, `Plan.md`, `Tasks.md`, `Discovery/01-MockLogic-Inventory.md`,
`Discovery/02-InforVisual-ReadShapes.md`, `Discovery/03-Hardcoded-MySQL-Sql.md` (read-only inputs; not modified by this plan).

**Plan revision 2 (2026-09-09)** — produced by a re-run of `/speckit.plan` after the project constitution was ratified
to v1.0.0. Revision 1 recorded the Constitution Check as *"NOT APPLICABLE — constitution is unratified"*; **revision 2
evaluates the gate against all six ratified principles and all three Additional Constraints (all PASS)**. The
Technical Context, Project Structure, `research.md` (R1–R15), `data-model.md`, `contracts/` (3 files), and
`quickstart.md` are carried forward from revision 1 unchanged except where the Constitution Check required an
explicit design commitment (localized resource files + `App.xaml` registration for the new UI, now named in the
Project Structure).

## Summary

The application currently carries two independent "mock" systems that short-circuit reads and writes to *all* data
sources — including its own `mtm_waitlist` database — which produces the reported defect (a workstation setup is
acknowledged but the work-center card never updates, and waitlist request lifecycle actions are skipped entirely).

This feature replaces both legacy mock systems with a single, automatic, read-only fallback for the **only** genuinely
unreachable source: **Infor Visual** (external, read-only SQL Server `VISUAL`/`MTMFG`).

The application's own `mtm_waitlist`, the floor/WIP store (`mtm_wip_application_winforms`), and the receiving store
(`mtm_receiving_application`) become **always live** — all mock short-circuits are deleted. Infor Visual reads gain a
mirror cache in a **new dedicated MySQL database `mtm_mock`**, holding one flattened result table per Visual **read
shape** (five initial shapes), refreshed by a staging table plus an **atomic `RENAME TABLE` swap** so readers only ever
observe a complete snapshot.

The cache stays warm from a new on-host service app (`MTM_Waitlist.Mock.Service`) that auto-starts to tray, performs
scheduled refreshes, produces per-store configurable `mysqldump` backups of all four MySQL databases, supports
host-only emergency restore, and exposes a shared-token network API for on-demand refresh and status. The in-app
library `MTM_Waitlist.Mock` owns the reachability detector, the five mirror fallback read services, the force-refresh
client, and the read-only status surface. All inline MySQL SQL is converted to stored procedures, and an extensibility
playbook documents how to add a sixth read shape.

## Technical Context

**Language/Version**: C# (latest, `LangVersion` default for .NET 10) targeting `net10.0-windows10.0.19041.0`
**Primary Dependencies**: WinUI 3 / Windows App SDK `2.3.1`; CommunityToolkit.Mvvm `8.4.2`;
CommunityToolkit.WinUI.Controls.SettingsControls `8.2.251219`; WinUIEx `2.9.2`; `Microsoft.Extensions.Hosting`
`10.0.10`; `MySqlConnector` `2.6.1`; `Microsoft.Data.SqlClient` `6.1.1`; **new** `Microsoft.AspNetCore.App`
framework reference (service app host only)
**Storage**: MySQL 5.7 on one host — `mtm_waitlist` (own store), `mtm_wip_application_winforms` (floor/WIP store),
`mtm_receiving_application` (receiving store), and **new** `mtm_mock` (Visual mirror cache). Read-only external
SQL Server Infor Visual (`VISUAL` / `MTMFG`) via `Microsoft.Data.SqlClient`. No cache for floor/WIP or receiving data.
**Testing**: MSTest in the single test project `MTM_Waitlist.Tests` (test namespaces mirror production namespaces);
opt-in live MySQL DB-integration tests gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`
**Target Platform**: Windows 10 1809+ (`TargetPlatformMinVersion 10.0.17763.0`), x64 only; **unpackaged**
(`WindowsPackageType=None`), packaged-free self-contained WinAppSDK (`WindowsAppSDKSelfContained=true`) with
`RuntimeIdentifier=win-x64`; PowerShell 7 (`pwsh`) scripts, Windows only
**Project Type**: desktop application (WinUI 3) with per-module class libraries; XAML Views stay in the app project.
Two new projects are added: one class library and one WinUI 3 desktop app (the on-host service).
**Performance Goals** *(non-gating design intent, not a success criterion)*: cached reads complete within the same envelope as live reads (target: cached-read p95 ≤ live-read p95 + 20 ms);
a refresh never blocks or partially exposes readers (`RENAME` swap only); service API responds in < 500 ms excluding
refresh work; zero added startup cost when the service is absent
**Constraints**: MySQL 5.7 syntax only; no inline/hard-coded SQL statement text may remain in C# (SP-first);
`Database/**` SQL must satisfy the locked DB naming ruleset plus a matching rollback and
`update_table_descriptions.sql` update in the same change; restore is host-only and never network-exposed; the shared
credential must never be displayed or logged; WinUI 3 APIs only (`Microsoft.UI.Xaml`, no `Windows.UI.Xaml`); build
must stay 0 warnings / 0 errors and the full test suite green; startup shape-artifact validation MUST read shape
metadata through the dedicated `sp_visual_read_shape_metadata_get` procedure (no inline SQL and no schema-API SQL text)
**Scale/Scope**: 1 new MySQL database; 5 mirror tables + 5 stage twins; 10 new `sp_visual_*` procedures; 1 new metadata
procedure (`sp_visual_read_shape_metadata_get`, deliberately named outside the retired `sp_mock_*` prefix); an estimated 14 further new stored procedures for the SP-first
conversion; 5 routed read call sites; removal of an estimated 40 C# artifacts, 8 `mock_*` tables, an estimated 16
`sp_mock_*` procedures, seeds, the Settings toggle, and the tests that exercised them. Counts described as *estimated*
are planning-time approximations, not commitments; final counts are recorded at the Phase 10 gates.
**Phase numbering**: this plan's §3 phasing (0–8) and `tasks.md`'s execution Phases 1–10 are related but not identical;
`tasks.md` carries the authoritative mapping (`tasks.md` §Phase numbering).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Constitution in force**: `.specify/memory/constitution.md` — **v1.1.1 (last amended 2026-09-10)**; six core principles
plus Additional Constraints and Governance, superseding the repo instruction files where they conflict.

**Amendment note (2026-09-10)**: the gate tables below were evaluated against v1.0.0. The intervening amendments were
**1.1.0** (Principle IV made NON-NEGOTIABLE, plus the mandatory Serena location/config self-healing rule) and **1.1.1**
(a PATCH closing the two follow-ups that amendment recorded: the `Resolve-TemplateContent` scaffold repair and the
propagation of Principle IV into `.github/instructions/mcp-doc-research.instructions.md`). Neither amendment adds,
removes, redefines, or weakens any obligation this plan relies on, so every verdict below still holds; Principle IV is
in fact now stronger, and this plan's Phase 0 entry for it already recorded the MCP-first grounding evidence it demands.

The previous revision of this plan recorded **"NOT APPLICABLE — constitution is unratified"**. That stub is
**superseded** by the evaluation below. The former informative repo gates (G1–G6) are now subsumed by principles I–VI
and are no longer tracked separately.

### Phase 0 gate — principle-by-principle evaluation

| # | Principle | Verdict | Evidence / rationale |
|---|---|---|---|
| I | **Spec-First, Verified Delivery** | **PASS** | `spec.md` (28 FRs, 16 SCs, 7 user stories) precedes this plan, and `checklists/requirements.md` exists; the `WeekendProject/Module_Mock/*` docs were reviewed as read-only grounding. The sequence `specify → plan → tasks → implement` is intact — this command creates **no** `tasks.md` and marks nothing complete. The phase order (R14: additive → switch → remove → clean-up) keeps the app shippable at every boundary. |
| II | **Live Data Integrity — Internal Stores Are Never Mocked** | **PASS** (this feature is the *enforcement* of the principle) | FR-001/FR-014/FR-018 delete every short-circuit to `mtm_waitlist`, `mtm_wip_application_winforms`, and `mtm_receiving_application`; the demo toggles (`Feature.InforVisualMockData`, `Feature.RecvMockData`), the Settings "use demo data" control, and the sample catalogs are removed, and FR-003 forbids any manual demo/mock mode. The only fallback is the **automatic** cached read for external Infor Visual while it is unreachable (FR-002/FR-024) — exactly what the principle permits. Internal writes always reach the real store, and `quickstart.md` §4 proves the reported defect is gone. `mtm_mock` is a dedicated, wholesale-replaced cache that is never authoritative for internal data (FR-027) and is read-only to the app (only `sp_visual_<shape>_get`). The `VisualReadShape.IsEnabled` park flag and the status indicator affect only *external* refresh/visibility — neither gates an internal read or write, and neither is a data mode. |
| III | **Stored-Procedure-First Database Discipline** | **PASS** | `mtm_mock` artifacts follow the locked DB rules — `Database/Mock/<Kind>/<name>/{create.sql,rollback.sql}`, the mandatory `Bootstrap/create_database.sql` + `Bootstrap/update_table_descriptions.sql`, and the `AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` master lists (R9, R10). Every new data operation is a procedure: `sp_visual_<shape>_get` and `sp_visual_<shape>_refresh`, with the atomic `RENAME TABLE` swap executed **inside** the refresh procedure rather than as inline C# (R1). the SP-first conversion phase (tasks Phase 10.1) converts the whole inline-SQL inventory from `Discovery/03` into procedures (FR-015), enforced by the automated inline-SQL audit (R13, SC-013). Every artifact ships with its matching rollback in the same change, and schema/seed/procedure changes are validated against a live MySQL before being called complete (`quickstart.md` §8). One naming deviation is recorded and approved at the Phase 1 DDL review — `work_order` is a rules-listed banned word retained as the business entity name (R9) — surfaced, not silently diverged. |
| IV | **MCP-First, Grounded Decisions** | **PASS** (one tooling gap recorded) | Serena was used for indexed repository exploration; Context7 (`/dotmorten/winuiex`, `/microsoft/windowsappsdk`) and vendor docs grounded the Windows/WinUI decisions (R3, R4); MySQL behaviour (`RENAME TABLE` atomicity; `mysqldump --result-file` / `--single-transaction` / `--routines`) is grounded in the MySQL reference manual (R1, R7, R8). **Recorded gap**: the Microsoft Learn MCP server reported *currently disabled by the user* this session, so the two Windows-specific decisions were taken from Context7 + vendor docs and are flagged for re-confirmation against Microsoft Learn when available (see the tooling note in `research.md`). No decision rests on model memory where documentation was obtainable. |
| V | **WinUI 3 Platform Conformance** | **PASS** | **Scope**: **N/A to the database-only parts** of this feature — schema, procedures, seeds, the SP-first conversion, and the audits are not UI. **Applicable** to the two UI surfaces: the in-app read-only status indicator and the new `MTM_Waitlist.Mock.Service` WinUI 3 app. Both use `Microsoft.UI.Xaml` only. The indicator is a non-interactive `InfoBar` inside the existing shell with no new navigation and no ToggleSwitch/mode control (R12), wired through the existing DI/navigation pattern. User-facing strings are resource-backed via `Strings/en-us/Resources.resw` + `GetLocalized()` — no inline literals — and any converter/resource the indicator requires is registered in `App.xaml` in the same change. The service is **unpackaged** (`WindowsPackageType=None`) and deliberately uses **no** MSIX-only API (per-user `Run` key auto-start rather than `StartupTask`), so no `RuntimeHelper.IsMSIX` guard is introduced and none is needed (R4). Settings UI uses fluid `Auto`/star sizing with `Min*`/`Max*` bounds, ThemeResource text styles, `MaxWidth` + `TextTrimming` for headers, scrolling/wrapping for overflow, and `AdaptiveTrigger` states for any interactive overlay with matching content padding. Generated artifacts (`obj/`, `*.g.cs`, `*.g.i.cs`) are never edited. |
| VI | **Evidence-Based Verification Gates** | **PASS** | The repository gates are this plan's acceptance gates and are listed verbatim in `quickstart.md` §8: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → **0 warnings / 0 errors**, and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` green. Live-DB integration tests are gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` and must pass or be explicitly recorded as environment-gated (SC-015, FR-013). `WMC9999` is treated as a masked XAML error to be surfaced, never ignored, and the documented `PRI175`/`PRI224` technique is used rather than disabling the check (both carried into `quickstart.md`). The two mechanical audits (retired-symbol, inline-SQL) turn SC-013 into a reproducible test gate instead of a manual review (R13). |

### Phase 0 gate — Additional Constraints

| Constraint | Verdict | Evidence |
|---|---|---|
| **Security & Secrets** | **PASS** | The shared credential is generated on first run and stored only as a DPAPI (`CurrentUser`) blob; it is never rendered in the settings UI, never written to a log or status payload, and is compared in constant time (R5, R6, FR-026, SC-010). Connection/credential material stays in `appsettings.json` / environment variables — no hardcoded secrets — and the MySQL password reaches `mysqldump` through an option file, never a command line (R7). Destructive restore is confirmation-gated and takes a safety snapshot first (R8). |
| **External Integration & Cache Boundaries** | **PASS** | Infor Visual is never written to (spec non-goal NG3). Its cached copy lives in its own dedicated store `mtm_mock`, is a fallback only and never authoritative for internal data (FR-027), and is updated as a complete snapshot through one atomic `RENAME TABLE` swap so readers never observe partial data (FR-006, SC-005). This feature adds no AI capability, so the privacy-first/local-by-default AI clause is untouched. |
| **Documentation & Extensibility** | **PASS** | FR-028 requires the module to be documented and all references to the retired sample/demo systems removed in the same change (SC-016); `WeekendProject/ChangeLog.md` and the Module_Mock docs are named in `quickstart.md` §8. The extensibility playbook — six ordered steps, each naming the single artifact it produces — ships in `contracts/mock-service-configuration.md` §4 and is the acceptance vehicle for FR-016 / SC-012. |

**Gate result (Phase 0): PASS.** No principle and no Additional Constraint fails. **No violation requires a
Complexity Tracking row.** Complexity Tracking below therefore records the structural additions this feature
necessarily introduces — which Governance requires to be justified and reviewable — not a deviation on security, data
integrity, or verification.

### Phase 1 gate — re-check after design

| # | Principle | Phase 1 verdict | What the Phase 1 design fixed |
|---|---|---|---|
| I | Spec-First, Verified Delivery | **PASS** | `data-model.md`, `contracts/` (3 files), `quickstart.md`, and `research.md` (R1–R15) are the design artifacts; nothing is marked complete and `tasks.md` is left to `/speckit.tasks`. |
| II | Live Data Integrity | **PASS** | `data-model.md` §1 fixes every store's mode (three internal = always live, `mtm_mock` = cache only, Visual = read-only external) and §10 defines the internal-store unavailable state with **no** sample substitution (FR-021). |
| III | SP-First DB Discipline | **PASS** | `data-model.md` §3 specifies compliant columns/indexes (`id`, `refreshed_utc`, `is_seed_content`, `uq_*` names) and §3.6 places the atomic swap inside `sp_visual_<shape>_refresh`; §11 lists the retired DB objects with their rollback disposition. |
| IV | MCP-First Grounding | **PASS** | `research.md` closes every NEEDS CLARIFICATION (R1–R15) with the source behind each decision and records the Microsoft Learn gap explicitly. |
| V | WinUI 3 Conformance | **PASS** | The design confines UI to the indicator and the service app, both resource-backed and unpackaged-safe (R4, R12); no `Windows.UI.Xaml` and no MSIX-only API is introduced. |
| VI | Evidence-Based Gates | **PASS** | Every FR maps to a verifiable step with a named command/evidence in `quickstart.md`; the SC-013 audits and the SC-015 build/test thresholds are the gates. |

**Gate result (Phase 1): PASS — no unresolved `NEEDS CLARIFICATION`, no unjustified violation. Cleared for
`/speckit.tasks`.**

## Project Structure

### Documentation (this feature)

```text
specs/001-module-mock-visual-fallback/
├── plan.md                          # This file (/speckit.plan command output)
├── spec.md                          # Authoritative requirements (28 FRs, 16 SCs, 7 user stories) — unchanged
├── research.md                      # Phase 0 output (/speckit.plan command)
├── data-model.md                    # Phase 1 output (/speckit.plan command)
├── quickstart.md                    # Phase 1 output (/speckit.plan command)
├── contracts/                       # Phase 1 output (/speckit.plan command)
│   ├── visual-read-fallback.md      # In-app live→cached read contract for the five shapes
│   ├── mock-service-http-api.md     # Service HTTP API (refresh/status), shared-token auth, host-only restore
│   └── mock-service-configuration.md# Service configuration + read-shape catalog registration (extension seam)
├── checklists/
│   └── requirements.md              # Spec quality checklist (pre-existing)
└── tasks.md                         # Phase 2 output (/speckit.tasks command — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
MTM_Waitlist.sln

# NEW — in-app fallback library (class library, no XAML Views)
# NOTE (T126, 2026-09-11): this listing is the SHIPPED layout. The live-read plumbing that Phase 4 moved
# here (shape catalog, script store, classified executor, per-shape row/request models) is listed below and
# justified in Complexity Tracking.
MTM_Waitlist.Mock/
├── Contracts/
│   ├── IVisualReachabilityDetector.cs
│   ├── IReadStatusProvider.cs
│   ├── IMockServiceRefreshClient.cs
│   ├── IInforVisualScriptStore.cs
│   ├── IVisualConnectivityProbe.cs
│   ├── IVisualConnectionStringProvider.cs
│   ├── IVisualQueryExecutor.cs
│   ├── IVisualShapeFreshnessReader.cs
│   ├── IVisualReadFallback.cs            # per-shape live→cached seam (one impl per read shape)
│   └── IVisualReachabilityProbeHost.cs
├── Models/
│   ├── VisualReadStatus.cs               # enum: Unknown | Live | Cached (authoritative status enum name)
│   ├── ReadStatusSnapshot.cs             # status model exposed by IReadStatusProvider (FR-005/FR-022)
│   ├── VisualReadShape.cs                # shape definition shared by the app and the service catalog (FR-016)
│   ├── VisualReadShapeModule.cs          # module folder a shape's scripts live under
│   ├── VisualShapeColumn.cs / VisualShapeParameter.cs
│   ├── VisualQueryStatus.cs              # Ok | Unreachable | Failed (classification — see Complexity Tracking)
│   ├── VisualQueryOutcome.cs
│   ├── VisualShapeFreshness.cs
│   ├── MockServiceClientOptions.cs       # app-side endpoint/credential installation (env-first, never a secret in appsettings)
│   ├── CachedReadResult.cs
│   └── Visual*Request.cs / Visual*Row.cs # one request + one row type per read shape
└── Services/
    ├── VisualReadShapeCatalog.cs        # the ONE catalog both the app and the service read
    ├── VisualQueryExecutor.cs           # the ONE classified live-read executor (WMC: replaces two per-module executors)
    ├── InforVisualScriptStore.cs        # the ONE script store (replaces Setup/Waitlist duplicates)
    ├── VisualConnectionStringProvider.cs
    ├── VisualConnectivityProbe.cs
    ├── VisualRowReader.cs
    ├── VisualReachabilityDetector.cs     # independent probe (does NOT reuse the removed MockRouting stack)
    ├── VisualReachabilityProbeHost.cs    # probing only — never schedules a refresh (FR-025)
    ├── ReadStatusProvider.cs             # state + change event + cached-data age for the indicator
    ├── MySqlVisualShapeFreshnessReader.cs# freshness via sp_visual_read_shape_freshness_get (FR-017/FR-022)
    ├── MockServiceRefreshClient.cs       # token-gated POST /api/refresh (graceful when uninstalled)
    ├── VisualWorkOrderLookupFallback.cs          # shape 1
    ├── VisualOperationSequencesFallback.cs       # shape 2
    ├── VisualSubordinatePartsFallback.cs         # shape 3
    ├── VisualInventoryLocationsFallback.cs       # shape 4
    └── VisualDispositionInputFallback.cs         # shape 5

# NEW — on-host service app (WinUI 3 desktop app; tray-only lifetime)
MTM_Waitlist.Mock.Service/
├── App.xaml / App.xaml.cs                # single instance + tray lifetime (no main window required)
├── Strings/en-us/Resources.resw          # localized service UI strings (no inline literals)
├── Views/
│   ├── ServiceSettingsPage.xaml(.cs)
│   └── ServiceStatusPage.xaml(.cs)
├── ViewModels/
│   ├── ServiceSettingsViewModel.cs
│   └── ServiceStatusViewModel.cs
├── Models/
│   ├── ServiceConfiguration.cs
│   ├── RefreshRunRecord.cs
│   ├── BackupPolicy.cs
│   ├── BackupArtifact.cs
│   ├── RestoreOutcome.cs
│   └── SharedCredential.cs        # shape catalog reuses MTM_Waitlist.Mock/Models/VisualReadShape.cs
├── Services/
│   ├── ServiceConfigurationStore.cs       # durable local config; DPAPI credential; auto-start reconciliation
│   ├── RefreshShapeCatalogProvider.cs     # the shape catalog read by the refresh engine (validates via a procedure)
│   ├── VisualShapeMetadataReader.cs       # wraps sp_visual_read_shape_metadata_get (no inline SQL)
│   ├── VisualShapeFreshnessReader.cs      # wraps sp_visual_read_shape_freshness_get (no inline SQL)
│   ├── VisualShapePayloadSource.cs        # one set-based population read per shape (T113)
│   ├── MockMirrorRefreshWriter.cs         # calls sp_visual_<shape>_refresh with the JSON payload
│   ├── RefreshEngine.cs                   # staging + atomic RENAME swap; skip-on-unreachable; single-cycle gate
│   ├── RefreshRunRecordStore.cs / RefreshRunRecordRecorder.cs  # durable per-shape last-run outcomes (FR-013)
│   ├── BackupEngine.cs                    # mysqldump per store; availability probe; retention pruning
│   ├── BackupArtifactStore.cs
│   ├── BackupScheduler.cs                 # per-store independent slots (T115)
│   ├── RestoreService.cs                  # host-only, confirmation-gated, safety snapshot first
│   ├── ServiceHostBuilder.cs              # composition root (T117)
│   ├── ServiceApiOperations.cs            # API operations behind the endpoints
│   ├── MySqlConnectionSettings.cs / MySqlConnectionStringResolver.cs
│   └── ServiceApiHost.cs                  # Kestrel host + lifetime wiring
├── Api/
│   ├── ServiceApiEndpoints.cs
│   ├── ServiceApiContracts.cs
│   └── SharedTokenAuthenticationHandler.cs
└── Properties/PublishProfiles/win-x64-selfcontained.pubxml   # the deploy artifact (T120) — see README.md

# NEW — reviewed SQL artifacts the service streams to the mysql client (T138); not stored procedures,
# because MySQL rejects DROP/CREATE DATABASE inside a routine (recorded deviation below)
Database/Mock.Service/Restore/
├── replace_database.sql
├── verify_restore.sql
└── README.md

# EXISTING app — one new read-only status surface (all other edits are removals/reroutes)
MTM_Waitlist/
├── Module_Mock/
│   └── Views/
│       └── ReadStatusIndicator.xaml(.cs)  # non-interactive InfoBar; no toggle, no buttons
├── Strings/en-us/Resources.resw           # + new keys for the indicator text (FR-005/FR-022); no inline literals
└── App.xaml                               # + registration for any converter/resource the indicator needs, in the same change

# EXISTING callers rerouted through MTM_Waitlist.Mock (no new files)
MTM_Waitlist.Setup/Services/SetupLookupService.cs                    # shapes 1, 2, 3
MTM_Waitlist.Waitlist.View/Services/WaitlistInventoryService.cs      # shape 4
MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs         # shape 5

# NEW — database artifact tree for the new mtm_mock database
# (per-DB subtree style already used by Database/MTMReceivingApp/ and Database/MTMWipApp/)
Database/Mock/
├── Bootstrap/
│   ├── create_database.sql                # re-runnable; creates mtm_mock if absent
│   └── update_table_descriptions.sql      # mandatory maintenance file for mtm_mock
├── Tables/<table_name>/{create.sql,rollback.sql}                 # 5 result tables + 5 stage twins
├── StoredProcedures/<procedure_name>/{create.sql,rollback.sql}   # sp_visual_<shape>_{get,refresh}
├── Seeds/<seed_name>/{create.sql,rollback.sql}                   # baseline mirror seed content
├── Validation/<validation_name>/validate.sql
├── AllTables.sql
├── AllSPs.sql
└── AllSeeds.sql

# EXISTING — unchanged read sources that define the shapes
Database/InforVisual/Queues/Module_Setup/Queries/{LookupWorkOrder,GetSequences,GetSubordinateParts}.sql
Database/InforVisual/Queues/Module_Waitlist/Queries/{GetInventoryLocations,GetDispositionInput}.sql

# EXISTING — artefacts scheduled for removal in Phase 4 (not created here)
Database/Tables/{20_mock_master_tables_registry,22_mock_work_orders,23_mock_work_centers,24_mock_locations,
                 25_mock_requesters,26_mock_inventory_locations,27_mock_request_types}/  (+ mock_parts)
Database/StoredProcedures/sp_mock_*/
Database/Seeds/seed_mock_master_default/

# EXISTING test project — new suites mirror production namespaces
MTM_Waitlist.Tests/
├── Module_Mock/                     # MTM_Waitlist.Mock library tests (reachability, 5x fallback parity)
└── Module_Mock_Service/             # service tests (refresh swap, backup, restore guard, API auth) — pure-unit only
```

**Structure Decision**: The repository's existing structure is preserved exactly — XAML Views stay in their owning
projects, per-module logic lives in `MTM_Waitlist.*` class libraries, and DB artifacts remain file-per-artifact under
`Database/`. Two projects are **added**: `MTM_Waitlist.Mock` (class library — referenced by the app and by the
modules that read Visual) and `MTM_Waitlist.Mock.Service` (a second WinUI 3 desktop app — the on-host service, which
must run independently of the client app). One database is **added**: `mtm_mock`, whose artifacts live under the
per-database subtree `Database/Mock/` following the established `Database/MTMReceivingApp/` and `Database/MTMWipApp/`
precedent. No existing directory is relocated and no existing project is split.

## Complexity Tracking

> **No constitution violation is being justified here** — the Constitution Check records PASS for all six principles
> and all three Additional Constraints. These rows record the structural additions this feature necessarily introduces,
> which Governance requires to be justified and reviewable.

| Addition | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New class library `MTM_Waitlist.Mock` (a 3rd fallback-related assembly, after all mock code is deleted) | The fallback must be consumed by the app **and** by three separate modules (`MTM_Waitlist.Setup`, `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.Settings`) that each read Visual. Putting it in the app project would reverse the existing dependency direction (modules do not reference the app). | Putting the fallback in `MTM_Waitlist.Core` was rejected: Core already holds the MySQL/SQL-Server executors and the legacy mock-routing stack being deleted, so co-locating would entangle the new detector with the removed one and keep Visual-fallback concerns inside the shared core contract surface. |
| New WinUI 3 app `MTM_Waitlist.Mock.Service` | FR-007/FR-009/FR-010/FR-011 require an always-on process, owned by the MySQL host, that outlives any client session: scheduled refresh, per-store backups, host-only restore, and a network API. | A Windows Service was rejected because the deliverable must ship a settings UI and a tray presence (FR-007, FR-012) and the repo standard is WinUI 3; a console daemon was rejected for the same UI reason and because WinUIEx already supplies the required tray lifetime. Running refresh in the client app was rejected outright by FR-025 (scheduling is owned by the service; the app must not schedule refreshes). |
| New MySQL database `mtm_mock` | FR-027 requires the cached-data store to be a **dedicated** store, separate from `mtm_waitlist`, and never authoritative for internal data. | Adding mirror tables to `mtm_waitlist` was rejected: it would mix a disposable, wholesale-replaced cache into the always-live store, break the "internal store is sacred" principle, and make the independent per-store backup requirement (FR-009) impossible to express. |
| `Microsoft.AspNetCore.App` framework reference in the service app | The service must expose a network HTTP API with token gating (FR-011) and status serialization. | `System.Net.HttpListener` was rejected: no routing, no middleware/handler pipeline, and token gating plus JSON binding would be hand-rolled — more code and more room for the auth mistakes SC-010 forbids. |
| Shared-token auth instead of a role/permission system | Spec Assumptions explicitly scope a single shared credential as acceptable because the service runs on a restricted host and no RBAC is in scope. | A role system was rejected as out of scope (spec Assumptions; service non-goal NG5). |
| **Phase-4 deviation** — the live-read plumbing moved **into** `MTM_Waitlist.Mock`: one `VisualReadShapeCatalog`, one `VisualQueryExecutor`, one `InforVisualScriptStore`, and the per-shape `Visual*Request`/`Visual*Row` types (T126) | All five shapes need the same live read, and both the app and the on-host service need the same shape definitions. Putting the catalog and executor in the library means one classification rule and one script-resolution rule serve every shape, and the dependency direction stays modules → `Mock` (a caller module never has to supply a live-read delegate). | The design's delegate-injection option was rejected after implementation: with the executor owned by each caller, the two module-local executors had already duplicated the same connect/timeout/login handling, and the service would have needed a third copy to build the mirror. Duplication here is what makes "unreachable" mean different things on different screens — the exact failure that would silently disable the fallback. |
| **Phase-4 addition** — `VisualQueryExecutor` classifies each attempt as `Ok` / `Unreachable` / `Failed` (`Models/VisualQueryStatus.cs`) | `IVisualReadFallback` may only consult the mirror on *unreachability* (FR-002/FR-024). The classification is what makes that decidable: connect, timeout, and login failures (SQL error numbers −2, 20, 53, 64, 121, 233, 258, 1231, 4060, 10053, 10054, 10060, 10061, 11001, 18456; `COMException`; timeouts) are `Unreachable`; every other error — including a missing script or a missing connection — is `Failed` and surfaces as an error rather than a cache hit. | A boolean "did it work" was rejected: the retired executors returned an empty list on *every* failure path, so "Infor Visual is unreachable" and "Visual answered with zero rows" were indistinguishable — which would have made every read look editable from cache and disabled the fallback entirely. |
| **Phase-4 addition** — `MySqlHelperServer` gained an `MtmMock` target (`MySqlDatabaseTarget`) | The fallback reads the mirror, and the shape metadata/freshness readers read it too. The seam that owns the four stores' connection resolution is the natural place for the fifth, and it keeps `mtm_mock` read-only to the application by construction (its only procedures are `sp_visual_<shape>_get` and the two metadata reads). | A separate connection factory inside `MTM_Waitlist.Mock` was rejected: it would duplicate the environment-then-configuration resolution that `MySqlHelperServer` already owns and would make the mirror connection invisible to the one place a reviewer checks store wiring. |
| **Phase-4 known limitation** — `CachedReadResult.RefreshedUtc` is `null` on the cached path | `sp_visual_<shape>_get` must project **exactly** the live column shape (FR-004), so it cannot also return `refreshed_utc`. Cached-data age therefore comes from a separate read, `sp_visual_read_shape_freshness_get` (`IVisualShapeFreshnessReader`, T124), which is what `ReadStatusSnapshot.CachedDataAgeUtc` renders. | Adding `refreshed_utc` to the shape `get` procedures was rejected: it would break the structural identity between live and cached results that FR-004 and the parity tests assert, to carry one value the freshness read already answers for every shape in one round trip. |

### Recorded deviation — the restore operation's SQL steps (constitution III)

Constitution Governance requires a written record for any deviation, so the one deviation this feature
carries is recorded here rather than left implicit.

**What deviates.** Principle III says *"Every data operation MUST go through a stored procedure."* The
emergency restore's step 2 — dropping and recreating the target store so the replacement is wholesale —
is SQL that cannot be expressed as a stored procedure. MySQL rejects `CREATE DATABASE` and `DROP DATABASE`
inside a stored routine, and a routine performing the replacement would have to live in the database being
dropped, so it would destroy itself before the reload could restore into it.

**What is preserved.** The part of Principle III that protects this codebase — *"inline or hard-coded SQL
statement text MUST NOT remain in application code"* (FR-015, SC-013) — is honoured in full. Both SQL steps
of the restore are reviewed artifacts under `Database/Mock.Service/Restore/` (`replace_database.sql`,
`verify_restore.sql`), read at run time, given the store's database name from `BackupStore.ToDatabaseName()`
(one of four fixed names, never operator input), and streamed to the `mysql` client on **stdin**. No SQL
statement text remains in `RestoreService.cs` or anywhere else in the service.

**Why this is preferable to the alternatives.** The alternative for step 4 (a per-store row-count procedure
in each of the four schemas) was rejected for a reason discovered while implementing T138: the previous
hard-coded verification counted rows in two *waitlist* tables, so three of the four stores could only ever
report "unverified" — and one of those tables (`core_workstations_registry`) no longer exists in the schema,
so it proved nothing even for the waitlist store. The verification is now store-agnostic (it reports the
store's base-table count) and needs no per-schema objects at all.

**Evidence.** Both artifacts were executed against the live MySQL 5.7.24 host on 2026-09-10 via the same
stdin mechanism the service uses: `replace_database.sql` against a throwaway database name exited `0` (the
throwaway was then dropped), and `verify_restore.sql` returned `10` base tables for `mtm_mock` — exactly the
five mirrors plus their five stage twins. Recorded in `Database/Mock.Service/Restore/README.md` and in
`tasks.md` (T138, Phase 16 execution note).
