## Role & context

You are working in the **MTM_Waitlist** WinUI 3 (.NET 10) repo at the workspace root. Continue the
Type/Category/Item + Unified Waitlist Card refactor being tracked by the **checklist-execution** skill.
First, read these to re-establish context before changing anything:

1. `WeekendProject/PromptFiles/14-55%-Phase2-UnifiedWaitlistCard.md` — the active implementation checklist
   (was `14-53%`; **Phase 5 defect role-gate service added on 2026-09-09 → 55% — 26/47 boxes**).
2. `WeekendProject/Documents/Request-Config-Template.csv` — the **source of truth** row spec, **expanded
   2026-09-08 from 18 rows to 23** (Pickup 11 / Deliver 8 / Assist 3 / Other 1) by adding legacy-only
   concepts (Wrong Coil, Wrong Flatstock, Scrap/Offal removal, Pickup Hopper no-return, Table Remove Parts).
   Each column is a concrete implementation value.
3. The design docs in `WeekendProject/PromptFiles/Plan-Design-*.md` (TypeCategoryActionRefactor,
   TypesSubtypesCatalog [current-state reference], UnifiedWaitlistCard [card-grid layout]).
4. Repo/session memory: `/memories/repo/*` and `/memories/session/*` (esp. `infor-visual-disposition.md`,
   `checklist14-session-2026-09-08.md`, `mcp-tooling.md`, `module-setup`, `waitlist`).
5. Repo instructions: `.github/instructions/*.md` and `.github/copilot-instructions.md`.

Adopt personas per checklist task (Database Engineer / Backend Engineer / etc.). Tick `- [x]` ONLY when
implemented, builds clean, and tests pass.

## Build / test commands (use these exactly)

- Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
- Test:  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
- Kill stale build locks first if MSB3026/PRI errors: kill `MTM_Waitlist`, `VBCSCompiler`, `MSBuild` processes.
- WMC9999 = masked XAML error (see repo instructions) — treat as a real but unspecified XAML problem.
- This terminal drops dotnet stdout sometimes: redirect to a log file (`*> build_log.txt 2>&1`) and read the tail.

## Session status (2026-09-09) — Phase 0, 1.1 (live-green), 1.2 #1/#2, 6.0/6.1/6.2 groundwork, **Phase 2** DONE, **Phase 3 data/flow core (picker rules + picker-option assembly)** DONE, and **Phase 5 NCM defect backend (table + CRUD SPs + role-gated service + tests) DONE** — build 0w/0e; full suite 608 green / 0 failed / 17 opt-in skipped offline (DB-integration incl. defect/SP/catalog tests pass live on localhost). Checklist now `14-55%-Phase2-UnifiedWaitlistCard.md` (26/47 boxes).

## ✅ FULL SESSION RECORD — EVERYTHING DONE (through 2026-09-09)

> Consolidated record of every code/SQL/test change made across this whole session, so you can review,
> rebuild, and re-verify on any machine (including one with Infor Visual access). All of the below builds
> `0w/0e`, passes the offline suite (`608 passed / 0 failed / 17 opt-in skipped`), and the MySQL-localhost
> DB-integration tests pass (`14/14`). Infor-Visual-gated tests are listed in the test section below and
> only turn green when Infor Visual (`VISUAL`/`MTMFG`) is reachable.

### 1) Infor Visual status-code + disposition derivation (resolved against live data — Objective A)
- **No C#/SQL changed by me this session for this** (was resolved 2026-09-08 and already committed). Reference:
  `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`.
