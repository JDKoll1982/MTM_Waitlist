# App Validation Checklist — Type/Category/Item + Unified Waitlist Card refactor

> **Purpose:** Manual + automated checks to confirm the refactor edits work end-to-end.
> Companion to `WeekendProject/PromptFiles/14-55%-Phase2-UnifiedWaitlistCard.md` and
> `CONTINUE-IMPLEMENTATION.md`.
> **Status:** Phase 0/1.1/1.2 #1-#2/2 + disposition groundwork + Phase 3 data/flow core (picker rules + picker-option
> assembly) + **Phase 5 NCM defect backend** landed (26/47 = 55%). Phase 3 UI picker, Phase 4 card,
> Phase 5 Frontend panel, Phase 6.2, Phase 7 still **in progress**.
> Sections marked **[NOW]** are executable today; sections marked **[WHEN Phase N lands]** are the
> behavioral checks to run once that phase's UI is built.

---

## 0. Pre-flight gates [NOW]

> Run from repo root. Kill stale build locks first if you hit `MSB3026`/`PRI` errors
> (`Get-Process -Name MTM_Waitlist,VBCSCompiler,MSBuild | Stop-Process -Force`).

- [ ] **Build clean:** `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
      → `Build succeeded. 0 Warning(s) 0 Error(s)`.
- [ ] **Unit suite green (offline):**
      `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
      → `Failed: 0` (opt-in MySQL DB-integration tests report Skipped/Inconclusive offline — that is expected).
- [ ] **Live DB-integration pass (localhost):**
      set `MTM_WAITLIST_TEST_DB_CONNECTION_STRING=Server=localhost;Database=mtm_waitlist;User ID=root;Password=root;`
      then run the filtered DB tests below → they pass (not inconclusive).
      - `--filter "FullyQualifiedName~ActiveJobReadBackSpIntegrationTests"` (2 SP round-trips)
      - `--filter "FullyQualifiedName~ConfigImagesLocationsIntegrationTests"` (8)
      - `--filter "FullyQualifiedName~ActiveJobItemResolverServiceTests"` (8)
- [ ] No `WMC9999` in the build log. If present → real (masked) XAML error; do not ignore.

---

### 0a. Live DB-integration pass [NOW] — verified 2026-09-09
- [ ] Run the opt-in DB-integration tests live on localhost (env `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`):
      `--filter "FullyQualifiedName~IntegrationTests|FullyQualifiedName~ActiveJobReadBackSp|FullyQualifiedName~DefectTypesCrud|FullyQualifiedName~RequestTypeCatalogService"` → **14/14 pass**.
- [ ] Confirms SPs + catalog read path + defect CRUD are live-green end to end.

## 1. Database / schema / stored-procedure checks [NOW]

> MySQL on localhost (MySQL 9.6). Databases: `mtm_waitlist`, `mtm_receiving_application`,
> `mtm_wip_application_winforms`. `MYSQL_PWD=root` for the CLI checks below.

### 1a. Infor status-code config (Phase 6.0/6.1)
- [ ] `RequestDispositionStatusCodes` (Settings) = Open `{R,U,F}`, Closed `{C}`, `X` in neither.
- [ ] Classifier precedence Outside → FG → WIP → Unknown holds (10 classifier tests green).

### 1b. Read-back SP — latest active setup job per work center
- [ ] `sp_setup_active_jobs_latest_by_work_center_get` returns the **two JSON columns**:
      `mysql -e "USE mtm_waitlist; CALL sp_setup_active_jobs_latest_by_work_center_get();"`
      → verify output includes `work_center, work_order, part_number, sequence_number,
      subordinate_parts_json, selected_dunnage_parts_json`.
- [ ] A seeded work-center row round-trips both JSON payloads (see `ActiveJobReadBackSpIntegrationTests`).

### 1c. Avg coil weight SP
- [ ] `sp_receiving_history_average_coil_weight('MMC0001000')`
      (`mysql -e "USE mtm_receiving_application; CALL sp_receiving_history_average_coil_weight('MMC0001000');"`)
      → `AverageWeight = 5000` (seed 4800+5000+5200 /3).
- [ ] Empty / unknown part returns one row with a rounded value or no rows (no throw).

