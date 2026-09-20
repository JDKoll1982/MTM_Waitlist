# App Validation Checklist — Type/Category/Item + Unified Waitlist Card refactor (NEXT process)

> **Purpose:** Manual + automated checks for the **interactive in-app session** that lands the remaining
> Phase 3/4 (New Request Category→Item picker + uniform 2-line card), Phase 5 Frontend (defect editor
> panel + NCM Item Line 2), Phase 6.2 (real FG/WIP/Outside item), and Phase 7 (cleanup) boxes in
> `WeekendProject/PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md`.
> Companion to `CONTINUE-IMPLEMENTATION.md`.
> **Status (2026-09-09):** Prior scope validated COMPLETE — verified baseline `dotnet build` 0w/0e and
> full offline suite `608 total / 0 failed / 595 passed / 13 skipped` (13 opt-in MySQL DB-integration
> skipped offline; localhost subset live-green). The request-type/subtype admin catalog editor was
> removed (code + DB). Sections **0/0a/1/2/3 below are the [NOW] regression baseline** (re-run whenever
> code changes); sections **4/5/6/7 are the new-process behavioral checks** to run once each UI phase lands.
> The prior `App-Validation-Checklist.md` content (checks for the 55% scope) is superseded by this file;
> its DB/backend checks are preserved below where still applicable.

> ### 🔎 §4–§8 reconciliation (2026-09-20) — read before counting or running this file
>
> The file holds **45** unchecked boxes: **18** are the §0–§3 `[NOW]` pre-flight gates, and **27** are the
> §4–§8 "when the phase lands" checks. Those 27 are **not one batch**, and only part of them is delivered:
>
> | § | Boxes | State (verified 2026-09-20) |
> | --- | ---: | --- |
> | **4** New Request Category → Item picker | 9 | **Delivered** by `specs/004-unified-card-item-picker` **US1** ("Raise a request by saying what it is"). Re-run as regression once that spec's UI gates close. |
> | **5** Uniform 2-line card | 7 | **Delivered** by `specs/004` **US2** ("Every request reads the same way"). Same treatment. |
> | **6** NCM defect panel | 4 | **Not delivered, and not planned.** `spec.md` line 509 puts `pickup-ncm` **out of scope** for `specs/004`, and `IDefectTypeCatalogService` / `DefectTypeCatalogService` **do not exist in the code** — the DB half survives (`Database/Tables/30_waitlist_defect_types` + its four `sp_waitlist_defect_types_*` procedures) with no consumer. This needs its own spec, or the artifacts retired. |
> | **7** Real FG/WIP/Outside item | 2 | **Half true, half out of scope.** The hard-coded placeholders it bans (`FG-10042`, `WO-073112`, `RM-48190`) are **gone from the tree**, so the first box passes; but `pickup-fg`, `pickup-wip` and `pickup-outside-service` are also **out of scope** for `specs/004` (same line 509), so the second box is unbuilt. |
> | **8** Final acceptance (Phase 7) | 5 | **Mixed.** The build/test and role-gating boxes are re-runnable now; the "23 CSV rows" and "advance `14-57%`" boxes are stale (see below). |
>
> **Two boxes in §8 are stale as written:** *"Tests cover all 23 CSV rows"* counts a template
> (`Documents/Request-Config-Template.csv`) whose row set changed with the Item catalog, and *"Checklist
> advanced (current `14-57%-Phase2-UnifiedWaitlistCard.md`)"* is superseded — that file is the **seed** for
> `specs/004`, and live task state now lives in `specs/004-unified-card-item-picker/tasks.md`.
>
> **The 27 still count as carry-forward in `OPEN-TASKS.md` §2**, deliberately: §4 and §5 describe behaviour
> that `specs/004` has built but whose running-app gates have not yet been exercised, so ticking them here
> would claim verification the spec itself has not claimed. When those gates close, 16 of the 27 come out
> of the count and the remaining 11 (§6–§8) become a workstream of their own.

---

## 0. Pre-flight gates [NOW — re-run on every code change]

> Run from repo root. Kill stale build locks first if you hit `MSB3026`/`PRI` errors
> (`Get-Process -Name MTM_Waitlist,VBCSCompiler,MSBuild | Stop-Process -Force`).

- [ ] **Build clean:** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
      → `Build succeeded. 0 Warning(s) 0 Error(s)`.