- Config (single editable source): `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
  → `OpenStatusCodes = { R, U, F }`, `ClosedStatusCodes = { C }`, `X` in neither.
- Pure classifier/mapper/resolver already present: `RequestDispositionClassifier`, `RequestDispositionMapper`,
  `RequestDispositionResolver`; `Core/Models/WipFloorQuantitySnapshot.cs`; `Core/Services/WipFloorInventoryService.cs`;
  `MySqlDatabaseTarget.MtmWipApplication` + env `MTM_WIP_APPLICATION_DB_CONNECTION_STRING`.
- Infor SQL read scripts (already committed): `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql`
  (+ `GetSubordinateParts.sql`, etc.); WIP floor script `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql`.

### 2) Phase 1.1 — DB-first Category/Item mapping (CLOSED, live-green)
- Table `category`/`item_id` columns + seed mapping already committed (2026-09-08). **This session (2026-09-09):**
  re-seeded + validated live on localhost and ticked the JSON Schema box. Shipped `waitlist-request-types.json`
  intentionally NOT extended.
- New opt-in test: `MTM_Waitlist.Tests/Module_Waitlist/Services/RequestTypeCatalogServiceIntegrationTests.cs` (2).
- Read path that surfaces Category/ItemId: `RequestTypeCatalogService` → `NewRequestTypeDefinition`/`NewRequestSubtypeDefinition`.

### 3) Phase 2 — Item data resolvers + read-back SPs (COMPLETE)
- **SP change:** `Database/StoredProcedures/sp_setup_active_jobs_latest_by_work_center_get/create.sql` + `AllSPs.sql`
  now also `SELECT aj.subordinate_parts_json, aj.selected_dunnage_parts_json`.
- **New SP:** `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql`
  (`ROUND(AVG(quantity),0)` WHERE part_id, ignores NULL/<=0). Live: `MMC0001000 → 5000`.
- **Resolver (Setup module):** `MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs` +
  `Contracts/Services/SetupContracts.cs` (`IActiveJobItemResolverService`) + `Models/SetupModels.cs`
  (`SetupActiveJobSnapshot`). Returns coil/flatstock/die/component, dunnage, sequence, die location.
  DI-registered in `MTM_Waitlist.Setup/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs`.
- Tests: `ActiveJobItemResolverServiceTests` (8), `ActiveJobReadBackSpIntegrationTests` (2).

### 4) Phase 3/4 data foundation — picker rules + option assembly (COMPLETE at data/UI-prep layer)
- `MTM_Waitlist.Settings/Models/RequestJobPartAvailability.cs` (`RequestJobPartKind` + availability record).
- `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` — CSV visibility/destination rules over the 23 items.
- `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs` (`INewRequestPickerService`) — ordered,
  availability-filtered Category→Item option sets (what the Phase 3 pages bind to).
- DI-registered at the composition root (`Services/DependencyInjection/ServiceRegistrationExtensions.cs`).
- Tests: `RequestItemPickerRulesTests` (9), `NewRequestPickerServiceTests` (4).
- **UI re-layout (Job Type → Category/Item) is NOT done** — needs in-app work.

### 5) Phase 5 — NCM defect feature (backend COMPLETE; Frontend panel open)
- **Table:** `Database/Tables/30_waitlist_defect_types/{create,rollback}.sql` + `AllTables.sql` +
  `Bootstrap/update_table_descriptions.sql`.
- **CRUD SPs:** `sp_waitlist_defect_types_{get_all,insert,update,delete}` per-artifact (create.sql+rollback) +
  `AllSPs.sql`.
- **Backend service:** `MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs` (`IDefectTypeCatalogService`,
  `DefectTypeDefinition`, `DefectTypeMutationResult`) — SP-only read/write with an Admin/Developer/Administrator
  role gate (`CanManage`) on mutations. DI-registered.
- Tests: `DefectTypesCrudIntegrationTests` (1, live), `DefectTypeCatalogServiceTests` (9).
- **Open (Frontend):** Module_Settings defect editor panel (surfaces the role gate); NCM Item Line 2 wiring.

### 6) Docs / checklist (this session)
- Checklist advanced **14 → 26/47 boxes**; current file `WeekendProject/PromptFiles/14-55%-Phase2-UnifiedWaitlistCard.md`
  (renamed from `14-30%` → `14-45%` → `14-47%` → `14-53%` → `14-55%`).
- `WeekendProject/PromptFiles/App-Validation-Checklist.md` — in-app validation checklist (sections 0a–8).
- This `CONTINUE-IMPLEMENTATION.md` — consolidated record + test steps.

### 7) Verification state (as of 2026-09-09)
- `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → **0 Warning(s) 0 Error(s)**.
- Offline suite: `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` → **608 passed /
  0 failed / 17 opt-in skipped**.
- MySQL-localhost DB-integration (env `MTM_WAITLIST_TEST_DB_CONNECTION_STRING=Server=localhost;Database=mtm_waitlist;User ID=root;Password=root;`)
  → **14/14 pass**: ActiveJobReadBackSp (2), RequestTypeCatalogService (2), DefectTypesCrud (1), ConfigImagesLocations (8), +1.

## 🧪 HOW TO TEST ON A PC WITH INFOR VISUAL ACCESS

