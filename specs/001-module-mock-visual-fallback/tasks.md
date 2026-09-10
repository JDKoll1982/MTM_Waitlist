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
independently. **Verification is mandatory** — the ratified constitution (`.specify/memory/constitution.md`, v1.0.0,
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

- [ ] T001 Capture the pre-change baseline: run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`; record warning/error counts and the test pass count in `specs/001-module-mock-visual-fallback/quickstart.md` §0 (verification)
- [ ] T002 [P] Create the `MTM_Waitlist.Mock` class library project `MTM_Waitlist.Mock/MTM_Waitlist.Mock.csproj` (`net10.0-windows10.0.19041.0`, no XAML Views) referencing `MTM_Waitlist.Core/MTM_Waitlist.Core.csproj`, and add it to `MTM_Waitlist.sln`
- [ ] T003 [P] Create the `MTM_Waitlist.Mock.Service` WinUI 3 desktop app project `MTM_Waitlist.Mock.Service/MTM_Waitlist.Mock.Service.csproj` (unpackaged `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `RuntimeIdentifier=win-x64`) with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and add it to `MTM_Waitlist.sln`
- [ ] T004 [P] Create the `Database/Mock/` artifact tree — `Database/Mock/Bootstrap/`, `Database/Mock/Tables/`, `Database/Mock/StoredProcedures/`, `Database/Mock/Seeds/`, `Database/Mock/Validation/`, plus empty master lists `Database/Mock/AllTables.sql`, `Database/Mock/AllSPs.sql`, `Database/Mock/AllSeeds.sql`
- [ ] T005 [P] Create the test namespaces/folders `MTM_Waitlist.Tests/Module_Mock/` and `MTM_Waitlist.Tests/Module_Mock_Service/`
- [ ] T006 Add project references to `MTM_Waitlist.Mock/MTM_Waitlist.Mock.csproj` from `MTM_Waitlist.csproj`, `MTM_Waitlist.Setup/MTM_Waitlist.Setup.csproj`, `MTM_Waitlist.Waitlist.View/MTM_Waitlist.Waitlist.View.csproj`, and `MTM_Waitlist.Settings/MTM_Waitlist.Settings.csproj`

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

- [ ] T007 [P] Create the `mtm_mock` database bootstrap `Database/Mock/Bootstrap/create_database.sql` + `Database/Mock/Bootstrap/rollback.sql` (re-runnable; creates the DB if absent, `utf8mb4` / `utf8mb4_unicode_ci`; no dependency on `mtm_waitlist`) — this dedicated cache is separate from the app's own store and is never authoritative for internal application data (FR-027)
- [ ] T008 [P] Create the mandatory `mtm_mock` maintenance file `Database/Mock/Bootstrap/update_table_descriptions.sql` (table/column descriptions for every `mtm_mock` object; updated in every later schema change)
- [ ] T009 [P] Create mirror table + stage twin for shape 1 in `Database/Mock/Tables/visual_work_order_lookup_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_work_order_lookup_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.1 (columns: `id`, `normalized_work_order`, `part_number`, `description`, `work_center`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_work_order_lookup_result_work_order`). **Record the DDL-review deviation note** for the rules-listed banned word `work_order` (R9) in the artifact's header comment.
- [ ] T010 [P] Create mirror table + stage twin for shape 2 in `Database/Mock/Tables/visual_operation_sequences_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_operation_sequences_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.2 (columns: `id`, `normalized_work_order`, `part_number`, `sequence_number`, `description`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_operation_sequences_result_lookup_key`)
- [ ] T011 [P] Create mirror table + stage twin for shape 3 in `Database/Mock/Tables/visual_subordinate_parts_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_subordinate_parts_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.3 (columns: `id`, `normalized_work_order`, `parent_part_number`, `sequence_number`, `category`, `part_number`, `description`, `location`, `user8`, `on_hand_quantity`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_subordinate_parts_result_lookup_key`)
- [ ] T012 [P] Create mirror table + stage twin for shape 4 in `Database/Mock/Tables/visual_inventory_locations_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_inventory_locations_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.4 (columns: `id`, `part_number`, `location`, `on_hand_quantity`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_inventory_locations_result_location`)
- [ ] T013 [P] Create mirror table + stage twin for shape 5 in `Database/Mock/Tables/visual_disposition_input_result/{create.sql,rollback.sql}` and `Database/Mock/Tables/visual_disposition_input_result_stage/{create.sql,rollback.sql}` per `data-model.md` §3.5 (columns: `id`, `work_order`, `part_number`, `work_order_status`, `open_work_order_quantity`, `finished_goods_quantity`, `has_outside_vendor_operation`, `refreshed_utc`, `is_seed_content`; unique `uq_visual_disposition_input_result_lookup_key`)
- [ ] T014 Register all five mirror tables and their stage twins in `Database/Mock/AllTables.sql` and `Database/Mock/Bootstrap/update_table_descriptions.sql`

### Subphase 2.2 — `mtm_mock` stored procedures (`sp_visual_<shape>_{refresh,get}`)