- [ ] **Unit suite green (offline):**
      `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
      → `Failed: 0` (opt-in MySQL DB-integration tests report Skipped/Inconclusive offline — expected).
- [ ] No `WMC9999` in the build log. If present → real (masked) XAML error; do not ignore (see repo
      instructions for surfacing the true error).

### 0a. Live DB-integration pass [NOW] — verified 2026-09-09 on localhost
- [ ] Set `MTM_WAITLIST_TEST_DB_CONNECTION_STRING=Server=localhost;Database=mtm_waitlist;User ID=root;Password=root;`
      and run the opt-in DB-integration tests → they pass (not inconclusive):
      - `--filter "FullyQualifiedName~ActiveJobReadBackSpIntegrationTests"` (2 SP round-trips)
      - `--filter "FullyQualifiedName~RequestTypeCatalogServiceIntegrationTests"` (2 catalog reads)
      - `--filter "FullyQualifiedName~DefectTypesCrudIntegrationTests"` (1 CRUD round-trip)
      - `--filter "FullyQualifiedName~ConfigImagesLocationsIntegrationTests"` (8)

## 1. Database / schema / stored-procedure checks [NOW]

> MySQL on localhost. `MYSQL_PWD=root` for the CLI checks below.

### 1a. Disposition status-code config (unchanged)
- [ ] `RequestDispositionStatusCodes` = Open `{R,U,F}`, Closed `{C}`, `X` in neither. Classifier precedence
      Outside → FG → WIP → Unknown holds (10 classifier tests green).

### 1b. Read-back SP — latest active setup job per work center
- [ ] `sp_setup_active_jobs_latest_by_work_center_get` returns `work_center, work_order, part_number,
      sequence_number, subordinate_parts_json, selected_dunnage_parts_json`
      (`mysql -e "USE mtm_waitlist; CALL sp_setup_active_jobs_latest_by_work_center_get();"`).

### 1c. Avg coil weight SP
- [ ] `sp_receiving_history_average_coil_weight('MMC0001000')` → `AverageWeight = 5000`
      (`mysql -e "USE mtm_receiving_application; CALL sp_receiving_history_average_coil_weight('MMC0001000');"`).

### 1d. ~~Schema/seed consistency (DB-first Category/Item mapping)~~ — RETIRED 2026-09-20

> **All three boxes below are struck, not re-run.** They assert against
> `waitlist_request_types` / `waitlist_request_subtypes` exposing `category` + `item_id`, and those two tables
> **no longer exist**: `specs/004-unified-card-item-picker` FR-023 removed the type/subtype vocabulary
> outright — both catalog tables, their seed, both read procedures, the display-label services and the
> picture dialog. `Database/Tables/28_waitlist_request_types/` and `29_.../` now hold **`rollback.sql` only**
> (retained drops), `sp_waitlist_request_types_get` / `sp_waitlist_request_subtypes_get` are rollback-only
> too, and `RetiredSymbolAuditTests` fails the build if any of it returns. The Category/Item mapping they
> were checking is now the **Item** catalog, checked by `specs/004` US4/US5. Running these would fail a
> shipped feature.

- ~~[ ] `waitlist_request_types` / `waitlist_request_subtypes` expose `category` + `item_id` on leaf rows (24/24 subtype leaves; Forklift Assist type-leaf → Other/other).~~ — **retired** (tables removed by FR-023).
- ~~[ ] `sp_waitlist_request_types_get` / `sp_waitlist_request_subtypes_get` return the columns.~~ — **retired** (both procedures are rollback-only).
- ~~[ ] `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`, `update_table_descriptions.sql` in sync with per-artifact create.sql files.~~ — **retired as written.** The sync rule still holds and is enforced by `validate-database-schema.ps1`, but the parenthetical about `sp_mock_master_table_columns_get` and the editor SPs no longer describes the tree. Re-check the aggregates against the live `create.sql` set instead of this list.

### 1e. Defect foundation — landed 2026-09-09
- [ ] `waitlist_defect_types` table exists (`mysql -e "USE mtm_waitlist; SHOW TABLES LIKE 'waitlist_defect_types';"`).
- [ ] CRUD SPs exist: `sp_waitlist_defect_types_get_all` / `_insert` / `_update` / `_delete`; live CRUD
      round-trip passes (also covered by opt-in `DefectTypesCrudIntegrationTests`).

## 2. Resolver behavior (Phase 2.2) [NOW — via unit + live integration tests]

- [ ] `ActiveJobItemResolverService.ResolveAsync(workCenter)` returns the active job's subordinate parts
      bucketed Coil/Flatstock/Die/Component (prefix authoritative), dunnage parts, `SequenceNumber`, and die
      `Location`. MMF part mis-tagged `Component` → re-classified Flatstock. Blank WC / no job → `null`;
      malformed JSON → empty lists (no crash). Covered by `ActiveJobItemResolverServiceTests` (8).

## 3. App smoke tests — no regression [NOW]

> Launch the app (Debug). Confirms the refactor + the admin catalog-editor removal did not break existing flows.

- [ ] App starts splash → login → shell with no unhandled exceptions in the debug output.
- [ ] **New Request wizard** still walks Work Center → Job Type → Subtype → Details → Preview → Summary →
      Result and creates a request (legacy type/subtype behavior intact until Phase 3 re-lay).
- [ ] **Waitlist board** loads and renders current cards; pinning/`WaitlistRequestTitles.For()` text unchanged.
- [ ] No binding/null crashes opening a card detail page.
- ~~Mock toggles behave: `Feature.RecvMockData` ON shows sample coil weight; OFF queries DB.~~
      **Retired 2026-09-11 (T142).** The `Feature.RecvMockData` key and the sample catalogs it exercised
      were deleted by FR-014 (task T042), so this item could never pass. There is no manual demo/mock mode
      and none may be reintroduced (FR-003, constitution II). The behaviour it guarded is covered instead by
      `InternalStoreAvailabilityTests` (internal stores read live, per-screen unavailable state +
      `RetryCount`/`NextRetryUtc`, FR-001/FR-021) and by `MockMirrorRefreshWriterIntegrationTests`
      (automatic external-read fallback, FR-002/FR-006).
- [ ] Settings screen opens without the InvalidCast first-chance noise being app-breaking (benign WinUI
      binding-engine noise documented 2026-09-06); Urgency allotment editor still loads subtype names
      (via `IRequestSubtypeNameReadService` now that the editor stack is gone).

---

## 4. Checks to run once Phase 3 lands (New Request Category → Item picker) [WHEN Phase 3]

- [ ] New Request Job Type step shows the four umbrella **Categories** (Pickup / Deliver / Assist / Other) in order.
- [ ] Selecting a Category shows its **Items** in CSV `Order` (e.g. Pickup: coil, die, component, FG, NCM,
      WIP, outside-service, riser-table, dunnage, scrap, hopper).
- [ ] **Conditional visibility:** auto-populated Items (coil/flatstock/die/component/dunnage) appear only
      when the requesting job has that part (resolver-fed via `RequestJobPartAvailability`).
- [ ] **Dunnage** uses the image-card style (like Module_Setup); user always picks the dunnage part.
- [ ] **User-entry Items** (die destination, component pick, NCM defect, Other free text) accept/validate input.
- [ ] **Deliver** items keep destination = requesting work center (no user entry of destination).
- [ ] **Zero-payload Items** (Riser Table / Hopper) are pure flag selections.
- [ ] Choosing an Item produces the intended request payload (Line 2 identifier) — no stale legacy `Type`/`Subtype`.
- [ ] Data core stays green: `RequestItemPickerRulesTests` (9), `NewRequestPickerServiceTests` (4).

## 5. Checks to run once Phase 4 lands (uniform 2-line card) [WHEN Phase 4]

- [ ] Every card renders as a uniform 2-line card: **Line 1 = umbrella verb**, **Line 2 = item identifier**.
- [ ] Type-specific detail fields moved to the detail page (not the card).
- [ ] The **Other** card renders full width on Row 1 with `RowSpan = 2`.
- [ ] Card re-resolves item data at render (coil/flatstock/die/dunnage) from the work center/job
      (resolver-fed) — shows real coil number / die number + location / dunnage part.
- [ ] Badge/selector maps each Category to the correct image family (Phase 1.2 #3 `ResolveImagePath` swap
      lands here; visible images/templates updated and re-pinned).
- [ ] `WaitlistRequestTitles` Line 1/Line 2 now drive visible card text; pinning text updated and re-pinned.
- [ ] `NewRequestFlowRules.GetDefaultTypes()` ordered by Category then Item (Phase 1.2 #4).

## 6. Checks to run once Phase 5 Frontend lands (NCM defect panel) [WHEN Phase 5]

- [ ] Module_Settings has a defect-types editor (searchable list, add/remove) consuming
      `IDefectTypeCatalogService`.
- [ ] Editor is gated to Admin/Developer roles (non-admin sees no mutation UI; service `CanManage` still
      enforces on the wire).
- [ ] Pickup NCM Item Line 2 = `{PartNumber} / {Defect(User Entry)}` from the managed list.
- [ ] `DefectTypeCatalogServiceTests` (9) stay green.

## 7. Checks to run once Phase 6.2 lands (real FG/WIP/Outside item) [WHEN Phase 6.2]

- [ ] A Pickup WIP / Outside Service request resolves a **real** `{PartNumber} / {Sequence}` from
      `setup_active_jobs.sequence_number` via the resolver — **no** hard-coded `FG-10042` / `WO-073112 / RM-48190`.
- [ ] The disposition (FG/WIP/Outside/Unknown) shown matches the live Infor Visual + floor data rules.

## 8. Final acceptance (Phase 7)

- [ ] Tests cover all **23** CSV rows (catalog load, resolvers, picker, card render).
- [ ] Obsolete mock field hard-coding removed from `WaitlistViewViewModel.AddRequestFields`.
- [ ] Role gating + Infor Visual validation verified for user-entry Items (Security Review).
- [ ] Full solution build **0 warnings** + full suite green.
- [ ] Checklist advanced (current `14-57%-Phase2-UnifiedWaitlistCard.md`) and `{Completion%}` updated.