Run these in order on the machine that can reach Infor Visual (`VISUAL`/`MTMFG`) and your MySQL dev/test DB.

### Step 0 — Preflight
- [ ] Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → `0w/0e`.
- [ ] Kill stale locks if `MSB3026`/`PRI` errors: `Get-Process -Name MTM_Waitlist,VBCSCompiler,MSBuild | Stop-Process -Force`.
- [ ] Confirm `appsettings.json` `InforVisualDatabaseOptions` points at your Infor SQL Server (`Server=VISUAL`, `Database=MTMFG`,
      user/password as configured) and your `StartupDatabaseOptions.ConnectionString` at your MySQL. Never hardcode/echo secrets.
- [ ] Set env (adjust to your server): `$env:MTM_WAITLIST_TEST_DB_CONNECTION_STRING='Server=<host>;Database=mtm_waitlist;User ID=...;Password=...;'`
      plus the other targets if you use them (`MTM_WIP_APPLICATION_DB_CONNECTION_STRING`, `MTM_RECEIVING_APPLICATION_DB_CONNECTION_STRING`).
- [ ] Apply the SQL artifacts to the DB being tested (re-runnable):
      `Database/Tables/30_waitlist_defect_types/create.sql`,
      `Database/StoredProcedures/sp_waitlist_defect_types_{get_all,insert,update,delete}/create.sql`,
      `Database/StoredProcedures/sp_setup_active_jobs_latest_by_work_center_get/create.sql`,
      `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql`
      (or run `Database/Tables/AllTables.sql` + `Database/StoredProcedures/AllSPs.sql`).

### Step 1 — Full suite (Infor + MySQL reachable)
- [ ] `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` with the env vars set.
      Expect the **Infor-Visual-gated integration tests to turn GREEN** (not skipped/inconclusive): the disposition
      derivation + Infor Visual lookup tests that hit `VISUAL`/`MTMFG` (e.g. `InforVisualSqlQueryService`, disposition
      integration against live `WO-074011`/`24733431`/seq `919`) and the WIP-floor MySQL tests. Locally these are gated off.

### Step 2 — Explicitly validate Phase 6 disposition against live Infor Visual
- [ ] Run the classifier/config unit tests: filter `RequestDisposition` → all green (pure, no DB).
- [ ] Run the Infor-disposition live path and confirm the derivation SQL returns expected rows for a real WO/part:
      `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` (returns WorkOrderStatus /
      OpenWorkOrderQuantity = DESIRED−RECEIVED / FinishedGoodsQuantity / HasOutsideVendorOperation). Confirm the status
      codes match config: `R`/`U`/`F` open, `C` closed, `X` excluded.
- [ ] Confirm Outside Service = OPERATION row with non-empty `VENDOR_ID`/`SERVICE_ID`/`SERVICE_PART_ID`; FG = on-hand at an
      FG/shipping location; WIP = open qty. Reference: `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`.

### Step 3 — MySQL DB-integration + defect CRUD (this session's DB/backend)
- [ ] Run the opt-in MySQL DB-integration tests (as in verification state §7) → expect **14/14**.
- [ ] Live defect CRUD round-trip via the SPs (insert → list → update → delete) using
      `sp_waitlist_defect_types_*` (also covered by `DefectTypesCrudIntegrationTests`).

### Step 4 — In-app checks
- [ ] Follow `WeekendProject/PromptFiles/App-Validation-Checklist.md` sections **0a / 1 / 2 / 3** (DB, resolver,
      no-regression smoke tests) which are executable now.
- [ ] Sections 4–8 mark the still-open Phase 3/4 picker+card UI, Phase 5 settings panel, Phase 6.2, and Phase 7 —
      those need the running WinUI app to verify and are NOT yet wired (see Remaining).

### Step 5 — Report / finish
- [ ] Leave the repo building clean and the new/affected tests green.
- [ ] Record which Infor-gated tests passed against live Visual (they will NOT pass on a machine without Infor).
- [ ] Tick any newly proven boxes in `14-55%-Phase2-UnifiedWaitlistCard.md` with proof notes + bump the `{Completion%}`.