- [ ] T015 Create the reference refresh procedure `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_refresh/{create.sql,rollback.sql}` — truncate the `_stage` twin, load the complete new result (`refreshed_utc = UTC_TIMESTAMP()`, `is_seed_content = 0`), validate the load, then perform the **atomic multi-table `RENAME TABLE` swap inside the procedure** (`visual_x_result` → `_prev`, `_stage` → live, `_prev` → `_stage`); on any error leave the live table untouched (`research.md` R1, `data-model.md` §3.6, FR-006/SC-005)
- [ ] T016 [P] Create `Database/Mock/StoredProcedures/sp_visual_operation_sequences_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [ ] T017 [P] Create `Database/Mock/StoredProcedures/sp_visual_subordinate_parts_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [ ] T018 [P] Create `Database/Mock/StoredProcedures/sp_visual_inventory_locations_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [ ] T019 [P] Create `Database/Mock/StoredProcedures/sp_visual_disposition_input_refresh/{create.sql,rollback.sql}` following the T015 reference pattern
- [ ] T020 [P] Create the parameterized read `Database/Mock/StoredProcedures/sp_visual_work_order_lookup_get/{create.sql,rollback.sql}` returning the live mirror's exact column shape for shape 1 (`normalized_work_order` in → `part_number`, `description`, `work_center` out)
- [ ] T021 [P] Create `Database/Mock/StoredProcedures/sp_visual_operation_sequences_get/{create.sql,rollback.sql}` for shape 2 (`normalized_work_order`, `part_number` in → `sequence_number`, `description` out)
- [ ] T022 [P] Create `Database/Mock/StoredProcedures/sp_visual_subordinate_parts_get/{create.sql,rollback.sql}` for shape 3 (`p_part_number` exposes the operation's part context → `category`, `part_number`, `description`, `location`, `user8`, `on_hand_quantity` out, per `data-model.md` §3.3 note)
- [ ] T023 [P] Create `Database/Mock/StoredProcedures/sp_visual_inventory_locations_get/{create.sql,rollback.sql}` for shape 4 (`part_number` in → `part_number`, `location`, `on_hand_quantity` out)
- [ ] T024 [P] Create `Database/Mock/StoredProcedures/sp_visual_disposition_input_get/{create.sql,rollback.sql}` for shape 5 (`work_order`, `part_number` in → `work_order_status`, `open_work_order_quantity`, `finished_goods_quantity`, `has_outside_vendor_operation` out)
- [ ] T025 Register all ten `sp_visual_*` procedures in `Database/Mock/AllSPs.sql`
- [ ] T026 Add the baseline seed content for all five mirror tables (rows with `is_seed_content = 1`) in `Database/Mock/Seeds/seed_visual_mirror_baseline/{create.sql,rollback.sql}` and register it in `Database/Mock/AllSeeds.sql`, so a fresh install serves a usable result before the first refresh (FR-017)

### Subphase 2.3 — Service-app core (configuration, credential, shape catalog, refresh-engine skeleton)

- [ ] T027 [P] Create the service models in `MTM_Waitlist.Mock.Service/Models/` — `ServiceConfiguration.cs`, `RefreshRunRecord.cs`, `BackupPolicy.cs`, `BackupArtifact.cs`, `RestoreOutcome.cs`, `SharedCredential.cs` (fields per `data-model.md` §4/§5/§6/§7/§9); shape definitions reuse the shared `MTM_Waitlist.Mock/Models/VisualReadShape.cs` (`data-model.md` §2) — there is no service-local `RefreshShapeDefinition` duplicate
- [ ] T028 Create `MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs` — durable service-local configuration (all-or-nothing atomic save, value validation at save time) with the shared credential stored **only** as a DPAPI `CurrentUser` protected blob (generate-once, rotate-only, never displayed or logged); per `contracts/mock-service-configuration.md` §1/§2, FR-012/FR-026
- [ ] T029 [P] Create `MTM_Waitlist.Mock.Service/Services/RefreshShapeCatalogProvider.cs` — the catalog for the five initial shapes (`shapeKey`, `module`, `sourceScriptRelativePath`, `inputParameters`, `outputColumns`, derived `mirrorTable`/`stageTable`/`getProcedure`/`refreshProcedure`, `refreshIntervalMinutes`, `isEnabled`) plus startup validation that the script, tables, and both procedures exist and `outputColumns` matches the recorded live projection — **performed by calling `sp_visual_read_shape_metadata_get`** (`data-model.md` §12), never by inline SQL; invalid shapes are excluded and reported, never crash the service (FR-020, constitution III, `contracts/mock-service-configuration.md` §3)
- [ ] T030 Create `MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs` **skeleton** — catalog-driven per-shape refresh that calls `CALL sp_visual_<shape>_refresh`, enumerates outcomes as `RefreshRunRecord`, and **skips + logs** when Infor Visual is unreachable, leaving the live snapshot untouched (FR-008); the scheduling loop itself is completed in US3

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

- [ ] T031 [US1] Remove the `MtmWaitlist` mock short-circuit in `MTM_Waitlist.Setup/Services/SetupPersistenceService.cs` — delete `SaveMockAsync` and the mock branch so `SaveAsync` always writes the real `mtm_waitlist` store (root cause of the setup-save defect; FR-001, SC-001)
- [ ] T032 [US1] Remove the mock gating in `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` — delete `IsMockDataEnabled()` and every skip-read/skip-persist branch so `sp_waitlist_request_list` and all accept/note/status/cancel/audit persists always execute (FR-001, SC-002)
- [ ] T033 [US1] Remove the `Feature.RecvMockData` gating in `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` (including the `IsMockDataEnabledAsync(target)` path that gates the `MtmWaitlist` target) so no MySQL target is ever mock-gated (FR-001)
- [ ] T034 [P] [US1] Delete the mock toggle keys and service — `MTM_Waitlist.Core/Services/MockSettingKeys.cs`, `MTM_Waitlist.Core/Services/MockToggleService.cs` and its contract — and remove every raw-key read of `Feature.InforVisualMockData` / `Feature.RecvMockData` (FR-003, FR-014)
- [ ] T035 [P] [US1] Delete the mock routing / auto-force / monitoring stack from `MTM_Waitlist.Core/Services/` (`MockRoutingService`, `MockRoutingCoordinator`, `MockConfigurationService`, `MockRoutingRefreshService`, `MockRoutingMonitorService`, `MockModePollingHost`, `MockFallbackDebouncer`, `MockModeChangeDetector`, `MockModeSummaryProvider`, `DispatcherPollScheduler`) plus `MockModeToastCoordinator` and their contracts/models, and remove the wiring from `Module_Core/Views/ShellPage.xaml.cs` and `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (FR-014)
- [ ] T036 [US1] Remove the `mockAction` short-circuit branch from `MTM_Waitlist.Core/Services/SqlHelperServer.cs` (depends on T034)
- [ ] T037 [P] [US1] Remove the Settings "Mock Data" surface — `Module_Settings/Views/SettingsPage.xaml` (`MockDataExpander`, the `UseMockData` `ToggleSwitch`) and `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs` (`UseMockData`, `OnUseMockDataChanged`, `IsMockDataPanelVisible`, the `IMockToggleService` injection) — so no demo control exists (US1 acceptance 4, FR-014)
- [ ] T038 [P] [US1] Drop the DB-backed demo tables — `Database/Tables/20_mock_master_tables_registry/`, `22_mock_work_orders/`, `23_mock_work_centers/`, `24_mock_locations/`, `25_mock_requesters/`, `26_mock_inventory_locations/`, `27_mock_request_types/`, and `mock_parts` — as `rollback.sql` artifacts, and remove their entries from `Database/Tables/AllTables.sql` and `Database/Bootstrap/update_table_descriptions.sql` (FR-014)
- [ ] T039 [P] [US1] Drop the `sp_mock_*` procedures (including `sp_mock_master_table_columns_get`) from `Database/StoredProcedures/sp_mock_*/` as `rollback.sql` artifacts and remove them from `Database/StoredProcedures/AllSPs.sql` (FR-014)
- [ ] T040 [P] [US1] Remove the demo seed artifact `Database/Seeds/seed_mock_master_default/{create.sql,rollback.sql}` and its registration in `Database/Seeds/AllSeeds.sql`
- [ ] T041 [US1] Delete `MockMasterDataService` and its contract/models/validator (`IMockMasterDataService`, `MockMasterRowEditValidator`, `MockMasterTableDefinition`, `MockMasterColumnDefinition`) and their DI registration in `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (FR-014)
- [ ] T042 [US1] Delete the in-app sample catalogs and their contract — `MTM_Waitlist.Waitlist.View/Services/SampleDataService.cs`, `SampleWaitlistRequestCatalog.cs`, `SampleInventoryLocationCatalog.cs`, `SampleAverageCoilWeightCatalog.cs`, `MTM_Waitlist.Waitlist.NewRequest/Services/SampleJobCoilCatalog.cs`, `MTM_Waitlist.Setup/Services/SetupDataCatalog.cs`, `MTM_Waitlist.Waitlist.View/Models/SampleOrder.cs`, `MTM_Waitlist.Waitlist.NewRequest/Models/WaitlistCoilInfo.cs`, and `MTM_Waitlist.Core/Contracts/Services/ISampleDataService.cs` (FR-014; depends on US2 T053–T055 rerouting their consumers)
- [ ] T043 [US1] Rework/delete the tests that exercise the retired mock behavior — the `Mock*`/`Sample*`/`HelperServers` suites under `MTM_Waitlist.Tests/Core/`, `MTM_Waitlist.Tests/Module_Waitlist/`, `MTM_Waitlist.Tests/Module_Setup/`, `MTM_Waitlist.Tests/Module_Settings/` and the `FakeSampleDataService` double in `MTM_Waitlist.Tests/Module_Settings/TestDoubles.cs` (SC-015: 0 tests reference a retired system)
- [ ] T044 [US1] (verification) Add/rework assertions that internal stores are always live — `MTM_Waitlist.Tests/Module_Setup/SetupPersistenceServiceTests.cs` (save always hits the real store) and `MTM_Waitlist.Tests/Module_Waitlist/WaitlistRequestServiceTests.cs` (every lifecycle action persists) — then run `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` (0w/0e) and `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`

**Checkpoint**: US1 is independently functional — the work-center card updates after a save, the lifecycle persists,
and no demo control exists. The retired demo systems are gone from code and database.

---

## Phase 4: User Story 2 — Automatic cached fallback for external reads (Priority: P1) 🎯 MVP

**Goal**: Detect Infor Visual unreachability automatically and serve each of the five Visual read shapes from the
`mtm_mock` mirror with an identical result shape — with no user action and no manual mode — and return to live reads
when the source recovers. Owns the `MTM_Waitlist.Mock` library and the routing of its five call sites.

**Independent Test**: Make Infor Visual unreachable, exercise all five read journeys, and confirm each returns a
complete, correctly shaped result with no error and no partial rows; restore the source and confirm live reads return
without restarting the app; confirm a reachable-but-empty live result is never replaced by cached data.

- [ ] T045 [P] [US2] Create the fallback contracts `MTM_Waitlist.Mock/Contracts/IVisualReachabilityDetector.cs` and `MTM_Waitlist.Mock/Contracts/IVisualReadFallback.cs` exactly as specified in `contracts/visual-read-fallback.md` §1/§2
- [ ] T046 [P] [US2] Create the fallback model `MTM_Waitlist.Mock/Models/CachedReadResult.cs` (`ServedFromLive | ServedFromCache` + `refreshed_utc` provenance); the detector state enum is `VisualReadStatus` (`Unknown | Live | Cached`), created with the status model in T065
- [ ] T047 [US2] Implement `MTM_Waitlist.Mock/Services/VisualReachabilityDetector.cs` — an independent connectivity probe (no reference to the retired `MockRoutingService`/`MockModeChangeDetector`/`MockConfigurationService` stack) with 2-consecutive-failure → `Cached`, 1-success → `Live` hysteresis and backoff probing while cached (FR-002/FR-003, flapping edge case, `research.md` R11)
- [ ] T048 [P] [US2] Implement shape 1 `MTM_Waitlist.Mock/Services/VisualWorkOrderLookupFallback.cs` — attempt live, on unreachability read `sp_visual_work_order_lookup_get`; return live rows **including an empty set**; surface non-unreachability errors (FR-024)
- [ ] T049 [P] [US2] Implement shape 2 `MTM_Waitlist.Mock/Services/VisualOperationSequencesFallback.cs` (mirror `sp_visual_operation_sequences_get`)
- [ ] T050 [P] [US2] Implement shape 3 `MTM_Waitlist.Mock/Services/VisualSubordinatePartsFallback.cs` (mirror `sp_visual_subordinate_parts_get`)
- [ ] T051 [P] [US2] Implement shape 4 `MTM_Waitlist.Mock/Services/VisualInventoryLocationsFallback.cs` (mirror `sp_visual_inventory_locations_get`)
- [ ] T052 [P] [US2] Implement shape 5 `MTM_Waitlist.Mock/Services/VisualDispositionInputFallback.cs` (mirror `sp_visual_disposition_input_get`)
- [ ] T053 [US2] Route `MTM_Waitlist.Setup/Services/SetupLookupService.cs` through the fallbacks — `LookupWorkOrderFromBackendAsync` (shape 1), `GetSequencesFromBackendAsync` (shape 2), `GetSubordinatePartsFromBackendAsync` (shape 3) — removing the `*FromMockAsync` paths (FR-002/FR-004)
- [ ] T054 [US2] Route `MTM_Waitlist.Waitlist.View/Services/WaitlistInventoryService.cs` `GetInventoryLocationsFromBackendAsync` through shape 4, keeping the caller-side on-hand ≥ 1 and ignored-location filtering unchanged
- [ ] T055 [US2] Route `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs` `GetDispositionInputAsync` through shape 5, keeping `RequestDispositionStatusCodes` (Open = {R,U,F}, Closed = {C}, X never FG) as the only status authority and keeping the floor/WIP half reading the live WIP store (FR-018)
- [ ] T056 [US2] Add the fallback DI registrations for the detector and the five `IVisualReadFallback` implementations in the `MTM_Waitlist.Mock` service-registration extension, wired from `Services/DependencyInjection/ServiceRegistrationExtensions.cs` (depends on T047–T052)
- [ ] T057 [US2] Remove the Visual/sample short-circuit branches — the `ExecuteReadOnlyQueueAsync` mock overloads in `MTM_Waitlist.Core/Services/SqlHelperServer.cs` and the mock overloads in `MTM_Waitlist.Core/Services/MySqlHelperServer.cs` — after T053–T055 reroute their consumers (FR-002/FR-014)
- [ ] T058 [US2] (verification) Add fallback-parity tests in `MTM_Waitlist.Tests/Module_Mock/VisualReadFallbackParityTests.cs` — for all five shapes: live vs mirror are structurally identical (column set + ordering), unreachable → mirror rows served, and a reachable-but-legitimate-empty live result **is not** replaced by cache — `VisualReachabilityDetectorTests.cs` for the hysteresis/backoff state machine, plus a caller test proving a routed caller cannot distinguish the source, and an assertion that the cache is **never** consulted for an internal-store read (FR-027, constitution II); then run the build and full test suite gates (verification)

**Checkpoint**: US1 **and** US2 both work — with Infor Visual unreachable all five reads serve from `mtm_mock`; with it
up, live reads are used and return to live without a restart. This completes the P1 MVP.

---

## Phase 5: User Story 3 — The cache stays warm without anyone asking (Priority: P2)

**Goal**: The on-host service starts automatically at logon, sits in the notification area, and refreshes the mirror
on a configurable schedule — logging and skipping unreachable cycles without disturbing the last good snapshot.

**Independent Test**: Install and run the service on a host; observe a scheduled refresh complete for all enabled
shapes; make the source unreachable for one cycle and observe a logged skip with an unchanged `refreshed_utc` and the
next cycle still on schedule.

- [ ] T059 [US3] Implement the tray-only, single-instance service lifetime in `MTM_Waitlist.Mock.Service/App.xaml.cs` — `OnLaunched` creates only a `WinUIEx.TrayIcon` and starts the background engines (no main window), single instance enforced with `AppInstance.FindOrRegisterForKey` + `RedirectActivationToAsync`, closing the settings window hides it, and the process exits only on an explicit Quit (`research.md` R3)
- [ ] T060 [US3] Implement auto-start at logon — write/remove the per-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry from `MTM_Waitlist.Mock.Service/Services/ServiceConfigurationStore.cs`, reconcile setting-vs-registry state at startup, and report a mismatch instead of ignoring it (FR-007, `research.md` R4)
- [ ] T061 [P] [US3] Create `MTM_Waitlist.Mock.Service/Strings/en-us/Resources.resw` and consume every service UI/status string through `GetLocalized()` — no inline literals (constitution V)
- [ ] T062 [US3] Complete the scheduling loop in `MTM_Waitlist.Mock.Service/Services/RefreshEngine.cs` — global interval with per-shape overrides from `RefreshShapeCatalogProvider`, one cycle at a time (reject/guard re-entrancy), `skippedSourceUnreachable` recorded as a normal outcome, previous snapshot intact, next cycle attempted on schedule (FR-008, US3 acceptance 2/3/4)
- [ ] T063 [US3] Add the durable service-local run record store `MTM_Waitlist.Mock.Service/Services/RefreshRunRecordStore.cs` (per-shape last-run outcome/timestamp, sanitized error text with no credential; deliberately **not** in MySQL per `data-model.md` §4) (FR-013)
- [ ] T064 [US3] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/RefreshEngineTests.cs` — swap is atomic, a failed/skipped refresh leaves the live snapshot untouched, and a concurrent read during a refresh never observes a partial or empty mirror; then run the build and full test suite gates (verification)

