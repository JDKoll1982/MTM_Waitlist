# Implementation Plan: Module_Mock — Automatic Infor Visual Read Fallback

**Branch**: `001-module-mock-visual-fallback` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-module-mock-visual-fallback/spec.md`
**Grounding**: `WeekendProject/Module_Mock/Spec.md`, `Plan.md`, `Tasks.md`, `Discovery/01-MockLogic-Inventory.md`,
`Discovery/02-InforVisual-ReadShapes.md`, `Discovery/03-Hardcoded-MySQL-Sql.md` (read-only inputs; not modified by this plan).

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

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
**Performance Goals**: cached reads complete within the same envelope as live reads (no added user-visible latency);
a refresh never blocks or partially exposes readers (`RENAME` swap only); service API responds in < 500 ms excluding
refresh work; zero added startup cost when the service is absent
**Constraints**: MySQL 5.7 syntax only; no inline/hard-coded SQL statement text may remain in C# (SP-first);
`Database/**` SQL must satisfy the locked DB naming ruleset plus a matching rollback and
`update_table_descriptions.sql` update in the same change; restore is host-only and never network-exposed; the shared
credential must never be displayed or logged; WinUI 3 APIs only (`Microsoft.UI.Xaml`, no `Windows.UI.Xaml`); build
must stay 0 warnings / 0 errors and the full test suite green
**Scale/Scope**: 1 new MySQL database; 5 mirror tables + 5 stage twins; 10 new `sp_visual_*` procedures; `+ ~14` new
stored procedures for the SP-first conversion; 5 routed read call sites; removal of ~40 C# artifacts, 8 `mock_*`
tables, ~16 `sp_mock_*` procedures, seeds, the Settings toggle, and the tests that exercised them

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status: NOT APPLICABLE — constitution is unratified.**

`.specify/memory/constitution.md` is still the **Spec Kit template** (`[PROJECT_NAME]`, `[PRINCIPLE_1_NAME]`,
`[CONSTITUTION_VERSION]` placeholders; no ratified content). It therefore defines **no governing principles**, and no
compliance gate can be evaluated against it. Per this command's instructions this is recorded rather than hard-failed,
and no project principles are invented to fill the gap.

**Action**: run `/speckit.constitution` to ratify project principles before relying on the constitution as a governing
document. Once ratified, re-run `/speckit.plan` (or `/speckit.analyze`) so the gates below become enforceable.

**Repo-level gates applied instead (informative, from `.github/instructions/` and the grounding docs)** — these are
standing repository rules, not constitution principles:

| # | Gate (source) | Phase 0 status | Phase 1 status |
|---|---|---|---|
| G1 | WinUI 3 APIs only; `Microsoft.UI.Xaml`; no `Windows.UI.Xaml`; guard MSIX-only APIs behind `RuntimeHelper.IsMSIX` (`csharp-xaml-naming-rules`, `.github/copilot-instructions.md`) | PASS — plan introduces no legacy namespaces; service is unpackaged and uses non-MSIX APIs | PASS — service design is unpackaged-safe (tray + registry auto-start, not `StartupTask`) |
| G2 | C#/XAML naming rules: I-prefixed interfaces, `Async` suffix, `_camelCase` private fields, `XxxPage`/`XxxView`/`XxxViewModel` suffixes, folder↔namespace alignment, one public type per file (`csharp-xaml-naming-rules`) | PASS — see Project Structure; names below follow the rules | PASS — data-model/contracts keep the suffix and naming conventions |
| G3 | DB rules: lowercase snake_case, plural structured-prefix tables, `id` PK, `public_id`, `is_`/`has_` booleans, `_utc` datetimes, `idx_`/`uq_`/`fk_` naming, ≤64 chars, file-per-artifact with `create.sql`+`rollback.sql`, and `Bootstrap/update_table_descriptions.sql` updated in the same change (`database-schema-rules`) | PASS — `mtm_mock` artifacts follow the per-artifact layout (see Project Structure) | PASS — `data-model.md` specifies compliant column/index names and the mandatory maintenance files |
| G4 | SP-first: no inline/hard-coded SQL; every statement is a stored procedure (spec FR-015, `Discovery/03`) | PASS — Phase 5 covers the full file:line inventory | PASS — enforcement test is a named artifact (SC-013) |
| G5 | MCP-first grounding: prefer Serena for repo symbols, Context7 / Microsoft Learn for APIs (`mcp-doc-research`) | PASS — Serena/Context7 used; Microsoft Learn MCP reported disabled this session, MySQL API facts taken from vendor docs (see `research.md`) | PASS — see `research.md` sources |
| G6 | Spec Kit ordering: `specify` → `plan` → `tasks` → `implement`; `tasks.md` is an executable checklist and tasks are ticked only when verified (`spec-kit.instructions.md`) | PASS — spec exists; this command produces only plan-phase artifacts and does **not** create `tasks.md` | PASS — `tasks.md` is left to `/speckit.tasks` |

No violations require justification, so Complexity Tracking below records only the structural additions this feature
necessarily introduces.

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
MTM_Waitlist.Mock/
├── Contracts/
│   ├── IVisualReachabilityDetector.cs
│   ├── IReadStatusProvider.cs
│   ├── IMockServiceRefreshClient.cs
│   └── IVisualReadFallback.cs            # per-shape live→cached seam (one impl per read shape)
├── Models/
│   ├── VisualReachabilityState.cs
│   ├── ReadStatusSnapshot.cs
│   ├── VisualReadStatus.cs
│   └── CachedReadResult.cs
└── Services/
    ├── VisualReachabilityDetector.cs     # independent probe (does NOT reuse the removed MockRouting stack)
    ├── ReadStatusProvider.cs             # state + change event + cached-data age for the indicator
    ├── MockServiceRefreshClient.cs       # token-gated POST /api/refresh
    ├── VisualWorkOrderLookupFallback.cs          # shape 1
    ├── VisualOperationSequencesFallback.cs       # shape 2
    ├── VisualSubordinatePartsFallback.cs         # shape 3
    ├── VisualInventoryLocationsFallback.cs       # shape 4
    └── VisualDispositionInputFallback.cs         # shape 5

# NEW — on-host service app (WinUI 3 desktop app; tray-only lifetime)
MTM_Waitlist.Mock.Service/
├── App.xaml / App.xaml.cs                # single instance + tray lifetime (no main window required)
├── Views/
│   ├── ServiceSettingsPage.xaml(.cs)
│   └── ServiceStatusPage.xaml(.cs)
├── ViewModels/
│   ├── ServiceSettingsViewModel.cs
│   └── ServiceStatusViewModel.cs
├── Models/
│   ├── ServiceConfiguration.cs
│   ├── RefreshShapeDefinition.cs
│   ├── RefreshRunRecord.cs
│   ├── BackupPolicy.cs
│   ├── BackupArtifact.cs
│   ├── RestoreOutcome.cs
│   └── SharedCredential.cs
├── Services/
│   ├── ServiceConfigurationStore.cs       # durable local config; credential via DPAPI
│   ├── RefreshShapeCatalogProvider.cs     # the shape catalog read by the refresh engine
│   ├── RefreshEngine.cs                   # staging + atomic RENAME swap; skip-on-unreachable
│   ├── BackupEngine.cs                    # mysqldump per store; availability probe
│   ├── RestoreService.cs                  # host-only, confirmation-gated, safety snapshot first
│   └── ServiceApiHost.cs                  # Kestrel host + lifetime wiring
└── Api/
    ├── ServiceApiEndpoints.cs
    └── SharedTokenAuthenticationHandler.cs

# EXISTING app — one new read-only status surface (all other edits are removals/reroutes)
MTM_Waitlist/
└── Module_Mock/
    └── Views/
        └── ReadStatusIndicator.xaml(.cs)  # non-interactive InfoBar; no toggle, no buttons

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

> Filled because the feature adds structural elements (not constitution violations — see Constitution Check).

| Addition | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New class library `MTM_Waitlist.Mock` (a 3rd fallback-related assembly, after all mock code is deleted) | The fallback must be consumed by the app **and** by three separate modules (`MTM_Waitlist.Setup`, `MTM_Waitlist.Waitlist.View`, `MTM_Waitlist.Settings`) that each read Visual. Putting it in the app project would reverse the existing dependency direction (modules do not reference the app). | Putting the fallback in `MTM_Waitlist.Core` was rejected: Core already holds the MySQL/SQL-Server executors and the legacy mock-routing stack being deleted, so co-locating would entangle the new detector with the removed one and keep Visual-fallback concerns inside the shared core contract surface. |
| New WinUI 3 app `MTM_Waitlist.Mock.Service` | FR-007/FR-009/FR-010/FR-011 require an always-on process, owned by the MySQL host, that outlives any client session: scheduled refresh, per-store backups, host-only restore, and a network API. | A Windows Service was rejected because the deliverable must ship a settings UI and a tray presence (FR-007, FR-012) and the repo standard is WinUI 3; a console daemon was rejected for the same UI reason and because WinUIEx already supplies the required tray lifetime. Running refresh in the client app was rejected outright by FR-025 (scheduling is owned by the service; the app must not schedule refreshes). |
| New MySQL database `mtm_mock` | FR-027 requires the cached-data store to be a **dedicated** store, separate from `mtm_waitlist`, and never authoritative for internal data. | Adding mirror tables to `mtm_waitlist` was rejected: it would mix a disposable, wholesale-replaced cache into the always-live store, break the "internal store is sacred" principle, and make the independent per-store backup requirement (FR-009) impossible to express. |
| `Microsoft.AspNetCore.App` framework reference in the service app | The service must expose a network HTTP API with token gating (FR-011) and status serialization. | `System.Net.HttpListener` was rejected: no routing, no middleware/handler pipeline, and token gating plus JSON binding would be hand-rolled — more code and more room for the auth mistakes SC-010 forbids. |
| Shared-token auth instead of a role/permission system | Spec Assumptions explicitly scope a single shared credential as acceptable because the service runs on a restricted host and no RBAC is in scope. | A role system was rejected as out of scope (spec Assumptions; service non-goal NG5). |
