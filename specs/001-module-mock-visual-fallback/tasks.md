---
description: "Task list for Module_Mock — Automatic Infor Visual Read Fallback"
---

# Tasks: Module_Mock — Automatic Infor Visual Read Fallback

**Feature**: `001-module-mock-visual-fallback`
**Input**: Design documents from `/specs/001-module-mock-visual-fallback/`
**Prerequisites**: `plan.md` (revision 2), `spec.md` (7 user stories · 28 FRs · 16 SCs), `research.md` (R1–R15), `data-model.md`, `contracts/` (`visual-read-fallback.md`, `mock-service-http-api.md`, `mock-service-configuration.md`), `quickstart.md`

**Execution-side companion (do not duplicate)**: `WeekendProject/Module_Mock/Tasks.md` is the repo's persona-tagged
execution checklist for this same workstream (phases 0–8, File/Line refs, personas). This file is the spec-driven,
dependency-ordered task list; the two are cross-linked, not copies. Grounding inventories (read-only inputs, not
modified): `WeekendProject/Module_Mock/Discovery/01-MockLogic-Inventory.md`, `Discovery/02-InforVisual-ReadShapes.md`,
`Discovery/03-Hardcoded-MySQL-Sql.md`.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and delivered
independently. **Verification is mandatory** — the ratified constitution (`.specify/memory/constitution.md`, v1.1.1,
Principle VI *Evidence-Based Verification Gates*) makes verification a gate, so verification tasks are included even
though the generic template marks tests optional. Verification tasks are marked `(verification)`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: `US1`…`US7` — required on every user-story-phase task; omitted in Setup, Foundational, and Polish
- Every task names an **exact file path**
- **Story priorities** (from `spec.md`): US1 = P1, US2 = P1, US3 = P2, US4 = P2, US5 = P3, US6 = P3, US7 = P3

## Path Conventions

- WinUI 3 app project root: repository root (`MTM_Waitlist.csproj`); XAML Views stay in `Module_*/Views/`
- Per-module class libraries: `MTM_Waitlist.<Module>/`
- **Two new projects**: `MTM_Waitlist.Mock/` (class library) and `MTM_Waitlist.Mock.Service/` (WinUI 3 tray app)
- **One new database**: `mtm_mock`, artifacts under `Database/Mock/` (file-per-artifact)
- Tests: single project `MTM_Waitlist.Tests/`, namespaces mirror production (`Module_Mock/`, `Module_Mock_Service/`)
- Build gate: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → 0 warnings / 0 errors
- Test gate: `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`
  (live MySQL integration additionally gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization — the two new projects, the `Database/Mock/` artifact tree, test folders, and the
baseline record. No user-story behavior is produced here.

- [x] T001 Capture the pre-change baseline: run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`; record warning/error counts and the test pass count in `specs/001-module-mock-visual-fallback/quickstart.md` §0 (verification)
- [x] T002 [P] Create the `MTM_Waitlist.Mock` class library project `MTM_Waitlist.Mock/MTM_Waitlist.Mock.csproj` (`net10.0-windows10.0.19041.0`, no XAML Views) referencing `MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`, and add it to `MTM_Waitlist.sln`
- [x] T003 [P] Create the `MTM_Waitlist.Mock.Service` WinUI 3 desktop app project `MTM_Waitlist.Mock.Service/MTM_Waitlist.Mock.Service.csproj` (unpackaged `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `RuntimeIdentifier=win-x64`) with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and add it to `MTM_Waitlist.sln`
- [x] T004 [P] Create the `Database/Mock/` artifact tree — `Database/Mock/Bootstrap/`, `Database/Mock/Tables/`, `Database/Mock/StoredProcedures/`, `Database/Mock/Seeds/`, `Database/Mock/Validation/`, plus empty master lists `Database/Mock/AllTables.sql`, `Database/Mock/AllSPs.sql`, `Database/Mock/AllSeeds.sql`
- [x] T005 [P] Create the test namespaces/folders `MTM_Waitlist.Tests/Module_Mock/` and `MTM_Waitlist.Tests/Module_Mock_Service/`
- [x] T006 Add project references to `MTM_Waitlist.Mock/MTM_Waitlist.Mock.csproj` from `MTM_Waitlist.csproj`, `MTM_Waitlist.Setup/MTM_Waitlist.Setup.csproj`, `MTM_Waitlist.Waitlist.View/MTM_Waitlist.Waitlist.View.csproj`, and `MTM_Waitlist.Settings/MTM_Waitlist.Settings.csproj`

**Checkpoint**: Both projects build; `Database/Mock/` exists; baseline recorded.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The additive `mtm_mock` cache (tables → procedures → seed) and the service-app core (models, durable
configuration + DPAPI credential, shape catalog, refresh-engine skeleton). These block every user story.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

**DB naming rules (apply to every task below)**: per the locked ruleset (`Database/Database-Ruleset.md`,
`.github/instructions/database-schema-rules.instructions.md`) every artifact ships **file-per-artifact** as
`<name>/create.sql` + a matching `<name>/rollback.sql` in the **same change**, and is registered in
`Database/Mock/AllTables.sql` / `AllSPs.sql` / `AllSeeds.sql` and in `Database/Mock/Bootstrap/update_table_descriptions.sql`.
Table columns follow the ruleset: PK `id`, `_utc` timestamps (`refreshed_utc`), `is_`-prefixed flags
(`is_seed_content`), and `uq_` unique-key index names. The initial schema uses **only** `PRIMARY` and `uq_` indexes
(no `idx_`); `data-model.md` §3 is the authoritative index list. Engine: `ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci`.

**Implicit prerequisites (C5)**: T010–T013 follow the same artifact pattern established by T009; T016–T024 require
T015 (the reference refresh procedure) and T009–T013 (the tables). These dependencies are inherent in the `[P]` markers
(parallel across *different* files once the pattern exists) — a parallel run must complete T009 and T015 before fanning out.

### Subphase 2.1 — `mtm_mock` database, mirror tables, and stage twins

- [x] T007 [P] Create the `mtm_mock` database bootstrap `Database/Mock/Bootstrap/create_database.sql` + `Database/Mock/Bootstrap/rollback.sql` (re-runnable; creates the DB if absent, `utf8mb4` / `utf8mb4_unicode_ci`; no dependency on `mtm_waitlist`) — this dedicated cache is separate from the app's own store and is never authoritative for internal application data (FR-027)
- [x] T008 [P] Create the mandatory `mtm_mock` maintenance file `Database/Mock/Bootstrap/update_table_descriptions.sql` (table/column descriptions for every `mtm_mock` object; updated in every later schema change)
- [x] T009 [P] Create mirror table + stage twin for shape 1 in `Database/Mock/Tables/visual_work_order_lookup_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_work_order_lookup_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.1 (columns: `id`, `normalized_work_order`, `part_number`, `description`, `work_center`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_work_order_lookup_result_work_order`). **Record the DDL-review deviation note** for the rules-listed banned word `work_order` (R9) in the artifact's header comment.
- [x] T010 [P] Create mirror table + stage twin for shape 2 in `Database/Mock/Tables/visual_operation_sequences_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_operation_sequences_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.2 (columns: `id`, `normalized_work_order`, `part_number`, `sequence_number`, `description`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_operation_sequences_result_lookup_key`)
- [x] T011 [P] Create mirror table + stage twin for shape 3 in `Database/Mock/Tables/visual_subordinate_parts_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_subordinate_parts_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.3 (columns: `id`, `normalized_work_order`, `parent_part_number`, `sequence_number`, `category`, `part_number`, `description`, `location`, `user8`, `on_hand_quantity`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_subordinate_parts_result_lookup_key`)
- [x] T012 [P] Create mirror table + stage twin for shape 4 in `Database/Mock/Tables/visual_inventory_locations_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_inventory_locations_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.4 (columns: `id`, `part_number`, `location`, `on_hand_quantity`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_inventory_locations_result_location`)
- [x] T013 [P] Create mirror table + stage twin for shape 5 in `Database/Mock/Tables/visual_disposition_input_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_disposition_input_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.5 (columns: `id`, `work_order`, `part_number`, `work_order_status`, `open_work_order_quantity`, `finished_goods_quantity`, `has_outside_vendor_operation`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_disposition_input_result_lookup_key`)
- [x] T014 Register all five mirror tables and their stage twins in `Database/Mock/AllTables.sql` and `Database/Mock/Bootstrap/update_table_descriptions.sql`

### Subphase 2.2 — `mtm_mock` stored procedures (`sp_visual_<shape>_{refresh,get}`)

