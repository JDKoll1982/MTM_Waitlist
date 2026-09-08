## Role & context

You are working in the **MTM_Waitlist** WinUI 3 (.NET 10) repo at the workspace root. Continue the
Type/Category/Item + Unified Waitlist Card refactor being tracked by the **checklist-execution** skill.
First, read these to re-establish context before changing anything:

1. `WeekendProject/PromptFiles/14-26%-Phase2-UnifiedWaitlistCard.md` — the active implementation checklist
   (was `14-0%` when this session started; advanced to **26%** on 2026-09-08).
2. `WeekendProject/Documents/Request-Config-Template.csv` — the **source of truth** 18-row spec (Pickup 9 /
   Deliver 6 / Assist 2 / Other 1). Each column is a concrete implementation value.
3. The design docs in `WeekendProject/PromptFiles/Plan-Design-*.md` (TypeCategoryActionRefactor,
   TypesSubtypesCatalog [current-state reference], UnifiedWaitlistCard [card-grid layout]).
4. Repo/session memory: `/memories/repo/*` and `/memories/session/*` (esp. `infor-visual-disposition.md`,
   anything with `checklist14`, `refactor`, `mcp-tooling`, `module-setup`, `waitlist`).
5. Repo instructions: `.github/instructions/*.md` and `.github/copilot-instructions.md`.

Adopt personas per checklist task (Database Engineer / Backend Engineer / etc.). Tick `- [x]` ONLY when
implemented, builds clean, and tests pass.

## Build / test commands (use these exactly)

- Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
- Test:  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
- Kill stale build locks first if MSB3026/PRI errors: kill `MTM_Waitlist`, `VBCSCompiler`, `MSBuild` processes.
- WMC9999 = masked XAML error (see repo instructions) — treat as a real but unspecified XAML problem.

## Session status (2026-09-08) — build 0w/0e, full suite 581 green

**Objective A (Infor Visual parse) — DONE.** See
`Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md` + repo memory.
**Objective B — Phase 0, Phase 6.1, Phase 6.2 groundwork, and Phase 1.1 catalog loader DONE.** Checklist
renamed to `14-26%-Phase2-UnifiedWaitlistCard.md` (12/47 boxes).

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

### B) DONE this session

- **Phase 0 (2/2):** solution builds `0w/0e`; full `MTM_Waitlist.Tests` suite green (581). Repaired 4
  stale Infor DB-integration tests (stale `EMPLOYEE.NAME` → `FIRST_NAME`/`LAST_NAME`; dead WO fixtures →
  live `WO-074011`/`24733431`/seq `919`) and mirrored `Database/InforVisual/Queues/**` + `Database/MTMWipApp/**`
  SQL content into `MTM_Waitlist.Tests.csproj`.
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
- **Phase 1.1 (partial):** catalog loader `Settings/Services/IRequestItemCatalogService.cs` +
  `RequestItemCatalogService.cs` (wraps static `RequestItemCatalog` 18-row CSV mirror; DI-registered;
  5/5 tests).

### B) Remaining open tasks (next)

Work `- [ ]` tasks in `14-26%-Phase2-UnifiedWaitlistCard.md` in order:
- **Phase 1.1** JSON Schema: extend `Assets/Config/waitlist-request-types.json` (legacy Type/Subtype
  tree feeding live New Request flows via `NewRequestFlowRules`/`RequestTypeCatalogService`) so each row
  carries Category + Item + source fields. Read `Plan-Design-TypesSubtypesCatalog.md` first; map each
  legacy node to the 18-row taxonomy carefully.
- **Phase 1.2** re-map `RequestType`/`Subtype` → Category/Item (preserve stable GUIDs), update
  `WaitlistRequestTitles` (Line1=umbrella, Line2=item), replace `ResolveImagePath` ad-hoc keyword
  matching, and order `NewRequestFlowRules.GetDefaultTypes()` by Category then Item.
- **Phase 2** read-back SPs (`sp_setup_active_jobs_latest_by_work_center_get` →
  subordinate/dunnage JSON; avg coil weight SP) + job-item/dunnage/sequence resolvers + die location.
- **Phase 3** New Request Category → Item picker (visibility, dunnage image cards, user-entry items).
- **Phase 4** uniform 2-line card (Line1 umbrella / Line2 item), full-width Other card, render-time
  item resolution, badge image family.
- **Phase 5** NCM defect feature (`waitlist_defect_types` + CRUD SPs + Module_Settings panel, role-gated).
- **Phase 6.2 (finish)** wire a real FG/WIP/Outside request Item (`{PartNumber} / {Sequence}` from
  `setup_active_jobs.sequence_number`) into the Phase 2/3/4 pipeline (no mock `FG-10042`).
- **Phase 7** validation/cleanup (tests across 18 rows, remove obsolete mock hard-coding, security review).

Keep the DB/SQL status-code knowledge in the single editable config
(`RequestDispositionStatusCodes.cs`) so future code changes are trivial.

## When done

- Leave the repo building clean and the new/affected tests green.
- Update `14-...md`: tick completed tasks with proof notes, update the `{Completion%}` in the filename.
- Summarize what you resolved from the Infor Visual parse (status codes, FG/WIP/Outside rules) and what
  you implemented next.
