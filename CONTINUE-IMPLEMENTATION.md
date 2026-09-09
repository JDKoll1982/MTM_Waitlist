## Role & context

You are working in the **MTM_Waitlist** WinUI 3 (.NET 10) repo at the workspace root. Continue the
Type/Category/Item + Unified Waitlist Card refactor being tracked by the **checklist-execution** skill.
First, read these to re-establish context before changing anything:

1. `WeekendProject/PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md` — the active implementation checklist
   (advanced from `14-55%` on 2026-09-09; **57% — 27/47 boxes**). Its **"NEXT IMPLEMENTATION PROCESS"**
   preamble scopes the open interactive session (Phases 3/4/5-Frontend/6.2/7).
2. `WeekendProject/Documents/Request-Config-Template.csv` — the **source of truth** row spec,
   **expanded 2026-09-08 from 18 rows to 23** (Pickup 11 / Deliver 8 / Assist 3 / Other 1) by adding
   legacy-only concepts (Wrong Coil, Wrong Flatstock, Scrap/Offal removal, Pickup Hopper no-return,
   Table Remove Parts). Each column is a concrete implementation value.
3. The design docs in `WeekendProject/PromptFiles/Plan-Design-*.md` (TypeCategoryActionRefactor,
   TypesSubtypesCatalog [current-state reference], UnifiedWaitlistCard [card-grid layout]).
4. Repo/session memory: `/memories/repo/*` and `/memories/session/*` (esp. `infor-visual-disposition.md`,
   `mtm-waitlist-db-and-test-notes.md`, `winui-waitlist-debug-notes.md`, `mcp-tooling.md`, `module-setup`,
   `waitlist`).
5. Repo instructions: `.github/instructions/*.md` and `.github/copilot-instructions.md`.

Adopt personas per checklist task (Frontend Engineer / Backend Engineer / QA / Security / etc.). Tick
`- [x]` ONLY when implemented, builds clean, and tests pass. Because the remaining boxes flip live UI
surfaces, this is an **interactive in-app session**: build/run the app and use XamlMcp to visually verify
each Phase 3/4/5-Frontend change before ticking (see the XamlMcp workflow in `.github/copilot-instructions.md`
and `/memories/repo/mcp-tooling.md`).

## Build / test commands (use these exactly)

- Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
- Test:  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
- Kill stale build locks first if MSB3026/PRI errors: kill `MTM_Waitlist`, `VBCSCompiler`, `MSBuild` processes.
- WMC9999 = masked XAML error (see repo instructions) — treat as a real but unspecified XAML problem.
- This terminal drops dotnet stdout sometimes: redirect to a log file (`*> build_log.txt 2>&1`) and read the tail.
- Do NOT create temp probe projects under `%LOCALAPPDATA%\Temp` while VS Code sln-tooling is active — they get
  auto-injected into `MTM_Waitlist.sln` (repair pattern documented in repo memory).

## Session status (2026-09-09) — checkpoint + verified baseline; next process = interactive UI phases

- **Verified baseline (2026-09-09):** `dotnet build` → `0 Warning(s) 0 Error(s)`; full offline suite →
  `total: 608, failed: 0, succeeded: 595, skipped: 13` (13 opt-in MySQL DB-integration tests skipped
  offline; the DB-integration subset passes live on localhost).
- **Admin request-type catalog editor REMOVED (2026-09-09, code + DB)** — completed this session outside
  file 14 (see §2 below). The app was re-validated as working with that editor gone.
- **File-14 checklist now `14-57%-Phase2-UnifiedWaitlistCard.md` (27/47 boxes)** — this session added the
  Phase 7 CI/CD build+suite gate tick with proof. Every verifiable non-UI unit of Phases 0–2, 3
  data/flow core, 5 backend, and 6 groundwork is implemented and validated. The remaining 20 boxes are
  **interactive UI work**: Phase 3 New Request Category→Item picker, Phase 4 uniform 2-line card, Phase 5
  Frontend defect panel + NCM Line 2, Phase 6.2 real FG/WIP/Outside item wiring, Phase 7 tests/cleanup/
  security. Per repo guidance, further non-UI code would be speculative/unbound until the picker/card UI
  exists, so this next session must build + run the app (XamlMcp) to land those boxes.

