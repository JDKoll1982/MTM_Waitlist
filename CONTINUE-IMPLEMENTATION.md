## Role & context

You are working in the **MTM_Waitlist** WinUI 3 (.NET 10) repo at the workspace root. Continue the
Type/Category/Item + Unified Waitlist Card refactor being tracked by the **checklist-execution** skill.
First, read these to re-establish context before changing anything:

1. `WeekendProject/PromptFiles/14-30%-Phase2-UnifiedWaitlistCard.md` — the active implementation checklist
   (was `14-0%`; advanced through **26%** to **30%** — 14/47 boxes — on 2026-09-08).
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

## Session status (2026-09-08) — build 0w/0e, full suite 582 green / 0 failed / 8 skipped (MySQL-gated)

**Objective A (Infor Visual parse) — DONE.** See
`Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md` + repo memory.
**Objective B — Phase 0, Phase 6.0/6.1/6.2 (groundwork), Phase 1.1 (catalog expansion + DB-first mapping),
and Phase 1.2 #1 (mapper) + #2 (WaitlistRequestTitles accessors) DONE.** Checklist now
`14-30%-Phase2-UnifiedWaitlistCard.md` (14/47 boxes).

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

Work `- [ ]` tasks in `14-30%-Phase2-UnifiedWaitlistCard.md` in order:
- **Phase 1.1** JSON Schema box — left UNTICKED with a DB-first re-scope note: do NOT extend the shipped
  JSON; the DB-first mapping above is the carrier. Requires the live re-seed validation noted above before
  the box is provably green.
- **Phase 1.2** #3 (`ResolveImagePath` ad-hoc → explicit Category/Item) and #4 (`GetDefaultTypes` order by
  Category then Item) — DEFERRED to Phase 3/4 per user additive scope (both flip live UI surfaces that Phase
  3/4 rebuild). Re-scope notes are written into the two boxes; visible swap + test re-pin lands with the
  Phase 4 uniform 2-line card / badge-selector, and picker ordering with the Phase 3 Category→Item picker.
- **Phase 2** read-back SPs (`sp_setup_active_jobs_latest_by_work_center_get` →
  subordinate/dunnage JSON; avg coil weight SP) + job-item/dunnage/sequence resolvers + die location.
- **Phase 3** New Request Category → Item picker (visibility, dunnage image cards, user-entry items).
- **Phase 4** uniform 2-line card (Line1 umbrella / Line2 item), full-width Other card, render-time
  item resolution, badge image family.
- **Phase 5** NCM defect feature (`waitlist_defect_types` + CRUD SPs + Module_Settings panel, role-gated).
- **Phase 6.2 (finish)** wire a real FG/WIP/Outside request Item (`{PartNumber} / {Sequence}` from
  `setup_active_jobs.sequence_number`) into the Phase 2/3/4 pipeline (no mock `FG-10042`).
- **Phase 7** validation/cleanup (tests across all 23 CSV rows, remove obsolete mock hard-coding, security review).

Keep the DB/SQL status-code knowledge in the single editable config
(`RequestDispositionStatusCodes.cs`) so future code changes are trivial.

## When done

- Leave the repo building clean and the new/affected tests green.
- Update `14-30%-Phase2-UnifiedWaitlistCard.md`: tick completed tasks with proof notes, update the
  `{Completion%}` in the filename.
- Summarize what you resolved from the Infor Visual parse (status codes, FG/WIP/Outside rules) and what
  you implemented next.