**Checkpoint**: The service starts unattended, refreshes on schedule, and degrades to a logged skip when the source is down.

---

## Phase 6: User Story 4 — Read-only fallback status indicator (Priority: P2)

**Goal**: While cached data is being served, the shell shows a non-interactive indicator stating that Infor Visual is
unreachable, that cached data is in use, and **how old** that cached data is — with nothing to click and no mode
change. It clears automatically when live reads resume.

**Independent Test**: Force the source offline and confirm the indicator appears, shows the cached-data age, and is
non-interactive (click, Enter, and tab all trigger nothing); restore the source and confirm it clears.

- [ ] T065 [P] [US4] Create `MTM_Waitlist.Mock/Contracts/IReadStatusProvider.cs` and the models `MTM_Waitlist.Mock/Models/ReadStatusSnapshot.cs` + `MTM_Waitlist.Mock/Models/VisualReadStatus.cs` per `contracts/visual-read-fallback.md` §4 (state, last successful refresh, cached age, `IsSeedContentOnly`, per-shape last refresh)
- [ ] T066 [US4] Implement `MTM_Waitlist.Mock/Services/ReadStatusProvider.cs` — tracks the detector state, exposes `Current` + a `Changed` event, and reports `CachedDataAgeUtc` (null when only seed content exists); data is never refused based on age (FR-005/FR-022)
- [ ] T067 [US4] Create the non-interactive indicator `Module_Mock/Views/ReadStatusIndicator.xaml` + `Module_Mock/Views/ReadStatusIndicator.xaml.cs` — an `InfoBar` with `IsClosable="False"`, no action buttons and no command binding, bound to `IsCachedDataInUse` and the cached-data age, visible only while `Cached` (FR-003/FR-005, `research.md` R12)
- [ ] T068 [P] [US4] Add the indicator string keys to `Strings/en-us/Resources.resw` (unreachable message, cached-data-in-use statement, cached-data age/seed display) and consume them via `GetLocalized()` — no inline literals (FR-005/FR-022)
- [ ] T069 [US4] Host the indicator in the existing shell `Module_Core/Views/ShellPage.xaml` (no new navigation), register `IReadStatusProvider` in `Services/DependencyInjection/ServiceRegistrationExtensions.cs`, and register any converter/resource the indicator needs in `App.xaml` in the same change (constitution V; a missing `x:Key`/`xmlns` causes a `WMC0001`/`WMC9999` failure)
- [ ] T070 [US4] (verification) Add `MTM_Waitlist.Tests/Module_Mock/ReadStatusProviderTests.cs` covering the visibility rule (`visible ⇔ state == Cached`), the age value, the seed-content case, and that no public API can change the read mode; then run the build and full test suite gates (verification)

