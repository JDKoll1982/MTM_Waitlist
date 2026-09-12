# Infor Visual / WIP disposition facts (file 14 Phase 6, verified 2026-09-08 against live data)

> Durable copy — canonical long-form research note:
> `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`
> Checklist: `WeekendProject/PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md`

## Phase 6 implementation state (2026-09-08) — checklist `14-57%`
- Phase 0 done (build 0w/0e; full suite 581 green). Phase 6.1 done (config + GetDispositionInput.sql + validation). Phase 6.2 groundwork done (MySQL MtmWipApplication target + WipFloorInventoryService + RequestDispositionMapper + RequestDispositionResolver), UI item/picker wiring pending.
- Phase 1.1 partial: catalog loader done - `Settings/Services/IRequestItemCatalogService.cs` + `RequestItemCatalogService.cs` (wraps static `RequestItemCatalog` 18-row CSV mirror; GetAllItems/GetByCategory/FindById/GetCategoriesInOrder), DI-registered, 5/5 tests. JSON schema extension (`Assets/Config/waitlist-request-types.json` legacy Type/Subtype tree) + RequestType/Subtype->Category/Item re-map + WaitlistRequestTitles + ResolveImagePath + GetDefaultTypes ordering still open (large, Phase 2-4 tied).
- Infor derivation SQL: `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` (validated live: WO-074011/24733431 -> R, open 33, on-hand 54, outside 0; WO-074010/12-32754-000 -> R, outside 1).
- MySQL floor script: `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql` (validated live). New app content folder `Database/MTMWipApp/Queues/Module_Waitlist/**` added to app + test csproj.
- New code: `MySqlDatabaseTarget.MtmWipApplication` + env `MTM_WIP_APPLICATION_DB_CONNECTION_STRING` in `MTM_Waitlist.Core/Services/MySqlHelperServer.cs`; `Core/Models/WipFloorQuantitySnapshot.cs`; `Core/Services/WipFloorInventoryService.cs` (+ internal `WaitlistWipMySqlScriptStore`); `Settings/Services/RequestDispositionMapper.cs` (MapInforRow + BuildDispositionInput); `Settings/Services/RequestDispositionResolver.cs` (runs Infor + floor -> DispositionInput/RequestDisposition; DI-registered).
- Tests: WipFloorInventoryServiceTests(5), RequestDispositionMapperTests(11), RequestDispositionClassifierTests(10) all green. Full suite 581 green (with MTM_WAITLIST_TEST_DB_CONNECTION_STRING set live).
- WIP MySQL floor location taxonomy (mtm_wip_application_winforms): FG = md_locations.Location IN ('FG','DC-FG') or name/building contains 'FINISHED GOODS' (currently 0 stock); Outside = Location LIKE 'O/S%' ('O/S - VITS'); NCM = Location LIKE 'NCM%'; DIE SHOP/QUALITY separate. inv_inventory.ItemType stays 'WIP' even at O/S/NCM - the LOCATION is the signal. Exclude ItemType='Dunnage'.
- Remaining for file 14: wire request Item (part/sequence/disposition) into Phase 2/3/4 resolver + New Request picker/card, Phases 1-5/7 open. **Superseded in part (2026-09-11):** there is no demo/mock mode any more — the retired sample catalogs and mock toggles are gone (`RetiredSymbolAuditTests` enforces it), internal stores are always read live, and only Infor Visual reads fall back to the `mtm_mock` mirror (`specs/001-module-mock-visual-fallback`).

## Confirmed status codes (VISUAL/MTMFG, ENUM_CODES + counts)
- WORK_ORDER.STATUS & OPERATION.STATUS share one nchar(1) set: C=Closed, F=Firmed, R=Released, U=Unreleased, X=Cancelled.
- In use: C (68k WO / 244k OP), R (680/2840), U (42k/13k), X (1404/9030). F has 0 rows.
- Disposition mapping: Closed={C}; Open={R,U,F}; X=Cancelled is NEITHER (never FG).
- Update ONLY `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs` to change codes.
- Outside Service = OPERATION VENDOR_ID/SERVICE_ID/SERVICE_PART_ID non-empty (RESOURCE_ID='OUTSIDE_SERVICE'), NOT a status. ~26.9k rows.
- WORK_ORDER.PROD_ORDER_TYPE all NULL (unused). Do NOT gate on it.
- Open qty = WORK_ORDER.DESIRED_QTY - RECEIVED_QTY. Infor MT_WIP_INVENTORY is EMPTY (unused).

