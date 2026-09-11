# Module_Mock — Tasks (implementation checklist)

> **Purpose:** Execution checklist for the Module_Mock workstream (Infor Visual read mirror + service app +
> legacy-mock removal + SP-first cleanup). Source of truth: `Spec.md` (Spec) + `Plan.md` (Plan) +
> `Discovery/01..03`.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented,
> builds clean, and tests pass; append a ` — verified <date>: <proof>` note.
> **Phases:** 0 baseline → 1 `mtm_mock` schema → 2 service app → 3 in-app lib → 4 removal → 5 SP-first →
> 6 real-data gaps → 7 docs → 8 validation.
> **First task:** Phase 0 `CI/CD: capture green baseline` | **Persona: DevOps Engineer**.

---

## Phase 0 — Baseline & design freeze

### Subphase 0.1: Baseline
- [ ] **CI/CD: capture green baseline** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → 0 warnings / 0 errors and full `MTM_Waitlist.Tests` suite pass (record counts). (Ref: Plan §3 Phase 0) | **Persona: DevOps Engineer**
- [ ] **Testing: snapshot DB-integration subset** status (opt-in MySQL tests) against localhost for later comparison. (Ref: Plan §8) | **Persona: QA Engineer**

### Subphase 0.2: Freeze design
- [ ] **Tech Lead: approve `Spec.md`** (scope: only Infor Visual mocked; `mtm_waitlist`/WIP/Receiving always live; 5 read shapes; automatic fallback) and record the decision. (Ref: Spec §1–§4) | **Persona: Tech Lead**
- [ ] **Tech Lead: freeze `mtm_mock` naming + DDL conventions** (`visual_<read>_result`, `sp_visual_<read>_{get,refresh}`, staging + atomic swap). (Ref: Spec §6) | **Persona: Tech Lead**

**GATE: baseline green + Spec approved before Phase 1.**

Next task: **CI/CD: capture green baseline** | **Persona: DevOps Engineer**

---

## Phase 1 — `mtm_mock` database + mirror schema + stored procedures (additive)