**Checkpoint**: Users can tell cached from live data, cannot change a data mode, and the indicator clears on recovery.

---

## Phase 7: User Story 5 — On-demand refresh and operational visibility (Priority: P3)

**Goal**: An authorized caller can request an immediate refresh and retrieve per-item last-run status over a
small credential-gated HTTP interface; the operator can configure and persist the service settings.

**Independent Test**: Request an immediate refresh through the service and observe it complete; request status and
observe per-item last-run results; make an unauthenticated request and observe a refusal.

- [ ] T071 [P] [US5] Create `MTM_Waitlist.Mock/Contracts/IMockServiceRefreshClient.cs` and the request-result model `MTM_Waitlist.Mock/Models/RefreshRequestResult.cs` per `contracts/visual-read-fallback.md` §5
- [ ] T072 [US5] Implement `MTM_Waitlist.Mock/Services/MockServiceRefreshClient.cs` — `POST /api/refresh` on the service API with the `X-MTM-Mock-Token` header; a missing service, missing credential, or error fails gracefully so the app continues on cached content (FR-025/SC-011, `contracts/mock-service-configuration.md` §5)
- [ ] T073 [US5] Implement `MTM_Waitlist.Mock.Service/Api/ServiceApiEndpoints.cs` — `POST /api/refresh` (optional `shapeKeys`; `200` with per-shape outcomes including `skippedSourceUnreachable`; `400` unknown shape; `409` refresh in progress) and `GET /api/status` (per-shape and per-store last-run outcome/timestamp, `toolAvailable`, no secrets, no side effects); `/api/restore` and any equivalent path are **not routed** (`contracts/mock-service-http-api.md` §2/§3/§5, FR-023)
- [ ] T074 [US5] Implement `MTM_Waitlist.Mock.Service/Api/SharedTokenAuthenticationHandler.cs` — constant-time comparison against the DPAPI-protected credential, `401 {"error":"unauthorized"}` for every endpoint, unauthorized attempts logged with source/method/path/timestamp only, and the credential never echoed in a response, header, error, log line, or status payload (FR-026/SC-010)
- [ ] T075 [US5] Implement `MTM_Waitlist.Mock.Service/Services/ServiceApiHost.cs` — Kestrel host bound to `ApiSettings.BindAddress`/`ApiSettings.Port`, started by the same host the tray app creates; the API host must start with `RestoreService` absent/unregistered (the two are independent registrations) (FR-011/FR-012)
- [ ] T076 [P] [US5] Implement the service settings surface `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml` + `.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` — refresh interval, API bind address/port, credential generate/rotate (write-only, never displayed), Visual connection details, and `mysqldump` path, with save-time validation and no partially written configuration (FR-012, `contracts/mock-service-configuration.md` §1/§2). **Record the shipped default for every setting** (refresh interval, per-store schedule/retention/destination, bind address/port) so SC-007/SC-008 are measured against a known baseline (`data-model.md` §5/§6).
- [ ] T077 [P] [US5] Implement the service status surface `MTM_Waitlist.Mock.Service/Views/ServiceStatusPage.xaml` + `.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceStatusViewModel.cs` — per-shape last-refresh outcome/timestamp and per-store last-backup outcome/timestamp, and a clear report when the backup tool is unavailable (FR-013)
- [ ] T078 [US5] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ServiceApiTests.cs` — unauthorized requests are refused 100% of the time, a credential-free `/api/status` payload contains no secret, an on-demand refresh returns per-shape outcomes, and `/api/restore` is unreachable; then run the build and full test suite gates (verification)

**Checkpoint**: Operators can force a refresh and see operational state; the network interface is token-gated and restore-free.

---

## Phase 8: User Story 6 — Backups and emergency restore of internal stores (Priority: P3)

**Goal**: Per-store configurable, periodic, restorable backups of all four MySQL stores, plus a host-only,
confirmation-gated emergency restore — never exposed on the network.

**Independent Test**: Configure a schedule for one store and observe a restorable artifact while the other stores back
up on their own schedules; with the tool absent observe a clear failure and no artifact; decline a restore prompt and
confirm no change, then accept it and confirm verified replacement.

- [ ] T079 [US6] Implement `MTM_Waitlist.Mock.Service/Services/BackupEngine.cs` — one `mysqldump` invocation per store with `--host/--port/--user/--password-file --single-transaction --routines --databases <db> --result-file=<path>` (password only through an option file; `--result-file` mandatory to avoid the Windows PowerShell UTF-16 dump pitfall), per-store enable/schedule/retention/destination, an up-front tool-availability probe reporting `toolUnavailable` **without** recording an artifact, and success recorded only on exit 0 **and** a non-zero file (FR-009/FR-013/SC-008, `research.md` R7, `data-model.md` §6)
- [ ] T080 [US6] Implement `MTM_Waitlist.Mock.Service/Services/RestoreService.cs` — host-only, callable only from a local UI action, gated by an explicit `ContentDialog` confirmation, and sequenced safety snapshot → `DROP DATABASE` + `CREATE DATABASE` (utf8mb4) → reload without `--force` → row-count verification → recorded outcome; an unconfirmed request changes nothing and a failed restore names the safety snapshot for recovery (FR-010/FR-023/SC-009, `research.md` R8, `data-model.md` §7)
- [ ] T081 [US6] Add the backup endpoints `POST /api/backup` and `GET /api/backups` to `MTM_Waitlist.Mock.Service/Api/ServiceApiEndpoints.cs` (same file as T073; do not parallelize) — `toolUnavailable` returns `200` with no artifact, never a partial or zero-length artifact recorded as success (FR-013)
- [ ] T082 [P] [US6] Add the per-store backup configuration section (enable, schedule, retention, destination) to `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml` + `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` — disabling one store must not change any other store's schedule or artifacts (FR-009, same files as T076; do not parallelize)
- [ ] T083 [US6] Add the host-only restore surface (artifact picker + confirmation prompt + outcome display) to `MTM_Waitlist.Mock.Service/Views/ServiceSettingsPage.xaml`/`.xaml.cs` and `MTM_Waitlist.Mock.Service/ViewModels/ServiceSettingsViewModel.cs` (depends on T080; same files as T076/T082)
- [ ] T084 [US6] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/BackupRestoreTests.cs` — per-store independence, a missing tool yields `toolUnavailable` with no artifact record, an unconfirmed restore is a no-op, a confirmed restore is a verified full replacement, and no code path reaches `RestoreService` from an HTTP request; then run the build and full test suite gates (verification)