### 1d. Schema/seed consistency (Phase 1.1 DB-first mapping) — LIVE-VALIDATED 2026-09-09
- [ ] `waitlist_request_types` / `waitlist_request_subtypes` expose `category` + `item_id` on leaf rows
      (`mysql -e "USE mtm_waitlist; SELECT id,request_type,category,item_id FROM waitlist_request_types;"`).
- [ ] Every subtype leaf carries a canonical `category` + `item_id` (24/24); Forklift Assist type-leaf → Other/other.
- [ ] `sp_waitlist_request_types_get(_all)` / `sp_waitlist_request_subtypes_get(_all)` return the columns
      (`mysql -e "USE mtm_waitlist; CALL sp_waitlist_request_subtypes_get_all();"`).
- [ ] `RequestTypeCatalogServiceIntegrationTests` (2/2) pass live against `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`.
- [ ] `AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`, `update_table_descriptions.sql` are in sync with the
      per-artifact `create.sql` files (no drift).
- [ ] Shipped `waitlist-request-types.json` is intentionally NOT extended (DB is the carrier).

---

## 2. Resolver behavior (Phase 2.2) [NOW — via unit + live integration tests]

- [ ] `ActiveJobItemResolverService.ResolveAsync(workCenter)` returns the active job's:
      - subordinate parts split into Coil / Flatstock / Die / Component buckets
        (prefix MMC=Coil, MMF=Flatstock, FGT=Die authoritative, else stored-or-Component);
      - dunnage parts from `selected_dunnage_parts_json`;
      - `SequenceNumber`;
      - die `Location` (Home Location) via `PrimaryDie`/`DieLocation`.
- [ ] MMF part mis-tagged `Component` is re-classified as Flatstock (prefix wins) — 1 unit test covers this.
- [ ] Blank work center / no active job → returns `null` (no throw).
- [ ] Malformed JSON payload → empty lists, not a crash.
- [ ] **Note:** resolver is NOT yet surfaced in any UI (Phase 3/4 wiring is next). You cannot click to see it
      yet; verify via the unit + live integration tests above.

---

## 3. App smoke tests — confirm no regression [NOW]

> Launch the app (Debug). These confirm the refactor did not break the existing waitlist / New Request flows.

- [ ] App starts to the splash → login → shell with no unhandled exceptions in the debug output.
- [ ] **New Request wizard** still walks: Work Center → Job Type → Subtype → Details → Preview → Summary → Result
      and creates a request (existing legacy type/subtype behavior intact).
- [ ] **Waitlist board** loads and renders the current cards; pinning/`WaitlistRequestTitles.For()` text unchanged.
- [ ] No binding/null crashes when opening a card detail page.
- [ ] Mock toggles behave: `Feature.RecvMockData` ON shows sample coil weight; OFF queries DB.

---

## 4. Checks to run once Phase 3 lands (New Request Category → Item picker) [WHEN Phase 3]

### 4a. Picker-rules data core — landed 2026-09-09 [NOW — unit-tested]
- [ ] `RequestItemPickerRulesTests` (9) green: `RequestItemPickerRules` + `RequestJobPartAvailability` encode the CSV
      visibility/destination rules over all 23 canonical rows.
- [ ] **Deliver destination rule:** every `Deliver` item → destination = requesting work center (no user entry).
- [ ] **Conditional visibility:** coil/flatstock/die/component/dunnage items show only when the requesting job has
      that part; FG/WIP/Outside/NCM show only with an active job; Riser Table/Hopper are always-available manual;
      Scrap/Other always visible.
- [ ] The running UI is NOT yet wired to these rules (picker re-layout pending). Verify via the unit tests until then.
- [ ] `NewRequestPickerServiceTests` (4) green: `NewRequestPickerService` assembles ordered Category→Item options and
      filters them by `RequestJobPartAvailability` — the data surface the Phase 3 Job-Type→Item pages will bind to.

### 4b. Picker behavior to check once wired [WHEN Phase 3]

- [ ] New Request Job Type step shows the four umbrella **Categories** (Pickup / Deliver / Assist / Other) in order.
- [ ] Selecting a Category shows its **Items** in CSV `Order` (e.g. Pickup: coil, die, component, FG, NCM, WIP,
      outside-service, riser-table, dunnage, scrap, hopper).