> **Autonomy note (2026-09-09):** Phase 3/4 (New Request picker UI + uniform card) and Phase 5's Frontend
> panel cannot be visually verified in this environment, so verifiable non-UI Phase 5 DB/backend work
> was implemented ahead of the strict checklist order (see section F). Phase 3/4 and Phase 5 Frontend remain open.
>
> **Phase boundary (2026-09-09):** Every verifiable non-UI unit of Phases 1.1, 1.2, 2, 3 data/flow core, and
> 5 backend is now implemented, built (0w/0e), offline-tested (608 green), and **live DB-integration verified
> (14/14 on localhost)**. The remaining open checklist boxes (Phase 3/4 picker+card UI, Phase 5 Frontend panel,
> Phase 6.2 item wiring, Phase 7 render/cleanup tests) all require building and visually verifying inside the
> running WinUI app. Adding further non-UI code now would be speculative/unbound, so the next session must be
> interactive (run the app + XamlMcp) to land those boxes.

### F) DONE 2026-09-09 — Phase 5 NCM defect backend (Database/QA/Backend, done ahead of UI phases)

- `waitlist_defect_types` table (per-artifact + AllTables.sql + update_table_descriptions.sql) + CRUD SPs
  `sp_waitlist_defect_types_{get_all,insert,update,delete}` (per-artifact + AllSPs.sql); live CRUD validated.
- Opt-in `DefectTypesCrudIntegrationTests.cs` (1) passes live.
- Backend editor service `MTM_Waitlist.Settings/Services/DefectTypeCatalogService.cs` (+ `IDefectTypeCatalogService`,
  `DefectTypeDefinition`, `DefectTypeMutationResult`): SP-only read/write with an Admin/Developer/Administrator
  role gate (`CanManage`) enforced on Add/Update/Delete; DI-registered. 9/9 `DefectTypeCatalogServiceTests` green.
- Open (Frontend, next): Module_Settings defect editor panel (surfaces the role gate), and wiring NCM Item
  Line 2 from the managed list once the Phase 3/4 picker exists.

### E) DONE 2026-09-09 — Phase 3/4 data foundation: canonical picker rules

- New pure `MTM_Waitlist.Settings/Services/RequestItemPickerRules.cs` + `Models/RequestJobPartAvailability.cs`
  (`RequestJobPartKind`). Encodes CSV visibility + destination rules over the canonical 23-row
  `RequestItemCatalog`: auto job-part Items (coil/flatstock/die/component/dunnage) show only when the
  requesting job has that part; Assist table place/remove need any subordinate; FG/WIP/Outside/NCM need an
  active job; Riser Table/Hopper are always-available manual equipment; Scrap/Other always visible; every
  Deliver Item's destination = requesting work center. DB-free/DB-agnostic so a Phase 3 caller maps an
  `IActiveJobItemResolverService` snapshot onto `RequestJobPartAvailability`.
- 9/9 `RequestItemPickerRulesTests` green (all 23 rows rule-covered).
- Picker option assembly: `MTM_Waitlist.Settings/Services/NewRequestPickerService.cs` (+ `INewRequestPickerService`)
  composes the catalog + rules into ordered, availability-filtered Category→Item option sets
  (`GetCategoriesInOrder`/`GetItems`/`GetVisibleItems`/`GetAllVisible`); DI-registered. 4/4
  `NewRequestPickerServiceTests` green. This is the exact data surface the Phase 3 Job-Type→Item pages bind to.
- Phase 3 **UI** re-layout (Job Type step → Category/Item) is NOT yet done — boxes remain open.

### D) DONE 2026-09-09 — Phase 1.1 JSON Schema box closed (DB-first mapping live-green)

- Re-seeded the catalog on localhost: `waitlist_request_types`/`waitlist_request_subtypes` `category`/`item_id`
  populated (Forklift Assist type-leaf → Other/other; 24/24 subtype leaves mapped per TypeCategoryActionRefactor).
- `sp_waitlist_request_types_get(_all)`/`sp_waitlist_request_subtypes_get(_all)` return the columns.
- New opt-in `MTM_Waitlist.Tests/Module_Waitlist/Services/RequestTypeCatalogServiceIntegrationTests.cs`
  (2/2) reads the live catalog through `RequestTypeCatalogService` and asserts every subtype leaf carries
  category+item_id and Forklift Assist → Other/other. Live-green on localhost; inconclusive offline.

### C) DONE 2026-09-09 — Phase 2 Item data resolvers + read-back SPs (validated live on localhost)

