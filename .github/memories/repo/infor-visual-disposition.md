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
- Remaining for file 14: wire request Item (part/sequence/disposition) into Phase 2/3/4 resolver + New Request picker/card (no mock FG-10042), Phases 1-5/7 open.

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