### Subphase 1.1: Database + mirror tables
- [ ] **Database Migration: create `mtm_mock` database** via a re-runnable bootstrap script (created on deploy if absent), on the same MySQL server as the other three DBs. (Ref: Spec §6) | **Persona: Database Engineer**
- [ ] **Database Table: create `visual_work_order_lookup_result`** (+ `_stage` twin) with inputs (NormalizedWorkOrder) + outputs (PartNumber, Description, WorkCenter) + `refreshed_utc`. (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Table: create `visual_operation_sequences_result`** (+ `_stage`) with inputs (NormalizedWorkOrder, PartNumber) + outputs (SequenceNumber, Description). (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Table: create `visual_subordinate_parts_result`** (+ `_stage`) with inputs (NormalizedWorkOrder, PartNumber, SequenceNumber) + outputs (Category, PartNumber, Description, Location, User8, OnHandQuantity). (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Table: create `visual_inventory_locations_result`** (+ `_stage`) with inputs (PartNumber) + outputs (PartNumber, Location, OnHandQuantity). (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Table: create `visual_disposition_input_result`** (+ `_stage`) with inputs (WorkOrder, PartNumber) + outputs (WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity, HasOutsideVendorOperation). (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Migration: register all five tables** in the `mtm_mock` master list + table-descriptions file, with create.sql + rollback.sql per artifact. (Ref: Spec §6.1) | **Persona: Database Engineer**

### Subphase 1.2: Stored procedures
- [ ] **Database Migration: create `sp_visual_<read>_refresh` ×5** that load the `_stage` twin and atomically `RENAME`-swap it into the live table (no reader lock/partial reads). (Ref: Spec §6; Plan §5) | **Persona: Database Engineer**
- [ ] **Database Migration: create `sp_visual_<read>_get` ×5** — parameterized reads returning the mirror's column shape identical to the live read. (Ref: Spec §6.1) | **Persona: Database Engineer**
- [ ] **Database Migration: register the ten SPs** in the `mtm_mock` SP master list with create/rollback. (Ref: Spec §6) | **Persona: Database Engineer**

### Subphase 1.3: Baseline seed
- [ ] **Database Migration: add baseline seed rows** for the five mirror tables (so a fresh mirror is usable before any live refresh); register in the seed master list. (Ref: Spec §6; Plan §3 Phase 1) | **Persona: Database Engineer**

**GATE: `mtm_mock` deploys cleanly; refresh + get verified live on a dev MySQL; a fresh machine has usable seed data before Phase 2.**

Next task: **Database Migration: create `mtm_mock` database** | **Persona: Database Engineer**

---

## Phase 2 — Service app `MTM_Waitlist.Mock.Service` (WinUI 3, additive)

### Subphase 2.1: Scaffold + lifetime
- [ ] **Configuration: create the `MTM_Waitlist.Mock.Service` WinUI 3 project** and add it to `MTM_Waitlist.sln` (builds 0w/0e). (Ref: Plan §2) | **Persona: DevOps Engineer**
- [ ] **Service Layer: implement single-instance lifetime** — auto-start on logon, minimize to system tray, keep the background engine alive; safe shutdown on exit. (Ref: Spec §8; round-4 decision) | **Persona: Full Stack Engineer**

### Subphase 2.2: Refresh engine
- [ ] **Service Layer: implement the refresh shape catalog** (name, source script, params, stage/target table, interval) covering the 5 shapes; loaded from service config. (Ref: Spec §7; Plan §7) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement scheduled warm refresh** Visual → `mtm_mock` via `sp_visual_<read>_refresh`, skipping unreachable Visual with logged status. (Ref: Spec §8.1) | **Persona: Backend Engineer**
- [ ] **Service Layer: add refresh status tracking** (last-run, per-shape success/failure, duration) surfaced via the API + settings UI. (Ref: Spec §8.4) | **Persona: Backend Engineer**

### Subphase 2.3: Backup engine
- [ ] **Service Layer: implement periodic backups of all four MySQL DBs** via `mysqldump`, configurable **per-DB** (enable, schedule, retention, location). (Ref: Spec §8.2) | **Persona: Backend Engineer**
- [ ] **Service Layer: detect + report `mysqldump` availability** and fail gracefully when absent. (Ref: Plan §5) | **Persona: Backend Engineer**

### Subphase 2.4: Restore
- [ ] **Service Layer: implement emergency restore** — full drop → restore of a selected DB from a chosen backup, guarded by an explicit confirmation. (Ref: Spec §8.3) | **Persona: Backend Engineer**

### Subphase 2.5: Settings UI
- [ ] **Settings Page: build the service settings UI** — refresh interval, per-DB backup schedule/retention/location, API port/token, Visual connection, and last-run status. (Ref: Spec §8.4) | **Persona: Frontend Engineer**
- [ ] **Configuration: persist service settings** to a durable local store and apply them without restart where feasible. (Ref: Spec §8.4) | **Persona: Backend Engineer**

### Subphase 2.6: API + token
- [ ] **Service Layer: host a network-bound HTTP API** (`POST /api/refresh`, `GET /api/status`, backup/restore endpoints) with a shared-token gate. (Ref: Spec §8.5) | **Persona: Backend Engineer**
- [ ] **Security Review: validate API hardening** — token enforced on every mutating endpoint, bind address configurable, restore requires confirmation, no secrets logged. (Ref: Spec §8.5) | **Persona: Security Engineer**

**GATE: service refreshes the mirror, backs up + restores a throwaway DB, and its API responds (token-gated) before Phase 3.**

Next task: **Configuration: create the `MTM_Waitlist.Mock.Service` WinUI 3 project** | **Persona: DevOps Engineer**

---

## Phase 3 — In-app `MTM_Waitlist.Mock` library + fallback wiring

### Subphase 3.1: Library + reachability detector
- [ ] **Configuration: create the `MTM_Waitlist.Mock` class library** and reference it from the app + Visual-reading modules; register in DI. (Ref: Plan §2) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the new Visual reachability detector** (independent probe; state + change event; no dependency on the legacy `MockRoutingService`). (Ref: Spec §7.1) | **Persona: Backend Engineer**

### Subphase 3.2: Fallback read services (5 shapes)
- [ ] **Service Layer: implement the mirror fallback read service for work-order lookup** (live Visual → `sp_visual_work_order_lookup_result_get`) and route `SetupLookupService.LookupWorkOrderFromBackendAsync` (`SetupLookupService.cs:123`). (Ref: Discovery/02) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the mirror fallback for operation sequences** and route `SetupLookupService.GetSequencesFromBackendAsync` (`SetupLookupService.cs:158`). (Ref: Discovery/02) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the mirror fallback for subordinate parts** and route `SetupLookupService.GetSubordinatePartsFromBackendAsync` (`SetupLookupService.cs:186`). (Ref: Discovery/02) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the mirror fallback for inventory locations** and route `WaitlistInventoryService.GetInventoryLocationsFromBackendAsync` (`WaitlistInventoryService.cs:71`). (Ref: Discovery/02) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the mirror fallback for disposition input** and route `RequestDispositionResolver.GetDispositionInputAsync` (`RequestDispositionResolver.cs:45`). (Ref: Discovery/02) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement the force-refresh HTTP client** that calls the service app's `POST /api/refresh` with the shared token. (Ref: Spec §7.4) | **Persona: Backend Engineer**

### Subphase 3.3: Status indicator
- [ ] **Settings Card: add the read-only status indicator** in the app shell when the Visual fallback is active ("Infor Visual unreachable — using cached data"); no toggle/interaction. (Ref: Spec §7.3) | **Persona: Frontend Engineer**

### Subphase 3.4: Verification
- [ ] **Testing: add fallback-parity + reachability tests** for all five shapes (live vs mirror identical shape; unreachable → mirror served). (Ref: Spec §12) | **Persona: QA Engineer**

**GATE: with Visual simulated unreachable the five reads serve from `mtm_mock`; with Visual up live reads are used; status indicator reflects state.**

Next task: **Configuration: create the `MTM_Waitlist.Mock` class library** | **Persona: Backend Engineer**

---

## Phase 4 — Remove the legacy mock systems (subtractive)

### Subphase 4.1: Family A — sample catalogs + contracts
- [ ] **Service Layer: delete the sample catalogs + contract** (`SampleDataService`/`ISampleDataService`, `SampleWaitlistRequestCatalog`, `SampleInventoryLocationCatalog`, `SampleAverageCoilWeightCatalog`, `SampleJobCoilCatalog`, `SetupDataCatalog`, `SampleOrder` if unused elsewhere). (Ref: Discovery/01 §1) | **Persona: Backend Engineer**
- [ ] **Configuration: remove the mock toggle keys + service** (`Feature.InforVisualMockData`/`RecvMockData`, `MockToggleService`, `MockSettingKeys`) and every raw-key read. (Ref: Discovery/01 §2) | **Persona: Backend Engineer**

### Subphase 4.2: Family A — routing/toggles/settings UI
- [ ] **Service Layer: remove the mock routing/auto-force stack** (`MockRoutingService`, `MockRoutingCoordinator`, `MockConfigurationService`, `MockRoutingRefreshService`, `MockRoutingMonitorService`, `MockModePollingHost`, `MockFallbackDebouncer`, `MockModeChangeDetector`, `MockModeSummaryProvider`, `DispatcherPollScheduler`, `MockModeToastCoordinator` + contracts/models + DI). (Ref: Discovery/01 §3) | **Persona: Backend Engineer**
- [ ] **Service Layer: strip helper-server mock branches** (`SqlHelperServer` `mockAction` path; `MySqlHelperServer` `IsMockDataEnabledAsync` + mock overloads). (Ref: Discovery/01 §4) | **Persona: Backend Engineer**
- [ ] **Settings Page: remove the Settings "Mock Data" toggle** (`MockDataExpander`, `SettingsViewModel.UseMockData`/`OnUseMockDataChanged`/`IsMockDataPanelVisible`, `IMockToggleService` injection). (Ref: Discovery/01 §2) | **Persona: Frontend Engineer**

### Subphase 4.3: Family B — DB-backed mock master tables
- [ ] **Database Migration: drop the `mock_*` tables + `mock_master_tables_registry`** (`mock_parts/_work_orders/_work_centers/_locations/_inventory_locations/_requesters/_request_types`) with rollback, and remove them from the master lists/descriptions. (Ref: Discovery/01 §5) | **Persona: Database Engineer**
- [ ] **Database Migration: drop the `sp_mock_*` procedures** (+ `sp_mock_master_table_columns_get`) with rollback and remove from the SP master list. (Ref: Discovery/01 §5) | **Persona: Database Engineer**
- [ ] **Service Layer: delete `MockMasterDataService` + models/validator** (`IMockMasterDataService`, `MockMasterRowEditValidator`, `MockMasterTableDefinition`, `MockMasterColumnDefinition`) and their DI. (Ref: Discovery/01 §5) | **Persona: Backend Engineer**
- [ ] **Database Migration: remove the mock seed artifacts** (`seed_mock_master_default` + registration in `AllSeeds.sql`). (Ref: Discovery/01 §5) | **Persona: Database Engineer**

### Subphase 4.4: Remove `mtm_waitlist` mock gating (fixes the defect)
- [ ] **Service Layer: remove the `MtmWaitlist` mock short-circuit in setup save** so `SetupPersistenceService.SaveAsync` always writes the real DB (delete `SaveMockAsync`). (Ref: Discovery/01 §4; Step: defect fix) | **Persona: Backend Engineer**
- [ ] **Service Layer: remove the `WaitlistRequestService` mock gating** (`IsMockDataEnabled()` + all skip-read/skip-persist branches) so request reads/writes always persist. (Ref: Discovery/01 §4) | **Persona: Backend Engineer**
- [ ] **Service Layer: remove receiving/infor mock short-circuits** in `SetupLookupService`, `DunnageWorkflowService`, `WaitlistInventoryService`, `AverageCoilWeightService`, `CoilAvailabilityService`. (Ref: Discovery/01 §4) | **Persona: Backend Engineer**

### Subphase 4.5: Tests rework
- [ ] **Testing: remove/rework all mock-behavior tests** to match the new architecture (routing/toggle/sample tests deleted; callers re-pointed). (Ref: Discovery/01 §8) | **Persona: QA Engineer**

**GATE: no references remain to removed symbols/keys/tables; build 0w/0e; full suite green; a mock-off setup save updates the work-center card.**

Next task: **Service Layer: delete the sample catalogs + contract** | **Persona: Backend Engineer**

---

## Phase 5 — SP-first conversion of hard-coded MySQL SQL

> Reference: `Discovery/03-Hardcoded-MySQL-Sql.md` (file:line + existing-SP mapping).

### Subphase 5.1: Route to existing SPs
- [ ] **Service Layer: route `AverageCoilWeightService` to `sp_receiving_history_average_coil_weight`** (replace inline SQL at `AverageCoilWeightService.cs:14`, exec `:45`). (Ref: Discovery/03 §B) | **Persona: Backend Engineer**
- [ ] **Service Layer: route `WorkCenterCatalogService` hot-WC read/delete/upsert + available-WC read to their SPs** (`WorkCenterCatalogService.cs:91`, `:272–279`, `:284–333`, `:342`). (Ref: Discovery/03 §A) | **Persona: Backend Engineer**
- [ ] **Service Layer: route `ImageLocationService.LoadWorkCenterDetailsAsync` to `sp_setup_work_centers_get_all`** (`ImageLocationService.cs:650–674`; verify the IN-filter semantics). (Ref: Discovery/03 §A) | **Persona: Backend Engineer**
- [ ] **Service Layer: route `ConfigSettingsValueService.GetSettingValueAsync` to a settings-read SP** (`ConfigSettingsValueService.cs:38–66`; verify `sp_config_settings_get_effective` semantics). (Ref: Discovery/03 §A) | **Persona: Backend Engineer**

### Subphase 5.2: Create new SPs + wire callers
- [ ] **Database Migration: create SPs for `core_computers_registry`** (lookup/upsert/update/delete/get-all) and wire `ComputerRegistryService` (`ComputerRegistryService.cs:21–216`). (Ref: Discovery/03 §A) | **Persona: Database Engineer**
- [ ] **Database Migration: create SPs for startup session/auth reads + password update** and wire `StartupSessionRepository` (`StartupSessionRepository.cs:41,135–151,215–224,242–249,265–278,301–310`). (Ref: Discovery/03 §A) | **Persona: Database Engineer**
- [ ] **Database Migration: create SPs for `config_images_locations` CRUD** and wire `ImageOverrideReadService` (`:66–481`) + `ImageOverrideWriteService` (`:101–679`). (Ref: Discovery/03 §A) | **Persona: Database Engineer**
- [ ] **Database Migration: create SPs for `config_dunnage_types_visibility`** (+ receiving `dunnage_types` read) and wire `DunnageTypeVisibilityCatalogService` (`:52,85,120–139`). (Ref: Discovery/03 §A/§B) | **Persona: Database Engineer**
- [ ] **Database Migration: create a delete SP for `config_settings_values`** and wire `ConfigSettingsValueService.DeleteSettingValueAsync` (`:142–148`). (Ref: Discovery/03 §A) | **Persona: Database Engineer**

### Subphase 5.3: Name/body consistency
- [x] **Database Migration: reconcile SP folder/body/caller name mismatches** (`sp_Dunnage_*` vs `sp_setup_dunnage_*`; `sp_config_hot_workcenters_*_for_workstation`; `sp_setup_workstations_*`) and fix callers. (Ref: Discovery/03 §E) | **Persona: Database Engineer** — done 2026-09-10 (spec task T097): verified against `information_schema.ROUTINES` that `sp_setup_dunnage_*`, `sp_config_hot_workcenters_*_for_workstation` and `sp_setup_workstations_touch` exist nowhere; renamed the two dependency notes to the live `sp_Dunnage_*` names + updated their two `LoadAsync` call sites, fixed a stale log line naming `sp_setup_workstations_touch`, and corrected Discovery/03 §E. Live procedures untouched.

### Subphase 5.4: Enforcement
- [ ] **Testing: assert no inline MySQL SQL remains** (a lint/grep-based test or CI check) so SP-first is enforced. (Ref: Spec §10) | **Persona: QA Engineer**

**GATE: no inline MySQL SQL literals in C#; all statements SP-backed; tests green.**

Next task: **Service Layer: route `AverageCoilWeightService` to `sp_receiving_history_average_coil_weight`** | **Persona: Backend Engineer**

---

## Phase 6 — Real-data gap fixes

- [ ] **Service Layer: wire `CoilAvailabilityService` to a real source** (replace "coil assumed available" when not mocked). (Ref: Discovery/01 §4) | **Persona: Backend Engineer**
- [ ] **Service Layer: reconcile request-type sources** — make `RequestTypeCatalogService` (DB SPs) the single source and update `ImageLocationService` off `Assets/Config/waitlist-request-types.json` (`ImageLocationService.cs:772`). (Ref: Discovery/01 note 4) | **Persona: Backend Engineer**
- [ ] **Testing: add coverage for the two gap fixes.** (Ref: Spec §12) | **Persona: QA Engineer**

**GATE: both real-data gaps closed with tests; no legacy JSON/sample fallback remains.**

Next task: **Service Layer: wire `CoilAvailabilityService` to a real source** | **Persona: Backend Engineer**

---

## Phase 7 — Documentation accommodation (WeekendProject)

- [ ] **Tech Lead: document the "add a new Visual read shape" playbook** in `Spec.md` §11 + `Plan.md` §7 and cross-link from `Tasks.md`. (Ref: Spec §11; Plan §7 — user-required) | **Persona: Tech Lead**
- [ ] **Tech Lead: update `WeekendProject/ChangeLog.md`** with the Module_Mock architecture + removal record. (Ref: Plan §3 Phase 7) | **Persona: Tech Lead**
- [ ] **Tech Lead: sweep `WeekendProject/PromptFiles/*`** for stale mock references (e.g. mock-toggle/sample-catalog mentions) and correct them to the new model. (Ref: Plan §3 Phase 7) | **Persona: Tech Lead**
- [ ] **Tech Lead: reconcile `Discovery/*` + `Module_Mock-Planning-Progress.md`** so the final docs match what shipped. (Ref: Plan §3 Phase 7) | **Persona: Tech Lead**

**GATE: WeekendProject docs consistent with the implemented module; the extensibility playbook is published.**

Next task: **Tech Lead: document the "add a new Visual read shape" playbook** | **Persona: Tech Lead**

---

## Phase 8 — Validation & release readiness

- [ ] **Testing: full offline suite green** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`) and DB-integration subset live-green. (Ref: Plan §8) | **Persona: QA Engineer**
- [ ] **CI/CD: full solution build 0 warnings / 0 errors.** (Ref: Plan §8) | **Persona: DevOps Engineer**
- [ ] **Testing: in-app (XamlMcp) verification** of the Visual fallback + read-only status indicator, and that a setup save updates the work-center card. (Ref: Spec §12) | **Persona: QA Engineer**
- [ ] **Testing: service-app end-to-end** — scheduled refresh, per-DB backups, a throwaway restore, and the token-gated API. (Ref: Spec §8) | **Persona: QA Engineer**
- [ ] **Security Review: final pass** on the service API token/bind + restore confirmation + no secret leakage. (Ref: Spec §8.5) | **Persona: Security Engineer**

**GATE: acceptance criteria in `Spec.md` met; docs + this checklist updated.**

Next task: **Testing: full offline suite green** | **Persona: QA Engineer**