## MTM WIP Application (real floor WIP/FG source)
- MySQL server 172.16.1.104, database = `mtm_wip_application_winforms` (NO plain `mtm_wip_application`). App dev root account is provided out-of-band via env/appsettings - do not commit or log the password.
- `inv_inventory`: PartID/Location/Operation/Quantity/ItemType (WIP 594, Dunnage 21, Other 4, None 1), WorkOrder col all 'Unknown'.
- `md_locations` marks FG areas: 'FG','DC-FG','FLOOR - FINISHED GOODS' (Buildings Expo/Vits).
- `md_part_ids` ItemType incl 'Outside Service'/'Die Shop'/'Dunnage'. Part IDs numeric (139523) or xxxxx or dash parts.
- Repo code now wires this WIP DB via `MySqlDatabaseTarget.MtmWipApplication` (2026-09-08).

## Tooling quirks
- `mysql.exe` (Workbench) stdout is NOT captured by the tool terminal (empty output even with Start-Process redirect).
  Use PowerShell + repo's MySqlConnector.dll from `bin/Debug/net10.0-windows10.0.19041.0/win-x64/MySqlConnector.dll`.
- sqlcmd (ODBC170) works fine for Infor Visual (VISUAL/MTMFG) with `$env:SQLCMDPASSWORD`.
- Multi-line inline PowerShell is truncated by the terminal: put queries in a .ps1 file under $env:TEMP and run it.
- 'RowCount' is a reserved alias in T-SQL; use 'Cnt'.
- `appsettings.json` has **no `ConnectionStrings` section** — the waitlist string is `StartupDatabaseOptions.ConnectionString`
  (and the receiving one is `ReceivingDatabaseOptions.ConnectionString`). The live-database test suites are gated on
  `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`. `pwsh` (PowerShell 7) is now installed on this workstation; Windows
  PowerShell 5.1 has no ternary operator, and assigning to the reserved `$args` then splatting `@args` does not pass
  the intended argument list (use `$arguments`). `$host` is also a **read-only automatic variable** — a script that
  assigns `$host = '172.16.1.104'` silently keeps the PowerShell `Host` object and every DB call then fails with
  "Unknown MySQL server host 'System.Management.Automation.Internal.Host.InternalHost'". Use `$dbHost`.
- **The `MTM_Waitlist.Mock.Service` host is the shared MySQL / Infor Visual host at `172.16.1.104`
  (`V-MTMFG-5.mantoolmfg.com`) — NOT `MTMFG-161`, which `workstation-elevation.md` correctly describes as the
  development workstation.** Deploy it with `MTM_Waitlist.Mock.Service/deploy/install-mock-service.ps1` (README
  beside it): it refuses to run unless the local machine's own IPv4 addresses include the expected server address, so
  **it only runs when VS Code is on the server**. It installs to `C:\Services\MTM_Waitlist.Mock.Service\`; the
  service's state lives separately under `%LOCALAPPDATA%\MTM_Waitlist.Mock.Service\` (config, run records,
  `backups\<database>\`) — deleting the install folder alone leaves the DPAPI credential and backups behind. The
  service needs `MTM_MYSQL_PASSWORD` (or a full `MTM_MOCK_DB_CONNECTION_STRING` / `MTM_WAITLIST_DB_CONNECTION_STRING`)
  and `INFOR_VISUAL_SQL_USER` / `INFOR_VISUAL_SQL_PASSWORD`; without them it starts and reports "no mtm_mock
  connection" rather than failing. See `.github/memories/repo/workstation-secrets.md` for the owner-approved
  exception that lets the agent set those.

## Work-order families and addressing (verified live 2026-09-11)

Full evidence and the resolution: `specs/001-module-mock-visual-fallback` → `tasks.md` Phase 23 (the defect) and
Phase 24 (the 2026-09-12 fix).

- **How to query Visual faithfully (use this, not `sqlcmd`).** Build the connection string with
  `VisualConnectionStringProvider.Resolve()`'s rules — `INFOR_VISUAL_SQL_CONNECTION_STRING`, else
  `INFOR_VISUAL_SQL_{SERVER,DATABASE,USER,PASSWORD}`, else `InforVisualDatabaseOptions:*`, with
  `TrustServerCertificate=true; Encrypt=false; Connect Timeout=<n>` — and run each script exactly as
  `VisualQueryExecutor.ExecuteAsync` does: UTF-8 script text from the content root, `CommandType.Text`,
  15 s timeout, `AddWithValue("@Name", value)`. In PowerShell 7 load
  `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\{Microsoft.Data.SqlClient,MySqlConnector}.dll`. Scraping
  `sqlcmd`'s text output **mis-counts rows** (its `-W -s '|'` separator line looks like a data row), and
  `SqlConnectionStringBuilder` property assignment resolves to the keyword indexer, which rejects `DataSource` —
  use `$b['Data Source'] = …`. The terminal truncates multi-line inline PowerShell: put it in a `.ps1` file.
- Open `WORK_ORDER` rows come in several families (**42,852 open total**): `Q` 37,259 (ids like `CQ-016245-19`,
  `.054" X .500"`), `M` 4,898 (`BASE_ID` = bare numeric = `PART_ID`, e.g. `10089`; 230 of these are all-digits with
  `LEN` 5–6), `W` 695 (`BASE_ID` literally `WO-…`, 555 of them `WO-` + digits, e.g. `WO-041652`, `WO-074011`).
- **The app's work-order key is the `WO-######` form** — `WorkOrderValidationService` **auto-formats** the operator's
  raw input to uppercase `WO-` + exactly six digits (zero-padded), and that canonical string is what it hands to
  Visual and to the cache. The **input** stays deliberately lenient — `76951`, `076951`, `WO-76951` and `WO-076951`
  all normalize to `WO-076951` — so the textbox autoformatter keeps working; only the **key sent** is strict. (The
  rule was briefly tightened to `^WO-(\d{6})$` for input too, then restored on 2026-09-12 because it broke the
  autoformatter.) That key is what `setup_active_jobs.work_order` holds. The real Setup workflow's only saved
  job is `WO-041652` → live `TYPE=W, BASE_ID='WO-041652'`, i.e. the `W` family — the only family that key resolves.
