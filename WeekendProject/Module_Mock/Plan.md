# Module_Mock — Implementation Plan

> **Status:** Draft for review (2026-09-09). Companion to `Spec.md` and `Tasks.md`.
> **Grounding:** `Discovery/01-MockLogic-Inventory.md`, `Discovery/02-InforVisual-ReadShapes.md`,
> `Discovery/03-Hardcoded-MySQL-Sql.md`.
> **Execution:** driven by `Tasks.md` (checklist-execution skill, persona per task).

---

## 1. Principles

- **P1 — Own DB is sacred.** `mtm_waitlist` is never mocked. Remove every short-circuit that skips real
  reads/writes (setup save, request lifecycle).
- **P2 — Only Visual can be unreachable.** WIP + Receiving + waitlist share one MySQL server → always live.
- **P3 — Fallback is silent + automatic.** No manual mock toggle; a read-only status indicator only.
- **P4 — Cache, not sample.** The fallback is a real-data mirror in `mtm_mock`, not hard-coded demo rows.
- **P5 — Extensible by design.** Adding a Visual read shape is a documented, repeatable 6-step playbook (§7).
- **P6 — SP-first.** No inline MySQL SQL; every statement is a stored procedure (existing or new).
- **P7 — Verify each phase.** Build 0w/0e + full suite green before advancing; DB changes validated live.

## 2. Target solution layout

- **`MTM_Waitlist.Mock`** (new class library) — reachability detector, mirror fallback read services, force-
  refresh HTTP client, status model. Referenced by `MTM_Waitlist` app + the modules that read Visual.
- **`MTM_Waitlist.Mock.Service`** (new WinUI 3 app project) — the on-host service (refresh/backup/restore/API/settings UI).
- **`mtm_mock`** (new MySQL DB) — mirror tables + SPs + seeds.
- **`Database/`** — new artifacts under `Database/Mock/` (or the established per-artifact tree) + registration
  in the applicable master lists.

## 3. Phasing

> Order matters: build the mirror + service first (additive), then switch the app to it, then remove the legacy
> system, then clean up. This keeps the app shippable at every phase boundary.

### Phase 0 — Baseline & design freeze
- Capture a green baseline (build 0w/0e + full suite) and freeze `Spec.md`.
- Confirm the 5 read shapes and the `mtm_mock` naming/DDL conventions.
- **Exit:** baseline recorded; spec approved.

### Phase 1 — `mtm_mock` DB + mirror schema + SPs (additive)
- Create the `mtm_mock` database (deployment/bootstrap script) and the 5 `visual_*_result` tables (+ `_stage`).
- Create `sp_visual_<read>_refresh` + `sp_visual_<read>_get` per shape; register in the mock master lists.
- Add baseline seed rows (so a fresh mirror is usable before any live refresh).
- **Exit:** DB artifacts authored per DB rules; deployable; live refresh + get verified on a dev MySQL.

### Phase 2 — Service app `MTM_Waitlist.Mock.Service` (additive)
- Scaffold the WinUI 3 project; single-instance, auto-start on logon, minimize-to-tray lifetime.
- Refresh engine (shape catalog from Phase 1; staging + atomic swap; skip-on-unreachable with logging).
- Backup engine (`mysqldump`, 4 DBs, per-DB schedule/retention/location, configurable).
- Restore (full drop → restore) with explicit confirmation.
- Settings UI + persistence of its own config.
- Network API + shared token (`/api/refresh`, `/api/status`, backup/restore).
- **Exit:** service runs on a host, refreshes the mirror, backs up + restores a throwaway DB, API responds.

### Phase 3 — In-app `MTM_Waitlist.Mock` lib + fallback wiring
- Implement the new Visual reachability detector (state + change event) and force-refresh HTTP client.
- Implement the 5 mirror fallback read services; route each C# caller through live→fallback:
  `SetupLookupService` (3 shapes), `WaitlistInventoryService` (1), `RequestDispositionResolver` (1).
- Add the read-only status indicator surface.
- **Exit:** with Visual simulated unreachable, the 5 reads serve from `mtm_mock`; with Visual up, live reads used.

### Phase 4 — Remove the legacy mock systems (subtractive)
- Remove family A: `Sample*` catalogs, `ISampleDataService`/`SampleDataService`, helper-server mock branches,
  `Feature.InforVisualMockData`/`RecvMockData` + `MockToggleService`, the `MockRouting*`/`MockMode*`/
  `MockConfigurationService`/`IPollScheduler`/toast stack, Settings `UseMockData` + `MockDataExpander`.
- Remove family B: `mock_*` tables, `sp_mock_*`, `MockMasterDataService` + models/validator, seeds, registry.
- **Remove the `MtmWaitlist` mock gating** (fixes setup save) and the `WaitlistRequestService` gating
  (requests always persist), plus receiving/infor mock short-circuits.
- Remove/rework affected tests.
- **Exit:** no references remain to the removed symbols/keys/tables; build 0w/0e; suite green.

### Phase 5 — SP-first conversion of hard-coded SQL
- Per `Discovery/03-Hardcoded-MySQL-Sql.md`: route each inline SQL site to the existing SP where one covers it
  (`AverageCoilWeightService`, `WorkCenterCatalogService` hot-WCs) and create new SPs for the rest (Startup
  auth/registry, Settings image overrides, Shared dunnage visibility, `ConfigSettingsValueService` delete, etc.).