- **Subphase 2.1 (Database Engineer/QA):**
  - `sp_setup_active_jobs_latest_by_work_center_get` (create.sql + AllSPs.sql) now also `SELECT`s
    `subordinate_parts_json` + `selected_dunnage_parts_json`; deployed + validated live on localhost.
  - New `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql`
    (`ROUND(AVG(quantity),0)` WHERE part_id, ignores NULL/<=0 skids); live-validated `MMC0001000 → 5000`.
  - Opt-in DB-integration tests `MTM_Waitlist.Tests/Module_Setup/ActiveJobReadBackSpIntegrationTests.cs`
    (2/2 live green on localhost `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`, inconclusive offline).
- **Subphase 2.2 (Backend Engineer):** resolver reuses Setup models/module per user —
  `MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs` + `IActiveJobItemResolverService` +
  `SetupActiveJobSnapshot` model. `ResolveAsync(workCenter)` reads the SP (mtm_waitlist), deserializes
  `subordinate_parts_json → SetupSubordinatePart[]` (prefix-canonical MMC=Coil / MMF=Flatstock / FGT=Die /
  else Component-or-stored) and `selected_dunnage_parts_json → SetupDunnagePart[]`; snapshot exposes
  `Coils/Flatstock/Dies/Components/PrimaryDie/DieLocation/SequenceNumber`. DI-registered. 8/8
  `ActiveJobItemResolverServiceTests` green. Die location = die row `Location` from the same JSON.

## Session status (2026-09-08) — build 0w/0e, full suite 582 green / 0 failed / 8 skipped (MySQL-gated)

**Objective A (Infor Visual parse) — DONE.** See
`Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md` + repo memory.
**Objective B — Phase 0, Phase 6.0/6.1/6.2 (groundwork), Phase 1.1 (catalog expansion + DB-first mapping),
and Phase 1.2 #1 (mapper) + #2 (WaitlistRequestTitles accessors) DONE.** Checklist now
`14-45%-Phase2-UnifiedWaitlistCard.md` (21/47 boxes).

### A) DONE — Infor Visual status codes + FG/WIP/Outside derivation (resolved against live data)

- Connection path is env-var driven then `appsettings.json` → `InforVisualDatabaseOptions`
  (dev default `VISUAL`/`MTMFG`); see `InforVisualSqlQueryService` + `SetupSqlScriptStore`. Do not
  hardcode/log credentials. Infor Visual SQL client: `sqlcmd` (`$env:SQLCMDPASSWORD`, do not echo).
- **`WORK_ORDER.STATUS` / `OPERATION.STATUS` (nchar(1), same set, from live `ENUM_CODES` + counts):**
  `C`=Closed (68,278 WO / 244,112 OP), `R`=Released (680 / 2,840), `U`=Unreleased (42,138 / 13,046),
  `X`=Cancelled (1,404 / 9,030), `F`=Firmed (defined, 0 rows).
- **Config updated:** `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs` →
  `OpenStatusCodes = { R, U, F }`, `ClosedStatusCodes = { C }`, `X` deliberately in neither (never FG).
- **Outside Service** = an `OPERATION` row with non-empty `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID`
  (≈26.9k rows, `RESOURCE_ID='OUTSIDE_SERVICE'`) — NOT a status code.
- **FG vs WIP:** open qty = `WORK_ORDER.DESIRED_QTY − RECEIVED_QTY`; Infor `MT_WIP_INVENTORY` is EMPTY
  (unused). `WORK_ORDER.PROD_ORDER_TYPE` all NULL → not a driver. FG = Closed + on-hand at an
  FG/shipping location.
- **MTM WIP Application (real floor WIP/FG source):** MySQL server `172.16.1.104`, database
  **`mtm_wip_application_winforms`** (there is no plain `mtm_wip_application`), app dev root account
  (provided out-of-band; do not commit). `inv_inventory` (PartID/Location/Operation/Quantity/ItemType)
  keyed by part; FG areas marked in `md_locations` (`FG`, `DC-FG`, `FLOOR - FINISHED GOODS`); floor
  signals by **location** (`O/S - VITS` = outside service staging, `NCM%` = non-conforming), ItemType
  stays `WIP` even there.

### B) DONE this session (2026-09-08) — build 0w/0e; full suite 582 passed / 0 failed / 8 skipped

- **Phase 0 (2/2):** solution builds `0w/0e`; full `MTM_Waitlist.Tests` suite green. Repaired 4
  stale Infor DB-integration tests (stale `EMPLOYEE.NAME` → `FIRST_NAME`/`LAST_NAME`; dead WO fixtures →
  live `WO-074011`/`24733431`/seq `919`) and mirrored `Database/InforVisual/Queues/**` + `Database/MTMWipApp/**`
  SQL content into `MTM_Waitlist.Tests.csproj`.