- The live queue scripts match `BASE_ID = @NormalizedWorkOrder[…]` **verbatim**. The old stripped-base-id fallback
  (`BASE_ID IN (normalized, stripped)`, via `@WorkOrderBaseId`) was **removed 2026-09-12** (T144) from
  `LookupWorkOrder.sql`, `GetSequences.sql`, `GetSubordinateParts.sql` and `GetDispositionInput.sql`: it could still
  resolve a bare numeric `M` order the app can no longer ask for, and where the id also existed as a `WO-0xxxxx`
  order it returned a *different* order than the cache served. Under the old rule, normalization collapsed `10089`
  and `010089` into `WO-010089`, so a cached `M` row and a live `W` row answered the *same key* with **different
  orders** (`WO-010089` → live `24 126 172` / *Bracket, Control* vs cache `10089` / *Brake Pad*). That collision
  class is now unreachable by design, not merely unfixed.
- **Numeric ids are no longer addressable at all (as of 2026-09-12).** Under the retired rule the app padded the
  operator's 5–6 digits to 6, so a **6-digit** numeric id was addressable (key `WO-102776`, matched live through the
  stripped `@WorkOrderBaseId`) but a **5-digit** one was not. Measured open then: 154 six-digit, 76 five-digit, of
  which 3 coincided with a `WO-0xxxxx` order — i.e. the padded key would answer with a *different* order. The
  `WO-######`-only rule removes both forms from the accepted domain.
- **Resolved 2026-09-12 (T144, both halves).** All five `Module_Mock/Populations/*.sql` guards now accept only
  `BASE_ID` literally `WO-` + 6 digits and key it as the app does, and the four work-order live reads match that
  verbatim key, so cache and live are keyed by the same string. The bare-6-digit and 5-digit numeric forms are no
  longer accepted or cached: 42,143 of the 42,852 open orders were never expressible, and the operator decided on
  2026-09-12 to accept the `WO-######` form **only**, i.e. to narrow rather than widen. The mirror is correspondingly
  smaller (the 146 numeric-form keys are gone). Shape 4 additionally drops zero-stock locations
  (`HAVING MAX(COALESCE(pl.QTY, part.QTY_ON_HAND, 0)) >= 1`, T150, 2026-09-12) — the same rule the caller applies, so
  a location that drops to zero simply disappears at the next refresh, which always replaces the mirror wholesale.
  The population reads still need a live re-run on the cache host to confirm the new row counts.