---

## 1. FULL SESSION RECORD (through 2026-09-09) — everything landed, re-verifiable on any machine

> Consolidated record of every code/SQL/test change across the Type/Category/Item + Unified Waitlist Card
> refactor. All listed work builds `0w/0e`, passes the offline suite (608 total / 595 passed / 13 skipped),
> and the MySQL-localhost DB-integration subset passes live. Infor-Visual-gated tests only turn green where
> Infor Visual (`VISUAL`/`MTMFG`) is reachable.

### Phase 0 / baseline
- Solution builds 0w/0e; full `MTM_Waitlist.Tests` suite green. Repaired 4 stale Infor DB-integration tests
  (stale `EMPLOYEE.NAME` → `FIRST_NAME`/`LAST_NAME`; dead WO fixtures → live `WO-074011`/`24733431`/seq `919`)
  and mirrored `Database/InforVisual/Queues/**` + `Database/MTMWipApp/**` SQL into `MTM_Waitlist.Tests.csproj`.

### Phase 1.1 — DB-first Category/Item mapping (live-green)
- `waitlist_request_types` / `waitlist_request_subtypes` gained `category`/`item_id` (leaf rows) — per-artifact
  create.sql ×2 + `AllTables.sql` + `update_table_descriptions.sql`; seed mapping
  (`seed_waitlist_request_catalog` + `AllSeeds.sql`; Forklift Assist type-leaf → Other/other, 24/24 subtype
  leaves mapped); get-SPs (`*_get` + `*_get_all`, both AllSPs zones) select the columns.
- Read path surfaces Category/ItemId: `RequestTypeCatalogService` → `NewRequestTypeDefinition` /
  `NewRequestSubtypeDefinition`. Catalog expanded 18 → 23 rows in `Request-Config-Template.csv` +
  `RequestItemCatalog`. Opt-in `RequestTypeCatalogServiceIntegrationTests` (2/2) live-green on localhost.

### Phase 1.2 #1/#2 — canonical mapping + additive titles
- `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs` (pure static): `Map`, `ResolveUmbrellaVerb`,
  `KnownLeafKeys` over 25 legacy keys (24 seeded subtype leaves + `PICKUP/OUTSIDE SERVICE` superset) and
  `TypeLeafToItemId` (`FORKLIFT ASSIST` → `other`). 4/4 tests green.
- `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs` gains `ResolveLine1`/`ResolveItem`
  (additive; `For()` output unchanged). 4 new tests green.
- #3 (`ResolveImagePath` → Category/Item) and #4 (`GetDefaultTypes()` order) are DEFERRED to Phase 4 / Phase 3
  picker per user scope (they flip live UI surfaces); re-scope notes are in the checklist boxes.

### Phase 2 — Item data resolvers + read-back SPs (COMPLETE, live-validated on localhost)
- `sp_setup_active_jobs_latest_by_work_center_get` now `SELECT`s `subordinate_parts_json` +
  `selected_dunnage_parts_json` (create.sql + AllSPs.sql); deployed + validated live.
- New `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql`
  (`ROUND(AVG(quantity),0)`; live `MMC0001000 → 5000`).
- `MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs` (`IActiveJobItemResolverService`,
  `SetupActiveJobSnapshot`): parses subordinate parts (prefix MMC=Coil/MMF=Flatstock/FGT=Die/else Component),
  dunnage parts, sequence number, and die home location. 8/8 `ActiveJobItemResolverServiceTests` green;
  `ActiveJobReadBackSpIntegrationTests` (2) live-green.

### Phase 3/4 data foundation — picker rules + option assembly (COMPLETE at data layer)
- `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` (`RequestJobPartKind`).
- `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` — visibility/destination rules over all 23
  canonical rows. 9/9 `RequestItemPickerRulesTests` green.
- `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs` (`INewRequestPickerService`) — ordered,
  availability-filtered Category→Item option sets. 4/4 `NewRequestPickerServiceTests` green. DI-registered.
- **UI re-layout is NOT done** — that is the Phase 3 work of the next (interactive) session.