- [ ] **Conditional visibility:** auto-populated Items (coil/flatstock/die/component/dunnage) appear only when the
      selected work center's active job actually has that part (resolver-fed).
- [ ] **Dunnage** uses the image-card style (like Module_Setup); user always picks the dunnage part.
- [ ] **User-entry Items** (die destination, component pick, NCM defect, Other free text) accept/validate input.
- [ ] **Deliver** items keep destination = requesting work center (no user entry of destination).
- [ ] **Zero-payload Items** (Riser Table / Hopper) are pure flag selections.
- [ ] Choosing an Item produces the intended request payload (Line2 identifier) — no stale legacy `Type`/`Subtype`.

---

## 5. Checks to run once Phase 4 lands (uniform 2-line card) [WHEN Phase 4]

- [ ] Every card renders as a uniform 2-line card: **Line 1 = umbrella verb**, **Line 2 = item identifier**.
- [ ] Type-specific detail fields moved to the detail page (not the card).
- [ ] The **Other** card renders full width on Row 1 with `RowSpan = 2`.
- [ ] Card re-resolves item data at render (coil/flatstock/die/dunnage) from the work center/job (resolver-fed) —
      e.g. shows real coil number / die number + location / dunnage part.
- [ ] Badge/selector maps each Category to the correct image family (Phase 1.2 #3 ResolveImagePath swap).

---

## 6. Checks to run once Phase 5 lands (NCM defect feature) [WHEN Phase 5]

### 6a. Defect DB foundation — landed 2026-09-09 [NOW — DB + live integration test]
- [ ] `waitlist_defect_types` table exists in `mtm_waitlist` (`mysql -e "USE mtm_waitlist; SHOW TABLES LIKE 'waitlist_defect_types';"`).
- [ ] CRUD SPs exist: `sp_waitlist_defect_types_get_all` / `_insert` / `_update` / `_delete`.
- [ ] Live CRUD round-trip: insert → list shows it → update name/order → list shows update → delete → gone
      (also covered by opt-in `DefectTypesCrudIntegrationTests`, 1 live green on localhost).
- [ ] `AllTables.sql`, `AllSPs.sql`, `update_table_descriptions.sql` are in sync with per-artifact `create.sql`.

### 6a2. Defect backend service + role gate — landed 2026-09-09 [NOW — unit-tested]
- [ ] `DefectTypeCatalogServiceTests` (9) green: `DefectTypeCatalogService` reads via the SPs and gates
      Add/Update/Delete behind `CanManage` (Admin/Developer/Administrator); non-admin roles are denied and
      no SP call is made.
- [ ] The service is DI-registered and ready for the Module_Settings editor panel to consume.

### 6b. Settings-panel + NCM Item wiring [WHEN Phase 5 Frontend]

- [ ] Module_Settings has a defect-types editor (searchable list, add/remove).
- [ ] Editor is gated to Admin/Developer roles.
- [ ] Pickup NCM Item Line 2 = `{PartNumber} / {Defect(User Entry)}` from the managed list.

---

## 7. Checks to run once Phase 6.2 finish lands (real FG/WIP/Outside item) [WHEN Phase 6.2]

- [ ] A Pickup WIP / Outside Service request resolves a **real** `{PartNumber} / {Sequence}` from
      `setup_active_jobs.sequence_number` via the resolver — **no** hard-coded `FG-10042` / `WO-073112 / RM-48190`.
- [ ] The disposition (FG/WIP/Outside/Unknown) shown matches the live Infor Visual + floor data rules.

---

## 8. Final acceptance (Phase 7) [WHEN Phase 7]

- [ ] Tests cover all **23** CSV rows (catalog load, resolvers, picker, card render).
- [ ] Obsolete mock field hard-coding removed from `WaitlistViewViewModel.AddRequestFields`.
- [ ] Role gating + Infor Visual validation verified for user-entry Items.
- [ ] Full solution build **0 warnings** + full suite green.
- [ ] Checklist file advanced (current `14-55%-Phase2-UnifiedWaitlistCard.md`) and `{Completion%}` updated.
