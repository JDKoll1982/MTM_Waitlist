# CONTINUE IMPLEMENTATION — MTM_Waitlist (paste this whole file into Copilot Chat)

> Paste the text below into a fresh Copilot Chat / agent session on the MTM_Waitlist repo to continue
> the Unified Waitlist Card + Type/Category/Item refactor and to parse the Infor Visual database.

---

## Role & context

You are working in the **MTM_Waitlist** WinUI 3 (.NET 10) repo at the workspace root. Continue the
Type/Category/Item + Unified Waitlist Card refactor being tracked by the **checklist-execution** skill.
First, read these to re-establish context before changing anything:

1. `WeekendProject/PromptFiles/14-0%-Phase2-UnifiedWaitlistCard.md` — the active implementation checklist.
2. `WeekendProject/Documents/Request-Config-Template.csv` — the **source of truth** 18-row spec (Pickup 9 /
   Deliver 6 / Assist 2 / Other 1). Each column is a concrete implementation value.
3. The design docs in `WeekendProject/PromptFiles/Plan-Design-*.md` (TypeCategoryActionRefactor,
   TypesSubtypesCatalog [current-state reference], UnifiedWaitlistCard [card-grid layout]).
4. Repo/session memory: `/memories/repo/*` and `/memories/session/*` (esp. anything with `checklist14`,
   `refactor`, `mcp-tooling`, `module-setup`, `waitlist`).
5. Repo instructions: `.github/instructions/*.md` and `.github/copilot-instructions.md`.

Adopt personas per checklist task (Database Engineer / Backend Engineer / etc.). Tick `- [x]` ONLY when
implemented, builds clean, and tests pass.

## Build / test commands (use these exactly)

- Build: `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`
- Test:  `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64 /m:1`
- Kill stale build locks first if MSB3026/PRI errors: kill `MTM_Waitlist`, `VBCSCompiler`, `MSBuild` processes.
- WMC9999 = masked XAML error (see repo instructions) — treat as a real but unspecified XAML problem.

## PRIMARY OBJECTIVE this session

**A) Parse the Infor Visual database** to resolve the remaining Phase 6 unknowns (exact `WORK_ORDER.STATUS`
and `OPERATION.STATUS` codes; confirm FG vs WIP vs Outside Service derivation) and update the config class.

**B) Continue implementing the remaining open tasks in `14-...md`** phase by phase (Phases 1–7), updating
the checklist and the `{Completion%}` in the file name as work completes.

---

## A) Parse Infor Visual database (do this first)

Connect to the **Infor Visual** SQL Server instance and pull real schema/data to confirm the status codes.
Use the app's existing connection path (env-var driven). The code reads these env vars
(see `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` and `SetupSqlScriptStore.cs`):

- `INFOR_VISUAL_SQL_CONNECTION_STRING`
- `INFOR_VISUAL_SQL_SERVER`
- `INFOR_VISUAL_SQL_DATABASE`
- `INFOR_VISUAL_SQL_USER`
- `INFOR_VISUAL_SQL_PASSWORD`

Dev credentials (provided by the user, dev-only, do NOT commit to source control or log them):
Server `VISUAL`, DB `MTMFG`, user `JKOLL`, password `KOLL`.

> SECURITY: Set these as environment variables (or pass at connect time) rather than hardcoding them into
> committed files. Do not print the password in output or commit it.

Parse and document (write findings into a new research note under `Documents/Development/InforVisual/`
and/or into the repo memory), then update `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
(the single editable config) with the confirmed codes:

1. Distinct `WORK_ORDER.STATUS` values in use, with row counts + meaning.
2. Distinct `OPERATION.STATUS` values in use.
3. How Outside Service is signaled (confirm `OPERATION.VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID` non-empty).
4. FG vs WIP: confirm a part is "Finished Goods" when closed/completed with `PART.QTY_ON_HAND` /
   `PART_LOCATION.QTY > 0` at a non-ignored location, vs "WIP" with open quantity on the order /
   `MT_WIP_INVENTORY`.
5. Look for any `WORK_ORDER.PROD_ORDER_TYPE` values that map to FG / WIP / Outside and note them.

Reference table/column schemas under `Documents/Development/InforVisual/DatabaseCSVFiles/ColumnDetails/`
(`WORK_ORDER.csv`, `OPERATION.csv`, `MT_WIP_INVENTORY.csv`, `PART.csv`, `PART_LOCATION.csv`) and the queue
SQL patterns in `Database/InforVisual/Queues/`.

Reusable SQL query files live at: `Database/InforVisual/Queues/Module_Setup/Queries/*.sql` and
`Database/InforVisual/Queues/Module_Waitlist/Queries/*.sql` (e.g. `GetSubordinateParts.sql`,
`LookupWorkOrder.sql`, `GetSequences.sql`, `GetInventoryLocations.sql`).

## B) Continue implementation (after A)

Work the open `- [ ]` tasks in `14-...md` in order. The config-driven disposition core (Phase 6 Subphase 6.0)
is ALREADY implemented + tested:
- `MTM_Waitlist.Settings/Models/RequestDisposition.cs`
- `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs` (edit status codes here)
- `MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs`
- `MTM_Waitlist.Tests/Module_Settings/RequestDispositionClassifierTests.cs`

Candidate next work (confirm against checklist state):
- Draft the live disposition SQL queue script following `GetInventoryLocations.sql` / `LookupWorkOrder.sql`
  patterns (VISUAL / MTMFG), feeding the classifier's `DispositionInput`.
- Wire `{PartNumber} / {Sequence}` from `setup_active_jobs.sequence_number`.
- Phase 1 JSON/`waitlist-request-types.json` extension, catalog loader, `ResolveImagePath`/`WaitlistRequestTitles`
  re-mapping, `NewRequestFlowRules.GetDefaultTypes()` ordering.
- Phase 5 NCM defect feature (`waitlist_defect_types` table + CRUD SPs + Module_Settings panel).
- Phase 2 / 3 / 4 resolver + picker + uniform 2-line card work.
- Phase 7 validation/cleanup.

Keep the DB/SQL status-code knowledge in the single editable config so future code changes are trivial.

## When done

- Leave the repo building clean and the new/affected tests green.
- Update `14-...md`: tick completed tasks with proof notes, update the `{Completion%}` in the filename.
- Summarize what you resolved from the Infor Visual parse (status codes, FG/WIP/Outside rules) and what
  you implemented next.
