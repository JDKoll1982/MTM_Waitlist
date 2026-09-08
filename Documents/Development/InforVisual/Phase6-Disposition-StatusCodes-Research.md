# Phase 6 — FG / WIP / Outside Service: Live-Data Status-Code Research

> **Date:** 2026-09-08 · **Author:** Copilot (checklist file `14` Phase 6)
> **Scope:** Resolve the remaining Phase 6 unknowns from live data and update
> `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs` (the single editable config).
> Dev credentials are env/`appsettings.json` driven (`InforVisualDatabaseOptions`); none are stored in
> this note or in source control beyond the existing dev config.

## 1. Data sources used

| Source | Purpose |
|---|---|
| Infor Visual SQL Server `VISUAL` / `MTMFG` (dev login from `appsettings.json` → `InforVisualSqlQueryService`/`SetupSqlScriptStore`) | Authoritative ERP: `WORK_ORDER`, `OPERATION`, `PART`, `ENUM_CODES` |
| MTM WIP Application (MySQL `172.16.1.104` / `mtm_wip_application_winforms`) | Floor WIP/FG quantity tracker (`inv_inventory`, `md_part_ids`, `md_locations`) |

## 2. WORK_ORDER.STATUS — CONFIRMED (ENUM_CODES + observed counts)

Observed in use (`SELECT STATUS, COUNT(*) FROM WORK_ORDER GROUP BY STATUS`; total 112,500):

| Code | Meaning (ENUM_CODES) | WORK_ORDER count | OPERATION count | Disposition role |
|---|---|---|---|---|
| `C` | Closed | 68,278 | 244,112 | **Closed → Finished Goods** (when on-hand) |
| `R` | Released | 680 | 2,840 | **Open → WIP** |
| `U` | Unreleased | 42,138 | 13,046 | **Open → WIP** |
| `X` | Cancelled | 1,404 | 9,030 | **Neither** — never FG; Unknown unless open qty physically remains |
| `F` | Firmed | 0 | 0 | Open/planned (defined by ENUM_CODES; 0 rows in this install) |

- `ENUM_CODES` rows: `TABLE_NAME='WORK_ORDER'` and `'OPERATION'`, `COLUMN_NAME='STATUS'`,
  `ENUM_TEXT` = Closed / Firmed / Released / Unreleased / Cancelled (codes C / F / R / U / X).
- Both `WORK_ORDER.STATUS` and `OPERATION.STATUS` are `nchar(1)` and share the same code set.

## 3. Outside Service — CONFIRMED

- Signaled by a non-empty **vendor / service** on an `OPERATION` row, **not** by a status code.
- Confirmed live: ~26.9k `OPERATION` rows carry `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID`
  (`RESOURCE_ID = 'OUTSIDE_SERVICE'`), e.g. `W|007979|900|OUTSIDE_SERVICE|C|VID-000248-01|PAINT|A03-29236-000`.
- These operations span statuses C / R / U / X — status does **not** gate Outside Service.
- Classifier rule (unchanged): `HasOutsideVendorOperation` → `OutsideService` first.

## 4. FG vs WIP — CONFIRMED derivation

- `WORK_ORDER.DESIRED_QTY − WORK_ORDER.RECEIVED_QTY` = remaining open quantity on the order.
  Verified: `WO-074011` (R) `84 − 51 = 33` open; unreleased `U` orders have `RECEIVED_QTY = 0`.
- **FG** = work order `Closed (C)` **and** stock on hand at a finished-goods location.
- **WIP** = open quantity still on the order / floor WIP.
- Infor's own `MT_WIP_INVENTORY` (PART_ID/WAREHOUSE/LOCATION/QTY) is **empty** in this install — unused.
- Real floor WIP/FG quantities live in the **MTM WIP Application** MySQL DB
  (`mtm_wip_application_winforms`):
  - `inv_inventory` (620 rows): `PartID`, `Location`, `Operation`, `Quantity`, `ItemType`
    (WIP 594 / Dunnage 21 / Other 4 / None 1), `WorkOrder` (all `Unknown` today).
  - `md_locations` marks finished-goods areas: `FG`, `DC-FG`, `FLOOR - FINISHED GOODS` (Buildings Expo/Vits).
  - `md_part_ids` `ItemType` can carry `Outside Service` / `Die Shop` / `Dunnage` / `Other`.

## 5. PROD_ORDER_TYPE — NOT a driver

- `WORK_ORDER.PROD_ORDER_TYPE` is `NULL` for all 112,500 rows in this install (no `ENUM_CODES`).
- Do **not** gate disposition on it.

## 6. Code/config changes applied

- `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
  - `OpenStatusCodes` = `R` (Released), `U` (Unreleased), `F` (Firmed — defined, unused).
  - `ClosedStatusCodes` = `C` (Closed).
  - `X` (Cancelled) is intentionally in neither set; header documents the confirmed codes + derivations.
- `MTM_Waitlist.Tests/Module_Settings/RequestDispositionClassifierTests.cs`
  - Existing `"O"` placeholder cases updated to confirmed codes (`R`/`U`/`C`).
  - Added `Classify_UnreleasedStatus_ReturnsWorkInProcess` and
    `Classify_CancelledStatusWithOnHand_ReturnsUnknownNotFinishedGoods`.
  - Result: 10/10 classifier tests green.