- **Phase 6.0 (4/4):** `RequestDisposition` enum + `RequestDispositionClassifier` (pure over
  `DispositionInput`) + `RequestDispositionStatusCodes` config + 8 classifier tests green.
- **Phase 6.1 (3/3):** `GetDispositionInput.sql`
  (`Database/InforVisual/Queues/Module_Waitlist/Queries/`) + `GetWipFloorQuantities.sql`
  (`Database/MTMWipApp/Queues/Module_Waitlist/Queues/`) drafted and validated live; status codes
  confirmed in config; classifier tests updated to real codes (10/10).
- **Phase 6.2 groundwork:** MySQL data path added —
  `MySqlDatabaseTarget.MtmWipApplication` + env `MTM_WIP_APPLICATION_DB_CONNECTION_STRING` in
  `MySqlHelperServer`; `Core/Models/WipFloorQuantitySnapshot.cs`;
  `Core/Services/WipFloorInventoryService.cs` (+ internal `WaitlistWipMySqlScriptStore`);
  `Settings/Services/RequestDispositionMapper.cs` (`MapInforRow`/`BuildDispositionInput`);
  `Settings/Services/RequestDispositionResolver.cs` (runs Infor + floor → `DispositionInput` →
  `RequestDisposition`); DI-registered. New tests: 5 + 11 (+ 10 classifier) green.
- **Phase 1.1 — catalog expanded 18 → 23 rows (user direction 2026-09-08):** added legacy-only rows to
  `Request-Config-Template.csv` + `MTM_Waitlist.Settings/Models/RequestItemCatalog.cs` — `deliver-wrong-coil`,
  `deliver-wrong-flatstock` (Deliver-correct + Pickup-wrong, user-entry), `pickup-scrap` (legacy Scrap/Empty),
  `pickup-hopper` (legacy Pickup Hopper do-not-return), `assist-table-remove` (legacy Table Remove Parts).
  Category counts now Pickup 11 / Deliver 8 / Assist 3 / Other 1. `RequestItemCatalogTests` +
  `RequestItemCatalogServiceTests` updated → green.
- **Phase 1.1 — DB-first Category/Item mapping (user re-scope: route through DB, NOT JSON):**
  - DDL: `waitlist_request_types` + `waitlist_request_subtypes` gained `category VARCHAR(16) NULL` +
    `item_id VARCHAR(64) NULL` (leaf rows only) — per-artifact create.sql ×2 + `Database/Tables/AllTables.sql`
    + `Database/Bootstrap/update_table_descriptions.sql` (mandatory maintenance file).
  - Seed mapping: `Database/Seeds/seed_waitlist_request_catalog/create.sql` + `AllSeeds.sql` populate
    category/item_id on every leaf row (Forklift Assist type-leaf → Other/other; all 24 subtype leaves →
    canonical per TypeCategoryActionRefactor; e.g. Coil Bring → Deliver/deliver-coil, Pickup Coil →
    Pickup/pickup-coil, Scrap Empty → Pickup/pickup-scrap, Pull Die variants → Pickup/pickup-die).
  - Read-back SPs select the new columns: `sp_waitlist_request_types_get` + `_get_all`,
    `sp_waitlist_request_subtypes_get` + `_get_all` (per-artifact create.sql ×4 + `StoredProcedures/AllSPs.sql`,
    both duplicate zones).
  - C# read path surfaces them: `NewRequestTypeDefinition` + `NewRequestSubtypeDefinition` carry
    `Category` + `ItemId`; `RequestTypeCatalogService` reads them; `RequestTypeCatalogServiceTests` updated → green.
  - **CAVEAT:** no live MySQL this session (env absent) → schema/seed/SP authored consistently per DB rules
    but **NOT live re-seeded/validated**. Re-run seed + SPs + DB-integration tests on a machine with
    `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` before declaring the DB path live-green.
