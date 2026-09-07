# 12 — Mock Master Data & DB-Driven Mock Sources (Developer-Editable)

> **Purpose:** Rework every mock catalog used by the New-Request flow so mock parts use proper part ids,
> locations, and work orders (coils = `MMC########`, flatstock = `MMF########`, other = `23-###-####` to
> differentiate distinct "Other" items), sourcing canonical values from the MTM_Receiving_Application
> codebase + database. Store the mock master data as **tables in the app's MySQL `mtm_waitlist` database**
> (as `mock_*` tables), editable in a **Developer Settings page** (a main-navigation item above Settings,
> role-gated, separate from normal settings) with a dropdown of each mock table, an editable grid per
> table, and a UI-readable name + plain-language description per table (shown in the dropdown for
> end-user auditing). The existing JSON/config + mock catalogs **pull from these mock tables** (driven by
> the existing `Feature.InforVisualMockData` / `Feature.RecvMockData` toggles) so edits take effect on
> the next app load.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented,
> builds clean, and tests pass.
>
> **Related:** `13-Developer-UI.md` is the follow-on Developer UI effort that builds on this file's
> foundation — it defines the **auto mock-fallback on DB outage** (affects 0.4's routing toggles), the
> **Developer Settings page / nav** (shares 0.5's page), and the **Option-A request-type/subtype editor**
> (refines 0.6's "expose real tables via Developer editor" into a concrete drill-in + wizard UI).

## Task 0 — Green baseline + full read

- [x] **DevOps: confirm `MTM_Waitlist.sln` builds clean (0 errors/warnings)** (`dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`). | **Persona: DevOps Engineer**
- [x] **QA: full suite green** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
- [x] **Tech Lead: read ALL `WeekendProject/PromptFiles/*.md` (01–11 + prompt.md) and reconcile the mock-data master set** (requests, work centers/jobs, coil/flatstock/other parts, locations incl. ignored codes, requesters, request types/subtypes, stock snapshots). Confirm no mock data type is missing. | **Persona: Tech Lead**
**GATE: baseline green and weekend-file read complete before any edits.**

## Subphase 0.1 — Audit current mock-data sources & per-type field requirements

- [x] **Backend Engineer: inventory every mock producer** (`SampleDataService`, `SampleJobCoilCatalog`, `SampleInventoryLocationCatalog`, `SampleWaitlistRequestCatalog`, `SetupDataCatalog`, `WaitlistViewViewModel.AddRequestFields`, New-Request flow services) and record the field contract each mock part must satisfy. | **Persona: Backend Engineer**
- [x] **Full Stack Engineer: confirm per-request-type field requirements from code** (coil: requested coil/quantity in house/description/average coil weight/work center; flatstock: part/qty/location; other: description/work center; pickup/scrap/etc.) so each mock part carries required columns. | **Persona: Full Stack Engineer**

## Subphase 0.2 — Source canonical values from MTM_Receiving_Application + DB

- [x] **Database/Backend Engineer: parse `MTM_Receiving_Application` (checked-in `Database/` SQL + code/seeds) AND verify against the live receiving DB** for canonical part ids (`MMC########`/`MMF########`/`23-###-####`), descriptions, units, locations, and work-order formats. | **Persona: Database Engineer**
- [x] **Backend Engineer: confirm the mock "other" `23-###-####` scheme** is an artificial discriminator per distinct Other item (not a real part rule). | **Persona: Backend Engineer**
**Subphase 0.1/0.2 record (2026-09-06):**
- Mock producers audited: `SampleDataService`, `SampleJobCoilCatalog`, `SampleInventoryLocationCatalog`, `SampleWaitlistRequestCatalog`, `SetupDataCatalog`, `WaitlistViewViewModel.AddRequestFields`, New-Request flow services, and `Assets/Config/waitlist-request-types.json`.
- Per-request-type field contract each mock part row must satisfy: **Coil** = part (`MMC`) + requested coil + quantity in house + description + avg coil weight + work center; **Flatstock** = part (`MMF`) + qty/location; **Other** = `23-###-####` discriminator + description/work center; **Pickup/Scrap** = fields as modeled in `AddRequestFields`.
- Canonical values sourced from `MTM_Receiving_Application` checked-in SQL (real `MMC########`/`MMF########` ids) plus the app's work-center/location conventions; `23-###-####` is an artificial per-Other discriminator, not a real part rule. (Live receiving DB was not reachable in this environment; checked-in artifacts were used.)
- **Correction (2026-09-06): `Assets/Config/waitlist-request-types.json` is NOT mock data.** It is the **real, authoritative request-type/subtype catalog** that drives the actual New-Request flow. `NewRequestFlowService.LoadRequestTypesAsync()` reads it **unconditionally** — there is NO `Feature.InforVisualMockData` / `Feature.RecvMockData` gate around it — and `NewRequestJobTypeViewModel.OnNavigatedTo` consumes it on **every** wizard run (mock or live), only passing rows through `ApplyActiveJobEligibility` (which filters on coil/job state, not mock mode). It is schema-heavy config: stable GUIDs, concrete WinUI control type names, `centerDataGridFields`, and per-subtype `flow`/`promptText`/`minLength`/`maxLength`/`imagePath`. Treat it as real runtime config to be migrated to **non-mock** reference tables (see Subphase 0.6), NOT as a mock producer.

## Subphase 0.3 — MySQL mock-master tables + registry

- [x] **Database Engineer: create the mock `mock_*` tables in `mtm_waitlist`** per the weekend-file master set (e.g. `mock_parts`, `mock_work_orders`, `mock_locations`, `mock_requesters`, `mock_request_types`, `mock_inventory`), each carrying required columns; follow the repo database naming rules and mirror the app's Migration/Bootstrap artifact layout. | **Persona: Database Engineer**
- [x] **Database Engineer: create a registry table (e.g. `mock_master_tables`)** that lists each mock table with its UI-readable name + plain-language description (for the Developer settings dropdown); seed it with all mock tables. | **Persona: Database Engineer**
- [x] **Database Engineer: seed realistic values** (from Subphase 0.2) into each `mock_*` table, and update `Database/Bootstrap/update_table_descriptions.sql` in the same change. | **Persona: Database Engineer**

## Subphase 0.4 — Service layer: catalogs read from mock tables

- [ ] **Backend Engineer: add a MySQL-backed mock-master read service** and route the C# sample catalogs (`SampleDataService`, `SampleJobCoilCatalog`, `SampleInventoryLocationCatalog`, `SampleWaitlistRequestCatalog`, `SetupDataCatalog`, `SampleAverageCoilWeightCatalog`) to read from the `mock_*` tables when the relevant mock toggle is ON (InforVisual for Infor/coil/inventory flows; Recv for receiving/flatstock), so edits take effect on next load. NOTE: `Assets/Config/waitlist-request-types.json` is NOT a mock catalog (it is the real request-type catalog; see Subphase 0.6) and is intentionally excluded here. | **Persona: Backend Engineer**
- [x] **Backend Engineer: add a mock-master read/write service** for the Developer settings editor (enumerate tables via the registry; read/write rows). NOTE: DB access is SP-only (no inline SQL). Reads route through per-table get SPs (`sp_mock_<table>_get`); writes use the per-table insert/update/delete SPs. Real (non-mock) `waitlist_request_types`/`waitlist_request_subtypes` are NOT part of this mock service — they have their own dedicated SPs + a separate real-catalog service. | **Persona: Backend Engineer** — verified 2026-09-06: `MockMasterDataService` now exposes registry enumeration, per-table row reads, `GetTableColumnsAsync` (via `sp_mock_master_table_columns_get`), and table-driven `AddTableRowAsync`/`UpdateTableRowAsync`/`DeleteTableRowAsync` dispatching to the exact per-table insert/update/delete SPs (editable-column contracts confirmed against the generated SPs). Fixed the registry get SP name to `sp_mock_master_tables_registry_get`. `MockMasterDataServiceTests` (9) pass; live round-trip on `mock_requesters` (insert→read→update→delete) verified; full suite green (390 passed).

## Subphase 0.5 — Developer Settings page (separate from normal Settings)

- [ ] **Frontend/Backend Engineer: add a Developer Settings button to the main navigation UI above Settings** (role-gated like manage-settings) and move all existing Developer-related settings cards into this new page, separating normal and developer settings. | **Persona: Frontend Engineer**
- [ ] **Frontend/Backend Engineer: on the new Developer Settings page, add the mock-master editor** — a dropdown populated by the registry UI-readable names, showing each selected table's plain-language description, and an editable grid (add/edit/delete) per mock table. | **Persona: Frontend Engineer**
- [ ] **QA Engineer: tests** for registry enumeration, per-table CRUD, description display, role gating, and that catalog reads reflect DB edits; run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.6 — DB-driven real request-type catalog (waitlist-request-types.json is NOT mock)

> Finding (2026-09-06): `Assets/Config/waitlist-request-types.json` is the **real, authoritative request-type/subtype catalog**, not mock sample data. It is loaded unconditionally by `NewRequestFlowService.LoadRequestTypesAsync()` (no mock-toggle gate) and consumed by `NewRequestJobTypeViewModel` on every wizard run. It carries stable GUIDs, WinUI control type names, `centerDataGridFields`, and per-subtype `flow`/`promptText`/`minLength`/`maxLength`/`imagePath`. Goal (per user, "instead of using JSONs at all"): stop shipping/reading this catalog from JSON and make **MySQL 5.7 reference tables in `mtm_waitlist`** the source of truth for the real flow.

- [x] **Database Engineer: design + create schema-complete reference tables in `mtm_waitlist`** for the real request-type/subtype catalog (`waitlist_request_types`/`waitlist_request_subtypes`, `Database/Tables/28_…`/`29_…`); follow repo DB naming rules and the Migration/Bootstrap artifact layout. | **Persona: Database Engineer** — verified 2026-09-06: tables created in live DB (`mtm_waitlist`), `AllTables.sql` + `update_table_descriptions.sql` updated.
- [x] **Database Engineer: seed the reference tables** from the exact rows in `Assets/Config/waitlist-request-types.json` (all 8 request types incl. Forklift Assist and every subtype) so the DB matches the current JSON 1:1, and update `Database/Bootstrap/update_table_descriptions.sql` in the same change. | **Persona: Database Engineer** — verified 2026-09-06: `Database/Seeds/seed_waitlist_request_catalog` + `AllSeeds.sql`; live DB shows 8 types / 24 subtypes matching JSON 1:1.
- [x] **Backend Engineer: add a DB read service** for the request-type catalog (types + subtypes with control/grid-field/validation/image columns) and change `NewRequestFlowService.LoadRequestTypesAsync()` to read from it; keep `NewRequestFlowRules.GetDefaultTypes()` only as the empty-DB/unreachable fallback. | **Persona: Backend Engineer** — verified 2026-09-06: `RequestTypeCatalogService`/`IRequestTypeCatalogService` reads `waitlist_request_types`+`waitlist_request_subtypes`; `NewRequestFlowService` now reads from DB with `GetDefaultTypes()` fallback; DI registered; `RequestTypeCatalogServiceTests` passes; live-DB query returns 8 types/24 subtypes.
- [x] **Backend Engineer: expose the request-type/subtype tables through the Developer-settings read/write service** (same registry pattern used for `mock_*`, so request types/subtypes are editable alongside the mock tables). | **Persona: Backend Engineer** — verified 2026-09-06: `DeveloperEditableCatalogService`/`IDeveloperEditableCatalogService` (Core) returns the unified Developer-editable table list = the `mock_*` tables (from `mock_master_tables_registry` via `MockMasterDataService`) **plus** the two real request tables (`waitlist_request_types`/`waitlist_request_subtypes`), each flagged by `DeveloperTableSourceKind` (Mock vs RealCatalog) so the editor routes reads/writes to `MockMasterDataService` (per-table SPs) or `RequestTypeEditorService` (dedicated real-catalog SPs). DI-registered; `DeveloperEditableCatalogServiceTests` (3) pass; build clean; full suite green (437 passed). Grid editor UI (Subphase 0.5/0.6) still to build.
- [ ] **Frontend Engineer: extend the Developer Settings editor** (or add a sibling editor) so each real request-type/subtype table is selectable and its rows (subtype name, control, flow, validation, grid fields, image path) are editable in the grid. | **Persona: Frontend Engineer**
- [ ] **QA Engineer: tests** that the real catalog loads from DB (types/subtypes + grid fields + validation), DB edits are reflected on next load, `GetDefaultTypes()` fallback works when the DB is empty/unreachable, and the JSON file is no longer the runtime source; run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: `mock_*` tables + registry exist and are seeded; C# sample catalogs read from them under the existing mock toggles; the real request-type/subtype catalog is migrated to schema-complete MySQL reference tables and read by the New-Request flow (JSON retired as the runtime source, defaults kept only as fallback); a role-gated Developer Settings page exists in the main nav above Settings (developer cards moved there) and lists/describes/edits each mock table and the real request-type tables; New-Request mock data uses proper part ids/locations/work orders; no mock type missing; full suite green.**

---
> **Out-of-scope work & architecture notes moved to `WeekendProject/ChangeLog.md`** (see its
> "For developers / implementation notes" appendix): SP-only DB access decision, real-vs-mock catalog
> separation, the DB + SP layer added, and the C# service-layer groundwork completed on 2026-09-06.
> `ChangeLog.md` is the weekend project's end-user changelog (Keep a Changelog format).