**Checkpoint**: Every enabled store produces restorable artifacts on its own schedule, and restore is host-only and confirmation-gated.

---

## Phase 9: User Story 7 — Extend to a new external read shape (Priority: P3)

**Goal**: Publish the ordered, six-step playbook (each step naming its single artifact) so a maintainer who has never
worked on the cache can add a sixth read shape end-to-end without changing any existing shape or contract.

**Independent Test**: Hand the playbook to a maintainer who has not worked on the cache; they add a read shape
end-to-end and demonstrate its fallback while the existing five behave unchanged.

- [ ] T085 [P] [US7] Publish the six-step playbook — reproduced from `contracts/mock-service-configuration.md` §4 (capture the read → mirror schema → procedures → service registration → in-app fallback → verify) into `WeekendProject/Module_Mock/Spec.md` §11 and `WeekendProject/Module_Mock/Plan.md` §7, cross-linked from `WeekendProject/Module_Mock/Tasks.md` (FR-016/FR-028)
- [ ] T086 [US7] Implement the half-added-shape detection in `MTM_Waitlist.Mock.Service/Services/RefreshShapeCatalogProvider.cs` — a shape with tables/procedures but no catalog entry (never refreshed) and a shape with a catalog entry but no in-app fallback implementation (no fallback) are both reported, not silently ignored; the existence check calls `sp_visual_read_shape_metadata_get` (`data-model.md` §12), never inline SQL (FR-016/FR-020, constitution III, `contracts/mock-service-configuration.md` §4)
- [ ] T087 [US7] (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ShapeCatalogValidationTests.cs` proving a catalog-to-artifact mismatch is detected and reported while the service still starts and serves the remaining shapes; then run the build and full test suite gates (verification)

**Checkpoint**: The extension path is documented and mechanically guarded; adding a shape changes no existing contract.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: SP-first conversion of **all** inline MySQL SQL, the real-data gap fixes, documentation accommodation,
the two mechanical audits (SC-013), and the final build/test gates (SC-015).

### Subphase 10.1 — SP-first conversion of the inline-SQL inventory (FR-015, `Discovery/03`)

> Route to the **existing** procedure where one covers the site; otherwise create a new procedure (with `rollback.sql`
> and master-list + `update_table_descriptions.sql` registration in the same change). Sources are the file:line sites in
> `WeekendProject/Module_Mock/Discovery/03-Hardcoded-MySQL-Sql.md`.

- [ ] T088 Route `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` (inline SQL at L14, exec L45) to the existing `sp_receiving_history_average_coil_weight` in `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql` (Discovery/03 §B)
- [ ] T089 Route `MTM_Waitlist.Shared/Services/WorkCenterCatalogService.cs` to its existing SPs — hot-WC read L91 → `sp_config_hot_workcenters_get_for_workstation`, hot-WC delete L272–279 → `sp_config_hot_workcenters_delete_for_workstation`, hot-WC upsert L284–333 → `sp_config_hot_workcenters_upsert`, available WCs L342–350 → `sp_setup_work_centers_get_all` (Discovery/03 §A)
- [ ] T090 Route `MTM_Waitlist.Settings/Services/ImageLocationService.cs` `LoadWorkCenterDetailsAsync` (L650–674) to `sp_setup_work_centers_get_all`, verifying the `IN (...)` filter semantics before switching (Discovery/03 §A)
- [ ] T091 Route `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs` `GetSettingValueAsync` (L38–66) to a settings-read SP, verifying whether `sp_config_settings_get_effective` matches the required semantics (Discovery/03 §A)
- [ ] T092 Create the `core_computers_registry` procedures (lookup by name+mac, lookup-by-mac latest, upsert, update-by-mac, get-all, update-by-id, delete-by-id) under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Startup/Services/ComputerRegistryService.cs` (L21–216) (Discovery/03 §A). Exact procedure artifacts (names confirmed against the ruleset at implementation): `sp_core_computers_registry_lookup_by_name_mac_get`, `sp_core_computers_registry_lookup_by_mac_get`, `sp_core_computers_registry_upsert`, `sp_core_computers_registry_update_by_mac`, `sp_core_computers_registry_get_all`, `sp_core_computers_registry_update`, `sp_core_computers_registry_delete`
- [ ] T093 Create the startup session/auth procedures (`fn_server_utc_now` call, credentials check, password update, computer-registered read, user row read, session expiry read) under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Startup/Services/StartupSessionRepository.cs` (L41, L135–151, L215–224, L242–249, L265–278, L301–310) (Discovery/03 §A)
- [ ] T094 Create the `config_images_locations` CRUD procedures under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Settings/Services/ImageOverrideReadService.cs` (L66–481) and `MTM_Waitlist.Settings/Services/ImageOverrideWriteService.cs` (L101–679) (Discovery/03 §A). Exact procedure artifacts: `sp_config_images_locations_get`, `sp_config_images_locations_get_by_scope`, `sp_config_images_locations_count_active_get`, `sp_config_images_locations_count_by_scope_get`, `sp_config_images_locations_get_all`, `sp_config_images_locations_get_by_public_id`, `sp_config_images_locations_recent_get`, `sp_config_images_locations_insert`, `sp_config_images_locations_update`, `sp_config_images_locations_reactivate`, `sp_config_images_locations_delete`, `sp_config_images_locations_delete_by_public_id`, `sp_config_images_locations_purge_inactive`, `sp_config_images_locations_deactivate_for_scope`, `sp_setup_work_centers_exists_get`
- [ ] T095 Create the `config_dunnage_types_visibility` procedures (visibility read, delete-all, multi-row insert) under `Database/StoredProcedures/` with rollback + master-list registration, wire `MTM_Waitlist.Shared/Services/DunnageTypeVisibilityCatalogService.cs` (L52, L85, L120–139), and route the receiving `dunnage_types` read (L139) to the receiving store's procedure (Discovery/03 §A/§B). Exact procedure artifacts: `sp_config_dunnage_types_visibility_get`, `sp_config_dunnage_types_visibility_delete_all`, `sp_config_dunnage_types_visibility_insert_many`, `sp_receiving_dunnage_types_get_all`
- [ ] T096 Create the `config_settings_values` delete procedure under `Database/StoredProcedures/` with rollback + master-list registration, and wire `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.cs` `DeleteSettingValueAsync` (L142–148) (Discovery/03 §A). Exact procedure artifact: `sp_config_settings_values_delete`
- [ ] T097 Reconcile the SP folder-name/body-name/caller-name mismatches (`sp_Dunnage_*` vs `sp_setup_dunnage_*`; `sp_config_hot_workcenters_*_for_workstation`; `sp_setup_workstations_*`) and fix the callers, per `Discovery/03` §E
- [ ] T098 (verification) Add the **inline-SQL audit** test `MTM_Waitlist.Tests/Module_Mock/InlineSqlAuditTests.cs` that scans C# source and fails on MySQL statement markers (`SELECT`/`INSERT`/`UPDATE`/`DELETE`/`CALL`) outside stored-procedure call sites (SC-013, `research.md` R13)

### Subphase 10.2 — Real-data gap fixes (FR-019)

- [ ] T099 Wire `MTM_Waitlist.Waitlist.NewRequest/Services/CoilAvailabilityService.cs` to a real source, removing the "coil assumed available" default (FR-019, `research.md` R15)
- [ ] T100 Make `MTM_Waitlist.Waitlist.NewRequest/Services/RequestTypeCatalogService.cs` (DB stored procedures) the single authoritative request-type source and move `MTM_Waitlist.Settings/Services/ImageLocationService.cs` off `Assets/Config/waitlist-request-types.json` (FR-019, Discovery/01 note 4)
- [ ] T101 (verification) Add coverage for both gap fixes in `MTM_Waitlist.Tests/Module_Waitlist/` (coil availability comes from a real source; request-type definitions have one authoritative source)

### Subphase 10.3 — Documentation accommodation (FR-028)

- [ ] T102 [P] Update `WeekendProject/ChangeLog.md` and the `WeekendProject/Module_Mock/*` docs with the Module_Mock architecture and the removal record, sweep `WeekendProject/PromptFiles/*` for stale mock-toggle/sample-catalog references, and reconcile `WeekendProject/Module_Mock/Discovery/*` so the docs describe what shipped — **0 stale references** to the retired sample/demo systems (SC-016, FR-028)

### Subphase 10.4 — Mechanical audits and final gates (SC-013, SC-015)

- [ ] T103 (verification) Add the **retired-symbol audit** test `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs` that fails if any retired type/key is present or referenced (`ISampleDataService`, `Sample*Catalog`, `Feature.InforVisualMockData`, `Feature.RecvMockData`, `MockToggleService`, `MockRouting*`, `MockMode*`, `MockConfigurationService`, `MockMasterDataService`, `UseMockData`, `MockDataExpander`, `sp_mock_*`) (SC-013, `research.md` R13)
- [ ] T104 (verification) Run the **build gate** — `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` — and confirm **0 warnings / 0 errors** (SC-015); treat `PRI175`/`PRI224` as stale-PRI/running-exe issues and `WMC9999` as a masked XAML error to be surfaced, never ignored
- [ ] T105 (verification) Run the **test gate** — `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — all green with the retired-symbol and inline-SQL audits included, then re-run with `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` set for the live MySQL subset and record any environment-gated skips explicitly (SC-015/FR-013)
- [ ] T106 (verification) Execute the end-to-end acceptance walkthrough in `specs/001-module-mock-visual-fallback/quickstart.md` §1–§8 on a running build (cache deploy → service start → fallback proof → defect proof → backup/restore drill → sixth-shape playbook → final validation table). **Note (E5)**: SC-007 and SC-008 use **30-day observation windows** — these are post-deployment measurements, not one-shot tests; record the observation start here and re-check at the 30-day mark.

---

## Phase 11: Analysis remediation (added 2026-09-09 by `/speckit.analyze`)

**Purpose**: Close the findings from the cross-artifact consistency analysis that had no task coverage. Each task names
the phase/story it belongs to; run it in that position in the phase order.

- [ ] T107 [US1] Implement the internal-store unavailable handling — a bounded retry policy (up to three attempts with delays ≈ 1 s / 2 s / 4 s) in the `MTM_Waitlist.Core` data-access seam used by the affected screens, and a per-screen `Unavailable` state carrying `Store`/`LastAttemptUtc`/`RetryCount`/`NextRetryUtc` plus an operator-facing message with a manual retry action; no sample-data substitution and no persistent banner (`data-model.md` §10, FR-021)
- [ ] T108 [US1] (verification) Add `MTM_Waitlist.Tests/Module_Mock/InternalStoreAvailabilityTests.cs` — three bounded retries then `Unavailable`; no sample rows on failure; a manual retry recovers once the store returns (`data-model.md` §10, FR-021); then run the build and full test suite gates (verification)
- [ ] T109 Create the metadata procedure `Database/Mock/StoredProcedures/sp_visual_read_shape_metadata_get/{create.sql,rollback.sql}` (per-shape mirror/stage-table and `get`/`refresh`-procedure existence plus the mirror's actual `information_schema` columns) and register it in `Database/Mock/AllSPs.sql` (`data-model.md` §12, FR-016/FR-020, constitution III; closes finding D1)
- [ ] T110 Wire the shape-catalog startup validation (`RefreshShapeCatalogProvider`, T029/T086) to call `sp_visual_read_shape_metadata_get`, and confirm the inline-SQL audit (T098) needs no exemption for metadata reads (constitution III; closes finding D1)
- [ ] T111 (verification) Add `MTM_Waitlist.Tests/Module_Mock_Service/ShapeAdditionNoDowntimeTests.cs` — adding a catalog shape changes no existing procedure signature, contract, or result type, and already-deployed clients keep serving the existing shapes (FR-020)

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