- **Phase 1.2 #1 (ticked) — legacy→canonical re-map backbone:** NEW pure static
  `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs` — `Map(requestType, subtype) → RequestItemDefinition?`
  via `LegacyToItemId` (25 keys: 24 seeded subtype leaves + legacy superset `PICKUP\0x1OUTSIDE SERVICE`) and
  `TypeLeafToItemId` (`FORKLIFT ASSIST` → `other`); `ResolveUmbrellaVerb(...)` returns canonical umbrella verb
  or legacy-type fallback; `KnownLeafKeys`. Stable GUIDs untouched (`RequestTypeInventory`/`RequestSubtypeInventory`
  unchanged). 4/4 `RequestItemLegacyMapperTests` green.
  NOTE: pull-die variants map to `pickup-die` → **Pickup** umbrella (not Assist).
- **Phase 1.2 #2 (ticked) — `WaitlistRequestTitles` additive accessors (user chose "additive now, flip UI in
  Phase 4"):** `MTM_Waitlist.Waitlist.View/Models/WaitlistRequestTitles.cs` gains `ResolveLine1(type, subtype)`
  (canonical umbrella verb via `RequestItemLegacyMapper.ResolveUmbrellaVerb`, legacy-type fallback) and
  `ResolveItem(type, subtype)` (canonical `RequestItemDefinition` via mapper, or null). Existing `For()`
  output unchanged → visible card text + pinning tests untouched. 4 new `WaitlistRequestTitlesTests` → green.

### B) Remaining open tasks (next)

Work `- [ ]` tasks in `14-55%-Phase2-UnifiedWaitlistCard.md` in order:
- **Phase 1.1** JSON Schema box — **CLOSED 2026-09-09** (see status section D): DB-first mapping re-seeded +
  validated live on localhost; shipped JSON intentionally untouched.
- **Phase 1.2** #3 (`ResolveImagePath` ad-hoc → explicit Category/Item) and #4 (`GetDefaultTypes` order by
  Category then Item) — DEFERRED to Phase 3/4 per user additive scope (both flip live UI surfaces that Phase
  3/4 rebuild). Re-scope notes are in the two boxes; visible swap + test re-pin lands with Phase 4 badge/selector,
  picker ordering with Phase 3.
- **Phase 2 — DONE 2026-09-09** (see status section C above). Resolver ready to feed Phase 3 picker + Phase 4 card.
- **Phase 3** New Request Category → Item picker — NEXT. Data/flow core DONE 2026-09-09 (`RequestItemPickerRules`
  + `RequestJobPartAvailability`, 9 tests). Remaining = re-lay Job Type step to Category (Pickup/Deliver/
  Assist/Other) then Item; conditional visibility from `IActiveJobItemResolverService` snapshot mapped onto
  `RequestJobPartAvailability`; dunnage image-card selection; user-entry Item paths; destination = requesting WC
  for Deliver (rule ready); zero-payload Riser/Hopper. NOTE:
  `MTM_Waitlist.Waitlist.NewRequest` references Settings (catalog) but NOT Setup — introduce a composition-root
  bridge (app service or move contract) to reach `IActiveJobItemResolverService` before wiring visibility.
- **Phase 4** uniform 2-line card (Line1 umbrella / Line2 item), full-width Other card, render-time
  item resolution, badge image family.
- **Phase 5** NCM defect feature — backend (table + CRUD SPs + role-gated service + tests) DONE 2026-09-09
  (see section F); remaining = Module_Settings editor panel (Frontend) and NCM Item Line 2 wiring.
- **Phase 6.2 (finish)** wire a real FG/WIP/Outside request Item (`{PartNumber} / {Sequence}` from
  `setup_active_jobs.sequence_number`, via the Phase 2.2 resolver) into the Phase 3/4 pipeline (no mock `FG-10042`).
- **Phase 7** validation/cleanup (tests across all 23 CSV rows, remove obsolete mock hard-coding, security review).

Keep the DB/SQL status-code knowledge in the single editable config
(`RequestDispositionStatusCodes.cs`) so future code changes are trivial.

## When done

- Leave the repo building clean and the new/affected tests green.
- Update `14-55%-Phase2-UnifiedWaitlistCard.md`: tick completed tasks with proof notes, update the
  `{Completion%}` in the filename.
- Summarize what you resolved from the Infor Visual parse (status codes, FG/WIP/Outside rules) and what
  you implemented next.
- Run the app-side checks in `WeekendProject/PromptFiles/App-Validation-Checklist.md` to confirm the
  refactor edits work end to end.
- The remaining boxes need interactive, in-app development (build/run the app + XamlMcp to visually verify the
  Phase 3/4 picker+card and Phase 5 settings panel).