- Verify SP name/body/folder consistency for the flagged mismatches (`sp_Dunnage_*`, `sp_config_hot_workcenters_*`,
  `sp_setup_workstations_*`). **Resolved 2026-09-10 (T097)** — none of the three old names exists on the server:
  the hot-workcenter pair is `…_for_computer` (callers already correct), the touch routine is
  `sp_setup_work_centers_touch` (only a log line still said `sp_setup_workstations_touch`), and the two
  `sp_setup_dunnage_*` files were dependency notes for MTM_Receiving_Application's live `sp_Dunnage_*` procedures
  and were renamed to match. See `Discovery/03` §E.1. **Live names win** — nothing another application calls is
  renamed.
- **Exit:** no inline MySQL SQL literals in C#; all calls SP-backed; tests green.

### Phase 6 — Real-data gap fixes
- Wire `CoilAvailabilityService` to a real source (currently "coil assumed available").
- Reconcile the DB request-type catalog (`RequestTypeCatalogService`) vs the legacy
  `Assets/Config/waitlist-request-types.json` consumed by `ImageLocationService` (single source of truth).
- **Exit:** the two gaps closed with tests.

### Phase 7 — Documentation accommodation (WeekendProject)
- Update `WeekendProject/ChangeLog.md`, relevant `PromptFiles/*` (mock references), and the Module_Mock docs to
  reflect the new module; retire stale mock references.
- **Exit:** WeekendProject docs consistent with the implemented module.

### Phase 8 — Validation & release readiness
- Full build 0w/0e; full offline suite green; DB-integration subset live-green.
- In-app (XamlMcp) verification of the fallback + status indicator; service-app end-to-end (refresh/backup/restore).
- **Exit:** acceptance criteria in `Spec.md` met; docs + checklist updated.

## 4. Dependencies & ordering

- Phase 1 → Phase 2 (service refresh needs the schema/SPs).
- Phase 2 → Phase 3 (force-refresh client + fallback rely on the mirror being populated).
- Phase 3 → Phase 4 (fallback must exist before deleting the old mock system).
- Phase 5 independent of 2–4 but should land before Phase 6.
- Phase 7 depends on 1–6; Phase 8 last.

## 5. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Refresh causes client lag / partial reads | staging table + **atomic `RENAME` swap** (readers see a complete snapshot) |
| Visual schema/query drift breaks the mirror | read-shape playbook (§7); refresh validates column mapping; status surfaces failures |
| Removing mock gating regresses areas that relied on sample data | add/adjust tests per phase; keep phases additive-then-subtractive |
| `mysqldump` absent on host | detect + report in settings UI; document prerequisite |
| WinUI single-process lifetime (no login / crash) | auto-start on logon + tray; log to file; document operational expectations |
| API exposure on the network | shared-token gate; bind configurable; restore behind explicit confirmation |
| Two request-type sources diverge | Phase 6 reconciliation to one source |
| Test churn from deletes | remove/rework tests in the same phase as the code they cover |

## 6. Rollout / operations

- Deploy `mtm_mock` + SPs first (no app impact).
- Deploy the service app to the MySQL host; verify refresh/backups.
- Ship the app change that adds fallback + removes legacy mock.
- Monitor the status indicator + service logs; verify the work-center card updates after a save (defect fix).

## 7. Playbook — adding a new Visual read shape (detail)

Reproduced from `specs/001-module-mock-visual-fallback/contracts/mock-service-configuration.md` §4 (the
authoritative wording). Every step produces exactly one artifact; steps 1–3b are database/design work, 4–6 are code.

1. **Capture the read.** Add `Database/InforVisual/Queues/<Module>/Queries/<Read>.sql` (parameterized, returns
   the exact column set the app consumes). Record input params + output columns.
2. **Mirror schema.** Create `mtm_mock.visual_<read>_result` (input key columns + output columns + `refreshed_utc`
   + `is_seed_content`) and its `..._stage` twin, with create/rollback per the DB naming rules; register both in
   `Database/Mock/AllTables.sql` and `Database/Mock/Bootstrap/update_table_descriptions.sql`.
3. **Procedures.** Create `sp_visual_<read>_refresh` (truncate stage → load → validate → atomic 3-name `RENAME`
   swap) and `sp_visual_<read>_get` (parameterized read of the live mirror); register both in
   `Database/Mock/AllSPs.sql`.
3b. **Population read.** Add `Database/InforVisual/Queues/Module_Mock/Populations/<read>_population.sql` — the
    set-based snapshot the service runs: it enumerates the shape's **whole driver population** and returns the
    complete result in one result set, projecting exactly the shape's input parameters then its output columns.
    Add the shape's `UNION ALL` branch to
    `Database/Mock/StoredProcedures/sp_visual_read_shape_freshness_get/create.sql`, or its cached-data age is
    silently absent from the status indicator.
4. **Service registration.** Add the shape-catalog entry (name, source script, `populationScriptRelativePath`,
   inputs, outputs, derived mirror/stage table + `get`/`refresh` procedure names, interval). **No engine code
   changes** — the engine is catalog-driven, and startup validation reports the shape if it is half-added.
5. **In-app fallback.** Add an `IVisualReadFallback<TRequest,TRow>` implementation in `MTM_Waitlist.Mock` that
   attempts the live read and, on **unreachability only**, reads `sp_visual_<read>_get`; route the originating C#
   caller through it and register it in DI. No existing shape's contract changes.
6. **Verify.** Add a seed row set, a refresh test (the swap is atomic; a failure leaves the snapshot intact), a
   fallback-parity test (live vs mirror identical shape, including the empty-live-result case), and a caller test.
   Update the status/summary surface if the shape is user-visible.

> Keep this playbook in `Spec.md` §11 / `Plan.md` §7 in sync; a `Tasks.md` item enforces documenting each new shape.