### Phase 5 — NCM defect feature (backend COMPLETE; Frontend panel open)
- Table `waitlist_defect_types` (per-artifact + AllTables.sql + update_table_descriptions.sql) + CRUD SPs
  `sp_waitlist_defect_types_{get_all,insert,update,delete}` (per-artifact + AllSPs.sql); live CRUD validated.
- `MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs` (`IDefectTypeCatalogService`,
  `DefectTypeDefinition`, `DefectTypeMutationResult`) with Admin/Developer/Administrator `CanManage` gate;
  DI-registered. 9/9 tests green. Opt-in `DefectTypesCrudIntegrationTests` (1) live-green.
- Open (Frontend, next session): Module_Settings defect editor panel; NCM Item Line 2 wiring (needs Phase 3/4).

### Phase 6 — disposition derivation (groundwork COMPLETE; UI wiring open)
- Infor Visual research + status-code config: `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
  (`OpenStatusCodes = {R,U,F}`, `ClosedStatusCodes = {C}`, X excluded). Classifier/mapper/resolver present
  (`RequestDispositionClassifier`, `RequestDispositionMapper`, `RequestDispositionResolver`,
  `WipFloorInventoryService`, `WipFloorQuantitySnapshot`); `MySqlDatabaseTarget.MtmWipApplication` + env
  `MTM_WIP_APPLICATION_DB_CONNECTION_STRING`. Infor SQL reads under `Database/InforVisual/Queues/**` +
  `Database/MTMWipApp/**`. Full note:
  `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`.
- Open: wire a real request Item (part/sequence/disposition) into the Phase 2/3/4 resolver + picker/card
  pipeline (no mock `FG-10042`).

### Infor Visual status-code / FG-WIP-Outside knowledge (single editable config)
- `WORK_ORDER.STATUS` / `OPERATION.STATUS` (nchar1): `C`=Closed, `R`=Released, `U`=Unreleased, `X`=Cancelled,
  `F`=Firmed. Open = R/U/F; closed = C; X never FG.
- Outside Service = an `OPERATION` row with non-empty `VENDOR_ID`/`SERVICE_ID`/`SERVICE_PART_ID`
  (`RESOURCE_ID='OUTSIDE_SERVICE'`). FG vs WIP: open qty = `DESIRED_QTY − RECEIVED_QTY`; FG = on-hand at an
  FG/shipping location. MTM WIP App = MySQL `mtm_wip_application_winforms` on `172.16.1.104`, floor signals
  by location (`FG`/`DC-FG`/`FLOOR - FINISHED GOODS`, `O/S - VITS`, `NCM%`).

---

## 2. ADDITIONAL CLEANUP DONE 2026-09-09 — Admin request-type catalog editor removed (code + DB)

> User directive: remove the "Change Request Type" / Admin request-type catalog editor — "don't want
> anything related left over." There was NO wired UI; it existed at service layer + developer-editable
> routing + DI + tests + DB SPs. Fully removed and verified (build 0w/0e; suite 608 total/0 failed/595
> passed/13 skipped). Recorded in `/memories/repo/mtm-waitlist-db-and-test-notes.md`.

- **Deleted (Core):** `IRequestTypeEditorService`/`RequestTypeEditorService`,
  `IDeveloperEditableCatalogService`/`DeveloperEditableCatalogService`, `IDeveloperAccessGuard`/
  `DeveloperAccessGuard`, `RequestCatalogEditValidator`, `RequestCatalogControlValidation`, models
  `RequestTypeEditorItem`/`RequestSubtypeEditorItem`/`DeveloperEditableTable`/`DeveloperTableSourceKind`.
- **Deleted (Tests):** 5 editor test files (RequestTypeEditorServiceTests, RequestCatalogEditValidatorTests,
  RequestCatalogControlValidationTests, DeveloperEditableCatalogServiceTests, DeveloperAccessGuardTests);
  removed `RoleGating_DeniesOperatorRole_ForConfigAndCatalogEdits` from `MockModeQaTests`.
- **Added (read-only replacement):** `IRequestSubtypeNameReadService`/`RequestSubtypeNameReadService` (Core)
  → calls `sp_waitlist_request_subtypes_get`, maps/orders `subtype_name`, active-only. Rewired
  `UrgencyAllotmentEditorViewModel` (Settings) to use it for its subtype minutes list.
- **DB layer:** deleted 8 per-artifact SP folders
  `Database/StoredProcedures/sp_waitlist_request_{types,subtypes}_{insert,update,delete,get_all}` and removed
  their blocks from hand-maintained `AllSPs.sql` (1739 → 1486 lines; deterministic block removal, blank-line
  seams restored). KEPT runtime `sp_waitlist_request_types_get`/`subtypes_get` (both AllSPs zones — New
  Request wizard + the new read service use them), `sp_mock_master_table_columns_get`, and
  `sp_waitlist_defect_types_*`. `update_table_descriptions.sql` untouched (tables kept; it tracks
  tables/columns, not SPs). Verified 0 references remain under `Database/` and in code/tests.
- Earlier in the same removal scope: removed the unwired card **Edit** button (`WaitlistLineCardView`),
  `RequestActionPolicy.CanViewerEdit`, its test, and the `Waitlist_Edit_Tooltip` resw entries.

---

## 3. NEXT IMPLEMENTATION PROCESS — interactive in-app session (Phases 3/4/5F/6.2/7)

> All remaining file-14 boxes are bound by the CSV/design specs and the landed data core, but each flips a
> live UI surface, so build + run the app (Debug) and verify with XamlMcp before ticking. Work the
> `- [ ]` tasks in `14-57%-Phase2-UnifiedWaitlistCard.md` in this order (persona per box):

1. **Phase 3 — New Request Category→Item picker (Frontend/Full Stack).**
   - Re-lay the New Request Job-Type step to Category (Pickup/Deliver/Assist/Other) then Item, matching CSV
     Order; bind to `INewRequestPickerService` option sets + `IRequestItemCatalogService`.
   - Conditional visibility from `IActiveJobItemResolverService` snapshot mapped onto
     `RequestJobPartAvailability` (coil/flatstock/die/component/dunnage only when the job has that part;
     FG/WIP/Outside/NCM only with an active job; Riser Table/Hopper always-available manual; Scrap/Other
     always visible).
   - **Composition-root bridge required:** `MTM_Waitlist.Waitlist.NewRequest` references Settings but NOT
     Setup — add an app-level service (or contract move) to reach `IActiveJobItemResolverService` (Setup)
     before wiring visibility.
   - Dunnage image-card selection (Module_Setup card style; user always picks the dunnage part).
   - User-entry Item paths (die destination, component pick, NCM defect, Other free text) validated per CSV;
     Deliver destination = requesting work center (no user entry); zero-payload Riser Table/Hopper flags.
   - Produces the intended request payload (Line 2 identifier) — no stale legacy Type/Subtype.

2. **Phase 4 — Uniform 2-line card (Line1 umbrella / Line2 item).**
   - Single uniform 2-line card control; type-specific detail fields move to the detail page; full-width
     "Other" card (Row 1, RowSpan 2); render-time item data resolution (coil/flatstock/die/dunnage) via the
     resolver; badge/selector → correct image family (this lands Phase 1.2 #3 `ResolveImagePath` swap +
     `WaitlistRequestTitles` Line1/Line2 flip + Phase 1.2 #4 `GetDefaultTypes()` Category-then-Item order).

3. **Phase 5 Frontend — Module_Settings defect editor panel + NCM Item Line 2.**
   - Searchable defect-types list + add/remove in Module_Settings, consuming
     `IDefectTypeCatalogService`; gate the panel to Admin/Developer roles (surface the service's `CanManage`);
     localize strings.
   - Wire NCM Item Line 2 = `{PartNumber} / {Defect(User Entry)}` from the managed list (needs the picker).

4. **Phase 6.2 finish — real FG/WIP/Outside item.** Wire `{PartNumber} / {Sequence}` from
   `setup_active_jobs.sequence_number` (resolver, Phase 2.2) + the derived disposition; no mock `FG-10042`.

5. **Phase 7 — validation & cleanup.** Tests across all 23 CSV rows (catalog/resolvers/picker/card render);
   remove obsolete mock field hard-coding in `WaitlistViewViewModel.AddRequestFields` replaced by resolvers;
   Security Review of role gating + Infor Visual validation for user-entry Items; final 0-warning build +
   full green suite.

**Phase boundary (2026-09-09):** every verifiable non-UI unit is implemented, built (0w/0e), offline-tested
(608 total / 595 passed / 13 skipped), and the DB-integration subset is live-green on localhost. The open
boxes require building and visually verifying inside the running WinUI app (XamlMcp-assisted). When a UI
phase lands, tick its boxes with proof notes, re-run build + full suite, and bump the `{Completion%}` in the
checklist filename.

---

## 4. HOW TO TEST ON A PC WITH INFOR VISUAL ACCESS

Run in order on the machine that can reach Infor Visual (`VISUAL`/`MTMFG`) and your MySQL dev/test DB.

### Step 0 — Preflight
- [ ] Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → `0w/0e`.
- [ ] Kill stale locks if `MSB3026`/`PRI` errors: `Get-Process -Name MTM_Waitlist,VBCSCompiler,MSBuild | Stop-Process -Force`.
- [ ] Confirm `appsettings.json` `InforVisualDatabaseOptions` points at your Infor SQL Server (`Server=VISUAL`,
      `Database=MTMFG`) and `StartupDatabaseOptions.ConnectionString` at your MySQL. Never hardcode/echo secrets.
- [ ] Set env (adjust to your server): `$env:MTM_WAITLIST_TEST_DB_CONNECTION_STRING='Server=<host>;Database=mtm_waitlist;User ID=...;Password=...;'`
      plus the other targets if used (`MTM_WIP_APPLICATION_DB_CONNECTION_STRING`,
      `MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING`).
- [ ] Apply SQL artifacts to the DB under test (re-runnable): `Database/Tables/AllTables.sql` +
      `Database/StoredProcedures/AllSPs.sql` (+ `Database/Tables/AllSeeds.sql` if re-seeding the catalog).
      NOTE: `AllSPs.sql` no longer contains the removed request-type/subtype editor SPs.

### Step 1 — Full suite (Infor + MySQL reachable)
- [ ] `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` with the env vars set.
      Expect the Infor-Visual-gated integration tests to turn GREEN (disposition derivation + Infor Visual
      lookups hitting `VISUAL`/`MTMFG`, and the WIP-floor MySQL tests). Locally these are gated off.

### Step 2 — Explicitly validate Phase 6 disposition against live Infor Visual
- [ ] Run the classifier/config unit tests: filter `RequestDisposition` → all green (pure, no DB).
- [ ] Confirm the derivation SQL returns expected rows for a real WO/part:
      `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` — status codes match
      config (`R`/`U`/`F` open, `C` closed, `X` excluded); Outside = OPERATION row with non-empty
      VENDOR_ID/SERVICE_ID/SERVICE_PART_ID; FG = on-hand at FG/shipping; WIP = open qty.

### Step 3 — MySQL DB-integration + defect CRUD
- [ ] Run the opt-in MySQL DB-integration tests → expect the localhost subset to pass
      (`ActiveJobReadBackSp`, `RequestTypeCatalogService`, `DefectTypesCrud`, `ConfigImagesLocations`).
- [ ] Live defect CRUD round-trip via `sp_waitlist_defect_types_*` (also covered by
      `DefectTypesCrudIntegrationTests`).

### Step 4 — Interactive in-app checks (this is the next process)
- [ ] Run the app in Debug; use XamlMcp to walk the New Request wizard and the Waitlist board.
- [ ] Execute the Phase 3/4 picker+card checks in `WeekendProject/PromptFiles/App-Validation-Checklist.md`
      (sections 4b/5/6b/7) plus the regression smoke tests (sections 0a/1/2/3).

### Step 5 — Report / finish
- [ ] Leave the repo building clean and the new/affected tests green.
- [ ] Tick boxes in `14-57%-Phase2-UnifiedWaitlistCard.md` with proof notes; bump the `{Completion%}` in the
      filename; rewrite the top "NEXT IMPLEMENTATION PROCESS" preamble as scope closes.
- [ ] Record which Infor-gated tests passed against live Visual.

> **Autonomy note (2026-09-09):** The previous sessions implemented every verifiable non-UI unit ahead of
> strict checklist order (documented in this record). The next session MUST be interactive — run the app +
> XamlMcp — to build and visually verify the Phase 3/4/5F/6.2 UI before ticking. Do not add further
> speculative non-UI code until the picker/card UI exists (repo guidance).