- [x] T015 Create the reference refresh procedure `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_refresh/{create.sql,rollback.sql}` — truncate the `_stage` twin, load the complete new result (`refreshed_utc = UTC_TIMESTAMP()`, `is_seed_content = 0`), validate the load, then perform the **atomic multi-table `RENAME TABLE` swap inside the procedure** (`visual_x_result` → `_prev`, `_stage` → live, `_prev` → `_stage`); on any error leave the live table untouched (`research.md` R1, `data-model.md` §3.6, FR-006/SC-005)
- [x] T016 [P] Create `Database/Mock/StoredProcedures/sp_visual_operation_sequences_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [x] T017 [P] Create `Database/Mock/StoredProcedures/sp_visual_subordinate_parts_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [x] T018 [P] Create `Database/Mock/StoredProcedures/sp_visual_inventory_locations_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [x] T019 [P] Create `Database/Mock/StoredProcedures/sp_visual_disposition_input_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [x] T020 [P] Create the parameterized read `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_get/{create.sql,rollback.sql}` returning the live mirror's exact column shape for shape 1 (`normalized_work_order` in → `part_number`, `description`, `work_center` out)
- [x] T021 [P] Create `Database/Mock/StoredProcedures/sp_visual_operation_sequences_get/{create.sql,rollback.sql}` for shape 2 (`normalized_work_order`, `part_number` in → `sequence_number`, `description` out)
- [x] T022 [P] Create `Database/Mock/StoredProcedures/sp_visual_subordinate_parts_get/{create.sql,rollback.sql}` for shape 3 (`p_part_number` exposes the operation's part context → `category`, `part_number`, `description`, `location`, `user8`, `on_hand_quantity` out, per `data-model.md` §3.3 note)
- [x] T023 [P] Create `Database/Mock/StoredProcedures/sp_visual_inventory_locations_get/{create.sql,rollback.sql}` for shape 4 (`part_number` in → `part_number`, `location`, `on_hand_quantity` out)
- [x] T024 [P] Create `Database/Mock/StoredProcedures/sp_visual_disposition_input_get/{create.sql,rollback.sql}` for shape 5 (`work_order`, `part_number` in → `work_order_status`, `open_work_order_quantity`, `finished_goods_quantity`, `has_outside_vendor_operation` out)
- [x] T025 Register all ten `sp_visual_*` procedures in `Database/Mock/AllSPs.sql`
- [x] T026 Add the baseline seed content for all five mirror tables (rows with `is_seed_content = 1`) in `Database/Mock/Seeds/seed_visual_mirror_baseline/{create.sql,rollback.sql}` and register it in `Database/Mock/AllSeeds.sql`, so a fresh install serves a usable result before the first refresh (FR-017)

### Subphase 2.3 — Service-app core (configuration, credential, shape catalog, refresh-engine skeleton)

- [x] T027 [P] Create the service models in `MTM_Waitlist.Mock.Service/Models/` — `ServiceConfiguration.cs`, `RefreshRunRecord.cs`, `BackupPolicy.cs`, `BackupArtifact.cs`, `RestoreOutcome.cs`, `SharedCredential.cs` (fields per `data-model.md` §4/§5/§6/§7/§9); shape definitions reuse the shared `MTM_Waitlist.Mock/Models/VisualReadShape.cs` (`data-model.md` §2) — there is no service-local `RefreshShapeDefinition` duplicate
- [x] T028 Create `MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs` — durable service-local configuration (all-or-nothing atomic save, value validation at save time) with the shared credential stored **only** as a DPAPI `CurrentUser` protected blob (generate-once, rotate-only, never displayed or logged); per `contracts/mock-service-configuration.md` §1/§2, FR-012/FR-026
- [x] T029 [P] Create `MTM_Waitlist.Mock.Service/Services/RefreshShapeCatalogProvider.cs` — the catalog for the five initial shapes (`shapeKey`, `module`, `sourceScriptRelativePath`, `inputParameters`, `outputColumns`, derived `mirrorTable`/`stageTable`/`getProcedure`/`refreshProcedure`, `refreshIntervalMinutes`, `isEnabled`) plus startup validation that the script, tables, and both procedures exist and `outputColumns` matches the recorded live projection — **performed by calling `sp_visual_read_shape_metadata_get`** (`data-model.md` §12), never by inline SQL; invalid shapes are excluded and reported, never crash the service (FR-020, constitution III, `contracts/mock-service-configuration.md` §3)
- [x] T030 Create `MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs` **skeleton** — catalog-driven per-shape refresh that calls `CALL sp_visual_<shape>_refresh`, enumerates outcomes as `RefreshRunRecord`, and **skips + logs** when Infor Visual is unreachable, leaving the live snapshot untouched (FR-008); the scheduling loop itself is completed in US3

**Checkpoint**: `mtm_mock` deploys cleanly; a live refresh + get round-trips on a dev MySQL; a fresh mirror has usable
seed data; the service builds and loads its configuration and shape catalog. User-story implementation can now begin.

---

## Phase 3: User Story 1 — Internal application data is always live (Priority: P1) 🎯 MVP

**Goal**: Delete every short-circuit, demo toggle, routing stack, and DB-backed mock family so the app's own store,
the floor/WIP store, and the receiving store are always read and written live. This is the reported production defect
(a setup save is acknowledged but the work-center card never updates; waitlist lifecycle actions are skipped).

**Independent Test**: Save a workstation setup and confirm the work-center card shows the new assignment without a
manual refresh or restart; run a full waitlist request lifecycle (accept, note, status change, cancel) and confirm
every action persists and survives an app restart and is visible to a second session; confirm no demo/sample control
exists in Settings.

> **⚠️ Removal ordering (R14: additive → switch → remove)**: the tasks that delete the **shared sample contracts**
> (`ISampleDataService`/`Sample*Catalog`, T042) and the helper-server mock overloads (US2 T057) are coupled to the
> Visual caller rerouting in **US2**. Do not delete the shared sample types before their last consumers are rerouted
> (T053–T055). The **internal-store** remediation tasks (T031–T033) have no such constraint and should be done first —
> they alone fix the reported defect.

- [x] T031 [US1] Remove the `MtmWaitlist` mock short-circuit in `MTM_Waitlist.Setup/Services/SetupPersistenceService.cs` — delete `SaveMockAsync` and the mock branch so `SaveAsync` always writes the real `mtm_waitlist` store (root cause of the setup-save defect; FR-001, SC-001)
- [x] T032 [US1] Remove the mock gating in `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` — delete `IsMockDataEnabled()` and every skip-read/skip-persist branch so `sp_waitlist_request_list` and all accept/note/status/cancel/audit persists always execute (FR-001, SC-002)
- [x] T033 [US1] Remove the `Feature.RecvMockData` gating in `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` (including the `IsMockDataEnabledAsync(target)` path that gates the `MtmWaitlist` target) so no MySQL target is ever mock-gated (FR-001)
- [x] T034 [P] [US1] Delete the mock toggle keys and service — `MTM_Waitlist.Core/Services/MockSettingKeys.cs`, `MTM_Waitlist.Core/Services/MockToggleService.cs` and its contract — and remove every raw-key read of `Feature.InforVisualMockData` / `Feature.RecvMockData` (FR-003, FR-014)
- [x] T035 [P] [US1] Delete the mock routing / auto-force / monitoring stack from `MTM_Waitlist.Core/Services/` (`MockRoutingService`, `MockRoutingCoordinator`, `MockConfigurationService`, `MockRoutingRefreshService`, `MockRoutingMonitorService`, `MockModePollingHost`, `MockFallbackDebouncer`, `MockModeChangeDetector`, `MockModeSummaryProvider`, `DispatcherPollScheduler`) plus `MockModeToastCoordinator` and their contracts/models, and remove the wiring from `Module_Core/Views/ShellPage.xaml.cs` and `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (FR-014)
- [x] T036 [US1] Remove the `mockAction` short-circuit branch from `MTM_Waitlist.Core/Services/SqlHelperServer.cs` (depends on T034)
- [x] T037 [P] [US1] Remove the Settings "Mock Data" surface — `Module_Settings/Views/SettingsPage.xaml` (`MockDataExpander`, the `UseMockData` `ToggleSwitch`) and `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` (`UseMockData`, `OnUseMockDataChanged`, `IsMockDataPanelVisible`, the `IMockToggleService` injection) — so no demo control exists (US1 acceptance 4, FR-014)
- [x] T038 [P] [US1] Drop the DB-backed demo tables — `Database/Tables/20_mock_master_tables_registry/`, `22_mock_work_orders/`, `23_mock_work_centers/`, `24_mock_locations/`, `25_mock_requesters/`, `26_mock_inventory_locations/`, `27_mock_request_types/`, and `mock_parts` — as `rollback.sql` artifacts, and remove their entries from `Database/Tables/AllTables.sql` and `Database/Bootstrap/update_table_descriptions.sql` (FR-014)
- [x] T039 [P] [US1] Drop the `sp_mock_*` procedures (including `sp_mock_master_table_columns_get`) from `Database/StoredProcedures/sp_mock_*/` as `rollback.sql` artifacts and remove them from `Database/StoredProcedures/AllSPs.sql` (FR-014)
- [x] T040 [P] [US1] Remove the demo seed artifact `Database/Seeds/seed_mock_master_default/{create.sql,rollback.sql}` and its registration in `Database/Seeds/AllSeeds.sql`
- [x] T041 [US1] Delete `MockMasterDataService` and its contract/models/validator (`IMockMasterDataService`, `MockMasterRowEditValidator`, `MockMasterTableDefinition`, `MockMasterColumnDefinition`) and their DI registration in `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (FR-014)
- [x] T042 [US1] Delete the in-app sample catalogs and their contract — `MTM_Waitlist.Waitlist.View/Services/SampleDataService.cs`, `SampleWaitlistRequestCatalog.cs`, `SampleInventoryLocationCatalog.cs`, `SampleAverageCoilWeightCatalog.cs`, `MTM_Waitlist.Waitlist.NewRequest/Services/SampleJobCoilCatalog.cs`, `MTM_Waitlist.Setup/Services/SetupDataCatalog.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `MTM_Waitlist.Waitlist.NewRequest/Models/WaitlistCoilInfo.cs`, and `MTM_Waitlist.Core/Contracts/Services/ISampleDataService.cs` (FR-014; depends on US2 T053–T055 rerouting their consumers)
- [x] T043 [US1] Rework/delete the tests that exercise the retired mock behavior — the `Mock*`/`Sample*`/`HelperServers` suites under `MTM_Waitlist.Tests/Core/`, `MTM_Waitlist.Tests/Module_Waitlist/`, `MTM_Waitlist.Tests/Module_Setup/`, `MTM_Waitlist.Tests/Module_Settings/` and the `FakeSampleDataService` double in `MTM_Waitlist.Tests/Module_Settings/TestDoubles.cs` (SC-015: 0 tests reference a retired system)
- [x] T044 [US1] (verification) Add/rework assertions that internal stores are always live — `MTM_Waitlist.Tests/Module_Setup/SetupPersistenceServiceTests.cs` (save always hits the real store) and `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestServiceTests.cs` (every lifecycle action persists) — then run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` (0w/0e) and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`

**Checkpoint**: US1 is independently functional — the work-center card updates after a save, the lifecycle persists,
and no demo control exists. The retired demo systems are gone from code and database.

> **Phase 3 execution note (2026-09-10, `/speckit.implement`)** — US1 + T057 landed together; build gate
> **0 warnings / 0 errors** and test gate **584 total / 0 failed / 571 passed / 13 skipped** (baseline 605/0/571+34/17).
>
> - **Deleted (production, 38 files)**: the whole mock routing/auto-force/monitoring stack and its contracts/models
>   (`MockRouting*`, `MockMode*`, `MockConfigurationService`, `MockFallbackDebouncer`, `MockMasterDataService` +
>   validator, `MockSettingKeys`, `MockToggleService`, `IPollScheduler`), the app-side `Services/MockMode/*`, the
>   `SqlHelperServer` mock router (dead once the mock branches went; it existed only to route mock-vs-backend), the
>   sample catalogs (`SampleDataService`, `SampleWaitlistRequestCatalog`, `SampleInventoryLocationCatalog`,
>   `SampleAverageCoilWeightCatalog`, `SampleJobCoilCatalog`, `SetupDataCatalog`) and `ISampleDataService`.
> - **Retained deliberately (not sample catalogs)**: `SampleOrder` and `WaitlistCoilInfo`. Both are live models —
>   `SampleOrder` is the waitlist list/card/detail display model populated from real requests
>   (`WaitlistViewViewModel.CreateSessionOrder`) and `WaitlistCoilInfo` is the coil DTO returned by
>   `ICoilAvailabilityService`; deleting them would have rewritten the whole waitlist card UI (T103's retired-symbol
>   audit lists `Sample*Catalog`/`ISampleDataService`, not these).
> - **DB retirement (T038–T040)**: each retired table/procedure artifact lost its `create.sql` and **kept its
>   `rollback.sql`**, which is the matching drop artifact named by these tasks, so a DBA can still promote the
>   removal. The eight `mock_*` table blocks, the 33 `sp_mock_*` procedure blocks, and the `seed_mock_master_default`
>   block were removed from `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`, and `update_table_descriptions.sql`, and a
>   "Retired objects" section recording the drops was appended to `update_table_descriptions.sql` (constitution III
>   requires that maintenance file to move in the same change).
> - **Test rework (T043)**: 18 retired-mock suites deleted; surviving suites were reworked onto the live-only
>   contract (`WaitlistRequestServiceTests`, `AverageCoilWeightServiceTests`, `CoilAvailabilityServiceTests`,
>   `InventoryLocationFilteringTests`, `SettingsViewModelTests`, the Waitlist view-model tests, the Module_Setup
>   workflow tests — the last now own their lookup fixture data in
>   `MTM_Waitlist.Tests/Module_Setup/Services/SetupLookupFixtureData.cs`).
> - **New coverage (T044)**: the reworked suites assert the live contract directly —
>   `SubmitAsync_AlwaysPersistsNewRequestToDatabase`, `RefreshFromDatabaseAsync_AlwaysQueriesTheDatabase`,
>   `TransitionStatusAsync_PersistsStatusUpdateToDb`, `AuditTrail_PersistsAuditEntriesToDb`,
>   `CancelOwnRequestAsync_PersistsCancelledRowToDb`, and `AverageCoilWeightServiceTests.Resolve_RunsQuery_AndFormatsAverage`.
> - **T057** needed no separate change beyond T036: `MySqlHelperServer`'s mock overloads and `IsMockDataEnabledAsync`
>   were already removed by T033, and the `SqlHelperServer` mock overloads lived in the class T036 deletes.

---

## Phase 4: User Story 2 — Automatic cached fallback for external reads (Priority: P1) 🎯 MVP

**Goal**: Detect Infor Visual unreachability automatically and serve each of the five Visual read shapes from the
`mtm_mock` mirror with an identical result shape — with no user action and no manual mode — and return to live reads
when the source recovers. Owns the `MTM_Waitlist.Mock` library and the routing of its five call sites.

**Independent Test**: Make Infor Visual unreachable, exercise all five read journeys, and confirm each returns a
complete, correctly shaped result with no error and no partial rows; restore the source and confirm live reads return
without restarting the app; confirm a reachable-but-empty live result is never replaced by cached data.

- [x] T045 [P] [US2] Create the fallback contracts `MTM_Waitlist.Mock/Contracts/IVisualReachabilityDetector.cs` and `MTM_Waitlist.Mock/Contracts/IVisualReadFallback.cs` exactly as specified in `contracts/visual-read-fallback.md` §1/§2
- [x] T046 [P] [US2] Create the fallback model `MTM_Waitlist.Mock/Models/CachedReadResult.cs` (`ServedFromLive | ServedFromCache` + `refreshed_utc` provenance); the detector state enum is `VisualReadStatus` (`Unknown | Live | Cached`), created with the status model in T065
- [x] T047 [US2] Implement `MTM_Waitlist.Mock/Services/VisualReachabilityDetector.cs` — an independent connectivity probe (no reference to the retired `MockRoutingService`/`MockModeChangeDetector`/`MockConfigurationService` stack) with 2-consecutive-failure → `Cached`, 1-success → `Live` hysteresis and backoff probing while cached (FR-002/FR-003, flapping edge case, `research.md` R11)
- [x] T048 [P] [US2] Implement shape 1 `MTM_Waitlist.Mock/Services/VisualWorkOrderLookupFallback.cs` — attempt live, on unreachability read `sp_visual_work_order_lookup_get`; return live rows **including an empty set**; surface non-unreachability errors (FR-024)
- [x] T049 [P] [US2] Implement shape 2 `MTM_Waitlist.Mock/Services/VisualOperationSequencesFallback.cs` (mirror `sp_visual_operation_sequences_get`)
- [x] T050 [P] [US2] Implement shape 3 `MTM_Waitlist.Mock/Services/VisualSubordinatePartsFallback.cs` (mirror `sp_visual_subordinate_parts_get`)
- [x] T051 [P] [US2] Implement shape 4 `MTM_Waitlist.Mock/Services/VisualInventoryLocationsFallback.cs` (mirror `sp_visual_inventory_locations_get`)
- [x] T052 [P] [US2] Implement shape 5 `MTM_Waitlist.Mock/Services/VisualDispositionInputFallback.cs` (mirror `sp_visual_disposition_input_get`)
- [x] T053 [US2] Route `MTM_Waitlist.Setup/Services/SetupLookupService.cs` through the fallbacks — `LookupWorkOrderFromBackendAsync` (shape 1), `GetSequencesFromBackendAsync` (shape 2), `GetSubordinatePartsFromBackendAsync` (shape 3) — removing the `*FromMockAsync` paths (FR-002/FR-004)
- [x] T054 [US2] Route `MTM_Waitlist.Waitlist.View/Services/WaitlistInventoryService.cs` `GetInventoryLocationsFromBackendAsync` through shape 4, keeping the caller-side on-hand ≥ 1 and ignored-location filtering unchanged
- [x] T055 [US2] Route `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs` `GetDispositionInputAsync` through shape 5, keeping `RequestDispositionStatusCodes` (Open = {R,U,F}, Closed = {C}, X never FG) as the only status authority and keeping the floor/WIP half reading the live WIP store (FR-018)
- [x] T056 [US2] Add the fallback DI registrations for the detector and the five `IVisualReadFallback` implementations in the `MTM_Waitlist.Mock` service-registration extension, wired from `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (depends on T047–T052)
- [x] T057 [US2] Remove the Visual/sample short-circuit branches — the `ExecuteReadOnlyQueueAsync` mock overloads in `MTM_Waitlist.Core/Services/SqlHelperServer.cs` and the mock overloads in `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` — after T053–T055 reroute their consumers (FR-002/FR-014)
- [x] T058 [US2] (verification) Add fallback-parity tests in `MTM_Waitlist.Tests/Module_Mock/VisualReadFallbackParityTests.cs` — for all five shapes: live vs mirror are structurally identical (column set + ordering), unreachable → mirror rows served, and a reachable-but-legitimate-empty live result **is not** replaced by cache — `VisualReachabilityDetectorTests.cs` for the hysteresis/backoff state machine, plus a caller test proving a routed caller cannot distinguish the source, and an assertion that the cache is **never** consulted for an internal-store read (FR-027, constitution II); then run the build and full test suite gates (verification)

**Checkpoint**: US1 **and** US2 both work — with Infor Visual unreachable all five reads serve from `mtm_mock`; with it
up, live reads are used and return to live without a restart. This completes the P1 MVP.

---

## Phase 5: User Story 3 — The cache stays warm without anyone asking (Priority: P2)

**Goal**: The on-host service starts automatically at logon, sits in the notification area, and refreshes the mirror
on a configurable schedule — logging and skipping unreachable cycles without disturbing the last good snapshot.

**Independent Test**: Install and run the service on a host; observe a scheduled refresh complete for all enabled
shapes; make the source unreachable for one cycle and observe a logged skip with an unchanged `refreshed_utc` and the
next cycle still on schedule.

- [x] T059 [US3] Implement the tray-only, single-instance service lifetime in `MTM_Waitlist.Mock.Service/App.xaml.cs` — `OnLaunched` creates only a `WinUIEx.TrayIcon` and starts the background engines (no main window), single instance enforced with `AppInstance.FindOrRegisterForKey` + `RedirectActivationToAsync`, closing the settings window hides it, and the process exits only on an explicit Quit (`research.md` R3)
- [x] T060 [US3] Implement auto-start at logon — write/remove the per-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry from `MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs`, reconcile setting-vs-registry state at startup, and report a mismatch instead of ignoring it (FR-007, `research.md` R4)
- [x] T061 [P] [US3] Create `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` and consume every service UI/status string through `GetLocalized()` — no inline literals (constitution V)
- [x] T062 [US3] Complete the scheduling loop in `MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs` — global interval with per-shape overrides from `RefreshShapeCatalogProvider`, one cycle at a time (reject/guard re-entrancy), `skippedSourceUnreachable` recorded as a normal outcome, previous snapshot intact, next cycle attempted on schedule (FR-008, US3 acceptance 2/3/4)
- [x] T063 [US3] Add the durable service-local run record store `MTM_Waitlist.Mock.Service/Services/RefreshRunRecordStore.cs` (per-shape last-run outcome/timestamp, sanitized error text with no credential; deliberately **not** in MySQL per `data-model.md` §4) (FR-013)
- [x] T064 [US3] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/RefreshEngineTests.cs` — swap is atomic, a failed/skipped refresh leaves the live snapshot untouched, and a concurrent read during a refresh never observes a partial or empty mirror; then run the build and full test suite gates (verification)

**Checkpoint**: The service starts unattended, refreshes on schedule, and degrades to a logged skip when the source is down.

> **Phase 5 partial execution note (2026-09-10, `/speckit.implement`)** — the engine-level half of US3 landed
> (T062–T064); T059–T061 and the service composition root still depend on decisions not yet made. Build gate
> **0 warnings / 0 errors**; test gate **594 total / 0 failed / 581 passed / 13 skipped** (581 vs 571 = the 10 new
> scheduling + run-record tests).
>
> - **T062** — `RefreshEngine` now owns the loop: `RunScheduledAsync(TimeProvider)` keeps one cycle at a time behind
>   an `Interlocked` guard (a second request is rejected, not overlapped — the state the on-demand API reports as
>   `409`), each shape carries its own next-due time using `VisualReadShape.RefreshIntervalOverride` first and the
>   service's global interval otherwise, a skip still re-schedules the shape so cadence is kept, and an unexpected
>   loop error is logged and retried after a bounded delay instead of ending the loop (FR-008, US3 acceptance 2/3/4).
>   The ctor took two new **optional** parameters (`refreshInterval`, `minimumScheduledWait`) so existing callers and
>   tests are unaffected.
> - **T063** — `RefreshRunRecordStore` keeps the most recent run per shape in a durable service-local file (atomic
>   temp-file swap, corrupt-file tolerance, credential-shaped text redacted on write). Deliberately not in MySQL, per
>   `data-model.md` §4.
> - **T064** — `RefreshEngineTests` gained the scheduling coverage (per-shape override, not-rerun-before-interval,
>   rerun-after-interval, re-entrancy rejection while a cycle is held open, loop survives a skipped cycle and keeps
>   refreshing) and `RefreshRunRecordStoreTests` the durability/no-credential/corrupt-file coverage.
>   **Scope note:** "a concurrent read never observes a partial or empty mirror" is asserted at the engine seam — the
>   mirror writer is invoked exactly once per shape and never in a partial state — while the database-level proof of
>   the atomic `RENAME` swap remains the `Database/Mock/Validation/refresh_swap_smoke` script plus T118's live-DB
>   integration test (this environment has no reachable MySQL/Visual).
>
> **Still open in US3 — now resolved (2026-09-10, operator decision; see `research.md` R16).** `T113` must "enumerate
> each shape's driver inputs", and neither `spec.md` nor `plan.md` fixed where that input set comes from. The operator
> answered two questions directly and asked for **no logic change** beyond them:
>
> 1. **Scope** — the mirror covers **every open Infor Visual work order**: `WORK_ORDER.STATUS` in
>    `{R Released, U Unreleased, F Firmed}`, plus its operation sequences and subordinate parts. Closed (`C`) and
>    cancelled (`X`) orders are never cached. That is ~42,800 orders today (confirmed live data, 2026-09-08: C 68,278 ·
>    X 1,404 · U 42,138 · R 680 · F 0) and is exactly the set `RequestDispositionStatusCodes.OpenStatusCodes` already
>    treats as open. The five flattened wipe-and-swap mirrors stay as approved (R2) — no normalisation into shared
>    master-data blocks, and no merge/upsert.
> 2. **Cadence** — the shipped default is **eight fixed times a day, three hours apart, anchored at local midnight**:
>    00:00, 03:00, 06:00, 09:00, 12:00, 15:00, 18:00, 21:00 **server-local**.
>
> **Implemented for (2)**: `ServiceConfiguration.RefreshInterval` now defaults to 3 hours, `RefreshEngine` schedules
> on a local-midnight grid through an injectable `TimeProvider` (so due-times and the scheduler agree even when a cycle
> is slow or skipped), and `RefreshEngineTests` asserts the 3-hour slots and the per-shape override grid; the refreshed
> default is also asserted in `ServiceConfigurationStoreTests`.
>
> **Still to do for (1)**: the Infor-side read that selects `STATUS IN ('R','U','F')` is authored with `T113` (payload
> source) — `T113`'s "driver inputs" are the open work-order population, and `T114` (connection settings) + `T117`
> (service composition root) then unblock `T059`–`T061` (tray lifetime, autostart, strings).

---

## Phase 6: User Story 4 — Read-only fallback status indicator (Priority: P2)

**Goal**: While cached data is being served, the shell shows a non-interactive indicator stating that Infor Visual is
unreachable, that cached data is in use, and **how old** that cached data is — with nothing to click and no mode
change. It clears automatically when live reads resume.

**Independent Test**: Force the source offline and confirm the indicator appears, shows the cached-data age, and is
non-interactive (click, Enter, and tab all trigger nothing); restore the source and confirm it clears.

- [x] T065 [P] [US4] Create `MTM_Waitlist.Mock/Contracts/IReadStatusProvider.cs` and the models `MTM_Waitlist.Mock/Models/ReadStatusSnapshot.cs` + `MTM_Waitlist.Mock/Models/VisualReadStatus.cs` per `contracts/visual-read-fallback.md` §4 (state, last successful refresh, cached age, `IsSeedContentOnly`, per-shape last refresh)
- [x] T066 [US4] Implement `MTM_Waitlist.Mock/Services/ReadStatusProvider.cs` — tracks the detector state, exposes `Current` + a `Changed` event, and reports `CachedDataAgeUtc` (null when only seed content exists); data is never refused based on age (FR-005/FR-022)
- [x] T067 [US4] Create the non-interactive indicator `Module_Mock/Views/ReadStatusIndicator.xaml` + `Module_Mock/Views/ReadStatusIndicator.xaml.cs` — an `InfoBar` with `IsClosable="False"`, no action buttons and no command binding, bound to `IsCachedDataInUse` and the cached-data age, visible only while `Cached` (FR-003/FR-005, `research.md` R12)
- [x] T068 [P] [US4] Add the indicator string keys to `Strings/en-us/Resources.resw` (unreachable message, cached-data-in-use statement, cached-data age/seed display) and consume them via `GetLocalized()` — no inline literals (FR-005/FR-022)
- [x] T069 [US4] Host the indicator in the existing shell `Module_Core/Views/ShellPage.xaml` (no new navigation), register `IReadStatusProvider` in `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, and register any converter/resource the indicator needs in `App.xaml` in the same change (constitution V; a missing `x:Key`/`xmlns` causes a `WMC0001`/`WMC9999` failure)
- [x] T070 [US4] (verification) Add `MTM_Waitlist.Tests/Module_Mock/ReadStatusProviderTests.cs` covering the visibility rule (`visible ⇔ state == Cached`), the age value, the seed-content case, and that no public API can change the read mode; then run the build and full test suite gates (verification)

**Checkpoint**: Users can tell cached from live data, cannot change a data mode, and the indicator clears on recovery.

---

## Phase 7: User Story 5 — On-demand refresh and operational visibility (Priority: P3)

**Goal**: An authorized caller can request an immediate refresh and retrieve per-item last-run status over a
small credential-gated HTTP interface; the operator can configure and persist the service settings.

**Independent Test**: Request an immediate refresh through the service and observe it complete; request status and
observe per-item last-run results; make an unauthenticated request and observe a refusal.

- [x] T071 [P] [US5] Create `MTM_Waitlist.Mock/Contracts/IMockServiceRefreshClient.cs` and the request-result model `MTM_Waitlist.Mock/Models/RefreshRequestResult.cs` per `contracts/visual-read-fallback.md` §5
- [x] T072 [US5] Implement `MTM_Waitlist.Mock/Services/MockServiceRefreshClient.cs` — `POST /api/refresh` on the service API with the `X-MTM-Mock-Token` header; a missing service, missing credential, or error fails gracefully so the app continues on cached content (FR-025/SC-011, `contracts/mock-service-configuration.md` §5)
- [x] T073 [US5] Implement `MTM_Waitlist.Mock.Service/Api/ServiceApiEndpoints.cs` — `POST /api/refresh` (optional `shapeKeys`; `200` with per-shape outcomes including `skippedSourceUnreachable`; `400` unknown shape; `409` refresh in progress) and `GET /api/status` (per-shape and per-store last-run outcome/timestamp, `toolAvailable`, no secrets, no side effects); `/api/restore` and any equivalent path are **not routed** (`contracts/mock-service-http-api.md` §2/§3/§5, FR-023)
- [x] T074 [US5] Implement `MTM_Waitlist.Mock.Service/Api/SharedTokenAuthenticationHandler.cs` — constant-time comparison against the DPAPI-protected credential, `401 {"error":"unauthorized"}` for every endpoint, unauthorized attempts logged with source/method/path/timestamp only, and the credential never echoed in a response, header, error, log line, or status payload (FR-026/SC-010)
- [x] T075 [US5] Implement `MTM_Waitlist.Mock.Service/Services/ServiceApiHost.cs` — Kestrel host bound to `ApiSettings.BindAddress`/`ApiSettings.Port`, started by the same host the tray app creates; the API host must start with `RestoreService` absent/unregistered (the two are independent registrations) (FR-011/FR-012)
- [x] T076 [P] [US5] Implement the service settings surface `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml` + `.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` — refresh interval, API bind address/port, credential generate/rotate (write-only, never displayed), Visual connection details, and `mysqldump` path, with save-time validation and no partially written configuration (FR-012, `contracts/mock-service-configuration.md` §1/§2). **Record the shipped default for every setting** (refresh interval, per-store schedule/retention/destination, bind address/port) so SC-007/SC-008 are measured against a known baseline (`data-model.md` §5/§6).
- [x] T077 [P] [US5] Implement the service status surface `MTM_Waitlist.Mock.Service/Views/ServiceStatusPage.xaml` + `.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceStatusViewModel.cs` — per-shape last-refresh outcome/timestamp and per-store last-backup outcome/timestamp, and a clear report when the backup tool is unavailable (FR-013)
- [x] T078 [US5] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ServiceApiTests.cs` — unauthorized requests are refused 100% of the time, a credential-free `/api/status` payload contains no secret, an on-demand refresh returns per-shape outcomes, and `/api/restore` is unreachable; then run the build and full test suite gates (verification)

**Checkpoint**: Operators can force a refresh and see operational state; the network interface is token-gated and restore-free.

---

## Phase 8: User Story 6 — Backups and emergency restore of internal stores (Priority: P3)

**Goal**: Per-store configurable, periodic, restorable backups of all four MySQL stores, plus a host-only,
confirmation-gated emergency restore — never exposed on the network.

**Independent Test**: Configure a schedule for one store and observe a restorable artifact while the other stores back
up on their own schedules; with the tool absent observe a clear failure and no artifact; decline a restore prompt and
confirm no change, then accept it and confirm verified replacement.

- [x] T079 [US6] Implement `MTM_Waitlist.Mock.Service/Services/BackupEngine.cs` — one `mysqldump` invocation per store with `--host/--port/--user/--password-file --single-transaction --routines --databases <db> --result-file=<path>` (password only through an option file; `--result-file` mandatory to avoid the Windows PowerShell UTF-16 dump pitfall), per-store enable/schedule/retention/destination, an up-front tool-availability probe reporting `toolUnavailable` **without** recording an artifact, and success recorded only on exit 0 **and** a non-zero file (FR-009/FR-013/SC-008, `research.md` R7, `data-model.md` §6)
- [x] T080 [US6] Implement `MTM_Waitlist.Mock.Service/Services/RestoreService.cs` — host-only, callable only from a local UI action, gated by an explicit `ContentDialog` confirmation, and sequenced safety snapshot → `DROP DATABASE` + `CREATE DATABASE` (utf8mb4) → reload without `--force` → row-count verification → recorded outcome; an unconfirmed request changes nothing and a failed restore names the safety snapshot for recovery (FR-010/FR-023/SC-009, `research.md` R8, `data-model.md` §7)
- [x] T081 [US6] Add the backup endpoints `POST /api/backup` and `GET /api/backups` to `MTM_Waitlist.Mock.Service/Api/ServiceApiEndpoints.cs` (same file as T073; do not parallelize) — `toolUnavailable` returns `200` with no artifact, never a partial or zero-length artifact recorded as success (FR-013)
- [x] T082 [P] [US6] Add the per-store backup configuration section (enable, schedule, retention, destination) to `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml` + `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` — disabling one store must not change any other store's schedule or artifacts (FR-009, same files as T076; do not parallelize)
- [x] T083 [US6] Add the host-only restore surface (artifact picker + confirmation prompt + outcome display) to `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml`/`.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` (depends on T080; same files as T076/T082)
- [x] T084 [US6] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/BackupRestoreTests.cs` — per-store independence, a missing tool yields `toolUnavailable` with no artifact record, an unconfirmed restore is a no-op, a confirmed restore is a verified full replacement, and no code path reaches `RestoreService` from an HTTP request; then run the build and full test suite gates (verification)

**Checkpoint**: Every enabled store produces restorable artifacts on its own schedule, and restore is host-only and confirmation-gated.

---

## Phase 9: User Story 7 — Extend to a new external read shape (Priority: P3)

**Goal**: Publish the ordered, six-step playbook (each step naming its single artifact) so a maintainer who has never
worked on the cache can add a sixth read shape end-to-end without changing any existing shape or contract.

**Independent Test**: Hand the playbook to a maintainer who has not worked on the cache; they add a read shape
end-to-end and demonstrate its fallback while the existing five behave unchanged.

- [x] T085 [P] [US7] Publish the six-step playbook — reproduced from `contracts/mock-service-configuration.md` §4 (capture the read → mirror schema → procedures → service registration → in-app fallback → verify) into `WeekendProject/Module_Mock/Spec.md` §11 and `WeekendProject/Module_Mock/Plan.md` §7, cross-linked from `WeekendProject/Module_Mock/Tasks.md` (FR-016/FR-028)
- [x] T086 [US7] Implement the half-added-shape detection in `MTM_Waitlist.Mock.Service/Services/RefreshShapeCatalogProvider.cs` — a shape with tables/procedures but no catalog entry (never refreshed) and a shape with a catalog entry but no in-app fallback implementation (no fallback) are both reported, not silently ignored; the existence check calls `sp_visual_read_shape_metadata_get` (`data-model.md` §12), never inline SQL (FR-016/FR-020, constitution III, `contracts/mock-service-configuration.md` §4)
- [x] T087 [US7] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ShapeCatalogValidationTests.cs` proving a catalog-to-artifact mismatch is detected and reported while the service still starts and serves the remaining shapes; then run the build and full test suite gates (verification)

**Checkpoint**: The extension path is documented and mechanically guarded; adding a shape changes no existing contract.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: SP-first conversion of **all** inline MySQL SQL, the real-data gap fixes, documentation accommodation,
the two mechanical audits (SC-013), and the final build/test gates (SC-015).

### Subphase 10.1 — SP-first conversion of the inline-SQL inventory (FR-015, `Discovery/03`)

> Route to the **existing** procedure where one covers the site; otherwise create a new procedure (with `rollback.sql`
> and master-list + `update_table_descriptions.sql` registration in the same change). Sources are the file:line sites in
> `WeekendProject/Module_Mock/Discovery/03-Hardcoded-MySQL-Sql.md`.

- [x] T088 Route `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` (inline SQL at L14, exec L45) to the existing `sp_receiving_history_average_coil_weight` in `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql` (Discovery/03 §B)

**T088 execution note** (2026-09-10, `/speckit.implement` run 5) — done, but not as the task describes:

- The named artifact was a **flat single file**, which the locked ruleset forbids ("Any schema artifact requires
  matching rollback"; file-per-artifact layout `<name>/{create,rollback}.sql`). It is now the pair
  `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight/{create,rollback}.sql`,
  and the flat file was deleted. The procedure body and its `p_part_id` contract are unchanged; the semantics
  the statement carried (skid weight averaged, NULL/non-positive rows ignored, no division by coils-on-skid)
  are now written into the artifact's header instead of being implicit.
- **It was not "existing" on the server.** Live `mtm_receiving_application` (5.7.24) had no such routine, so the
  artifact was **deployed** (additive — nothing was renamed or dropped) and validated live by comparison with
  the statement it replaces: `MMC0000659` → `3265` from both; `'  MMC0000364  '` → `4183` (the `TRIM` path);
  an unknown part → one `NULL` row, which the caller's existing null check already renders as empty text.
- Caller rewired to `ExecuteStoredProcedureQueryAsync("sp_receiving_history_average_coil_weight", ["@p_part_id"])`
  against `MySqlDatabaseTarget.MtmReceivingApplication`; the inline statement constant is gone.
- `AverageCoilWeightServiceTests`' double now observes the stored-procedure path, and a new test asserts the
  procedure name, the `@p_part_id` value, and **`SqlCallCount == 0`** — so reintroducing inline SQL fails the
  suite rather than only the T098 audit.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped** (the 13 remain
  the environment-gated live-DB integration tests).
- Discovery for T097 (recorded, not acted on): the same tree still holds two flat artifacts —
  `sp_setup_dunnage_part_insert.sql` and `sp_setup_dunnage_type_insert.sql` — which are layout deviations of the
  same kind, and neither is referenced by any task in this file.
- [x] T089 Route `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs` to its existing SPs — hot-WC read L91 → `sp_config_hot_workcenters_get_for_workstation`, hot-WC delete L272–279 → `sp_config_hot_workcenters_delete_for_workstation`, hot-WC upsert L284–333 → `sp_config_hot_workcenters_upsert`, available WCs L342–350 → `sp_setup_work_centers_get_all` (Discovery/03 §A)

**T089 execution note** (2026-09-10, `/speckit.implement` run 6) — all four sites routed, plus a fifth found in the same file:

- **Two of the four names in this task are wrong.** The live procedures are
  `sp_config_hot_workcenters_get_for_computer` and `sp_config_hot_workcenters_delete_for_computer` — the table they
  filter is `core_computers_registry`, and no `_for_workstation` name exists anywhere on the server. Per the
  standing decision (**live names win**; nothing another application calls is renamed) the service calls the live
  names. `sp_config_hot_workcenters_upsert` and `sp_setup_work_centers_get_all` were already correct. T097 still
  needs to reconcile the folder/body-name drift that produced this task text.
- **A fifth site was converted, in the same file** (`SaveHotWorkCentersAsync`'s own catalog lookup): it was the
  `setup_work_centers_catalog WHERE is_active = 1` read that `sp_setup_work_centers_get_all` already answers, so
  one procedure now serves both catalog sites. The task named only the L342 site.
- **Equivalence was proven from the live definitions, not assumed.** `sp_config_hot_workcenters_get_for_computer`
  is the same JOIN/`is_active`/`ORDER BY` text with the same two output columns (`work_center_name`, `sort_rank`);
  `_delete_for_computer` and `_upsert` are byte-for-byte the statements they replace; and
  `sp_setup_work_centers_get_all` selects from `vw_setup_work_centers_active`, whose definition filters
  `is_active = 1` — confirmed equal live (`view_rows` 25 = `catalog_active_rows` 25), so the payload is identical.
- **Both write calls stay inside the existing transaction**, so a failure between the clear and the re-assignment
  still cannot leave a computer with no hot work centers. The difference is that the batched multi-row
  `INSERT … ON DUPLICATE KEY UPDATE` became one procedure call per work center — the same columns and the same
  `ON DUPLICATE KEY UPDATE` clause, N round trips instead of one, which is the only shape the procedure has.
- **Live validation** (constitution III), all inside a transaction that was **rolled back**, so no live row changed:
  two `_upsert` calls produced 2 rows for `computer_id 1`, `sp_config_hot_workcenters_get_for_computer('johnspc')`
  returned `100-3 | 1` and `100-6 | 2` (name + sort rank, in rank order — exactly what the C# reads), and after
  `ROLLBACK` the table was back to its original **0** rows.
- `System.Text` (the `StringBuilder` insert builder) is no longer used by the file and its using directive was
  removed. No test constructs this service (all seven call sites use `FakeWorkCenterCatalogService`), so no test
  double needed updating.
- **Still inline in this file, and not covered by T089:** three `core_computers_registry` reads —
  `GetAvailableComputersAsync` (L41), the workstation-id resolve in `SaveHotWorkCentersAsync` (L196) and
  `ResolveCurrentComputerNameAsync` (L362). They need the procedures T092 creates; T092's text names only
  `ComputerRegistryService.cs`, so these sites are **unassigned in this file** and are recorded here rather than
  left silent. T098's audit will fail until they are routed.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.
- [x] T090 Route `MTM_Waitlist.Settings/Services/ImageLocationService.cs` `LoadWorkCenterDetailsAsync` (L650–674) to `sp_setup_work_centers_get_all`, verifying the `IN (...)` filter semantics before switching (Discovery/03 §A)

**T090 execution note** (2026-09-10, `/speckit.implement` runs 6–7) — the semantics check failed, so the named target
was **not** used; the read was routed to a new additive procedure instead:

- **Why `sp_setup_work_centers_get_all` could not serve this call site:** it selects no `sort_rank` (this caller
  maps it into `WorkCenterItem.SortRank`, so every rank would have become 0 — the live values are 10, 20, 30 …); it
  orders by `sort_rank, work_center_name` while this caller orders by `building, sort_rank, work_center_name` (all
  25 rows would have been reordered); and it takes no parameters, so it cannot express `work_center_name IN (…)`.
  Its scope was verified equal (`vw_setup_work_centers_active` filters `is_active = 1`; 25 = 25 live), so only the
  column list, the ordering, and the filter were the blockers.
- **New artifact:** `Database/StoredProcedures/sp_setup_work_centers_catalog_get/{create,rollback}.sql` —
  `id, work_center_name, building, sort_rank, is_active` over the active view, ordered
  `building ASC, sort_rank ASC, work_center_name ASC`. Registered in `Database/StoredProcedures/AllSPs.sql` in the
  same change. The existing procedure is left untouched for its other callers.
- **The name filter is applied in C#** and the artifact says why: a variable-length name list has no safe parameter
  form in MySQL 5.7 that is not a fragile delimited string, and the active catalog is small and bounded (25 rows
  live), so narrowing it in C# costs nothing and cannot break on a name containing a comma.
- **Live validation:** deployed (exit 0) and called against MySQL 5.7.24 — 25 rows, columns and order exactly as
  the caller needs them, with real `sort_rank` values.
- **A latent test bug was exposed and fixed.** `ImageOverrideDialogViewModelTests.WorkCenterRow` built rows keyed
  `workstation_name`, a column no query ever selected; the old code therefore read every display name as an empty
  string and the two dialog tests stayed green only because they assert on the **building** group. The moment the
  loader filtered on the name, both failed with 0 groups. The double now emits `work_center_name` (the column the
  view actually exposes), which is what it should always have done.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.
- [x] T091 Route `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs` `GetSettingValueAsync` (L38–66) to a settings-read SP, verifying whether `sp_config_settings_get_effective` matches the required semantics (Discovery/03 §A)

**T091 execution note** (2026-09-10, `/speckit.implement` run 6) — verified first, and the named target does **not** fit:

- `sp_config_settings_get_effective` was checked against its **live definition** before any switch, which is what
  this task asked for. It answers a different question: it resolves the *effective* value across the scope
  precedence chain (`WHERE setting_key = p_setting_key AND (computer match OR all_users OR user match OR
  admin OR developer) ORDER BY fn_config_settings_scope_rank(scope_type) DESC, updated_utc DESC LIMIT 1`) and it
  takes `(p_setting_key, p_computer_id, p_user_id)` — **no scope key at all**.
- The caller needs the **exact** override row for the scope it was asked about, and it parses `id`, `public_id`,
  `scope_key` and `scope_type` off the result — none of which that procedure returns. Routing to it would have
  silently changed *which row is read* (a broader-scope fallback where the caller expects `null`) and dropped
  four mapped columns. It was therefore **not** used.
- Instead the exact-match read became a new additive artifact:
  `Database/StoredProcedures/sp_config_settings_values_get/{create,rollback}.sql` — the same fifteen columns and
  the same `LIMIT 1` the inline statement returned, with parameter widths taken from the live table
  (`setting_key varchar(190)`, `scope_key varchar(255)`). Registered in `Database/StoredProcedures/AllSPs.sql`
  (hand-maintained concatenated master) and appended there in the same change.
- **Live validation**: `CALL sp_config_settings_values_get('sessions.retention_inactive_days','all_users')`
  returned the real row with all fifteen columns; `CALL …('no.such.key','all_users')` returned **no rows**,
  which is the caller's documented "no override, fall back to appsettings" path.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.
- [x] T092 Create the `core_computers_registry` procedures (lookup by name+mac, lookup-by-mac latest, upsert, update-by-mac, get-all, update-by-id, delete-by-id) under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Startup/Services/ComputerRegistryService.cs` (L21–216) (Discovery/03 §A). Exact procedure artifacts (names confirmed against the ruleset at implementation): `sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`, `sp_core_computers_registry_upsert`, `sp_core_computers_registry_update_by_mac`, `sp_core_computers_registry_get_all`, `sp_core_computers_registry_update`, `sp_core_computers_registry_delete`

**T092 execution note** (2026-09-10, `/speckit.implement` run 7) — all seven artifacts created under the exact names
this task specifies, plus two more the wiring proved necessary:

- **Seven create+rollback pairs** for the named procedures, bodies mirroring the inline statements they replace
  (same predicates, same `ORDER BY`/`LIMIT 1` tie-breaks, same `ON DUPLICATE KEY UPDATE` arms, same
  `public_id`/`created_utc` insert-only behaviour), parameter widths taken from the live table
  (`computer_name varchar(128)`, `hostname_normalized varchar(255)`, `mac_address_normalized varchar(64)`,
  `display_name varchar(128)`, `description varchar(255)`, `id bigint`, `is_registered tinyint`).
- **Two extra artifacts, found while routing** (recorded rather than improvised into a caller):
  `sp_core_computers_registry_lookup_by_name_get` — the shared service resolves a workstation by name **or**
  normalized hostname with an exact-match tie-break, and neither a name+MAC nor a MAC-only procedure can answer
  that; and `sp_core_computers_registry_registered_get` — the computer picker filters `is_registered = 1` while
  `get_all` must not, so keeping the filter in its own procedure avoids either widening the picker's list the
  first time an operator retires a machine or pushing the predicate into C#.
- **Both callers are now free of inline SQL.** `ComputerRegistryService` (7 sites) and the three
  `core_computers_registry` sites T089 left unassigned in `WorkCenterCatalogService`
  (`GetAvailableComputersAsync`, the workstation resolve in `SaveHotWorkCentersAsync`, and
  `ResolveCurrentComputerNameAsync`). A grep for statement markers over both files matches only comments.
- **Live deployment and validation** (constitution III) against MySQL 5.7.24: all nine `create.sql` artifacts
  exited 0, and every operation was exercised **inside a transaction that was rolled back**, so no production row
  changed — insert (rows 2→3, id 3), a duplicate upsert (still 3, proving the `ON DUPLICATE KEY UPDATE` arm),
  both lookups, `update_by_mac` then `update` (row read back as `zz-validate-rig-3`/`ZZ3`/`is_registered=0`),
  and delete (3→2); after `ROLLBACK` the count was back to **2**. `registered_get` returned both live machines in
  display order, and `lookup_by_name_get('JOHNSPC')` matched `johnspc` — the same case-insensitive behaviour the
  inline statement had under this schema's `utf8mb4_unicode_ci` collation.
- **Master list:** all nine appended to the hand-maintained `Database/StoredProcedures/AllSPs.sql` in the same
  change (constitution III).
- **A test-double gap surfaced and was fixed:** `ComputerRegistryServiceTests`' fake returned an empty result set
  and `0` for the two stored-procedure methods without counting them, so seven tests failed the moment the service
  switched seams. The double now serves and counts both paths, and two tests additionally assert the procedure
  names (`…_lookup_by_name_mac_get`, `…_upsert`) so the wiring is pinned rather than implied.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.
- [x] T093 Create the startup session/auth procedures (`fn_server_utc_now` call, credentials check, password update, computer-registered read, user row read, session expiry read) under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Startup/Services/StartupSessionRepository.cs` (L41, L135–151, L215–224, L242–249, L265–278, L301–310) (Discovery/03 §A)

**T093 execution note** (2026-09-10, `/speckit.implement` run 8) — six artifacts created, deployed, validated and wired:

- **Artifacts** (`sp_` + `auth_`/`server` prefix, matching the schema's own `auth_*` table prefix; the task named no
  procedure names): `sp_server_utc_now_get`, `sp_auth_credentials_check`, `sp_auth_user_password_update`,
  `sp_auth_computer_registered_get`, `sp_auth_user_row_get`, `sp_auth_session_expiry_get` — each a create+rollback
  pair, with parameter widths taken from the live tables (`username_normalized varchar(128)`,
  `password_hash varchar(128)`, `password_salt varbinary(32)`, `hostname_normalized varchar(255)`,
  `mac_address_normalized varchar(64)`). All six registered in `Database/StoredProcedures/AllSPs.sql`.
- **Semantics preserved exactly**, including the parts that matter for security: `is_active = 1` on both user reads
  and on the password update; the LEFT JOIN so a user with no role assignment is still a known user with an empty
  role; `ORDER BY ra.assigned_utc DESC LIMIT 1` so the newest assignment wins; `revoked_utc IS NULL AND
  is_active = 1` so a revoked token is never reported as a live session; and `require_password_change = 0` cleared
  only as part of an actual change. The credential check still returns the same seven columns.
- **A pre-existing asymmetry was preserved, not silently "fixed"**: `CheckCredentialsAsync` lower-cases the username
  before the lookup while `ReadUserRowAsync` passes it through unchanged, and both compare against
  `username_normalized`. Changing that would change which logons succeed, which is outside this task's scope, so it
  is recorded in the artifact header for review instead.
- **Live validation, and no credential was ever displayed** (FR-026). The safe paths were run directly: the server
  clock returned `2026-09-10 22:19:57`, `sp_auth_computer_registered_get('johnspc','d8-43-ae-47-d0-d6')` returned
  `1`, and there are no live sessions (`auth_sessions_tokens` empty) so session expiry correctly returned no row.
  The password update was exercised **inside a transaction that was rolled back** — a dummy hash marker appeared on
  exactly one row and was gone after `ROLLBACK`, so no real credential was touched. The two credential-returning
  calls were executed with their output redirected to a file and only **structural facts** reported (2 rows total
  across three calls, 7 columns in the credential row); the captured file was deleted immediately, so no hash or
  salt passed through the transcript.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.
- [x] T094 Create the `config_images_locations` CRUD procedures under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Settings/Services/ImageOverrideReadService.cs` (L66–481) and `MTM_Waitlist.Settings/Services/ImageOverrideWriteService.cs` (L101–679) (Discovery/03 §A). Exact procedure artifacts: `sp_config_images_locations_get`, `sp_config_images_locations_get_by_scope`, `sp_config_images_locations_count_active_get`, `sp_config_images_locations_count_by_scope_get`, `sp_config_images_locations_get_all`, `sp_config_images_locations_get_by_public_id`, `sp_config_images_locations_recent_get`, `sp_config_images_locations_insert`, `sp_config_images_locations_update`, `sp_config_images_locations_reactivate`, `sp_config_images_locations_delete`, `sp_config_images_locations_delete_by_public_id`, `sp_config_images_locations_purge_inactive`, `sp_config_images_locations_deactivate_for_scope`, `sp_setup_work_centers_exists_get`

**T094 execution note** (2026-09-10, `/speckit.implement` run 10) — both halves complete and verified.

- Eight artifacts created in both directions (`Database/StoredProcedures/<name>/{create,rollback}.sql`): the seven
  reads named in the task plus `sp_setup_work_centers_exists_get` for the orphan detector's work-center check.
  All eight registered in `Database/StoredProcedures/AllSPs.sql` in the same change.
- `ImageOverrideReadService.cs` now has **no statement text at all**: all eight sites (L66, L133, L186, L229,
  L265, L324, L379, L476) call `ExecuteStoredProcedureQueryAsync` with one named const per procedure.
- **Predicates preserved exactly, including the two that look like omissions:** `get_all` and `recent_get` are the
  two reads the task's column list does not pin down, and both keep the filters the inline statements had —
  `get_all` keeps `is_active = 1` (it feeds the orphan detector, which only considers live overrides) while
  `recent_get` deliberately has **no** `is_active` filter because it is the audit view and must be able to show a
  withdrawn override. `sp_setup_work_centers_exists_get` also has no `is_active` filter: filtering to active rows
  would report every override on a temporarily inactive work center as an orphan and offer a cleanup that should
  not happen. None of these three is a copy-paste of another read.
- **`recent_get`'s `LIMIT` was the one genuinely non-routable statement, and the accepted form was narrower than
  the obvious guess.** The site interpolated `LIMIT {maxRecordCount}` into the text. A first artifact used a
  `BEGIN … END` body clamping into a local variable; deploying it **failed** — the `mysql` client splits a routine
  body on `;` without a `DELIMITER` directive, and no other artifact in this repo uses a `BEGIN … END` body, so
  that was the wrong convention regardless. Probing the server directly settled it: a **routine parameter works in
  `LIMIT`** and **any expression does not** (`LIMIT GREATEST(p,1)` → `ERROR 1327 Undeclared variable: GREATEST`),
  and `LIMIT 0` is legal and returns no rows rather than erroring. The artifact is therefore a single-statement
  body with `LIMIT p_max_rows`, and the caller's existing `maxRecordCount < 1` guard stays the thing that keeps
  the cap valid. The rejected form and the probe results are recorded in the artifact header.
- **Live validation, 13 checks, all matching expectation** (MySQL 5.7.24, seeded inside a transaction that was
  **rolled back** — the table held 0 rows again afterwards, so no production row changed): `get` returned the one
  active row and returned nothing for an inactive one; `get_by_scope` returned the two active rows newest-first;
  `count_active` returned 3 and `count_by_scope` 2 against 3 active + 1 inactive seeded rows; `get_all` returned
  the 3 active; `get_by_public_id` returned its row; `recent_get(2)` returned the two newest **including the
  inactive one**, which is the no-filter behaviour proven rather than assumed, and `recent_get(0)` returned 0 rows
  without error; `exists_get` returned the real id for a real work center and nothing for `-1`. The two
  `tmp_limit_*` probe procedures created during the investigation were dropped and confirmed absent.
- **Tests:** the one assertion that read SQL text
  (`GetOverrideAsync_FiltersOnScopeItemAndActiveFlag`) now asserts the procedure seam instead, per the pattern
  used for `ComputerRegistryServiceTests` and `AverageCoilWeightServiceTests`; and three tests were added —
  `EveryRead_RoutesThroughItsProcedure_AndCarriesNoInlineStatement` (walks all seven reads and fails if any
  recorded statement contains `SELECT`/`FROM `), `GetRecentlyUpdatedOverridesAsync_BindsTheCap_InsteadOfInterpolatingIt`,
  and `DetectOrphanedOverridesAsync_ChecksWorkCenterExistenceThroughItsProcedure`.
- Gates: build **0 warnings / 0 errors**; suite **646 total, 0 failed, 633 passed, 13 skipped** (was 643/630
  before the three added tests).

**T094 write-half completion** (same run) — sixteen artifacts total for this task (eight read pairs above, eight
write pairs here), all deployed and registered.

- **The task's list was short by one procedure.** It names seven writes; the create path needs an eighth,
  `sp_config_images_locations_status_get`, for the existence probe at L101. That probe is `SELECT is_active` with
  **no** `is_active` filter, and it is the one read on this table that must not filter: the caller has to tell
  "no row" (INSERT) from "inactive row" (reactivate, because `uq_config_images_locations_scope_item` spans the
  pair regardless of `is_active`) from "active row" (DUPLICATE_KEY). `sp_config_images_locations_get` could not
  stand in for it — it is filtered to `is_active = 1` and that difference is exactly what the caller is asking.
- **Three sites were reporting wrong results before this change, and this is a genuine defect fix rather than a
  refactor.** `DeleteByPublicIdAsync`, `PurgeInactiveOverridesAsync` and `DeactivateAllForScopeAsync` all ran
  their `UPDATE`/`DELETE` through the **row-returning** helper and then read `rows.Count`. A statement that returns
  no rows yields an empty list, so that count was always `0`: the first method answered `NOT_FOUND` for every call
  **even when the row really was withdrawn**, and the other two returned `0` to the UI so it reported "nothing
  purged" and "0 deactivated" however many rows changed. The non-query path carries the real affected-row count,
  so routing these three through it is what fixes them; the predicates are unchanged. This is the same
  wrong-helper-for-DML mistake T096 found in `ConfigSettingsValueService` (and the same one the repo's own notes
  warn about), which makes a third occurrence worth watching for in T098's audit. No test covered these three
  methods, which is why it had gone unnoticed; three regression tests now do.
- **Semantics preserved:** `reactivate` keeps its missing `is_active` predicate (the caller only reaches it after
  the status read reported an inactive row, and `created_*` stay untouched because it is the same logical row);
  `delete` and `delete_by_public_id` keep `AND is_active = 1`, so the affected-row count stays a "did anything
  change" signal and re-withdrawing reports 0; `purge_inactive` remains the only hard delete and stays
  unconditional apart from `is_active = 0`; `deactivate_for_scope` remains a bulk soft delete, not a purge.
- **Live validation, 12 checks, all matching expectation** (MySQL 5.7.24, seeded inside a transaction that was
  **rolled back**; the table was confirmed at 0 rows before and after): the status read returned nothing for a
  never-seen pair, then `1` after insert; `update` returned affected 1 and left `created_by_user_id` and
  `updated_by_user_id` both at the real user id; `delete` returned 1 and a second `delete` returned **0**;
  the status read then returned `0`, proving it sees withdrawn rows; `reactivate` returned 1 with the new path
  and the original `created_by_user_id` intact; `delete_by_public_id` returned **1 then 0** (the defect fix, with
  the count now real); `purge_inactive` returned 3 against 3 inactive rows and left 0; `deactivate_for_scope`
  returned 2 against 2 active `request_subtype` rows, left 0, and touched no other scope.
- **One correction during validation, worth keeping:** the first attempt passed an arbitrary `p_user_id` of 7 and
  the server rejected it with `ERROR 1452` on `fk_config_images_locations_updated_by_user_id`, so the re-run used a
  real `core_users_profiles.id`. That foreign key is a live constraint on both `*_by_user_id` columns, and it means
  a caller passing a stale user id gets a key violation rather than a silent write.
- **Strongest evidence: the 8 gated integration tests were executed against the live server**
  (`MTM_WAITLIST_TEST_DB_CONNECTION_STRING` pointing at 172.16.1.104/mtm_waitlist) — **8 passed, 0 failed, 0
  skipped** — which exercises the whole converted path through the real `MySqlHelperServer`: procedure naming,
  parameter binding, affected-row counts, and read-back, including the create-after-delete reactivation path that
  spans two of the new procedures. The tests namespace their rows with a GUID and clean up after themselves; the
  table was confirmed empty afterwards with no stray `itest-%` rows, and the captured log file was deleted because
  it contained a connection string.
- **Tests added** (5): `DeleteByPublicIdAsync_WhenTheRowWasWithdrawn_ReportsSuccess`,
  `DeleteByPublicIdAsync_WhenNothingChanged_ReturnsNotFound`, `PurgeInactiveOverridesAsync_ReturnsTheAffectedRowCount`,
  `DeactivateAllForScopeAsync_ReturnsTheAffectedRowCount`, and
  `EveryWrite_RoutesThroughItsProcedure_AndCarriesNoInlineStatement` (walks every write path and the probe, and
  fails if any recorded statement contains `INSERT INTO` / `UPDATE ` / `DELETE FROM` / `SELECT `).
- **Whole-table check:** `config_images_locations` now appears in `MTM_Waitlist.Settings/**` only in doc comments
  and the procedure-name constants — no statement text anywhere in the two services, and no other production file
  in the repo touches the table.
- Gates: build **0 warnings / 0 errors**; suite **651 total, 0 failed, 638 passed, 13 skipped** (was 643/630
  before this task's 8 added tests; the 13 skips are the environment-gated live-DB tests, which were run
  separately above and all passed).
- [x] T095 Create the `config_dunnage_types_visibility` procedures (visibility read, delete-all, multi-row insert) under `Database/StoredProcedures/` with rollback + master-list registration, wire `MTM_Waitlist.Shared/Services/DunnageTypeVisibilityCatalogService.cs` (L52, L85, L120–139), and route the receiving `dunnage_types` read (L139) to the receiving store's procedure (Discovery/03 §A/§B). Exact procedure artifacts: `sp_config_dunnage_types_visibility_get`, `sp_config_dunnage_types_visibility_delete_all`, `sp_config_dunnage_types_visibility_insert_many`, `sp_receiving_dunnage_types_get_all`

**T095 execution note** (2026-09-10, `/speckit.implement` run 9) — all three call sites routed, with two design decisions recorded:

- **The task's `insert_many` name could not be honoured honestly, so the artifact is
  `sp_config_dunnage_types_visibility_insert_row`** plus one call per row from the caller. A single set-based
  statement needs the whole set inside the routine, and MySQL 5.7 cannot iterate a collection without a helper
  numbers table or a delimited string. The delimited form is unsafe here because one value per row
  (`dunnage_type_name`) is free text that may contain the delimiter; and the other way to make it set-based —
  letting an `mtm_waitlist` routine read `mtm_receiving_application.dunnage_types` directly — was rejected because
  this repo keeps the four stores separately configurable, so a cross-store read inside one store's procedure
  breaks as soon as they are not co-located. The caller's sequence is still delete-then-write, exactly as before,
  so no atomicity was lost relative to the statement this replaces (neither version wrapped the two steps in a
  transaction). The reasoning lives in the artifact header, not only here.
- **The receiving read is a new artifact, and the task's premise that one already existed was checked first.**
  The live `sp_Dunnage_Types_GetAll` is `SELECT * FROM dunnage_types ORDER BY id`: it includes rows with NULL or
  blank names and orders by id rather than by name, while this caller excludes blank names and lists
  alphabetically. Routing to it would have changed both the content and the order of the operator's list, so
  `sp_receiving_dunnage_types_get_all` was created in the receiving tree with the caller's exact predicate and
  ordering. The existing procedure is untouched — the receiving application's own screens still use it.
- **Semantics preserved:** the visibility read keeps no `is_visible` filter and no ordering (the caller folds rows
  into a dictionary and treats a *missing* entry as visible, so filtering here would have changed that default);
  `public_id`, `created_utc` and `updated_utc` are generated inside the procedure because the caller never chose
  them and a NULL would violate NOT NULL; both `*_by_user_id` columns stay NULL as before.
- **Live validation:** all four artifacts deployed (exit 0) and exercised against MySQL 5.7.24 —
  `sp_receiving_dunnage_types_get_all()` returned **23** rows, exactly matching the count of named types in
  `dunnage_types`; and inside a transaction that was **rolled back**, delete-all left 0 rows, two `insert_row`
  calls produced 2 rows with `SUM(is_visible) = 1`, the read returned `1|1` and `2|0`, and after `ROLLBACK` the
  table was back to its original 0 rows, so nothing persisted.
- **Master list:** the three app-store procedures appended to `Database/StoredProcedures/AllSPs.sql`; the receiving
  tree has no master list of its own, so its pair is the whole registration.
- No test constructs the real service (its callers use `FakeDunnageTypeVisibilityCatalogService`), so no double
  needed updating.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**.

> **T094 is complete** (see the execution note above); the write half landed in the same run and needed one extra
> procedure the task's list did not name (`sp_config_images_locations_status_get`) plus the fix for three sites
> that read a row count after DML and therefore always reported zero. The next SP-first task to work is T097
> (flat-artifact reconciliation), then T098 (the inline-SQL audit) — and T098 should specifically look for the
> same wrong-helper-for-DML shape, which has now been found three times.
- [x] T096 Create the `config_settings_values` delete procedure under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs` `DeleteSettingValueAsync` (L142–148) (Discovery/03 §A). Exact procedure artifact: `sp_config_settings_values_delete`

**T096 execution note** (2026-09-10, `/speckit.implement` run 6):

- Artifact pair created: `Database/StoredProcedures/sp_config_settings_values_delete/{create,rollback}.sql`, with
  the same predicate as the inline statement and parameter widths taken from the live table, registered in
  `Database/StoredProcedures/AllSPs.sql` in the same change.
- The call site was also using the **wrong helper**: `ExecuteSqlQueryAsync` for a `DELETE`. It now uses
  `ExecuteStoredProcedureNonQueryAsync`, matching the DML convention the rest of the codebase follows (and the
  one the repo's own notes warn about — a query helper returns rows, so a delete routed through it reports
  nothing about what happened).
- **Live validation**, inside a transaction that was **rolled back**: two delete calls reduced the real row
  count 3 → 2 and `ROLLBACK` restored it to 3, so the predicate deletes exactly the intended row and the
  procedure is safe to run.
- Gates: build **0 warnings / 0 errors**; suite **643 total, 0 failed, 630 passed, 13 skipped**. No test
  constructs the service (its callers use `FakeConfigSettingsValueService`).
- [x] T097 Reconcile the SP folder-name/body-name/caller-name mismatches (`sp_Dunnage_*` vs `sp_setup_dunnage_*`; `sp_config_hot_workcenters_*_for_workstation`; `sp_setup_workstations_*`) and fix the callers, per `Discovery/03` §E

**T097 execution note** (2026-09-10, `/speckit.implement` run 10) — every name in the task was checked against the live server before anything was changed, and **none of the three "wrong" names exists anywhere on either schema**:

- **`sp_setup_dunnage_*` — no such procedure exists** (`WHERE ROUTINE_NAME LIKE 'sp_setup_dunnage%'` returns
  nothing on `mtm_waitlist` or `mtm_receiving_application`). The two files carrying that name,
  `Database/MTMReceivingApp/StoredProcedures/sp_setup_dunnage_{type,part}_insert.sql`, turned out to be
  **dependency notes, not artifacts**: they contain no `CREATE PROCEDURE`, no `DROP`, and no executable statement
  beyond a `USE` — only a comment block naming the live procedure and its parameter list. They are also
  **runtime-loaded app content**, not dead files: `DunnageWorkflowService` loads each one by name through
  `SetupReceivingStoredProcedureScriptStore.LoadAsync` (via the `Database\MTMReceivingApp\StoredProcedures\**\*.sql`
  content wildcard in `MTM_Waitlist.csproj`), which is why the rename had to move the files *and* the loader names
  together. Renamed to the live names they document — `sp_Dunnage_Types_Insert.sql` and
  `sp_Dunnage_Parts_Insert.sql` — with the two `LoadAsync` call sites updated and the
  `.github/agents/module-setup.agent.md` path list corrected. Each file now states plainly that the procedure is
  owned by MTM_Receiving_Application, that this repository must not author it, and that the load is a **warm-up
  whose result the caller discards** (so nothing in the file executes; the live procedure is called on the next
  line). No `.csproj` edit was needed — the include is a wildcard — but the rename was verified all the way into
  the build output, because a loader that asks for a name that no longer ships would silently read `""`.
- **`sp_config_hot_workcenters_*_for_workstation` — no such name exists.** The live names are
  `sp_config_hot_workcenters_get_for_computer` and `sp_config_hot_workcenters_delete_for_computer`; the callers
  were **already correct** (T089 fixed them and recorded why). The drift lived entirely in this repository's
  prose, so the prose is what was fixed: `Discovery/03` §A, §E and its replace-candidate table,
  `WeekendProject/Module_Mock/{Plan.md,Spec.md}`.
- **`sp_setup_workstations_*` — no such name exists either; the live procedure is `sp_setup_work_centers_touch`.**
  Here there *was* a real code defect, and it was in a log line rather than a call: `SetupPersistenceService`
  invoked `sp_setup_work_centers_touch` correctly but then logged `sp_setup_workstations_touch completed`, so the
  startup log sent an operator looking for a procedure that does not exist. Fixed to name the procedure actually
  called.
- **Nothing on either server was renamed, altered or dropped**, per the standing rule that live names win and no
  object another application calls is touched. This task changed repository text and file layout only — which is
  why the suite count is unchanged rather than a sign the work was skipped.
- **The old names still appear in three intentional places**, so a later reviewer does not mistake them for
  leftovers: the rename notes inside the two renamed files, the historical task text and T089 note in this file
  (a record of what the task said, with the correction beside it), and `tools/scan_workstation_rename.ps1`, whose
  entire purpose is to search for the pre-rename vocabulary.
- **Discovery/03 §E was rewritten into §E.1 "Resolution"** rather than deleted, so the mismatch, the evidence and
  the outcome are readable in one place. Its §B note about `AverageCoilWeightService` was left for T102's doc
  sweep — that one belongs to T088/T102, not here.
- Gates: build **0 warnings / 0 errors**; suite **651 total, 0 failed, 638 passed, 13 skipped** (unchanged, as
  expected for a naming reconciliation).
- [x] T098 (verification) Add the **inline-SQL audit** test `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs` that scans C# source and fails on MySQL statement markers (`SELECT`/`INSERT`/`UPDATE`/`DELETE`/`CALL`) outside stored-procedure call sites (SC-013, `research.md` R13)

### Subphase 10.2 — Real-data gap fixes (FR-019)

- [x] T099 Wire `MTM_Waitlist.Waitlist.NewRequest/Services/CoilAvailabilityService.cs` to a real source, removing the "coil assumed available" default (FR-019, `research.md` R15)
- [x] T100 Make `MTM_Waitlist.Waitlist.NewRequest/Services/RequestTypeCatalogService.cs` (DB stored procedures) the single authoritative request-type source and move `MTM_Waitlist.Settings/Services/ImageLocationService.cs` off `Assets/Config/waitlist-request-types.json` (FR-019, Discovery/01 note 4)
- [x] T101 (verification) Add coverage for both gap fixes in `MTM_Waitlist.Tests/Module_Waitlist/` (coil availability comes from a real source; request-type definitions have one authoritative source)

### Subphase 10.3 — Documentation accommodation (FR-028)

- [x] T102 [P] Update `WeekendProject/ChangeLog.md` and the `WeekendProject/Module_Mock/*` docs with the Module_Mock architecture and the removal record, sweep `WeekendProject/PromptFiles/*` for stale mock-toggle/sample-catalog references, and reconcile `WeekendProject/Module_Mock/Discovery/*` so the docs describe what shipped — **0 stale references** to the retired sample/demo systems (SC-016, FR-028)

### Subphase 10.4 — Mechanical audits and final gates (SC-013, SC-015)

- [x] T103 (verification) Add the **retired-symbol audit** test `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` that fails if any retired type/key is present or referenced (`ISampleDataService`, `Sample*Catalog`, `Feature.InforVisualMockData`, `Feature.RecvMockData`, `MockToggleService`, `MockRouting*`, `MockMode*`, `MockConfigurationService`, `MockMasterDataService`, `UseMockData`, `MockDataExpander`, `sp_mock_*`) (SC-013, `research.md` R13)
- [x] T104 (verification) Run the **build gate** — `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` — and confirm **0 warnings / 0 errors** (SC-015); treat `PRI175`/`PRI224` as stale-PRI/running-exe issues and `WMC9999` as a masked XAML error to be surfaced, never ignored
- [x] T105 (verification) Run the **test gate** — `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — all green with the retired-symbol and inline-SQL audits included, then re-run with `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` set for the live MySQL subset and record any environment-gated skips explicitly (SC-015/FR-013) — **done 2026-09-11**: full suite `Failed: 0, Passed: 661, Skipped: 13, Total: 674` with both audits included; the 13 skips are the pre-existing live-database integration suites. **The live subset was completed later the same day** (Phase 19): with `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` pointed at `172.16.1.104`, the suite runs `Failed: 0, Passed: 699, Skipped: 0, Total: 699` — every previously-skipped live-database test now executes, and the 2 failures it first exposed in `MockMirrorRefreshWriterIntegrationTests` were a wrong-schema connection string, now fixed.
- [ ] T106 (verification) Execute the end-to-end acceptance walkthrough in `specs/001-module-mock-visual-fallback/quickstart.md` §1–§8 on a running build (cache deploy → service start → fallback proof → defect proof → backup/restore drill → sixth-shape playbook → final validation table). **Note (E5)**: SC-007 and SC-008 use **30-day observation windows** — these are post-deployment measurements, not one-shot tests; record the observation start here and re-check at the 30-day mark. — **Not executed (2026-09-11)**: the environment re-probe corrected Phase 18's claim — `mtm_mock` **is** deployed and Infor Visual **is** reachable (Phase 19) — but the walkthrough still cannot run: the service is neither published nor running, the fallback proof needs Infor Visual made unreachable, and the app stops at its **Sign in** gate. **Observation window not started.**

---

## Phase 11: Analysis remediation (added 2026-09-09 by `/speckit.analyze`)

**Purpose**: Close the findings from the cross-artifact consistency analysis that had no task coverage. Each task names
the phase/story it belongs to; run it in that position in the phase order.

- [x] T107 [US1] Implement the internal-store unavailable handling — a bounded retry policy (up to three attempts with delays ≈ 1 s / 2 s / 4 s) in the `MTM_Waitlist.Core` data-access seam used by the affected screens, and a per-screen `Unavailable` state carrying `Store`/`LastAttemptUtc`/`RetryCount`/`NextRetryUtc` plus an operator-facing message with a manual retry action; no sample-data substitution and no persistent banner (`data-model.md` §10, FR-021)
- [x] T108 [US1] (verification) Add `MTM_Waitlist.Tests/Module_Mock/InternalStoreAvailabilityTests.cs` — three bounded retries then `Unavailable`; no sample rows on failure; a manual retry recovers once the store returns (`data-model.md` §10, FR-021); then run the build and full test suite gates (verification)
- [x] T109 Create the metadata procedure `Database/Mock/StoredProcedures/sp_visual_read_shape_metadata_get/{create.sql,rollback.sql}` (per-shape mirror/stage-table and `get`/`refresh`-procedure existence plus the mirror's actual `information_schema` columns) and register it in `Database/Mock/AllSPs.sql` (`data-model.md` §12, FR-016/FR-020, constitution III; closes finding D1)
- [x] T110 Wire the shape-catalog startup validation (`RefreshShapeCatalogProvider`, T029/T086) to call `sp_visual_read_shape_metadata_get`, and confirm the inline-SQL audit (T098) needs no exemption for metadata reads (constitution III; closes finding D1)
- [x] T111 (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests.cs` — adding a catalog shape changes no existing procedure signature, contract, or result type, and already-deployed clients keep serving the existing shapes (FR-020)

---

## Phase numbering (plan ↔ tasks)

`plan.md` §3 uses strategic phases 0–8; this file uses execution Phases 1–10 (plus this remediation Phase 11). Mapping:

| `plan.md` §3 phase | `tasks.md` phase(s) |
|---|---|
| 0 Baseline & design freeze | Phase 1 (Setup) |
| 1 `mtm_mock` schema + SPs | Phase 2 (Subphases 2.1–2.2) + Phase 11 (T109) |
| 2 Service app | Phase 2.3, Phase 5, Phase 7, Phase 8 |
| 3 In-app `MTM_Waitlist.Mock` + fallback | Phase 3, Phase 4, Phase 6 |
| 4 Legacy-mock removal | Phase 3 (T034–T043) |
| 5 SP-first conversion | Phase 10.1 |
| 6 Real-data gap fixes | Phase 10.2 |
| 7 Documentation accommodation | Phase 10.3 |
| 8 Validation & release readiness | Phase 10.4 — plus FR-021 tasks T107/T108 (Phase 11) |

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately
- **Foundational (Phase 2)**: depends on Setup — **blocks all user stories**
- **User Stories (Phases 3–9)**: all depend on Foundational completion
- **Polish (Phase 10)**: depends on the desired user stories being complete (nothing may be audited as "0 inline SQL" or "0 retired symbols" until the removal and conversion have landed)

### Story Dependency Graph

```text
Phase 1 Setup
    │
    ▼
Phase 2 Foundational  (mtm_mock tables → SPs → seed; service config/credential/catalog/refresh skeleton)
    │
    ├──────────────► US1 (P1)  internal always live  ──┐
    │                    ▲                             ├── P1 MVP (deliver together)
    ├──────────────► US2 (P1)  automatic fallback  ────┘
    │                    │
    │                    ├──────────────► US4 (P2) indicator (needs US2 detector state)
    │                    └──────────────► US7 (P3) playbook (adds a shape over US2's contract)
    │
    └──────────────► US3 (P2) scheduled refresh (needs Foundational DB + service core)
                         │
                         ├──────────────► US5 (P3) on-demand refresh + status API/UI
                         └──────────────► US6 (P3) backups + host-only restore
                                              │
                                              ▼
                                    Phase 10 Polish (SP-first, FR-019, docs, audits, gates)
```

**Within the P1 pair (US1 ⟷ US2) — the R14 choreography**: additive → switch → remove. The coupling edges are:

- US1 T042 (`ISampleDataService`/`Sample*Catalog` deletion) **depends on** US2 T053–T055 (rerouting their last consumers)
- US1 T036 (`SqlHelperServer` `mockAction` removal) **depends on** US1 T034 (toggle-key deletion)
- US2 T057 (helper-server Visual mock-overload removal) **depends on** US2 T053–T055
- All other US1 tasks (internal-store remediation T031–T033, DB mock removal T038–T040, tests T043) are **independent** of US2 and should be done first — they alone fix the reported defect

### Within Each Story

- Contracts/models before services; services before routing/endpoints; core implementation before integration
- Verification tasks last in the phase; they are gates, not optional extras
- Story work should not start until the story's prerequisites above are complete

### Cross-Phase Notes

- **US5 and US6 share files** — `Api/ServiceApiEndpoints.cs` (T073 → T081) and `Views/ServiceSettingsPage.xaml` + `ViewModels/ServiceSettingsViewModel.cs` (T076 → T082 → T083). These tasks are deliberately **not** marked `[P]`; serialize them.
- **US3 and US5 both touch the service** — US3 owns lifetime + scheduled refresh; US5 owns the API host and UI. T075 (API host) wires into the host created in T059.

---

## Parallel Execution Examples

### Foundational — mirror tables (after T007/T008)

```text
Task: "Create visual_work_order_lookup_result (+ _stage) create/rollback in Database/Mock/Tables/"      (T009)
Task: "Create visual_operation_sequences_result (+ _stage) create/rollback in Database/Mock/Tables/"    (T010)
Task: "Create visual_subordinate_parts_result (+ _stage) create/rollback in Database/Mock/Tables/"      (T011)
Task: "Create visual_inventory_locations_result (+ _stage) create/rollback in Database/Mock/Tables/"    (T012)
Task: "Create visual_disposition_input_result (+ _stage) create/rollback in Database/Mock/Tables/"      (T013)
```

### Foundational — refresh + get procedures (after T015 establishes the reference pattern)

```text
Task: "sp_visual_operation_sequences_refresh create/rollback in Database/Mock/StoredProcedures/"        (T016)
Task: "sp_visual_subordinate_parts_refresh create/rollback in Database/Mock/StoredProcedures/"          (T017)
Task: "sp_visual_inventory_locations_refresh create/rollback in Database/Mock/StoredProcedures/"        (T018)
Task: "sp_visual_disposition_input_refresh create/rollback in Database/Mock/StoredProcedures/"          (T019)

Task: "sp_visual_work_order_lookup_get create/rollback in Database/Mock/StoredProcedures/"              (T020)
Task: "sp_visual_operation_sequences_get create/rollback in Database/Mock/StoredProcedures/"            (T021)
Task: "sp_visual_subordinate_parts_get create/rollback in Database/Mock/StoredProcedures/"              (T022)
Task: "sp_visual_inventory_locations_get create/rollback in Database/Mock/StoredProcedures/"            (T023)
Task: "sp_visual_disposition_input_get create/rollback in Database/Mock/StoredProcedures/"              (T024)
```

### US1 — independent internal-store remediation (can start as soon as Foundational is done)

```text
Task: "Remove MtmWaitlist mock short-circuit in MTM_Waitlist.Setup/Services/SetupPersistenceService.cs"  (T031)
Task: "Remove WaitlistRequestService mock gating in MTM_Waitlist.Waitlist.View/Services/"                (T032)
Task: "Remove Feature.RecvMockData gating in MTM_Waitlist.Core/Services/MySqlHelperServer.cs"           (T033)
```

### US2 — the five fallback implementations in parallel (after T045–T047)

```text
Task: "Implement VisualWorkOrderLookupFallback.cs (shape 1) in MTM_Waitlist.Mock/Services/"             (T048)
Task: "Implement VisualOperationSequencesFallback.cs (shape 2) in MTM_Waitlist.Mock/Services/"          (T049)
Task: "Implement VisualSubordinatePartsFallback.cs (shape 3) in MTM_Waitlist.Mock/Services/"            (T050)
Task: "Implement VisualInventoryLocationsFallback.cs (shape 4) in MTM_Waitlist.Mock/Services/"          (T051)
Task: "Implement VisualDispositionInputFallback.cs (shape 5) in MTM_Waitlist.Mock/Services/"            (T052)
```

### Polish — SP-first conversion (independent files; run the DB-side and C#-side tasks in parallel where the file sets differ)

```text
Task: "Route AverageCoilWeightService to sp_receiving_history_average_coil_weight"                       (T088)
Task: "Route WorkCenterCatalogService to its existing hot-WC/available-WC SPs"                           (T089)
Task: "Route ImageLocationService.LoadWorkCenterDetailsAsync to sp_setup_work_centers_get_all"           (T090)
Task: "Route ConfigSettingsValueService.GetSettingValueAsync to a settings-read SP"                      (T091)
Task: "Create core_computers_registry SPs + wire ComputerRegistryService"                                (T092)
Task: "Create startup session/auth SPs + wire StartupSessionRepository"                                  (T093)
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2 = the P1 pair)

1. Complete **Phase 1 Setup** → record the baseline
2. Complete **Phase 2 Foundational** (CRITICAL — blocks everything)
3. Complete **Phase 3 US1** — the internal-store remediation alone fixes the reported defect (SC-001/SC-002)
4. Complete **Phase 4 US2** — the cached fallback for the five Visual read shapes
5. **STOP and VALIDATE** against `quickstart.md` §3–§4 → deploy/demo the MVP

Both P1 stories are required for the MVP because the spec's own rationale pairs them ("It is the second half of the
MVP"), and because US1's full removal of the shared sample contracts is sequenced after US2's rerouting.

### Incremental Delivery

1. Setup + Foundational → `mtm_mock` deployable and populated; nothing in the app changes
2. **US1 + US2 (P1)** → test independently → deploy (MVP: truthful internal data + Visual resilience)
3. **US3 (P2)** → the cache stays warm unattended → deploy
4. **US4 (P2)** → users can tell cached from live → deploy
5. **US5 (P3)** → on-demand refresh + operational visibility → deploy
6. **US6 (P3)** → backups + emergency restore → deploy
7. **US7 (P3)** → published extension playbook → deploy
8. **Polish** → SP-first, gap fixes, docs, audits, final gates

### Parallel Team Strategy

With multiple developers, after Setup + Foundational:

- Developer A: US1 (internal always live + removal)
- Developer B: US2 (fallback library + routing) — coordinate the R14 coupling edges with A
- Developer C: US3 → US5 (service lifetime, scheduling, API, settings UI)
- Developer D: US4 (indicator) once US2's detector lands, then US6 (backups/restore), then US7 (playbook)

---

## Notes

- `[P]` = different files, no dependency on incomplete tasks. Tasks sharing a file are never `[P]`.
- `[Story]` labels provide traceability; Setup, Foundational, and Polish tasks carry no story label by design.
- Verification tasks are **gates** (constitution Principle VI). A task is only ticked `- [x]` when implemented,
  building clean, and verified.
- Never edit generated artifacts (`obj/`, `*.g.cs`, `*.g.i.cs`).
- `[RelayCommand]` strips a trailing `Async` from the generated command name (`SaveAsync` → `SaveCommand`); binding the
  wrong name surfaces only as a masked `WMC9999`.
- **Naming (resolved 2026-09-09)**: the derived form `sp_visual_<shapeKey>_{get,refresh}` is authoritative. During the
  `/speckit.analyze` remediation, `quickstart.md` §1 and `data-model.md` §3.3 were corrected to match it; the status
  model is `ReadStatusSnapshot` with the `VisualReadStatus` enum (`data-model.md` §8).
- **Phase 4 architecture decision (2026-09-09, `/speckit.implement`)**: all five read shapes and their live-read
  plumbing now live in `MTM_Waitlist.Mock` — the shape catalog (`Services/VisualReadShapeCatalog.cs`, moved out of the
  service so app and service share one catalog), one classified executor (`Services/VisualQueryExecutor.cs`)
  replacing the two duplicate per-module executors, one script store (`Services/InforVisualScriptStore.cs`), and the
  per-shape row/request types (`Models/Visual*Row.cs`, `Models/Visual*Request.cs`). This supersedes the
  delegate-injection option: the dependency direction stays modules → `Mock` and no caller module supplies a
  live-read delegate. Supporting change: `MySqlHelperServer` gained an `MtmMock` target, because no `mtm_mock` target
  existed for the fallback to read the mirror through. **Deviates from `plan.md` §Project Structure and needs a
  Complexity Tracking row on the next `/speckit.plan` touch.**
- **Phase 4 executor classification (new — not specified in the design docs)**: the retired executors returned an
  empty list on *every* failure path, so "Infor Visual is unreachable" and "Visual answered with zero rows" were
  indistinguishable — which would have disabled the fallback entirely (every read returns empty, the cache is never
  consulted). `VisualQueryExecutor` now classifies each attempt as `Ok` / `Unreachable` (connect, timeout, and login
  failures: SQL errors −2, 20, 53, 64, 121, 233, 258, 1231, 4060, 10053, 10054, 10060, 10061, 11001, 18456;
  `COMException`; timeouts) / `Failed` (every other error, plus a missing script or missing connection), and only
  `Unreachable` reads the mirror.
- **Phase 4 open gap — `CachedReadResult.RefreshedUtc`**: the cached path returns `null` because
  `sp_visual_<shape>_get` must not project `refreshed_utc`; adding that column would break the structural identity the
  fallback guarantees at FR-004. Snapshot age therefore needs a separate metadata read, still owned by T066/T110.
- **Phase 4 open gap — probe backoff**: T047's hysteresis is implemented and tested, but "backoff probing while
  cached" is a scheduling concern that the detector interface deliberately does not expose; it belongs to the host
  that calls `ProbeAsync` (T059/T062).
- Commit after each task or logical group.

---

## Format Validation

Validated before completion: every task line matches `- [ ] T### [P?] [Story?] Description with exact file path`.

- **Checkbox**: every task begins `- [ ]` ✓
- **Task ID**: `T001`–`T111`, unique and sequential (T107–T111 added by the 2026-09-09 `/speckit.analyze`
  remediation, in Phase 11) ✓
- **`[P]` marker**: present only on parallelizable tasks (different files, no incomplete dependency) ✓
- **`[Story]` label**: present (`[US1]`…`[US7]`) on every Phase 3–9 task (and on T107/T108 in Phase 11); absent on
  Phase 1 (Setup), Phase 2 (Foundational), Phase 10 (Polish), and the non-story Phase 11 tasks ✓
- **File paths**: every task names an exact path (project/`.cs`/`.xaml`/`.sql`/`.resw`/doc) or, for verification tasks,
  the exact command plus test file ✓
- **Count check**: Setup 6 · Foundational 24 · US1 14 · US2 14 · US3 6 · US4 6 · US5 8 · US6 6 · US7 3 · Polish 19 · Remediation 5 = **111**

**Result**: PASS — all tasks conform; the file is ready for `/speckit.analyze`.

---

## Phase 12: Convergence

**Added**: 2026-09-09 by `/speckit.converge`, after `/speckit.implement` ran through Phase 2 (T001–T030, T109).

These tasks close gaps between the specification/plan and the current code that **no existing task covers**.
Existing unticked tasks (T031–T108, T110–T111) are deliberately not duplicated here. Ordered CRITICAL/HIGH first.

- [x] T112 (CRITICAL) Resolve the cold-build `NETSDK1206` warning so the build gate can be satisfied honestly: a clean restore emits `warning NETSDK1206: Found version-specific or distribution-specific runtime identifier(s): win10-arm64, win10-x64, win10-x86. Affected libraries: Microsoft.Graphics.Win2D.` for every module library (reproduces on `MTM_Waitlist.Setup` after deleting its `obj/`), so the "0 warnings" gate is only met on a warm build. Either fix the RID configuration repo-wide in `Directory.Build.props` (preferred) or record an explicit, approved scoped deviation per Constitution VI / SC-015; then re-run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` after a cold restore and record the result (contradicts)
- [x] T113 (HIGH) Implement the concrete `IVisualShapePayloadSource` in `MTM_Waitlist.Mock.Service/Services/` (for example `VisualShapePayloadSource.cs`) — enumerate each shape's driver inputs, execute the shape's `Database/InforVisual/Queues/<module>/Queries/<Script>.sql` read against Infor Visual, and serialize the complete result to the JSON payload keyed by the live projection names (including `ParentPartNumber` for shape 3) that `sp_visual_<shape>_refresh` consumes; translate connectivity/timeout/login failures into `VisualSourceUnreachableException` and let genuine query errors surface as failures, then register it and add tests per FR-008 (missing)
- [x] T114 (HIGH) Add the service's and the app's MySQL connection settings and their sourcing — extend `MTM_Waitlist.Mock.Service/Models/ServiceConfiguration.cs` (and its persisted shape in `Services/ServiceConfigurationStore.cs`) with the `mtm_mock` cache connection plus the host/port/user/password-file details the four stores need for `mysqldump`, provide the `mtm_mock` connection to `VisualShapeMetadataReader` and `MockMirrorRefreshWriter`, and provide the app-side `mtm_mock` connection used by the `sp_visual_<shape>_get` fallbacks in `MTM_Waitlist.Mock`, keeping credentials out of plaintext config per FR-009/FR-012/FR-026 (missing)
- [x] T115 (HIGH) Implement the periodic per-store backup scheduler in `MTM_Waitlist.Mock.Service/` (for example `Services/BackupScheduler.cs`) — fire each store's `BackupEngine` run at its own `BackupPolicy.ScheduleLocalTime`, keep every store's schedule and artifacts independent (disabling one store must not alter another's), and record each run's outcome/timestamp; T062 covers only the refresh loop and T079 implements only the single invocation, so nothing currently triggers backups on schedule per FR-009/SC-008 (missing)
- [x] T116 (MEDIUM) Implement backup artifact retention pruning in `MTM_Waitlist.Mock.Service/Services/BackupEngine.cs` — enforce each store's `RetentionCount` by removing the oldest artifacts beyond the limit and marking `BackupArtifact.IsRetained = false`, never pruning a store's safety snapshot, and never treating a pruned file as a success; add coverage for per-store independence per FR-009 (missing)
- [x] T117 (MEDIUM) Add the service's composition root and dependency-injection wiring (for example `MTM_Waitlist.Mock.Service/Services/ServiceHostBuilder.cs`) — register `ServiceConfigurationStore`, `IVisualShapeMetadataReader`, `RefreshShapeCatalogProvider`, `IVisualShapePayloadSource`, `IMockMirrorRefreshWriter`, `RefreshEngine`, and the run-record store, run the catalog validation at startup, and report excluded shapes; `App.xaml.cs` currently starts nothing and no task specifies this wiring per T059/T075 (partial)
- [x] T118 (MEDIUM) Add the live-database integration test `MTM_Waitlist.Tests/Module_Mock_Service/MockMirrorRefreshWriterIntegrationTests.cs` (gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` with `Database=mtm_mock`) — prove a payload written through `MockMirrorRefreshWriter` reaches the real `sp_visual_<shape>_refresh`, that the swap is atomic (no partial/empty live table observed, no `_prev` table persists), and that the reported row count matches the loaded snapshot per FR-006/SC-005/FR-013; T064 exercises the engine with fakes only (missing)
- [x] T119 (MEDIUM) Define the app-side credential installation path for `IMockServiceRefreshClient` — add the client's service endpoint/token configuration (an `appsettings.json` section plus its environment-variable override) and wire it into the `MTM_Waitlist.Mock` registration added by T056, so a configured client can authenticate and an unconfigured one degrades gracefully and keeps serving cached content, with the token never logged per FR-025/FR-026 and `contracts/visual-read-fallback.md` §5 (partial)
- [x] T120 (LOW) Add the service's publish/deployment artifact and instructions for the unpackaged, self-contained `win-x64` build named in `quickstart.md` §2 step 1 (a publish profile or documented `dotnet publish` invocation under `MTM_Waitlist.Mock.Service/`), and reference it from `quickstart.md` §2 (missing)
- [x] T121 (LOW) Document or justify the generated master lists — `Database/Mock/AllTables.sql`, `AllSPs.sql`, and `AllSeeds.sql` are produced by `Database/CopilotScripts/build_mtm_mock_masters.ps1` (`-Check` is a staleness gate) rather than hand-maintained as Constitution III's wording assumes; either record the deviation with the rationale in `Database/CopilotScripts/README.md` and the DB ruleset review note, or revert to hand-maintained lists (unrequested)

---

## Phase 13: Convergence

**Added**: 2026-09-10 by `/speckit.converge` (second run), after `/speckit.implement` completed Phase 2 and most of
Phases 3–4 (T001–T031, T033, T045–T056, T109, T112).

These tasks close gaps between the specification/plan/constitution and the current code that **no existing task
covers**. Existing unticked tasks (T032, T034–T044, T057–T108, T110–T111, T113–T121) are deliberately not
duplicated here — they are already the remaining-work record for their phases. Ordered CRITICAL/HIGH first.

- [x] T122 (HIGH) Take the repository instruction and agent files off the retired mock-toggle standard and onto the Module_Mock cached-fallback contract: rewrite `.github/copilot-instructions.md` §"Mock Data & Helper Server Guidance" (currently presents `Feature.InforVisualMockData` / `Feature.RecvMockData` and the helper-server mock short-circuit as current behaviour), `.github/instructions/mcp-doc-research.instructions.md` §"Repo-Specific Focus" (same keys), and `.github/agents/module-create.agent.md` §5 "Mock data behavior" / §6 "Data path" + its "Add mock data toggle integration" step (currently *defaults* a new module to "create a new feature toggle and mock short-circuit pattern" and `Feature.<FeatureName>MockData`), so that no instruction still directs a contributor or agent to reintroduce a manual demo/mock mode; per FR-003/FR-014, SC-016, Constitution II (contradicts)
- [x] T123 (HIGH) Drive the app-side read-status state so the indicator can actually appear: nothing consumes the registered `MTM_Waitlist.Mock/Services/VisualReachabilityDetector.cs` (`IVisualReachabilityDetector.ProbeAsync` has no caller outside its own tests) and the read path in `MTM_Waitlist.Mock/Services/VisualReadFallback.cs` never feeds `IReadStatusProvider`, so the state `Module_Mock/Views/ReadStatusIndicator.xaml` renders can never leave `Unknown`; add the app-side driver (a probe loop with backoff while cached, and/or per-read `CachedReadResult.Source` provenance fed into the provider) in the `MTM_Waitlist.Mock` registration added by T056, scoped to **probing/status only** — refresh scheduling stays owned by the service per FR-025; per FR-005/FR-022, US4/AC1, plan: `ReadStatusProvider` (missing)
- [x] T124 (HIGH) Give `CachedDataAgeUtc` a real data path: the cached read cannot expose `refreshed_utc` (`sp_visual_<shape>_get` must project exactly the live column shape for FR-004, so `CachedReadResult.RefreshedUtc` is null — recorded in this file's Phase 4 Notes) and `sp_visual_read_shape_metadata_get` reports only artifact existence and the mirror's column list, so `ReadStatusSnapshot.CachedDataAgeUtc` / `IsSeedContentOnly` have no source; add an `mtm_mock` freshness read (a new procedure such as `sp_visual_<shape>_freshness_get`, or an extension of the metadata procedure) returning each shape's last successful `refreshed_utc` and seed-only state, with `create.sql` + `rollback.sql`, `Database/Mock/AllSPs.sql` and `Database/Mock/Bootstrap/update_table_descriptions.sql` registration in the same change, and read it through `IMySqlHelperServer` (`MtmMock` target) with no inline SQL; per FR-017/FR-022, `data-model.md` §12, Constitution III (partial)
- [x] T125 (MEDIUM) Extend the documentation accommodation beyond `WeekendProject/*`: `README.md` line 15 still lists `ISampleDataService` in the `MTM_Waitlist.Core` description, and `README.md` / `CHANGELOG.md` / `RELEASE-NOTES.md` contain no Module_Mock or `mtm_mock` documentation at all, so FR-028's "document the new module" and SC-016's "0 stale references in the project documentation" are met only for the `WeekendProject` subtree; sweep and update the root docs (add the new module + service app + `mtm_mock` cache to `README.md`, the end-user `CHANGELOG.md`/`RELEASE-NOTES.md`, and the stale mock reference in `.github/memories/repo/infor-visual-disposition.md`); per FR-014/FR-028, SC-016, Constitution "Documentation & Extensibility" (partial)
- [x] T126 (MEDIUM) Record the Phase-4 structural deviations in `plan.md` instead of leaving them in `tasks.md` Notes — `VisualReadShapeCatalog` and the live-read plumbing moved into `MTM_Waitlist.Mock`, `MySqlHelperServer` gained an `MtmMock` target, `VisualQueryExecutor` introduced the `Ok`/`Unreachable`/`Failed` classification that no design document specifies, and `CachedReadResult.RefreshedUtc` is null — so `plan.md` §Project Structure no longer describes the shipped architecture and the Complexity Tracking table has no row for these additions; on the next `/speckit.plan` touch add the Complexity Tracking rows (with the "why needed / simpler alternative rejected" rationale), update the Project Structure listing and `models`, and align `research.md` R1–R15 (especially R11, whose detector-driven design is superseded by per-read classification); per plan: Project Structure/Complexity Tracking, Constitution Governance (contradicts)

---

## Phase 14: Execution note — service app, US4 indicator, and T113/T114/T117 (2026-09-10, `/speckit.implement`)

**Gates at the end of this run**: build `0 warnings / 0 errors`; tests **609 total / 0 failed / 596 passed /
13 skipped** (previous run: 605/8/584/13 — the 8 `RefreshEngineTests` failures were a regression this run
introduced and then fixed; see below).

### What landed, by task

- **T059–T061 (tray lifetime, autostart, strings)**: `App.xaml.cs` is now the real lifetime — single instance via
  `AppInstance.FindOrRegisterForKey` + `RedirectActivationToAsync`, a `WinUIEx.TrayIcon` with a status/settings/
  backup-now/quit menu, no window at startup, window created lazily and **hidden** on close (the process exits only
  on Quit, which disposes the icon). Auto-start is a per-user `HKCU\...\Run` entry behind
  `IStartupRegistrationStore`, reconciled at startup by `ServiceConfigurationStore.ReconcileAutoStartAsync` and
  reported (never silently ignored) in the status payload. `Strings/en-us/Resources.resw` carries every service
  string, consumed via `GetLocalized()`.
- **T065–T070 (US4 indicator)**: `ReadStatusProvider` (detector state + freshness, no settable member — asserted by
  reflection), `Module_Mock/Views/ReadStatusIndicator.xaml` (non-interactive `InfoBar`, `IsClosable="False"`, no
  buttons, no command binding), the `Mock_Indicator.*` resource keys, and hosting inside the existing shell
  (`ShellPage.xaml`) with **no new navigation route**. The indicator needs no converter or `App.xaml` resource, so
  nothing was added there.
- **T071–T083 (US5/US6 service API and UI)**: `ServiceApiEndpoints` (`POST /api/refresh`, `GET /api/status`,
  `POST /api/backup`, `GET /api/backups`; **`/api/restore` is not routed at all**),
  `SharedTokenAuthenticationHandler` (constant-time compare, `401 {"error":"unauthorized"}`, metadata-only logging,
  and a host-level authorization fallback policy so a future route cannot be anonymous), `ServiceApiHost`
  (Kestrel on the configured bind address/port, non-fatal bind failure), `BackupEngine`, `BackupArtifactStore`,
  `RestoreService`, `ServiceSettingsPage`/`ServiceStatusPage` + view models, and the confirmation-gated restore.
- **T086/T087 (half-added shape)**: `RefreshShapeCatalogProvider` now enumerates the cache
  (`GetAllShapeMetadataAsync`) and reports shapes that have artifacts but **no catalog entry**, and the status
  payload lists them instead of omitting them.
- **T103 (retired-symbol audit)**: `RetiredSymbolAuditTests` scans code and live DB artifacts and fails on any
  retired type/key/`sp_mock_*`. It found and this run removed four genuine leftovers: the retired toggle keys seeded
  by `SetupWorkflowServiceTests`, a test that exercised the retired recv-toggle behaviour, and two retirement
  comments that named the retired catalogs.
- **T110**: the catalog validation already read artifact metadata through `sp_visual_read_shape_metadata_get`; this
  run added population-read and unregistered-shape checks through the same contract, so the inline-SQL audit still
  needs **no** exemption for metadata reads.
- **T113–T117 (service core)**: `VisualShapePayloadSource` (one set-based population read per shape, classified
  outcomes, payload keyed by the live projection names, flags emitted as 1/0), the five population scripts under
  `Database/InforVisual/Queues/Module_Mock/Populations/`, `MySqlConnectionSettings` +
  `MySqlConnectionStringResolver` (no secret ever persisted), `BackupScheduler` (per-store independent slots),
  `BackupEngine` retention pruning (safety snapshots never pruned or counted), and `ServiceHostBuilder` as the
  composition root. `VisualSourceSchemaMismatchException` was added so drift is reported as
  `failedSchemaMismatch` rather than as an anonymous failure.
- **T123/T124 (status driver + freshness)**: `VisualReachabilityProbeHost` (probing only — the app still never
  schedules a refresh, FR-025) drives the detector with a 30 s / 5 min backoff, and the new
  `sp_visual_read_shape_freshness_get` artifact plus `IVisualShapeFreshnessReader` give `CachedDataAgeUtc` a real
  source on both the app and service sides.

### Two design decisions this run had to make, recorded for review

1. **The driver population is read set-based, not key-by-key (T113).** T113's wording ("enumerate each shape's
   driver inputs, execute the shape's `<Script>.sql`") would mean one Infor Visual round trip per driver key —
   ~42,800 for the operator-approved open-work-order scope, plus their sequences and subordinate parts. Each shape
   therefore declares a `PopulationScriptRelativePath` that enumerates the population **and** projects the result
   inside Visual, so the mirror still covers exactly the approved scope with one round trip per shape. The
   per-key scripts remain the application's live reads, unchanged.
2. **The mirror row tally had to be widened (T113).** The refresh procedures' inline tally covered 0..1999 rows,
   which cannot hold the ~42,800-order population. All five refresh procedures now cover 0..99999 and the load
   validation still aborts with the live table untouched when the payload does not match what was staged.

### Two findings this run had to fix

- **The client app was compiling the service app's XAML.** `MTM_Waitlist.csproj` excluded sibling projects' `.cs`
  but not their `.xaml`, so the service pages were globbed into the client app (XamlCompiler `WMC0909`/`WMC1111`).
  `Page`/`ApplicationDefinition`/`PRIResource`/`Content`/`None` removals for `MTM_Waitlist.Mock.Service` were added.
- **Adding the population-script requirement invalidated the engine tests' synthetic shapes.** They are now given
  the shipped population paths, and the test project ships `Module_Mock/Populations` content like the service does.

### Still open after this run (not ticked)

- **Verification suites**: T058 (present and passing, but not re-reviewed), T078, T084, T111, T118 — the
  listener-level API assertions (a real `401`, a real `404` for `/api/restore`) and the live-MySQL integration
  tests are the gaps; `ServiceApiSecurityTests` covers the operation-level half of T078/T084 including the
  "no code path from the API to a restore" guard.
- **Phase 10.1 SP-first conversion** (T088–T097): **every conversion task (T088–T097) is now complete**, including
  T094 and T097 in the run recorded above. What remains in this block is its **guard, T098** — the inline-SQL
  audit. No C# file is known to still carry statement text for any converted artifact; T098 exists to prove that
  by construction rather than by review, and it should look specifically for the **wrong-helper-for-DML** shape
  found three times now (a `rows.Count` read after an `INSERT`/`UPDATE`/`DELETE`), which a marker-based scan
  alone will not catch.
- **Phase 10.2 gap fixes** (T099–T101), **T107/T108** (internal-store unavailable handling), **T085** (playbook),
  **T102/T119–T122/T125/T126** (documentation, credential installation path, publish artifact, generated
  master-list deviation record, instruction/agent rewrite, root-doc sweep, plan deviation rows).
- **T104–T106** were re-run informally for this change (the numbers above); they remain unticked because the
  end-to-end walkthrough (T106) and the live-DB test subset (T105) have not been executed in this environment,
  which has no reachable MySQL or Infor Visual.

---

## Phase 15: Convergence

**Added**: 2026-09-10 by `/speckit.converge` (third run), after `/speckit.implement` completed the service app,
the US4 indicator, and T113/T114/T117 (gates that run: build 0 warnings / 0 errors; tests 609 total / 0 failed).

These tasks close gaps between the specification, plan, and constitution and the current code that **no existing
task covers**. Existing unticked tasks (T058, T078, T084, T085, T088–T102, T104–T108, T111, T118–T122, T125,
T126) are deliberately not duplicated here — they are already the remaining-work record for their phases.
Ordered CRITICAL/HIGH first.

- [x] T127 (HIGH) Wire the refresh run records so per-item last-refresh status is actually recorded and surfaced — nothing in production subscribes to `RefreshEngine.RunCompleted` (`MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs` L116/L446; the only subscribers are in `MTM_Waitlist.Tests/Module_Mock_Service/RefreshEngineTests.cs`), so `RefreshRunRecordStore.RecordAsync` is never called and `GetLastRun` always returns `null`: `/api/status` reports `lastRunUtc`/`lastOutcome`/`lastRowCount` as null and the service status surface shows "Never" for every shape however many cycles have run (`BackupEngine` writes `BackupArtifactStore`, so only the refresh half is missing); subscribe the store in the service composition root/startup (`Services/ServiceHostBuilder.cs`, `App.xaml.cs`) and assert a completed cycle is readable back through `IReadOnlyList<RefreshRunRecord> GetAllLastRuns()` per FR-013 (missing)
- [x] T128 (HIGH) Put the on-demand refresh behind the engine's single-cycle gate — `ServiceApiOperations.RunRefreshAsync` checks `RefreshEngine.IsCycleRunning` (a read; `Services/ServiceApiOperations.cs` ~L241–252) and then calls the **ungated** public `RefreshEngine.RefreshShapeAsync` once per shape, so the check is a time-of-check/time-of-use gap: a scheduled cycle or a second request can start between the check and the call, and two writers can reach the same stage twin and `RENAME` swap; run the cycle through the gated seam (`TryRunShapesAsync`/`TryRunCycleAsync`, returning the contract's `409 refreshInProgress` when it is rejected) so `contracts/mock-service-http-api.md` §2's "one cycle at a time" and FR-006/SC-005's never-a-partial-snapshot guarantee hold on every path, and cover it with an assertion that a racing request is refused rather than overlapped (contradicts)
- [x] T129 (HIGH) Let the service start — tray and settings — when no cache connection is configured — `ServiceHostBuilder.RequireCacheConnection` throws while the container is resolved (`Services/ServiceHostBuilder.cs`: the `RefreshShapeCatalogProvider`, `IMockMirrorRefreshWriter`, and `IVisualShapeFreshnessReader` factories), and `App.StartAsync` catches it and calls `Exit()` **before** `CreateTrayIcon()`, so on a host that has not been pointed at MySQL there is no tray icon, no settings window, and no in-product way to configure the host — which contradicts FR-012 ("configure and persist ... external-source connection details") and the service-local configuration rationale in `contracts/mock-service-configuration.md` §1 (configurable and reportable while sources are unreachable); make the DB-dependent registrations resolve without throwing (report "not configured" as the normal outcome of a refresh/status read) and add a startup assertion that the tray/settings surface is created with and without a configured cache connection (contradicts)
- [x] T130 (MEDIUM) Complete the extensibility seam's published contract — the playbook's step 3 (`contracts/mock-service-configuration.md` §4) still lists only `sp_visual_<shape>_{refresh,get}`, but a new shape now also needs a branch in `Database/Mock/StoredProcedures/sp_visual_read_shape_freshness_get/create.sql` (a per-table `UNION ALL` — the one report that must enumerate shapes) or its freshness is silently absent; and the catalog record in §3, `data-model.md` §2, and `data-model.md` §12 do not contain `populationScriptRelativePath`, so a shape added by following the documents fails `RefreshShapeCatalogProvider` validation ("declares no population read") while one added without it is never refreshed — document the field, the required set-based population read, and the freshness branch in `contracts/mock-service-configuration.md` §3/§4 and `data-model.md` §2/§12, and record the widened mirror row tally the refresh procedures now carry (0..99999, `data-model.md` §3.6/§11) per FR-016/SC-012 and FR-028 (partial)
- [x] T131 (MEDIUM) Probe at the live cadence until a state is established — `VisualReachabilityProbeHost.RunAsync` selects `CachedProbeInterval` (5 minutes) whenever the detector is not `Live` (`MTM_Waitlist.Mock/Services/VisualReachabilityProbeHost.cs`), which includes `Unknown`, so with the documented 2-consecutive-failure hysteresis the first `Cached` transition lands ~5 minutes after application start: the read-status indicator cannot be observed to appear promptly and US4's "becomes visible on entering Cached" is unreasonably delayed; use the live interval while the state is `Unknown` (or probe until a state is established) and assert the first-transition timing per FR-005 and US4/AC1 (partial)
- [x] T132 (MEDIUM) Verify backup retention semantics — `BackupArtifactStore.PruneAsync` and `BackupEngine.EnforceRetentionAsync` have no assertions (the test project only constructs the store, in `Module_Mock_Service/ServiceApiSecurityTests.cs`), so "removes the oldest artifacts beyond `RetentionCount`", "never prunes a safety snapshot and never counts it against the limit", "marks a pruned artifact `IsRetained = false` rather than forgetting it", and "disabling one store changes no other store's schedules or artifacts" are all unproven; add coverage per FR-009/SC-008 (partial)
- [x] T133 (MEDIUM) Verify the service payload source — `VisualShapePayloadSource` has no test (only the `StubPayloadSource` double in `Module_Mock_Service/RefreshEngineTests.cs`), so the payload contract it must satisfy is unasserted: the keys are exactly the shape's input parameters then its output columns in catalog order, a `bool` flag is emitted as `1`/`0` (a JSON boolean would be cast to 0 by the procedures' `CAST(... AS SIGNED)`), a missing or unexpected column raises `VisualSourceSchemaMismatchException` and is recorded as `failedSchemaMismatch` with the live snapshot intact, and an unreachable source raises `VisualSourceUnreachableException`; add the tests T113's text requires per FR-008/FR-020 and SC-005 (partial)
- [x] T134 (MEDIUM) Repair the Spec Kit scaffold drift the constitution records as an open follow-up — `Resolve-TemplateContent` is called by `.specify/scripts/powershell/resolve-template.ps1` L25 and `.specify/scripts/powershell/setup-tasks.ps1` L60 and is defined in no repository script (a workspace search finds only those two call sites and the constitution's TODO), so template resolution fails with "Resolve-TemplateContent is not recognized as a name of a cmdlet" and the next `/speckit.specify` / `/speckit.tasks` run is blocked; repair by re-running `specify init` or syncing the PowerShell scripts from the installed core pack, then verify a template resolves, per `.specify/memory/constitution.md` TODO(scaffold-drift) (missing)
- [x] T135 (LOW) Verify the logon auto-start reconciliation — `ServiceConfigurationStore.ReconcileAutoStartAsync` has no test, so both directions (setting on with no `Run` entry registers it; setting off with an entry removes it), the re-registration when the entry points at a different executable, and the reported mismatch ("never silently ignored", FR-007, `contracts/mock-service-configuration.md` §2) are unproven; assert them against `IStartupRegistrationStore` with a fake (partial)
- [x] T136 (LOW) Propagate the constitution's v1.1.0 amendment to the instruction file it names — `.github/instructions/mcp-doc-research.instructions.md` still carries the pre-amendment MCP-first policy and does not mirror Principle IV's NON-NEGOTIABLE Context7 / Microsoft Learn rule or the mandatory Serena location-and-config self-healing rule, which the amendment's own Sync Impact Report lists as a required follow-up outside `/speckit.constitution`'s write scope (partial)
- [x] T137 (LOW) Align the plan's constitution citation with the ratified amendment — `plan.md` records "**Constitution**: ... — **v1.0.0, ratified 2026-09-09**" and evaluates its Phase 0/Phase 1 gates against "v1.0.0 ... six principles", while `.specify/memory/constitution.md` is now **v1.1.0 (2026-09-09)** with Principle IV materially expanded; update the citation and restate the gate verdicts against v1.1.0 on the next `/speckit.plan` touch, per Constitution Governance (contradicts)

**Phase 15 execution note** (2026-09-10, `/speckit.implement` run 4): all eleven tasks landed. Gates that ran:

- `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false`
  → `Build succeeded. 0 Warning(s) 0 Error(s)`.
- `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`
  → `Failed: 0, Passed: 629, Skipped: 13, Total: 642` (exit 0). The 13 skipped are the pre-existing
  live-database integration suites (e.g. `RequestTypeCatalogServiceIntegrationTests`,
  `ActiveJobReadBackSpIntegrationTests`, `ConfigImagesLocationsIntegrationTests`), which skip when no
  MySQL host is configured — not failures and not affected by this work.

Where each task landed, and what proves it:

- **T127** — `RefreshRunRecordRecorder` (`MTM_Waitlist.Mock.Service/Services/RefreshRunRecordRecorder.cs`)
  subscribes `RefreshEngine.RunCompleted` in its constructor and is registered in
  `ServiceHostBuilder.RegisterRefreshPipeline`, so the event now has a production subscriber. Proven by
  `RefreshCycleGateTests.Recorder_PersistsEveryCompletedRun_SoLastRunStatusIsReal` and
  `Recorder_StopsRecordingOnceDisposed`.
- **T128** — `ServiceApiOperations.RunRefreshAsync` now runs the cycle through the gated
  `RefreshEngine.TryRunShapesAsync`, mapping the rejected (`null`) result to the contract's
  `409 refreshInProgress`. The engine's overlapping start-or-refuse callback now covers this path. Proven by
  `RefreshCycleGateTests` (refusal while a cycle runs, acceptance once released, only-requested-shapes).
- **T129** — the cache-dependent registrations resolve without throwing (`ResolveCacheConnection` returns
  an empty string when nothing is configured; the reader/writer/freshness types report
  `MySqlConnectionStringResolver.NotConfiguredMessage` at operation time), and `App.xaml.cs` creates the
  tray icon before starting the engines and treats an engine-start failure as non-fatal. Proven by
  `ServiceStartupTests` (container + tray/settings surfaces resolve and validate unconfigured, with the
  unmet reason reported per shape and on the status payload).
- **T130** — `contracts/mock-service-configuration.md` §3/§4 and `data-model.md` §2/§12/§12a/§12b document
  `populationScriptRelativePath`, the set-based population read, the freshness-procedure branch, and the
  widened 0..99999 mirror row tally.
- **T131** — `VisualReachabilityProbeHost` probes at the live interval while the detector is not yet
  `Cached`, so the first `Cached` transition (and the indicator that depends on it) is established promptly
  instead of after one cached-interval wait; the XML docs state that the **probe** cadence is not the
  **refresh** cadence.
- **T132/T133/T135** — new suites `BackupRetentionTests` (6), `VisualShapePayloadSourceTests` (10),
  `AutoStartReconciliationTests` (8), plus `RefreshCycleGateTests` (5) and `ServiceStartupTests` (4), with
  the shared doubles in `AcceptingMetadataReader.cs`.
- **T134** — `Resolve-TemplateContent` added to `.specify/scripts/powershell/common.ps1`;
  `resolve-template.ps1 tasks-template` exits 0 and emits the real template markdown. (Landed with the
  constitution's 1.1.1 amendment.)
- **T136** — `.github/instructions/mcp-doc-research.instructions.md` now mirrors Principle IV as amended
  (NON-NEGOTIABLE Context7 / Microsoft Learn rule, mandatory Serena self-healing, cached-fallback focus).
- **T137** — `plan.md` and this file now cite **v1.1.1** with an amendment note.

**Cadence clarification recorded here because two different intervals were conflated.** The cache refresh
cadence is unchanged at **3 hours** on the eight local-midnight-anchored slots (00:00/03:00/06:00/09:00/
12:00/15:00/18:00/21:00 server-local), asserted by `RefreshEngineTests` and
`ServiceConfigurationStoreTests`. The **5 minutes** in `VisualReachabilityProbeHost.CachedProbeInterval` is
the *reachability probe* backoff used once the cached fallback is already in effect — it is not a refresh
interval and no refresh is triggered by it. The `Unknown`-state fix (T131) therefore changes only how
promptly a state is established, never the schedule.

---

## Phase 16: Convergence

**Added**: 2026-09-10 by `/speckit.converge` (fourth run), after `/speckit.implement` completed all of
Phase 15 (gates that run: build 0 warnings / 0 errors; tests 642 total / 0 failed / 629 passed / 13
environment-gated skips).

These tasks close gaps between the specification, plan, and constitution and the current code that **no
existing task covers** — every other remaining item is already recorded by an unticked task (T058, T078,
T084, T085, T088–T102, T104–T108, T111, T118–T122, T125, T126) and is deliberately not duplicated here.
Ordered CRITICAL first.

- [x] T138 **(CRITICAL)** Take the inline statement text out of the restore verification path — `RestoreService.BuildVerificationSummaryAsync` (`MTM_Waitlist.Mock.Service/Services/RestoreService.cs` L415) builds `--execute=SELECT COUNT(*) FROM {table};` and hands it to the `mysql` CLI, so application code carries a MySQL statement marker outside any stored-procedure call site; that is exactly what Constitution III forbids ("Every data operation MUST go through a stored procedure; inline or hard-coded SQL statement text MUST NOT remain in application code") and exactly what the SC-013 inline-SQL audit specified by T098 will flag, since the other inline-SQL sites are enumerated by T088–T096 and this file is enumerated by none of them. Either move the post-restore row-count check behind a stored procedure (a per-store row-count routine registered with `create.sql` + `rollback.sql`, the mock/app master list, and `update_table_descriptions.sql` in the same change) or record an explicit written exemption next to the call the way `VisualShapeMetadataReader` records "no SQL statement text other than the procedure call itself, so it satisfies the inline-SQL audit (task T098) without needing an exemption" — and make T098's audit agree with whichever route is chosen (contradicts)

**Phase 16 execution note — T138** (2026-09-10, `/speckit.implement` run 5):

- **Both statement sites left the C#**, not just the one the task named. `RestoreService` also built
  `DROP DATABASE IF EXISTS {db}; CREATE DATABASE {db} …` inline (L174) for step 2 of the restore. Both are now
  reviewed artifacts: `Database/Mock.Service/Restore/replace_database.sql` and `…/verify_restore.sql`. The
  service reads the artifact, substitutes the store's database name (from `BackupStore.ToDatabaseName()`, one
  of four fixed names — never operator input), writes it to a temporary file, streams it to the `mysql` client
  on stdin, and deletes the temporary file. `grep` for `SELECT |INSERT INTO |UPDATE …SET |DELETE FROM |CALL |
  DROP |CREATE ` over `RestoreService.cs` now matches only prose in doc comments.
- **A second defect was found in the same path and fixed.** `s_verificationTables` named
  `config_settings_values` and `core_workstations_registry`. Both are waitlist tables, so restoring
  `mtm_mock`, the receiving store, or the WIP store could only ever report "unverified"; and
  `core_workstations_registry` **no longer exists** in the schema (it is `core_computers_registry`), so the
  check proved nothing even for the waitlist store. Verification is now store-agnostic — it reports the
  replaced store's base-table count beside the artifact's size — which is true for all four stores and still
  catches the failure that matters (a reload that reported success but left nothing behind). The client's
  stdout is now read for this (`RunClientAsync` returns `ClientResult(ExitCode, StandardOutput)`).
- **Deviation recorded** (constitution Governance) in `plan.md` → *Complexity Tracking → Recorded deviation*,
  and in `Database/Mock.Service/Restore/README.md`. Step 2 cannot be a stored procedure: MySQL rejects
  `DROP`/`CREATE DATABASE` inside a routine, and such a routine would live in the database being dropped.
  Part of Principle III that protects the codebase (no statement text in C#) is preserved in full.
- **Live validation** (constitution III) against MySQL **5.7.24** at `172.16.1.104`, via the same stdin
  mechanism the service uses: `replace_database.sql` exited `0` against a throwaway database name (dropped
  immediately afterwards), and `verify_restore.sql` returned `10` base tables for `mtm_mock` — the five
  mirrors plus the five stage twins, which also independently confirms the cache schema is deployed.
- **Gates**: `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false`
  → `Build succeeded. 0 Warning(s) 0 Error(s)`; full suite → `total: 642, failed: 0, succeeded: 629,
  skipped: 13` (the 13 are the pre-existing live-DB integration tests, skipped because
  `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` is not set in this environment).
- **Environment findings for the remaining SP-first tasks (T088–T098)** recorded here so they are not
  re-derived: the live host is **MySQL 5.7.24** at `172.16.1.104` (root), a MySQL **8.0 client** is installed
  at `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe`, `localhost` has **no** `mtm*` schema, and the
  live routine names differ from this file's assumptions — `sp_config_hot_workcenters_{get,delete}_for_computer`
  (not `_for_workstation`), `sp_receiving_history_average_coil_weight` exists in the repo but is **not
  deployed**, and the receiving schema uses `sp_Dunnage_*` naming. Decision taken autonomously: **live names
  win** — no renames or drops of objects other applications call; only genuinely missing artifacts get
  created, additively.
- [x] T139 **(HIGH)** Correct the release notes to the architecture that shipped, including the refresh cadence — `RELEASE-NOTES.md` states that "a lightweight service … probes the Infor Visual database every 30 seconds" and "signals every running client to start reading from the cache", that while cached "probing continues on a backed-off schedule — 30 seconds, then 1 minute, then 5 minutes, repeating", and that on recovery the service "refreshes it at that moment as well". None of that is the shipped behaviour: the *client application* probes and decides locally (`MTM_Waitlist.Mock/Services/VisualReachabilityProbeHost.cs` — a two-value cadence of 30 s live / 5 min cached, with no 1-minute step and no client-signal broadcast), the service's connectivity probe only reports `visualReachable` in the status payload (`ServiceApiOperations.GetStatusAsync`), and no code path refreshes on recovery (the application is forbidden from refreshing at all by FR-025, and the app-side refresh client is still unwired — T119). The document also never states the real schedule, so its only time figures are the probe's — which is what makes it read as a 5-minute refresh. Rewrite the "How the switch happens" and "Decoupled Architecture" bullets to the shipped design and state the refresh cadence explicitly: **3 hours**, on the eight local-midnight-anchored slots (00:00/03:00/06:00/09:00/12:00/15:00/18:00/21:00 server-local), per `ServiceConfiguration.RefreshInterval` and `ServiceConfigurationStoreTests`; the 30 s / 5 min figures belong to detection, not refresh (FR-028/SC-016, Constitution *Documentation & Extensibility*) (contradicts)

---

## Phase 17: Execution note — SP-first audit, real-data gaps, credential path, docs (2026-09-11, `/speckit.implement`)

**Gates at the end of this run**: build `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64
/m:1 /nodeReuse:false` → `Build succeeded. 0 Warning(s) 0 Error(s)`; full suite
`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` →
`Failed: 0, Passed: 661, Skipped: 13, Total: 674`. The 13 skipped remain the pre-existing live-database
integration suites (they skip when `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` is unset, and this environment has no
reachable MySQL/Infor Visual, so the live subset of T105 was not re-run).

### What landed, by task

- **T098 (inline-SQL audit)** — `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs`, three tests.
  1. `NoMySqlStatementTextRemainsInApplicationCode` — strips C# comments (preserving string-literal contents,
     including verbatim, interpolated, and raw literals) and fails on `SELECT` / `INSERT INTO` / `UPDATE … SET` /
     `DELETE FROM` / `CALL` / DDL markers. Markers are matched **case-sensitively**, because SQL keywords are
     written upper-case here while `Select…`, `Call Initialize…`, and `selected_*_json` are prose or identifiers.
     The test project is excluded: its integration suites legitimately seed rows with raw SQL.
  2. `RawSqlSeamIsConfinedToItsReviewedAllowlist` — the wrong-helper-for-DML guard. The defect shape needs a DML
     statement somewhere to route, so the raw seams (`ExecuteSqlQueryAsync` / `ExecuteSqlNonQueryAsync`) are pinned
     to a three-entry allowlist: the contract, the implementation, and `WipFloorInventoryService` (which loads the
     checked-in MTMWipApp queue script `GetWipFloorQuantities.sql` by name — Discovery/03 §C records it as a file
     load, not an inline literal). A new raw-SQL call site now fails the audit instead of silently reopening the
     defect.
  3. `EveryRawSqlSeamAllowlistEntryIsStillAccurate` — a stale exemption fails rather than lingering.
  The scan needed no exemption for the shape metadata/freshness reads: they pass a procedure-name constant with
  `CommandType.StoredProcedure`, so they carry no statement text at all.

- **T099 (coil availability is read, not assumed)** — `MTM_Waitlist.Waitlist.NewRequest/Services/
  CoilAvailabilityService.cs` rewritten. The coil is taken from the **saved Work Center Setup job** for the work
  center (`sp_setup_active_jobs_latest_by_work_center_get`, `MtmWaitlist`), selecting the subordinate part whose
  part number carries the `MMC` prefix — the same rule `ActiveJobItemResolverService.CanonicalCategory` applies to
  the same payload — and the average skid weight comes from `IAverageCoilWeightService`. No active job, a job for a
  different work center, no coil part, or a malformed payload all report **no coil** rather than defaulting to one.
  **Recorded choice:** the interface doc said the source would be "the live Infor Visual job/coil data, resolved
  through the SQL queue path". The coil already lives on the job payload the Setup workflow populated from Visual
  (including that workflow's own fallback), so reading the saved record avoids inventing a second, unverifiable
  Visual query for the same fact. The interface doc was updated to describe what shipped rather than left
  contradicting the code.

- **T100 (one authoritative request-type source)** — `MTM_Waitlist.Settings/Services/ImageLocationService.cs` no
  longer reads `Assets/Config/waitlist-request-types.json`. A request type's or subtype's configured
  `default_image_path` is now read from `sp_waitlist_request_types_get` / `sp_waitlist_request_subtypes_get` by its
  stable `public_id` GUID. The JSON file is no longer a production source (it remains the documented seed
  provenance for `waitlist_request_types`, and a test fixture for the flow rules), so `RequestTypeCatalogService` +
  the catalog procedures are the single authoritative source. The cascade order is now database override → catalog
  `default_image_path` → default asset.

- **T101 (coverage for both gap fixes)** — `CoilAvailabilityServiceTests` rewritten to the new contract (7 tests:
  the real coil read with the procedure name, the target, and `SqlCallCount == 0` pinned; no saved job; a job
  belonging to another work center; a job with no coil part; a malformed payload; a missing work center reading
  nothing; cancellation). `ImageLocationServiceCascadeTests` gained three catalog tests (request-type path from the
  catalog procedure, subtype path from its procedure, a missing catalog path falling back to the default asset) and
  now drives `FakeMySqlHelperServer` instead of a connection-less helper.

- **T119 (app-side credential installation path)** — `MockServiceRefreshClient` **did not exist** (T072's tick
  covered the contract and model only), so this task implemented the client as well as the installation path:
  `MTM_Waitlist.Mock/Models/MockServiceClientOptions.cs` (resolved environment-first:
  `MTM_MOCK_SERVICE_ENDPOINT` / `MTM_MOCK_SERVICE_TOKEN`, then the `MockServiceClient` configuration section) and
  `MTM_Waitlist.Mock/Services/MockServiceRefreshClient.cs` (`POST /api/refresh` with the `X-MTM-Mock-Token`
  header). Every failure — no endpoint, no credential, service absent, `401`, `409`, timeout, unreadable response —
  returns `RefreshRequestResult.Unavailable(...)` and leaves the app on cached content; the credential is never
  echoed in a result message. Registered in `AddVisualReadFallback`, and `appsettings.json` gained an **empty**
  `MockServiceClient` section (the credential is installed through the environment, never a committed file, per
  FR-026). Tests: `MTM_Waitlist.Tests/Module_Mock/MockServiceRefreshClientTests.cs` (7 tests, including that the
  header carries the credential and that no failure message contains it).

- **T120 (publish/deploy artifact)** — `MTM_Waitlist.Mock.Service/Properties/PublishProfiles/
  win-x64-selfcontained.pubxml` (unpackaged, self-contained, `win-x64`, folder publish) plus
  `MTM_Waitlist.Mock.Service/README.md` (publish, deploy, first run, verify, uninstall). `quickstart.md` §2 step 1
  now names both and shows the CLI equivalent.

- **T121 (generated master lists)** — the deviation is recorded rather than reverted: a review note in
  `Database/Database-Ruleset.md` → *Artifact Layout and Release Governance* (why generated, why it is safe, that
  `-Check` is the staleness gate, that `mtm_mock` only is affected, and that the create/rollback +
  `update_table_descriptions.sql` obligations are unchanged) and a matching section in
  `Database/CopilotScripts/README.md`.

- **T122 (instructions off the retired standard)** — `.github/copilot-instructions.md`: the "Mock Data & Helper
  Server Guidance" section (which presented `Feature.InforVisualMockData` / `Feature.RecvMockData`,
  `MockToggleService`, and the helper-server short-circuit as current behaviour) is replaced by
  "Cached-Fallback & Helper Server Guidance", stating the five rules that now hold (no demo mode; internal stores
  always live; external reads fall back automatically on unreachability only; the app never refreshes; every data
  operation goes through a stored procedure). `.github/agents/module-create.agent.md`: the "Mock data behavior" and
  "Data path" defaults and the "Add mock data toggle integration" step are replaced by the real contract (live
  stores, stored-procedure seam, `IVisualReadFallback` for Visual reads); the mockup-UI question is reworded so it
  cannot be read as demo data. `mcp-doc-research.instructions.md` was already corrected by T136.

- **T125 (root docs)** — `README.md`: `ISampleDataService` removed from the `MTM_Waitlist.Core` description, the
  new `MTM_Waitlist.Mock` and `MTM_Waitlist.Mock.Service` bullets added, and a new *Infor Visual cache (`mtm_mock`)*
  section added (what the cache is, where each piece lives, the four rules, and the generated-master-list note).
  `CHANGELOG.md`: new end-user section 8 describing the automatic failover, the status indicator, and the fact that
  app data is always live. `RELEASE-NOTES.md` already carried the Module_Mock architecture from T139, so it needed
  no further change. `.github/memories/repo/infor-visual-disposition.md`: the stale "(no mock FG-10042)" reference
  replaced with the current boundary.

- **T126 (plan deviations)** — `plan.md` → *Project Structure* now lists the shipped `MTM_Waitlist.Mock` and
  `MTM_Waitlist.Mock.Service` layouts (including `VisualReadShapeCatalog`, `VisualQueryExecutor`,
  `VisualShapePayloadSource`, `RefreshRunRecordStore`, `BackupScheduler`, `ServiceHostBuilder`, the publish
  profile) and the `Database/Mock.Service/Restore/` artifacts; *Complexity Tracking* gained four rows with the
  why-needed / simpler-alternative-rejected rationale for the Phase-4 additions (the catalog + executor + script
  store + per-shape types moving into `MTM_Waitlist.Mock`; the `Ok`/`Unreachable`/`Failed` classification; the
  `MtmMock` target; and `CachedReadResult.RefreshedUtc` being null with freshness owned by
  `sp_visual_read_shape_freshness_get`). `research.md` R11 gained a *Superseded in part* note explaining that the
  **indicator** is detector-driven while each **read** decides for itself from the classification, and why that is
  stronger.

- **T085 (published playbook)** — reproduced, with the step the earlier drafts were missing (step **3b**, the
  set-based population read plus the shape's `UNION ALL` branch in `sp_visual_read_shape_freshness_get`) and the
  `populationScriptRelativePath` catalog field, into `WeekendProject/Module_Mock/Spec.md` §11 and
  `WeekendProject/Module_Mock/Plan.md` §7, and cross-linked from `WeekendProject/Module_Mock/Tasks.md` (Phase 1
  header). It points at `contracts/mock-service-configuration.md` §4 as the authoritative wording.

- **T102 (documentation accommodation)** — `WeekendProject/ChangeLog.md` gained a dated 2026-09-11 Added / Changed /
  Removed entry for the failover and the removal. New `WeekendProject/Module_Mock/README.md` records the shipped
  architecture, the rules enforced by tests, the removal record by family (A / A′ / B, with the deliberately
  retained `SampleOrder` and `WaitlistCoilInfo`), and which sibling documents are historical. All three
  `WeekendProject/Module_Mock/Discovery/*` files gained a status banner saying they are historical inventories
  (Discovery/03 records that every site was converted). `WeekendProject/PromptFiles/` gained a `README.md` archive
  note explaining that the two stale families there (mock toggles, sample catalogs) describe mechanisms that no
  longer exist, and that the files are kept as archives rather than rewritten — rewriting a 2026-08/09 checklist to
  match today's code would erase the record of what was asked for at the time.

- **T111 (shape-addition no-downtime)** — `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests.cs`,
  four tests: the shipped catalog is exactly the five published keys and they are unique; appending a sixth shape
  leaves every existing shape's derived artifact names, ordered inputs, and ordered outputs identical and cannot
  collide with them (the property FR-020 rests on — the names are derived from the key); every shipped shape's
  mirror table, stage twin, `get`/`refresh` procedures, source query, and population read exist on disk under those
  derived names; and each shipped shape has exactly one `IVisualReadFallback` registration.

- **T058 (re-reviewed, then closed)** — the parity suite was present and passing but had not been re-reviewed. It
  now also carries the two assertions the task named and the file did not have:
  `TheReadSeam_CannotTellACallerWhichSourceAnswered` (reflection over `IVisualReadFallback<,>`: `ReadAsync` returns
  a plain `IReadOnlyList<TRow>` and not a wrapper, and provenance is opt-in through exactly one method) and
  `NoFallbackCode_PointsAtAnInternalStore` (no file under `MTM_Waitlist.Mock/` may name
  `MySqlDatabaseTarget.MtmWaitlist` / `MtmReceivingApplication` / `MtmWipApplication` — the FR-027 guard, asserted
  by construction rather than by review). One assertion in the first of these was written wrongly on the first
  attempt (it inspected `IReadOnlyList<TRow>`'s own properties, which always exist) and was corrected to assert the
  return type's shape; the gate numbers above are the post-correction run.

### Still open after this run (not ticked)

- **T078** and **T084** — the listener-level halves: a real `401` from a running Kestrel host, a real `404` for
  `/api/restore`, and the `BackupRestoreTests` assertions. `ServiceApiSecurityTests` already covers the
  operation-level half of both, including the "no code path from the API to a restore" guard.
- **T106** — the end-to-end acceptance walkthrough needs a running service, a deployed `mtm_mock`, and reachable
  MySQL/Infor Visual; none is available in this environment, and its SC-007/SC-008 halves are 30-day observation
  windows rather than one-shot tests.
- **T107 / T108** — the internal-store bounded-retry policy and the per-screen `Unavailable` surface (FR-021). The
  design is settled (`data-model.md` §10); the work is a Core retry seam plus the affected screens' state surface,
  and it was deliberately not started rather than left half-built.
- **T118** — the live-`mtm_mock` integration test for `MockMirrorRefreshWriter`; gated on a reachable MySQL with
  `Database=mtm_mock`.

**Build-hygiene note for the next run**: the first full build in this run surfaced two genuine nullable warnings in
`CoilAvailabilityService.cs` (CS8604 / CS8601) that an incremental build hides, because the project is not
recompiled once it is up to date. They are fixed (the JSON DTO's string members are non-nullable with
`string.Empty` defaults, and the coil filter rejects an empty part number explicitly), and the gate above is the
post-fix whole-solution build. This is the same class of issue T112 recorded for `NETSDK1206`: a warm build can
report `0 Warning(s)` while a cold build does not.

---

## Phase 18: Execution note — internal-store availability, API/backup verification, live-DB mirror test (2026-09-11, `/speckit.implement`)

**Gates at the end of this run**: build `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64
/m:1 /nodeReuse:false` → `Build succeeded. 0 Warning(s) 0 Error(s)`; full suite
`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` →
`Failed: 0, Passed: 684, Skipped: 15, Total: 699`. The skips are the environment-gated live-database suites:
the 13 that pre-existed plus the 2 methods of the new `MockMirrorRefreshWriterIntegrationTests`, which report
inconclusive when `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` is unset.

### T107 — internal-store unavailable handling (FR-021, `data-model.md` §10)

The data-access seam used by every affected screen is `MySqlHelperServer`, so the bounded retry lives there
rather than in each caller:

- **`MTM_Waitlist.Core/Services/InternalStoreRetryPolicy.cs`** — new. Three attempts total, with 1 s before the
  second and 2 s before the third, and 4 s as the advertised next-retry guidance. The delay is injectable so a
  test asserts the attempt count without waiting seven seconds.
  **Ambiguity resolved explicitly**: FR-021's one sentence ("up to three attempts with delays of approximately
  1 s, 2 s, and 4 s") can be read as three attempts or as three retries after the first; `data-model.md` §10
  settles it with "after the third failure the screen shows the `Unavailable` state", so the policy is three
  attempts and the 4 s figure is what the state shows as "next attempt after". The reading is recorded in the
  type's XML docs so the next reader does not have to re-derive it.
- **`MTM_Waitlist.Core/Services/StoreAvailabilityTracker.cs`** + **`Contracts/Services/IStoreAvailabilityTracker.cs`**
  — new. Per-store `InternalStoreAvailability` (`Store`, `Status`, `LastAttemptUtc`, `RetryCount`,
  `NextRetryUtc`, `Message`) with a `Changed` event. Session-scoped by design: a stale "unavailable" claim must
  not survive a restart. A change of status notifies; a repeat failure also notifies, so a manual retry that
  fails again refreshes what the screen is showing; a repeat *success* does not, so a busy screen raises no
  notification storm.
- **`MySqlHelperServer`** — every seam method (stored procedure and raw SQL, query and non-query) now runs
  through one `ExecuteWithBoundedRetryAsync`, which records the outcome in the tracker. Caller cancellation is
  propagated, not retried and not reported as a store outage. A store with **no connection configured** is not
  reported as unavailable — an unconfigured deployment is not an outage, and reporting one would show an
  operator a failure they cannot act on.
- **`MTM_Waitlist.Core/Models/InternalStoreUnavailableState.cs`** — new. The per-screen state: `IsUnavailable`,
  `StoreName`, `LastAttemptUtc`, `RetryCount`, `NextRetryUtc`, `Message`, `IsRetrying`, and a generated
  `RetryCommand` that re-runs that screen's own load. It subscribes to the tracker and clears itself on
  recovery. It carries no user-facing English — the screen composes the sentence from its own localized labels
  (constitution V).
- **Wired into the waitlist list screen** (`MTM_Waitlist.Waitlist.View/ViewModels/WaitlistViewViewModel.cs` +
  `Module_Waitlist/Views/WaitlistViewPage.xaml`): the screen owns one state object for `mtm_waitlist`, shows an
  `InfoBar` in its own surface (not an app-wide banner, not closable), and its retry re-runs
  `LoadOrdersAsync`. Three new resource keys (`Store_Unavailable.Title` / `.Message` / `.Retry`) carry the text.
  **Scope recorded honestly**: the state type and retry seam are general and every screen can adopt them, but
  only the waitlist list screen is wired in this run; the other screens that read an internal store
  (Work Center Setup, Settings) still surface their failures as they did before.

### T108 — `InternalStoreAvailabilityTests` (11 tests)

Asserts the policy's three attempts and its 1 s/2 s/4 s schedule; that a store which never answers is retried a
bounded number of times and then reported `Unavailable` with populated `LastAttemptUtc`/`RetryCount=2`/
`NextRetryUtc`/`Message`; that the failed read returns **no rows at all** (never sample data, FR-001); that an
unconfigured store is `Unknown` rather than `Unavailable`; that caller cancellation propagates and is not
reported as an outage; the tracker's notification rules; and the per-screen state's visibility, clearing on
recovery, fresh detail on a second failure, retry-that-throws containment, and unsubscribe on dispose.

The failing reads are produced against a closed loopback port, so the failure is a real connection refusal
rather than a stub throwing. Two test-quality decisions are recorded in the file: the connection environment
variables are cleared per test (a developer machine with them set must still exercise the intended path), and
the delay delegate is replaced with a no-op while the *durations* are asserted separately.

### T078 — `ServiceApiTests` (6 tests, listener level)

Starts the real Kestrel host on a free loopback port and speaks HTTP to it, which is the only way to assert
pipeline properties: every routed endpoint returns a real `401` without a credential **and** with a wrong one;
the `/api/status` payload never contains the credential; a refresh request returns a well-formed outcome
document; an unknown shape is refused `400` naming the key; and `/api/restore` is a real `404` for both `GET`
and `POST` even with a valid credential (FR-023).

Two findings from building it, both recorded because they are easy to get wrong again:

- **The listener binds to the configuration store's live settings, not to the record handed to
  `ServiceHostBuilder.Build`.** The first version built the test client from the configuration it had passed in
  and every request was refused; the fixture now builds the client from `ServiceConfigurationStore.Current.Api`.
- **Startup validation must run before the API is served.** Without it `RefreshableShapes` is empty, so the
  status surface reports zero shapes and a refresh cycle has nothing to attempt. The endpoint's behaviour in
  that state is correct rather than broken — `results` is an empty array and the *status* payload is where the
  operator sees each shape's exclusion reason — so the test asserts the contract (a well-formed outcome
  document whose entries each name their shape and outcome) rather than a count, and says so.

### T084 — `BackupRestoreTests` (6 tests)

Per-store independence (a failed run for one store leaves another store's artifacts and files exactly as they
were); a missing tool reports `ToolUnavailable` with **no artifact recorded**; `RequestRestore` records the
intent and changes nothing on disk; a confirmed restore whose pre-restore safety snapshot cannot be taken
returns `FailedReload` with "nothing was changed" and names no recovery artifact; and confirming with an
artifact that was not requested, or with a file that has gone, is rejected. `PATH` is cleared per test so the
engine's `mysqldump` fallback cannot silently find a real client on a developer machine — recorded in the test
because it is the difference between testing the intended path and testing an accident.

**What this task could not assert here**: "a confirmed restore is a verified full replacement" needs a live
MySQL server. The refusal half (no recovery point → no destruction) is asserted; the replacement half remains
gated on the same environment as T118.

### T118 — `MockMirrorRefreshWriterIntegrationTests` (live database, 2 tests)

Gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` (the connection's database is rewritten to `mtm_mock`; the
procedures resolve their schema through `DATABASE()`), reporting inconclusive without it.

Proves a payload written through `MockMirrorRefreshWriter` reaches the real `sp_visual_work_order_lookup_refresh`,
that the reported row count equals the payload's, that the live mirror afterwards holds exactly that snapshot,
and that no seed content survives a real refresh.

**Atomicity is proven by the result of the swap rather than by racing a reader.** The three-name
`RENAME TABLE` leaves the outgoing snapshot in the stage twin, so the test captures the live table before the
refresh and asserts the stage twin holds *exactly* that afterwards — a truncate-and-reload, or a swap that moved
a partial set, cannot produce it, and unlike a concurrent read it cannot pass by timing. The test also asserts
the transient `_prev` table did not persist. It mutates one shape's snapshot in the disposable cache (which the
on-host service refreshes on its own schedule) and never touches an internal store (FR-027).

### T106 — end-to-end acceptance walkthrough — still open

Not executed. The walkthrough needs a running service, Infor Visual made unreachable, a signed-in client build,
and a backup/restore drill; the environment re-probe in Phase 19 shows the first and third of those are absent
and the second is a deliberate disruption of a live shared system. Its SC-007/SC-008 halves are 30-day
observation windows rather than one-shot tests, so even on a provisioned host the task completes at the 30-day
mark.

---

## Phase 19: Execution note — live-database subset and T106 environment re-probe (2026-09-11, `/speckit.implement`)

**Gates at the end of this run**: build `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64
/m:1 /nodeReuse:false` → `Build succeeded. 0 Warning(s) 0 Error(s)`; full suite with
`MTM_WAITLIST_TEST_DB_CONNECTION_STRING` set → `Failed: 0, Passed: 699, Skipped: 0, Total: 699`.

T106 is the only unticked task and it is not ticked here. What this run did instead was **re-probe the
environment** and then execute every part of the walkthrough the environment actually permits, plus the §8
final-validation gates. Phase 18 asserted the environment has "a deployed `mtm_mock` … none of which exists"; that is **partly wrong** and is corrected below.

### Environment re-probe (corrects quickstart §0/§1 and the Phase 18 claim)

| Prerequisite | State | Evidence |
|---|---|---|
| MySQL host reachable | **yes** | TCP `172.16.1.104:3306` open; `SELECT VERSION()` → **5.7.24** |
| `mtm_mock` deployed | **yes — already deployed** | 10 tables (5 mirrors + 5 `_stage` twins) and 11 routines; every mirror's `refreshed_utc` reads 2026-09-10 17:35:07 |
| Seed baseline (FR-017) | **yes** | row counts 1/2/2/2/1, all `is_seed_content = 1` — the cache had never been refreshed since deploy |
| Infor Visual reachable | **yes** | `VISUAL.mantoolmfg.com` → **172.16.1.75:1433** reachable from this workstation |
| MySQL client tools | **yes, but not on `PATH`** | `C:\Program Files\MySQL\MySQL Server 8.0\bin\{mysql,mysqldump}.exe` |
| `MTM_Waitlist.Mock.Service` deployed | **no** | no `MTM_Waitlist.Mock.Service/bin/publish`; host `172.16.1.104:5760` closed; no `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry |
| `pwsh` (PowerShell 7) | **no** | not installed; `Database/CopilotScripts/*.ps1` needs `powershell -NoProfile -ExecutionPolicy Bypass -File` |

### Executed and verified this run

| Walkthrough / gate | Result |
|---|---|
| §8 build gate | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| §8 offline suite | `Failed: 0, Passed: 684, Skipped: 15, Total: 699` — **not re-run in this run**; this is Phase 18's figure carried forward. The live run below is the strict superset (same suite, same 699 tests, 0 skips) |
| §8 live DB subset (FR-013) | `Failed: 0, Passed: 699, Skipped: 0, Total: 699` — **first time this has ever run**; the 15 skips drop to 0 |
| §1 verify — FR-017 seed read | `CALL sp_visual_work_order_lookup_get('SEED-WO-100')` → 1 row (`SEED-PART-100` / `Seeded baseline part` / `SEED-WC-A`) |
| §1 verify — shape parity (FR-004) | `sp_visual_read_shape_metadata_get(NULL)` → all five shapes report mirror + stage + `_get` + `_refresh` present, and every `mirror_columns` list matches `data-model.md` §3 |
| §1/§8 — atomic swap on the real host (FR-006/SC-005) | `MockMirrorRefreshWriterIntegrationTests` (2 tests) now pass live: the stage twin holds the complete outgoing snapshot, the transient `_prev` table does not persist, and no seed row survives a refresh |
| §3 step 1 — client wiring | `MTM_Waitlist.Mock` referenced by `MTM_Waitlist.csproj`, `MTM_Waitlist.Setup`, `MTM_Waitlist.Waitlist.View`, and `MTM_Waitlist.Settings` (plus the service and the test project) |

### Defects found and fixed

1. **`quickstart.md` §1 verify used the wrong seed key.** It called
   `sp_visual_work_order_lookup_get('SEED-WO-1')`, but the seeded journey defined in
   `Database/Mock/Seeds/seed_visual_mirror_baseline/create.sql` is **`SEED-WO-100` / `SEED-PART-100`**. Verified
   live: `SEED-WO-1` returns **0 rows**, `SEED-WO-100` returns the seeded row. The documented check therefore
   failed on a correct deployment. Corrected in `quickstart.md`.

2. **`MockMirrorRefreshWriterIntegrationTests` addressed the wrong schema** — the only real defect this run
   found, and it took the live subset to surface it. `TestInitialize` passed the test connection string (which
   names `mtm_waitlist`) straight to `MockMirrorRefreshWriter`, so the writer called
   `sp_visual_work_order_lookup_refresh` on the **wrong database** and failed with
   `PROCEDURE mtm_waitlist.sp_visual_work_order_lookup_refresh does not exist`. The class's own XML doc already
   claimed the opposite — "the connection's database is rewritten to `mtm_mock`" — so the code contradicted its
   documented contract. Fixed by resolving the writer's connection string the same way `MySqlHelperServer` does
   for `MySqlDatabaseTarget.MtmMock`: the dedicated `MTM_MOCK_DB_CONNECTION_STRING` when set, otherwise the test
   connection string with its `Database` overridden to `mtm_mock` (via `MySqlConnectionStringBuilder`). Both
   test methods then pass live. **Test-only change; no production code changed.**

**Cache hygiene**: the now-passing integration tests replace shape 1's snapshot in the deployed `mtm_mock` (the
class documents this — the cache is disposable and the on-host service rewrites it on schedule). The
`seed_visual_mirror_baseline` seed was re-applied afterwards, and the mirrors were re-verified at 1/2/2/2/1 rows
with `is_seed_content = 1` and no `_prev` table present, i.e. **byte-equivalent to the pre-test baseline**.

### Still blocked, and the specific blocker

| Step | Blocker |
|---|---|
| §2 service deploy/start, §2 step 7–8, §8 "Service end-to-end" | `MTM_Waitlist.Mock.Service` is not published or running. It is a **host-side** install (unpackaged, self-contained, tray-only) that belongs on the MySQL/Infor Visual host, not on a workstation, so deploying it is an infrastructure change to another machine |
| §3 fallback proof (FR-002/FR-004/FR-005, SC-003/SC-004/SC-006/SC-011) | Needs Infor Visual **made unreachable**. Visual is live and shared at `172.16.1.75`; deliberately disrupting it is out of scope for a verification pass |
| §4 defect proof (SC-001/SC-002, FR-021) | Needs a signed-in application session; the app stops at the **Sign in** gate and no credential was used |
| §5 backup/restore drill (SC-008/SC-009) | Needs a running service and a throwaway store. `mysqldump` is installed but off `PATH`, so only the `toolUnavailable` branch is reachable from here (which at least means the absence is *reported*, per FR-013, rather than worked around) |
| §7 sixth-shape playbook (SC-012) | A maintainer-day exercise, not a mechanical gate |
| SC-007 / SC-008 | **30-day observation windows — not started.** They cannot start until the service runs on the host |

**Net position: 110 of 111 tasks remain complete. T106 stays unticked**, now with an accurate description of what
is blocked on a host-side deployment versus what can never be checked from a workstation.

---

## Phase 20: Convergence

> Appended by `/speckit.converge` on 2026-09-11. Non-destructive assessment of the shipped code against
> `spec.md` (28 FRs, 16 SCs), `plan.md` / `research.md` / `data-model.md` / `contracts/`, and the ratified
> constitution (Principles I–VI + Security, Cache-boundary and Documentation constraints). No existing task,
> ID, or phase above was modified. Only the three gaps below are added.

- [x] T140 (CRITICAL) Sweep `WeekendProject/ChangeLog.Simple.md` and remove every stale reference to the retired demo/mock system — the manual "Infor Visual data" Settings toggle, the bundled sample cache, and the fallback notification — then re-verify SC-016 across the whole documentation set, per FR-028 (contradicts) — **Done 2026-09-11:** the outage-fallback section now states the fallback is automatic-only (toggle and bundled-sample claim removed; the notification is replaced by the read-only status indicator + `Retry`), feature 19 is marked `❌ withdrawn` with its own legend entry and a note that the time total is a record of work spent, and both stale "Still in progress" entries are corrected. SC-016 re-verified: sweeping the retired symbols repo-wide now returns **0** hits in any document that describes current behaviour — the only remaining hits are the deliberately historical archives (`ChangeLog.md`, `Module_Mock/**`, `PromptFiles/*`), the spec's own artifacts, and the intentional negative references in `RetiredSymbolAuditTests` and test comments. The two now-resolved rows in `WeekendProject/OPEN-WORK-NEXT-SPEC.md` were annotated so the next spec does not re-open them.
- [x] T141 (HIGH) Restore the deployed `mtm_mock` shape-1 mirror, which is currently empty while its own refresh run record reports 230 rows, and stop `MockMirrorRefreshWriterIntegrationTests` from leaving the shared deployed cache mutated or empty, per FR-017/FR-006 (partial) — **Done 2026-09-11:** root cause proven from the stage twin (see Phase 21), test now captures and restores the shared snapshot and refuses to run against a seed baseline it cannot put back, and the deployed mirror was re-warmed by a real refresh — all five shapes hold live data and `visual_work_order_lookup_result` = **230 rows**, `is_seed_content` = 0.
- [x] T142 (MEDIUM) Retire the still-open validation item in `WeekendProject/PromptFiles/App-Validation-Checklist.md` §3 that asserts the removed `Feature.RecvMockData` toggle's behaviour, so the checklist can close, per FR-028/SC-016 (contradicts) — **Done 2026-09-11:** the item is struck through with the reason and its replacement coverage (`InternalStoreAvailabilityTests` for FR-001/FR-021, `MockMirrorRefreshWriterIntegrationTests` for FR-002/FR-006), leaving no unfinishable box in §3.

### T140 — evidence

`WeekendProject/ChangeLog.Simple.md` (a user-facing plain-language doc, header "Last updated: 2026-09-06") still
describes the retired system as current, at lines 19–32:

- *"the app can automatically fall back to **cached Infor Visual data** (**presently a bundled sample**)"* — the
  bundled sample catalogs were deleted by T042 (FR-014).
- *"You can also **switch between live and cached Infor Visual data manually** with the single **\"Infor Visual
  data\"** toggle under **Settings**"* — that toggle and its Settings control were deleted by T034/T037. The
  document instructs a user to operate a control that no longer exists.
- *"When the app is automatically falling back or recovering, a **notification** explains…"* — the toast
  coordinator was deleted by T035, and FR-003 forbids a manual mode, so no such notification exists.

**Why the sweep missed it:** T102 covered `WeekendProject/ChangeLog.md`, `Module_Mock/*` and `PromptFiles/*`;
T125 covered root `README.md` / `CHANGELOG.md` / `RELEASE-NOTES.md`. `ChangeLog.Simple.md` appears in **neither**
list, so the SC-016 claim of "0 stale references" is not actually met and T102 was ticked without full coverage
(constitution I). Renaming/relocating that file is the durable fix — a path-based sweep cannot miss it again.

### T141 — evidence

Measured directly against the live host on 2026-09-11:

| Object | Rows | Last write |
|---|---|---|
| `mtm_mock.visual_work_order_lookup_result` | **0** | 2026-09-11 10:36:33 |
| `mtm_mock.visual_work_order_lookup_result_stage` | **2** | 2026-09-11 **11:45:27** |

while `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\refresh-run-records.json` reports
`work_order_lookup | Succeeded | RowCount 230 | 2026-09-11T15:36:33Z` (15:36 UTC = 10:36 local). The other four
shapes are healthy and hold real refreshed data (242 / 399 / 12711 / 230 rows, `is_seed_content = 0`).

Two candidate causes, both worth the task: (a) the live integration test mutates the **shared deployed** mirror
(Phase 19 documents that it replaces shape 1's snapshot) and nothing restores it afterwards — Phase 19 had to
re-apply `seed_visual_mirror_baseline` by hand, and the stage's 11:45:27 timestamp post-dates the service's last
refresh, which fits a test run rather than a service cycle; or (b) a swap completed with an empty stage while
still reporting the pre-swap count. The observed pairing — result empty, stage holding test-sized data — *appeared*
not to match the documented "stage holds the complete outgoing snapshot" semantics either way. **Superseded by T143
(2026-09-11): cause (a) was confirmed and cause (b) ruled out, and the pairing does in fact match the documented
semantics — the stage twin receives whatever `result` held immediately before each swap (after test 1: the 230 real
rows; after test 2: the 2 synthetic rows). See Phase 21 for the proof.**

**Impact if left:** with the primary read shape holding no rows, an Infor Visual outage leaves the work-order lookup
serving nothing from cache, degrading SC-003.

**Impact corrections (T143, 2026-09-11).** Two further claims in the sentence above were wrong and are withdrawn:

- **Not an FR-017 breach.** FR-017 governs a *fresh installation*, whose seed comes from the artifacts in
  `Database/Mock/Seeds/seed_visual_mirror_baseline`; those were never damaged, so the fresh-install guarantee held
  throughout. What broke was the deployed **development cache's** usability — it had already been populated, so
  FR-017 was not the requirement in play.
- **`quickstart.md` §1 does not fail on a correctly-deployed cache.** §1 is a **fresh-deploy** verification —
  *"shape reads return the seed content before any refresh has happened"* — and its own step 5 then performs the
  refresh and asserts `is_seed_content = 0`. Followed in order it works. The `CALL` returns nothing only if it is
  run out of sequence, against a cache whose seed a refresh has already consumed — which is what the empty result at
  the time of writing actually was.

Also fold in: the deployed `subordinate_parts` snapshot was produced by the pre-T141-fix population SQL, so cached
die locations differ from what the live read now returns until the next refresh — re-refresh after restoring.

### T142 — evidence

`WeekendProject/PromptFiles/App-Validation-Checklist.md` line 86 keeps an **open** item: *"Mock toggles behave:
`Feature.RecvMockData` ON shows sample coil weight; OFF queries DB."* The key and the sample catalog it exercises
were deleted (FR-014), so the item can never pass and that checklist can never close. §3's other items are the
`[NOW]` regression gates and are unaffected.

**Out of scope for this feature, recorded for awareness only** (both already tracked in
`WeekendProject/OPEN-WORK-NEXT-SPEC.md` §3 for the *next* spec, which owns them):
`PromptFiles/15-0%-UserManagement.md` line 51 still lists "honor the mock short-circuit pattern
(`Feature.RecvMockData` / Infor mock toggle)" as a **locked** requirement, and
`PromptFiles/12-71%-MockMasterData-DbDriven.md` still names the toggles in its purpose text. Neither file belongs
to this feature, and neither should be implemented as written — constitution II and FR-003 forbid the pattern.

---

## Phase 21: Execution note — convergence gap closure (2026-09-11, `/speckit.implement`)

> Appended by `/speckit.implement` after working through Phase 20. T140–T142 are complete and ticked above;
> no earlier task or phase was modified. T106 is re-assessed at the end and remains open.

### Gates

| Gate | Result |
|---|---|
| Build (`MTM_Waitlist.Tests.csproj`, Debug/x64) | **succeeded — 0 warnings / 0 errors** |
| Offline suite | `Failed: 0, Passed: 692, Skipped: 15, Total: 707` (the 15 skips are the live-database suites, including both mock-cache tests, which report inconclusive without `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`) |
| Live mock-cache subset | `Failed: 0, Passed: 2, Total: 2` |
| Deployed `mtm_mock` after the live run | all five shapes hold live data; `visual_work_order_lookup_result` = **230 rows**, `is_seed_content` = 0 |

### T141 root cause (proven, not inferred)

The stage twin held the literal rows `ITEST-A-4e5fc50f` / `ITEST-B-4e5fc50f` — the synthetic payload from
`MockMirrorRefreshWriterIntegrationTests`. Because `sp_visual_<shape>_refresh` swaps
`result → _prev → _stage` in one statement, the outgoing snapshot always lands in `_stage`. The sequence was:
the service refreshed 230 real rows at 10:36 → test 1 swapped in its 2 synthetic rows (the real rows moved to
`_stage`) → test 2 refreshed with `"[]"` and swapped in an empty snapshot, **destroying the real rows through
the `TRUNCATE` at the start of its own cycle**. Nothing restored them, so the app's fallback for the primary
read shape served an empty result while the shape's own run record still reported "Succeeded, RowCount 230".

The test's remarks had justified this on the grounds that "`mtm_mock` … is a disposable cache that the on-host
service refreshes on its own schedule (FR-025)" — true on the host, **false on a workstation**, which is where
the suite actually runs.

### T141 fix

`MockMirrorRefreshWriterIntegrationTests` now captures the live mirror in `TestInitialize` (the four
procedure-facing columns plus `is_seed_content`) and writes it back in `TestCleanup`, then asserts the restored
projection, so the shared cache is handed back as found. A captured snapshot containing any seed row makes the
test report **inconclusive without mutating anything**, because the procedure hardcodes `is_seed_content = 0`
and therefore cannot put a fresh install's baseline (FR-017) back — the one state that could not be restored is
the one state the test now refuses to destroy. Restoration is data-complete but not byte-identical:
`refreshed_utc` is necessarily re-stamped.

**Verified live:** the same run that previously left `result` = 0 rows and `_stage` = 2 synthetic rows now leaves
`result` = 230 real rows (`WO-010089 / Brake Pad / 020-01`, …) and `_stage` = 0.

### T106 re-assessment

Unchanged and still open. Closing it needs a **host-side** service deployment, Infor Visual deliberately made
unreachable, a signed-in application session, and the 30-day SC-007/SC-008 observation windows — none of which
this run provides. The convergence discovery that the cache could be left empty by the test suite did not
account for T106's blockage, and does not remove it.

### Environment side effects (reversed)

Re-warming the mirror required running the workstation copy of `MTM_Waitlist.Mock.Service` once.
`AutoStartAtLogon` was temporarily set to `false` so no `HKCU\…\Run` entry would be written, and was
**restored to `true` afterwards**; no Run entry exists (checked). The service was stopped and leaves no orphan
process. Captured `dotnet test` output was deleted because it contains the connection string.

---

## Phase 22: Convergence

> Appended by `/speckit.converge` on 2026-09-11 (second run on this feature). Non-destructive re-assessment after
> `/speckit.implement` completed T140–T142. Those three are confirmed closed below and **no existing task, ID, or
> phase was modified**. One artifact-accuracy gap remains; the pre-existing, environment-blocked T106 is not a code
> gap and is deliberately not re-appended.

- [x] T143 (LOW) Correct the two superseded claims in Phase 20's T141 evidence block so they agree with the Phase 21 root-cause finding, per Constitution I (contradicts) — **Done 2026-09-11:** both superseded claims are marked as superseded **in place** (the original suspicion is retained, per this file's convention for historical records) and the corrected reading is recorded in Phase 20's T141 evidence, together with two further corrections to the impact sentence that the same review exposed — FR-017 was never breached, and `quickstart.md` §1 does not fail on a correctly-deployed cache. Markdown-only change; no code, build, or test impact.

### T143 — evidence

Phase 20's T141 evidence (lines 1641–1646) records two suspicions that the Phase 21 execution note, in this same
file, then **disproved** — so the artifact currently contradicts itself:

1. *"The observed pairing — result empty, stage holding test-sized data — does not match the documented 'stage holds
   the complete outgoing snapshot' semantics either way."* Phase 21 proves it matches **exactly**: after test 1 the
   stage held the 230 real rows `result` held before that swap; after test 2 it held the 2 synthetic rows `result`
   held before that swap. Both are the complete outgoing snapshot, precisely as documented.
2. *"[…] and `quickstart.md` §1's `sp_visual_work_order_lookup_get('SEED-WO-100')` assertion now fails on a
   correctly-deployed cache."* `quickstart.md` §1 is a **fresh-deploy** verification — *"shape reads return the seed
   content before any refresh has happened"* — and its step 5 then performs the refresh and states
   `is_seed_content = 0`. Followed in order it does not fail on a correctly-deployed cache, so the claim is wrong.

The adjacent FR-017 sentence also overstates the impact: the deployed development cache's seed had been consumed by
a prior test run, but FR-017's fresh-install guarantee comes from the seed artifacts
(`Database/Mock/Seeds/seed_visual_mirror_baseline`), which were never damaged. Correct all three so the artifact set
records what actually happened — Constitution I requires completion records to be auditable, and a note that a later
section of the same file refutes is not.

### Verified closed in this run

| Prior finding | Task | Verification |
|---|---|---|
| F1 — stale demo/mock references in `ChangeLog.Simple.md` (CRITICAL) | T140 | A retired-symbol sweep over the whole repository returns **0** hits in any document describing current behaviour; the remaining hits are only the deliberately historical archives (`ChangeLog.md`, `Module_Mock/**`, `PromptFiles/*`), this feature's own artifacts, and the intentional negative references in `RetiredSymbolAuditTests` and its sibling test comments |
| F2 — empty shape-1 mirror (HIGH) | T141 | `mtm_mock` holds live data in **all five** mirrors — `work_order_lookup` = 230, `disposition_input` = 230, `operation_sequences` = 242, `subordinate_parts` = 399, `inventory_locations` = 12712 — with `is_seed_content` = 0 throughout. The shape-1 stage twin is empty, which is correct: it is scratch, and the next refresh truncates it. **No live mirror is empty** |
| F3 — unfinishable validation item (MEDIUM) | T142 | `App-Validation-Checklist.md` §3 no longer contains a checkbox for `Feature.RecvMockData`; the retired item is struck through and names the tests that now cover its intent |

Also confirmed this run: T137 is genuinely complete (`plan.md` cites **v1.1.1** with the amendment note and the gate
verdicts restated against it), and `tasks.md` carries **141 of 142** tasks complete with T106 the only open item.

### T106 — unchanged, and not a code gap

T106 is an environment-gated **verification** task: a host-side service deployment, Infor Visual deliberately made
unreachable, a signed-in application session, and the 30-day SC-007/SC-008 observation windows. It is already
triaged in Phases 18, 19 and 21 and is deliberately **not** re-appended as a finding — re-appending it every run
would add no information and would only inflate the phase list.

---

## Phase 23: Execution note — acceptance gates, shape-parity proof, and a cache key-domain defect (2026-09-11, `/speckit.implement`)

> Appended by `/speckit.implement`. No earlier task, ID, or phase was modified. T106 is **not** ticked; this run
> executed the parts of `quickstart.md` the environment permits, proved FR-004/SC-004 live, and found one defect
> that is recorded below as **T144 (open)**. Nothing in the shared cache or the deployed service was mutated.

### Gates

| Gate | Command | Result |
|---|---|---|
| §8 build (SC-015) | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` | `Build succeeded. 0 Warning(s) 0 Error(s)` |
| §8 offline suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` | `Total 707, Passed 692, Skipped 15, Failed 0` |
| §8 live-database subset (FR-013) | same, with `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` set | `Total 707, **Passed 707**, Skipped 0, Failed 0` (17.5 s) |

**The live run is the first time this suite has ever completed with zero skips.** Phase 19 recorded `Passed 699,
Skipped 0` on the then-current 699-test suite; the suite has since grown to 707, and Phase 21 recorded the offline
figure only (`692 / 15`). All 15 environment-gated tests — the 12 `ImageLocation`-override cases plus
`MockMirrorRefreshWriterIntegrationTests` and its siblings — passed against the live host.

**A self-inflicted false alarm, recorded so the next reader does not repeat it.** The first attempt at the live run
reported `Failed 6`. That was wrong, and the cause was the harness, not the product: `appsettings.json` has **no
`ConnectionStrings` section** (the waitlist string lives at `StartupDatabaseOptions.ConnectionString`), so the
script's read produced `$null`, and a `$null` connection string silently degraded into a malformed
`" AllowPublicKeyRetrieval=True;"` value that made every live test fail on connect. Reading
`StartupDatabaseOptions.ConnectionString` gives the green run above. Separately, two PowerShell hazards cost time
here and are worth knowing: Windows PowerShell 5.1 has no ternary operator (`pwsh` is now installed on this
workstation and is the right shell), and `$args` is a reserved automatic variable — a function that assigns to it
and then splats `@args` does not pass the intended argument list (the repo's own
`Database/CopilotScripts/verify_mtm_mock_deploy.ps1` deliberately uses `$arguments` for this reason).

### Environment re-probe (deltas against Phase 19)

| Prerequisite | Phase 19 | Now |
|---|---|---|
| `pwsh` (PowerShell 7) | absent | **present** |
| `mysql` client | installed but **off `PATH`** | **on `PATH`** |
| MySQL host `172.16.1.104:3306` | reachable | reachable (`5.7.24`) |
| Infor Visual `172.16.1.75:1433` | reachable | reachable |
| `mtm_mock` deployed | yes | yes — 5 mirrors + 5 `_stage` twins; `work_order_lookup` 230, `operation_sequences` 242, `subordinate_parts` 399, `inventory_locations` 12712, `disposition_input` 230 rows, all `is_seed_content = 0` |
| `MTM_Waitlist.Mock.Service` deployed/running | no | still **no** — no `bin/publish/win-x64`, `172.16.1.104:5760` closed, no `HKCU\…\Run` entry |

Each deployed mirror table carries one column the checked-in `create.sql` does not declare — an auto-increment
`id`. The five `create.sql` files and the deployed schemas otherwise agree exactly, and every shape's
`sp_visual_<shape>_get` projects only the catalog's columns, so the extra column is invisible to the read contract.

### `quickstart.md` §3 step 5 — cached vs live for the same input (FR-004, SC-004)

Never executed before this run. For each shape, a real cached key was read from the mirror, the same key was run
through `sp_visual_<shape>_get`, and the same key was run through the shape's **live source script** against
`VISUAL/MTMFG`. **Result: 5 of 5 shapes return identical, identically ordered column names.**

| Shape | Sampled key | Live columns | Cached columns | Parity |
|---|---|---|---|---|
| `work_order_lookup` | `WO-010089` | PartNumber, Description, WorkCenter | same | **MATCH** |
| `operation_sequences` | `WO-240282` / `240282` | SequenceNumber, Description | same | **MATCH** |
| `subordinate_parts` | `WO-010089` / `10089` / `20` | Category, PartNumber, Description, Location, User8, OnHandQuantity | same | **MATCH** |
| `inventory_locations` | `06000 00105` | PartNumber, Location, OnHandQuantity | same | **MATCH** |
| `disposition_input` | `WO-010089` / `10089` | WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity, HasOutsideVendorOperation | same | **MATCH** |

SC-004's column-set and structure halves are therefore **proven against the real Visual server**, not only against
the catalog. The **content** half is where the defect below lives.

### T144 — the cache's key domain is not the domain the live read resolves

- [ ] T144 (**transparency half FIXED 2026-09-11**; coverage half open — needs an operator decision) The mirror
  cached 230 work orders under zero-padded keys the live read cannot resolve and omitted the 555 open work orders the
  live read *does* resolve, so the same work-order input returned **different content** depending on whether Infor
  Visual was reachable (FR-002/FR-004/SC-003/SC-004, contradicts). The five population reads now emit exactly the
  live-resolvable key domain, verified live. What remains open is coverage, not transparency: the application's
  `^(?:WO-)?(\d{5,6})$` rule cannot express 42,143 of the 42,852 open orders, and widening it is a product decision.

> **Method note (this supersedes the first draft of this section).** The first pass talked to Infor Visual through
> `sqlcmd` and scraped its text output, which made the row counts unreliable (a `-W -s '|'` separator line was
> counted as a data row). This section's numbers come from the **production path**: the connection string built by
> `VisualConnectionStringProvider.Resolve()`, and each shape's script loaded as UTF-8 text and executed exactly as
> `VisualQueryExecutor.ExecuteAsync` does — `CommandType.Text`, 15 s timeout, `AddWithValue("@Name", value)` — with
> the mirror read through `sp_visual_<shape>_get` using each fallback's own `BuildCacheParameters` names.

**Evidence (all read live, 2026-09-11).** `WORK_ORDER` carries several families; the two that matter are:

| Family | `BASE_ID` form | Open (`STATUS` in `R`/`U`/`F`) | Example |
|---|---|---|---|
| `W` | literally `WO-` + digits | **695** (555 with the `WO-` prefix) | `WO-074011` / part `24733431` |
| `M` | bare numeric | **4,898** — of which **230** also match the shipped population's `LEN` 5–6 / all-digits guard | `10089` / part `10089` (*Brake Pad*) |

For scale: open orders total **42,852** (`Q` 37,259 · `M` 4,898 · `W` 695).

The app normalizes every accepted input to `WO-` + the 6-digit zero-padded base id
(`MTM_Waitlist.Setup/Services/WorkOrderValidationService.cs`) and passes that **same string** to both paths —
`@NormalizedWorkOrder` for the live script and `p_normalized_work_order` for the cache. The two paths then
diverge:

- The five live source scripts all derive a base id by stripping a leading `WO-` and match
  `wo.BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)` — verified identical in
  `LookupWorkOrder.sql`, `GetSequences.sql`, `GetSubordinateParts.sql`, `GetDispositionInput.sql`. That resolves the
  `W` family and **cannot** resolve the `M` family: for `M` row `10618` the app's key is `WO-010618`, and neither
  `WO-010618` nor `010618` equals `10618`.
- `Database/InforVisual/Queues/Module_Mock/Populations/work_order_lookup_population.sql` caches exactly the `M`
  family, via `LEN(BASE_ID) BETWEEN 5 AND 6` **and** `BASE_ID NOT LIKE '%[^0-9]%'`, padded to the same
  `WO-`-prefixed 6-digit form. The numeric guard excludes every `W` row, which is why the cache holds 230 rows
  rather than the ~42,800 the recorded scope approves. **The guard is therefore a deviation from T113's operator
  decision, not a restatement of it** — the comment that justifies it ("emits only base ids that the application
  can actually ask for") reasons about a domain the live predicate cannot answer.

Two consequences, both measured through the production path:

| Shape | Key | Live rows | Cache rows | Content |
|---|---|---|---|---|
| `work_order_lookup` | `WO-010089` | **1** — `24 126 172` / *Bracket, Control* / `100-04-GRP` | **1** — `10089` / *Brake Pad* / `020-01` | **different order** |
| `operation_sequences` | `WO-010089` | **0** | **1** — `20` / *Operation 20 / 020-01* | live empty |
| `subordinate_parts` | `WO-010089` / `10089` / `20` | **0** | **1** — `Flatstock` / `MMF0000250` | live empty |
| `disposition_input` | `WO-010089` / `10089` | **0** | **1** — status `U`, open 264 | live empty |
| `inventory_locations` | `06000 00105` | 3 | 3 | agree (keyed by part, not work order) |

1. **Same key, different order.** The one case where live returns a row returns the **`W`** order the padded key
   literally matches (`WO-010089`, *Bracket, Control*, `STATUS=C`) while the cache returns the **`M`** order
   (*Brake Pad*, `STATUS=U`). Where no `W` twin exists the live read returns nothing at all, so the same input
   yields rows with Visual down and an empty result with Visual up. FR-004's "a caller cannot distinguish cached
   from live" holds for the **column shape** (proven above) and fails for the **rows** — and because both answers
   have the identical column set, no shape-level assertion can catch it.
2. **The cache omits the family the live read serves.** Direct probe: `WORK_ORDER` holds `W` order `WO-074011`
   (`24733431`, the very key Phase 6 validated `GetDispositionInput.sql` against), and
   `SELECT COUNT(*) FROM visual_work_order_lookup_result WHERE normalized_work_order = 'WO-074011'` = **0**. All
   cached rows are `M` orders.
3. **The operator's own domain is the excluded one.** `mtm_waitlist.setup_active_jobs` and `setup_job_history`
   hold exactly one saved work order between them — `WO-041652` — and live that is
   `TYPE=W, BASE_ID='WO-041652', PART_ID='0K34360GN0R'`: the family the population selects out. So on a Visual
   outage the cache can serve neither the order the plant has actually set up nor the family the live read answers.
4. **Shape 5's mismatch is latent, not live.** `RequestDispositionResolver.GetDispositionInputAsync` forwards the
   work order **un-normalized** (no trim, no padding), unlike the Setup path, and the resolver has **no caller** —
   `Services/DependencyInjection/ServiceRegistrationExtensions.cs:114` registers it and nothing invokes it. Its
   divergence above is therefore recorded as reachable-but-unwired, not as an active runtime failure.

**Resolved in part (2026-09-11, `/speckit.implement` retry).** The transparency half is fixed; the coverage half is a
recorded operator decision this run deliberately did not take.

*Fixed — the mirror now holds exactly the keys the live read resolves.* All five population reads
(`Database/InforVisual/Queues/Module_Mock/Populations/*.sql`) had their driver guard and key derivation replaced.
The guard now accepts the two forms the application can ask for **and** the live read answers — `BASE_ID` literally
`WO-` + 6 digits (key = the `BASE_ID` itself) and exactly-6-digit numeric ids (key = `WO-` + those digits, which the
live predicate matches through `@WorkOrderBaseId`) — and drops the 5-digit numeric form, which the application pads
into a key nothing answers. Verified against the live source through the production client path:

| Shape | Rows before | Rows after | Keys |
|---|---|---|---|
| `work_order_lookup` | 230 | **701** | 701, all `WO-` + 6 digits, 0 malformed |
| `operation_sequences` | 242 | **893** | 683 work orders |
| `subordinate_parts` | 399 | **1,746** | 683 work orders |
| `inventory_locations` | 12,712 | **79,457** | 1,478 parts |
| `disposition_input` | 230 | **701** | 701, all `WO-` + 6 digits, 0 malformed |

Every sampled key's population rows now equal the live per-key read: `work_order_lookup` 1/1 ·
`operation_sequences` 1/1 · `subordinate_parts` 4/4 · `inventory_locations` 237/237 · `disposition_input` 1/1. The
old guard's `WO-010089` / `WO-010618` / `WO-010622` keys — where the live read answered with a *different* order —
are no longer emitted, so the 3 cross-order collisions and the 76 unreachable keys are gone. Offline suite
unchanged: **707 total, 692 passed, 15 skipped, 0 failed**.

*Left open — coverage, which is an operator decision.* The application's `^(?:WO-)?(\d{5,6})$` input rule cannot
express **42,143 of the 42,852** open orders (the `Q` family, non-numeric `M` ids, anything over 6 digits), so no
cache change can reach them: T113's recorded "every open Infor Visual work order" scope is unreachable end-to-end
without widening `MTM_Waitlist.Setup/Services/WorkOrderValidationService.cs` **and** the five shared live-read
scripts. That is product behaviour outside this feature's boundary and is therefore recorded, not taken here. Also
open: the deployed development cache still holds the pre-change 230 rows — it is refreshed by the on-host
`MTM_Waitlist.Mock.Service` (not deployed on this workstation), so the change takes effect at that service's next
refresh, not from this run. And §3 step 5 should become a **content** parity gate (identical rows for the same key):
the shape-only check this run proved is blind to this whole defect class.

### T145 — the service's publish profile never shipped, so the documented publish was not self-contained

- [x] T145 (MEDIUM) Restore the `MTM_Waitlist.Mock.Service` folder-publish profile so the command the README and
  `quickstart.md` §2 name actually resolves and produces the documented, self-contained artifact, per T120 (contradicts)
  — **Done 2026-09-11** (verified below).

**Evidence.** `MTM_Waitlist.Mock.Service/Properties/PublishProfiles/win-x64-selfcontained.pubxml` did **not** exist —
the project had no `Properties` folder at all — while T120's execution note, `MTM_Waitlist.Mock.Service/README.md` §1
and `quickstart.md` §2 step 1 all name it, and the README's guarantee table rests on it. Running the documented command
proved the consequence:

```text
warning NETSDK1198: A publish profile with the name 'win-x64-selfcontained' was not found in the project.
```

The publish then succeeded **silently into the wrong place and in the wrong shape**:
`bin/x64/Release/net10.0-windows10.0.19041.0/win-x64/publish/` (not the documented `bin/publish/win-x64/`, which did
not exist — matching Phase 19's probe), with `runtimeconfig.json` declaring `Microsoft.NETCore.App 10.0.0` and
`Microsoft.AspNetCore.App 10.0.0` as **`frameworks`** and no `System.Private.CoreLib.dll` / `hostfxr.dll` /
`coreclr.dll`. So the host **would** have needed the .NET 10 runtime installed, contradicting the README's
`SelfContained` row. Only the `WindowsAppSDKSelfContained` half held, because that property lives in the `.csproj`
rather than in the profile.

**Root cause.** `.gitignore:187` carries the stock Visual Studio rule `*.pubxml` (whose comment is about unencrypted
*web-deploy* connection strings). The profile was therefore never committable — it can only ever have existed on the
machine that created it, which is why the deliverable T120 ticked is absent from a clean clone.

**Fix.** (1) Created the profile with exactly the four guarantees the README documents, plus `PublishDir` set to the
documented `bin\publish\win-x64\`. (2) Added a narrow negation, `!MTM_Waitlist.Mock.Service/Properties/PublishProfiles/*.pubxml`,
with the sensitive-data rationale answered in place, so a real web-deploy profile is still ignored — verified:
`Some.Web/Properties/PublishProfiles/WebDeploy.pubxml` still matches `*.pubxml`.

**Verified.** The documented command now completes with **no warnings**, writes to
`MTM_Waitlist.Mock.Service\bin\publish\win-x64\`, and the output is genuinely self-contained: 727 files / 265 MB;
`System.Private.CoreLib.dll`, `hostfxr.dll`, `coreclr.dll` and `Microsoft.WindowsAppRuntime.dll` all present;
`runtimeconfig.json` uses **`includedFrameworks`** (`Microsoft.NETCore.App 10.0.11`, `Microsoft.AspNetCore.App 10.0.11`)
rather than `frameworks`. The content the deploy step requires ships beside the exe: `Assets/WindowIcon.ico`, all five
`Database/InforVisual/Queues/Module_Mock/Populations/*.sql` (including T144's corrected guards),
`Database/InforVisual/Queues/{Module_Setup,Module_Waitlist}/**/*.sql`, and `Database/Mock.Service/Restore/**`.

**Not verified — and this is the real remaining gate.** The published artifact was **not launched**. Whether the service
starts, shows its tray icon, generates the credential, honours auto-start, and serves the API is `quickstart.md` §2,
still unexecuted (see T106), and launching it here would start refresh and backup engines against the shared cache.
So: **publish readiness is established; deployment readiness is not.**

### `quickstart.md` §2 — the published artifact launched (2026-09-11, first run ever)

The T145 artifact was launched from `MTM_Waitlist.Mock.Service/bin/publish/win-x64/` on this workstation. This is
the first time the service has actually run, so it retires part of §2 and part of the T106 blocker list.

| §2 / requirement claim | Result |
|---|---|
| §2 step 2 — "no main window: a tray icon appears and the background engines start" | **Pass on the window half**: process responding, `MainWindowHandle = 0`, and **0** top-level windows found by process id. The icon itself is not observable (tray icons are outside the UIA tree and cannot be driven) — only inferred, since the design keeps the process alive through it and the process stayed alive. |
| FR-007 / research.md R3 — single instance | **Pass.** A second launch redirected to the running instance and exited: pids `20440` → second copy `22164` exited → one survivor, `20440`. |
| FR-012/FR-013 — the API is up and closed | **Pass.** Bound `0.0.0.0:5760`; `/api/status`, `/api/backups` and `/api/restore` each returned `401 {"Error":"unauthorized","Message":null}` with no credential. |
| FR-007 — auto-start reconciliation | **Pass, and it acts.** A first run with shipped defaults (`AutoStartAtLogon = true`) wrote `HKCU\…\Run\MTM_Waitlist.Mock.Service` pointing at the published exe. |
| Shipped defaults are host-appropriate | `VisualSource` = `VISUAL`/`MTMFG`; `MySqlConnection.Server` = `localhost` (correct — the service belongs on the MySQL host); API `0.0.0.0:5760`; 3-hour refresh grid; four backup policies at 01:00/01:20/01:40/02:00 local. No shared credential configured until generated in Settings. |
| No shared state written | **Pass.** `mtm_mock` unchanged — 230 / 242 / 399 / 12712 / 230 rows with the *same* `refreshed_utc` values as before the run; no `backups/**` artifacts (not due at 14:36); no refresh attempted (no MySQL credential configured). |

**Still not established by this run.** (1) `/api/restore` must be a **404** *with a valid credential*; without one,
authentication short-circuits to the 401 above, so that half is still covered only by `ServiceApiTests` (T078,
listener level) and could not be exercised here — a credential can only be generated from the tray Settings UI,
which UI Automation cannot reach. (2) §2 steps 3–4 (the Settings surface, generating the credential, choosing
schedules) and surviving a real sign-out/sign-in. (3) §5's backup/restore drill. (4) §1–§4 as a whole, i.e. the
fallback proof itself, which needs Infor Visual deliberately made unreachable and a signed-in client.

**Side effects reversed.** The process was stopped; the auto-start entry created by the first run was removed; the
`%LOCALAPPDATA%\MTM_Waitlist.Mock.Service` folder created by the first run was deleted (it did not exist before);
port 5760 is free again; scratch scripts deleted. **Operational note for the host:** auto-start is on by default and
is registered from wherever the executable is run, so the publish folder must be copied to its final install
location *before* the first run — which is the order `README.md` §2 → §3 already prescribes.

### §2 host installation — the "another machine" blocker was wrong (2026-09-11)

Phases 18, 19 and 21 all recorded §2 as blocked because the service "belongs on the MySQL/Infor Visual host, not on a
workstation, so deploying it is an infrastructure change to another machine". **The operator confirmed that the
working environment *is* the host machine**, and the machine was then verified to be exactly that: `V-MTMFG-5`
(`172.16.1.104`) — the shared MySQL / Infor Visual host, whose `localhost` carries all the `mtm*` schemas. (Note for
later readers: this is **not** `MTMFG-161`, which `workstation-elevation.md` correctly describes as the development
workstation — an earlier draft of this note conflated the two and was corrected.) The blocker is therefore retired:
what remains of §2 is configuration and credential work, not a machine move.

| §2 step | State |
|---|---|
| step 1 — publish | **Done and installed.** `C:\Services\MTM_Waitlist.Mock.Service\` — 727 files / 265 MB, copied from the publish output with a file-for-file match. Verified present: the exe, the tray icon, all five population reads *including T144's corrected guards*, the Module_Setup/Module_Waitlist queries, `Database/Mock.Service/Restore/**`, and the self-contained runtime. `C:\` proved writable by this account, so no elevation was needed. |
| step 2 — tray-only start | **Verified** (table above), from the publish folder; the installed copy is byte-identical. |
| steps 3–4 — Settings surface + credential | **Blocked on operator interaction.** The credential is generated from the tray Settings UI, which UI Automation cannot reach (tray icons are outside the UIA tree), and FR-026 makes it write-once. |
| secrets | **Set (2026-09-11), under an owner-approved exception scoped to this workstation** — recorded in `.github/memories/repo/workstation-secrets.md` and referenced from `.github/copilot-instructions.md`, mirroring the existing `workstation-elevation.md` precedent. At User scope: `MTM_WAITLIST_DB_CONNECTION_STRING` (the shared string the `mtm_mock` cache and all four backed-up stores reuse, per `MySqlConnectionStringResolver` option 2), `MTM_MYSQL_PASSWORD`, `INFOR_VISUAL_SQL_USER` (`SHOP2` — `VisualSourceSettings.UserId` defaults to empty, so this must come from the environment; `SHOP2` is also the login `appsettings.json` already uses) and `INFOR_VISUAL_SQL_PASSWORD`. Read back from `HKCU\Environment` and verified: all four MySQL databases connect on **5.7.24**, and `VISUAL`/`MTMFG` authenticates as `SHOP2` with 42,852 open work orders visible. (The owner first specified `SHOP` and corrected it to `SHOP2` the same day; both authenticate with password `SHOP`.) The values are the pre-existing development credentials already committed in `appsettings.json`, so this records the approval rather than disclosing anything new. |
| §5 backup tooling | **Verified present and compatible.** `mysqldumpPath` is unset (resolve from `PATH`) and `PATH` resolves to *MySQL Workbench 8.0*'s `mysqldump 8.0.46` against a **MySQL 5.7.24** server. Tested with `BackupEngine`'s exact argument set (`--host --port --user --single-transaction --routines --databases --result-file`) against `mtm_mock`: **exit 0 with a full 1,704,965-byte artifact**. The `column statistics not supported by the server` message is a stderr **warning** only, and adding `--column-statistics=0` changes nothing but the noise — so no engine change is needed. (The opposite was hypothesised from the version pair and then **disproved by test**, which is why the probe is recorded rather than the guess.) |

**`README.md` corrected as a result.** §5 previously claimed the configuration and run records "live beside the
executable", which would leave a supposedly clean reinstall still carrying the DPAPI credential, the schedules and the
backups — they actually live under `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\` (`ServiceHostBuilder.GetDefaultAppDataRoot()`,
confirmed by the first run). §5 now names each location, and §3 states that auto-start is **on by default**, records
the launch path, and names the two secrets the configuration deliberately never stores.

### T146 — the deployment is now a repo-bound, self-validating, host-guarded script

- [x] T146 (**MEDIUM**) Replace the throwaway `%TEMP%` install steps with a reviewed deployment script that lives in
  the repository, redeploys from clean, installs and verifies the secrets, refuses to run anywhere but the cache host,
  and validates its own work — then document it across the repository and tell the agent never to run it off-server,
  per FR-007/FR-012 and Constitution I (contradicts the previous "scratch script" state) — **Done 2026-09-11.**

**Artifacts.** `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` and
`MTM_Waitlist.Mock.Service/deploy/README.md`. Referenced from `MTM_Waitlist.Mock.Service/README.md` §1 (and §2–§4 as
its narrative authority), `quickstart.md` §2, `.github/copilot-instructions.md`, and
`.github/memories/repo/{workstation-secrets,infor-visual-disposition}.md`.

**Answers to the four questions that prompted it.**

| Question | Before | Now |
|---|---|---|
| Repo-bound, with a README? | **No** — it lived in `%TEMP%` and was deleted after use. | Script + README under `MTM_Waitlist.Mock.Service/deploy/`, committed with the feature. |
| Kills the running process, deletes the old deployment, redeploys? | **No** — it only copied into an existing folder, so stale files survived. | Stops every instance (waits ≤30 s, then confirms the API port is free), **deletes the install folder outright**, then redeploys. State under `%LOCALAPPDATA%` is kept unless `-PurgeState`. |
| Installs **and verifies** the secrets? | **No** — I set them by hand once. | Sets the four User-scope variables, reads them back from `HKCU\Environment`, then **proves** them by connecting to all four databases and authenticating to Infor Visual. Values derive from `appsettings.json`, so the script is not a second source of truth and holds no secrets. |
| Full validation? | **No** — file-existence checks only. | 21 checks: file-count parity, 11 required paths, self-contained markers **and** `includedFrameworks`, the deployed T144 population guard, secret read-back, four MySQL connections, a Visual login, process responding, tray-only (no window), port bound, `401` without a credential, single-instance redirect, and the auto-start entry pointing at *this* install folder. Any failure before step 7 **aborts before starting the service**. |

**The guard, verified.** The script resolves the expected host from `appsettings.json`'s MySQL `Server=` and requires
one of **this machine's own IPv4 addresses** to match; otherwise it exits `2` without touching anything. Proven both
ways:

| Scenario | Result |
|---|---|
| On the host | `this machine : V-MTMFG-5  addresses: 172.16.1.104` → guard PASS, then **21/21 PASS**, exit 0 |
| Simulated foreign host (`-ServerHost 10.99.99.99`) | `REFUSING TO RUN: this machine is not the cache host.` → **exit 2**, install folder untouched (727 files intact), service still running |

**Agent rule (also recorded in `.github/copilot-instructions.md`).** Do not run this script, bypass its guard with
`-AllowNonServerHost`, or hand-roll the steps elsewhere unless the environment *is* the server (`172.16.1.104`,
`V-MTMFG-5`) — i.e. VS Code running on the host. Otherwise hand the deployment to someone on the host.

**End-to-end run (2026-09-11, on `V-MTMFG-5`).** `-Publish` → publish with no warnings, 727 files deployed,
`Database/...` content and the corrected population guards present, secrets already correct and re-proven, service
started as pid 7916, port 5760 listening, no top-level window, `/api/status` → 401, second launch redirected and
exited, auto-start entry re-pointed at `C:\Services\MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.exe`.
Summary line: `DEPLOYMENT OK — 21 checks, 0 warning(s).`

### T147 — the shared API credential is generated but never obtainable

- [x] T147 (**RESOLVED 2026-09-12** — the shared credential was retired and authorization moved to the caller's application role; see the implementation record below) The service
  generates and stores its shared API credential without operator action, and **no surface can reveal it** — yet the
  documented deployment step says to record it out of band, the documented verification calls `/api/status` with it,
  and clients must be given it. As shipped, no operator and no client can ever authenticate, so the service's own
  status surface is unobservable (FR-026 vs FR-013/SC-010, contradicts).

> **Implementation record (2026-09-12, `/speckit.implement`).** Owner decision taken: authorization is by the
> caller's **application role**, not by a secret. The shared-token scheme was removed rather than repaired —
> `SharedTokenAuthenticationHandler` and `SharedCredential` are deleted, `ApiSettings` carries no credential, the
> settings surface no longer offers rotate, and `MockServiceClientOptions`/`MockServiceRefreshClient` present
> `X-MTM-Mock-User` instead of a token (`MTM_MOCK_SERVICE_TOKEN` → `MTM_MOCK_SERVICE_USER`). A new
> `ServiceOperatorAuthenticationHandler` plus `ServiceOperatorRoleResolver` resolve the asserted user through
> `sp_auth_user_row_get` against `mtm_waitlist` and require one of `ServiceOperatorRoles.Approved` (`Admin`,
> `Developer`, `Plant Manager`, `Setup Lead`, `Production Lead`); unknown user, inactive user, no role and
> unapproved role all answer `401 {"error":"unauthorized"}`, and an unreachable store **fails closed**. The status
> payload reports `operatorRoles` in place of `credentialConfigured`. The spec, both service contracts and
> `data-model.md` §9 were amended in the same change (FR-011/FR-012/FR-023/FR-026, SC-010).
>
> **Known limitation, accepted by the owner.** The caller *asserts* its user name over plain HTTP. The service
> verifies that the asserted user genuinely holds an approved role, so an invented name or an ordinary shop-floor
> user is refused, but this is authorization without cryptographic authentication.
>
> **Not verified here.** The role lookup needs a live `mtm_waitlist` connection and the API needs a deployed,
> running service; both are the host run handed over in `VALIDATION-PROMPT-SERVER.md`.

**Evidence (2026-09-11, from the first real deployment).**

- A credential appeared **without anyone generating one**: `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\service-configuration.json`
  carries `Api.CredentialProtected` (a DPAPI blob) with `CredentialCreatedUtc: 2026-09-11T19:50:49Z` — the moment the
  service started.
- The settings surface is deliberately **write-only**: `ServiceSettingsViewModel` states *"The credential is
  write-only from this surface — the page can rotate it, and never displays the value"*, exposes only
  `RotateCredentialText` / `CredentialWriteOnlyText`, and `RotateCredentialAsync` does nothing but call
  `GenerateCredentialAsync()`. There is no reveal path anywhere.
- But the documentation assumes the operator has it: `MTM_Waitlist.Mock.Service/README.md` §3 step 3 says *"record it
  out of band"*, §4 says to call `curl -H "X-MTM-Mock-Token: <credential>" .../api/status`, and the client reads the
  same value from `MTM_MOCK_SERVICE_TOKEN` (`MTM_Waitlist.Mock/Models/MockServiceClientOptions.cs`).
- **Observed consequence:** after deploying, all three probed endpoints answered `401`, and the correct token is
  unknowable — to the operator, to the clients, and to me. §2 step 7 (each shape's `lastOutcome` becomes `succeeded`
  with a fresh `refreshedUtc`) and step 8 are therefore **not executable**, and neither is the tray-independent
  verification in `README.md` §4.

**The decision needed:** either the credential is **revealed once** when generated/rotated (so §3's "record it out of
band" is achievable), or the operator **supplies** the value and the service stores that. FR-026's "never displayed
or logged" is about not persisting or leaking it, so a deliberate one-time reveal on rotation is compatible with it —
and is presumably the missing piece rather than a contradiction of it.

**Also unverified this run, for the same reason.** ~2 minutes after startup no refresh run record had been written and
the mirror's `refreshed_utc` values were unchanged, although all five shapes validate clean against
`sp_visual_read_shape_metadata_get(NULL)` (mirror, stage, `get` and `refresh` all present) and
`StartBackgroundEnginesAsync` starts the refresh loop unconditionally. Whether the first cycle was still in flight
(a 701/893/1,746/79,457/701-row load, with shape 4 alone ~79k rows) or had not started could not be established
because the status surface is unreachable. This is T106 §2 step 7's gap, now with a concrete, named blocker.

### T148 — the running service is invisible and unreachable, and nothing records why it does not refresh

- [x] T148 (**RESOLVED 2026-09-12** — (a) shipped as the show-request channel + desktop shortcut, (c) shipped as the service's own daily log file; (b) was fixed 2026-09-11) Three findings from
  running the deployed service on the host for ~22 minutes, all observed 2026-09-11.

**(a) The tray icon is created, but Windows 11 hides it — so the UI has no reachable entry point.**

`HKCU\Control Panel\NotifyIconSettings` (the Windows 11 notification-area state) contains **two** entries for this
service, one per exe it has been launched from — `…\bin\publish\win-x64\MTM_Waitlist.Mock.Service.exe` (the smoke test)
and `C:\Services\MTM_Waitlist.Mock.Service\MTM_Waitlist.Mock.Service.exe` (the deployment) — and **both have
`IsPromoted` unset**, i.e. hidden in the overflow. So the icon exists (`App.xaml.cs` sets `IsVisible = true` with a
valid `Assets/WindowIcon.ico`, verified present in the deployed folder) and the shell registered it; it is simply not
promoted, which is Windows' default for any new tray icon.

That would be cosmetic **except** that the tray icon is the *only* way to reach the service UI:
`ServiceShellWindow` (a real NavigationView window with the Status and Settings pages, created lazily and hidden on
close) is only opened from `_trayIcon.Selected` / its context menu, and **the running instance has no
`AppInstance.Activated` handler** — so re-launching the exe does not raise the window either; it redirects and exits
(confirmed: the second copy exits, one process survives, no window appears). With the icon in the overflow there is
therefore **no route to Settings/Status at all**, which is how a working install comes to look broken.

*Reveal it:* click the taskbar's `∧` overflow, or Settings → Personalization → Taskbar → *Other system tray icons* →
turn `MTM_Waitlist.Mock.Service` on (that writes the same `IsPromoted` value and takes effect immediately).

*The scope question:* `plan.md` ("tray-only lifetime … no main window required") and T059 ("creates only a
`WinUIEx.TrayIcon` … (no main window)") deliberately chose this shape, so the code matches the spec; what the spec did
not anticipate is that a hidden tray icon leaves the UI unreachable. The cheap, in-spirit fix is to make the UI
reachable without the icon — show the shell window on a second launch/activation — which does not change the
tray-only *lifetime*, only the entry points to it.

**(b) No refresh has completed, and nothing on the machine says why.** After ~22 minutes running (started 14:50:48,
observed 15:12) the 15:00 grid slot had passed and: `refresh-run-records.json` **did not exist**, every mirror's
`refreshed_utc` was unchanged, and process CPU was **2.3 s**. `RefreshRunRecordStore.RecordAsync` persists the whole
set on every completed run, so a missing file is not a persistence-timing artefact — **no shape refresh has
completed**. All five shapes pass the startup validation the engine depends on (deployed
`sp_visual_read_shape_metadata_get(NULL)` returns all four artifacts per shape, and the expected mirror column lists
derived from `BuildExpectedMirrorColumns` match the deployed tables), and `StartBackgroundEnginesAsync` starts the
refresh loop unconditionally, so the loop is running and its cycles are failing or stuck — each attempt fitting a
fast failure plus a bounded retry delay (CPU stayed at 2.3 s while ~4 retries would have elapsed).

**(c) There is no durable diagnostic, so (b) is undiscoverable.** The reason for those failures is written only
through `StartupDebugLog` → `Debug.WriteLine`, visible only to an attached debugger: the service does not register the
`MTM_Waitlist.Startup` log service that gives the *client* app its `startup_daily_<date>.jsonl` file, and nothing was
written to that folder during this run. The two surfaces that *would* report it — `GET /api/status` and the tray's
Status page — are exactly the ones (a) and T147 put out of reach. A service that cannot be asked why it is not working
is not operable.

**Next actions, in order:** fix the credential (T147) so `/api/status` can be read; give the service a durable log
file; then re-run the deployment and re-check §2 step 7. Making the window reachable without the tray icon (a) is
independent and small.

### T148(b) — root cause found and fixed: the service was started with no credentials (2026-09-11)

The operator's screenshot of the Status page named the cause outright, for all five shapes:

```text
Could not read <shape> artifact metadata: Access denied for user ''@'localhost' (using password: NO)
```

**This was my defect, not the service's.** `install-mock-service.ps1` wrote the four secrets at User scope, read
them back from `HKCU\Environment`, and proved them by connecting — all of which passed — and then launched the
service with `Start-Process`. But **writing a User-scope variable does not update any existing environment block**,
and `Start-Process` hands the child a copy of *the script process's* block. The script process had been started
before the variables existed, so the service inherited an environment with **no** `MTM_*` / `INFOR_VISUAL_*` values.
It therefore fell back to its own defaults (`MySqlConnection.Server = localhost`, empty login), and every metadata
read and every refresh cycle failed on the connection — which is exactly T148(b): a running service that never
refreshed, with nothing on the machine saying why.

**Fixed in three places.**

| Fix | Where |
|---|---|
| Apply the secrets to the script's **own** process before starting, and assert they are there, so the child inherits them | `deploy/install-mock-service.ps1` — new `secrets inherited by child` check (deploy is now **22** checks) |
| Treat a host with **no login** as unconfigured, so the operator sees the designed `NotConfiguredMessage` instead of a raw access-denied | `Services/MySqlConnectionStringResolver.BuildFromSettings` |
| Poll for process exit after the stop instead of sampling once — a WinUI process lingers briefly after termination, and one sample reported `1 process(es) survived` while the API port was already free | `deploy/install-mock-service.ps1` stop step |

### T148(a) and T148(c) — closed 2026-09-12

**T148(a) was already shipped, and the task text was stale.** Commit `c2d5e04` contains the fix the task asked for:
`App.xaml.cs` starts a show-request listener on two session-local named events
(`Services/ServiceShowChannel.cs`), a second launch parses its own command line
(`Services/ServiceActivationParser.cs`: `--open-status` / `--open-settings`) and signals the running instance before
redirecting and exiting, and `deploy/mock-service-control.ps1` — shipped inside the publish output and opened by the
desktop shortcut the installer places — is what an operator double-clicks. The tray-only *lifetime* is unchanged; only
the *entry points* to it changed, which is what the task proposed. `ServiceActivationParserTests` and
`ServiceShowChannelTests` pin the switch names and the event names.

**T148(c) implemented 2026-09-12.** The root cause was worse than "no log service": every service diagnostic went
through `StartupDebugLog`, whose methods are `[Conditional("DEBUG")]`, so a published `Release` build compiled them
**out entirely**, and in Debug they only reached `Debug.WriteLine`. Nothing on the host could record a failure.

| Artifact | What it does |
|---|---|
| `MTM_Waitlist.Mock.Service/Services/ServiceLog.cs` | Best-effort, thread-safe JSON-Lines append to `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\Logs\service_daily_<yyyy_MM_dd>.jsonl`; 30-day retention keyed off the file name; every failure swallowed after a `Debug.WriteLine` trace so logging can never stop the engines |
| `MTM_Waitlist.Mock.Service/Services/ServiceFileLoggerProvider.cs` | An `ILoggerProvider` registered in `ServiceHostBuilder.Build`, so every container log message — the refresh engine's cycle failures above all — is durable. The logger category becomes the log `Area` |
| `App.xaml.cs` / `ServiceShellWindow.xaml.cs` | Every `StartupDebugLog` call replaced with `ServiceLog`, plus a first-line `Start request` record naming the log directory so a launch that dies before the container exists still leaves a trace |
| `MTM_Waitlist.Tests/Module_Mock_Service/ServiceLogTests.cs` | 7 tests: the daily file is created and appended to, the line carries level/area/message/timestamp, an error keeps the exception text, an unusable directory does not throw, and the provider routes container messages |

**Gates.** Solution build `0 warnings / 0 errors`; full suite `Failed: 0, Passed: 720, Skipped: 19, Total: 739`. The
skips are the pre-existing live-database integration suites (no `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` in this
environment).

**Not verified here, and deliberately left to the host run.** Nothing in T147 or T148(c) can be proven from this
workstation: the role lookup needs a live `mtm_waitlist` connection and the API needs a deployed, running service, and
the durable log is only exercised by a real service process. `VALIDATION-PROMPT-SERVER.md` in the repository root is
the handover for that run.

**Verified.** Redeployed and started on the host: **22/22 checks pass**, and the service performed its **first ever
successful refresh** — all five shapes `Succeeded` in ~4.4 s (`work_order_lookup` 699 · `operation_sequences` 891 ·
`subordinate_parts` 1,742 · `inventory_locations` 79,305 · `disposition_input` 699 rows), with the row counts
matching the T144-corrected population reads. A second cycle completed after the UI redeploy. So §2's blocking
condition — "the service cannot read its own cache" — is gone.

**Not a bug, recorded so nobody chases it:** the screenshot's "refresh interval: **100** minutes" is an OCR misread
of **180**. The live configuration holds `RefreshIntervalMinutes: 180`, `ServiceApiOperations` passes
`configuration.RefreshInterval.TotalMinutes` straight through, and the resource string is
`Refresh interval: {0} minutes` — there is no 100 anywhere in the service. No change made.

### T149 — status and settings surfaces rebuilt (2026-09-11)

- [x] T149 (MEDIUM) The service UI read as a wall of unlabelled values — repeated `Never / Never / Unavailable`
  triples, raw shape keys, an always-present empty error line and no visual hierarchy — so the two surfaces were
  rebuilt on Fluent cards; per FR-013 and the "keep layouts Fluent and accessible" rule (`winui3-api-rules`).

**What changed** (presentation and display strings only — no behaviour, no new commands, no converter, no
`App.xaml` resource):

- **Status page** — a header with the page subtitle and the *Reload* action moved up beside the title; the flat
  seven-line summary is now a **card** of labelled rows (service version, started, Infor Visual, API credential,
  refresh schedule, backup tool, start at logon); each read shape and each store is now a **card** with a bold
  friendly name (`Work order lookup`, `Mock cache`) over its raw key, an outcome chip, and *labelled* detail
  columns (Last run · Rows · Cached data; Last run · Next due · Artifacts kept · Latest artifact); the
  startup-validation reason is a **critical-coloured block that is hidden when there is no error**
  (`Visibility="{x:Bind HasValidationError}"`, the model exposing a `bool`).
- **Settings page** — sections are cards with subtitles, consistent field spacing and the save action kept last.
- **Strings** — 35 new `Service_Status.*`, `Service_Shape.*` and `Service_Store.*` keys; friendly names resolve
  through `GetLocalized()` and fall back to the raw identifier when a string is not authored, so a sixth shape
  still renders (constitution V: no literal assembled in XAML).
- **Verified:** `dotnet build ... -c Release` → **0 warnings / 0 errors**, redeployed, service healthy, and the
  refresh still succeeding. The richer *colour-coded* outcome chips (green/amber/red) are deliberately not done —
  they need a converter or a brush on the row model, and neither is worth the XAML risk for this pass.

### T106 — still not ticked

§2's publish, install, tray-only start, single-instance and API-gating halves are now **done** — the host install is
in place and the blocker that stopped Phases 18/19/21 ("a host-side deployment to another machine") is retired. What
remains of §2 is the tray Settings surface, the write-once credential, and one real refresh cycle, all blocked on
operator interaction; §3's fallback proof and §4's defect proof need a **signed-in** application session; §5 needs a
configured service (two secrets) and a throwaway store; §7 is a maintainer-day exercise; SC-007/SC-008 are unstarted
30-day observation windows. Two of those
blockers were re-confirmed unchanged this run (`172.16.1.104:5760` closed, no `HKCU\…\Run` entry, no
`bin/publish/win-x64`). The new finding above adds a fourth reason the walkthrough cannot be signed off: even on a
fully provisioned host, §3 step 3's "every journey returns a complete, correctly shaped result" would pass on shape
and could still serve a different work order than the live read for the same input.

**Environment side effects: none.** Every command in this run was a read. No refresh was run, the service was not
started, no `HKCU\…\Run` entry was written, and the deployed `mtm_mock` snapshots are exactly as found (verified:
230 / 242 / 399 / 12712 / 230 rows, `is_seed_content = 0`). All scratch scripts under `%TEMP%` were deleted. The
separate T145 follow-up added one source file (the publish profile) plus one `.gitignore` negation and ran
`dotnet publish` into the gitignored `bin/publish/win-x64`; it touched no database and no service state either.
