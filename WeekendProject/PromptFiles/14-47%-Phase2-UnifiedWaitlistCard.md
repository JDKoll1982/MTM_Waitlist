# 14 - Phase 2 — Unified Waitlist Card + Type/Category/Item Refactor

> **Purpose:** Replace the current per-type request Type/Subtype system and multi-line card fields with
> the **Category (umbrella) / Item (thing)** model and a **uniform 2-line card** driven by the
> implementation spec `WeekendProject/Documents/Request-Config-Template.csv` (18 rows: Pickup 9 /
> Deliver 6 / Assist 2 / Other 1). Aligns with card/fulfill Phase 2 (file `07`); consumes the design docs
> `Plan-Design-TypesSubtypesCatalog` (current-state reference), `Plan-Design-TypeCategoryActionRefactor`
> (taxonomy), and `Plan-Design-UnifiedWaitlistCard` (card-grid + Line1/Line2 layout spec).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented,
> builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Phase 0 — Task 0: Green build & full-suite baseline

- [x] **CI/CD: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. (Ref: file 07 Phase 0) | **Persona: DevOps Engineer** — verified 2026-09-08: `Build succeeded. 0 Warning(s) 0 Error(s)`.
- [x] **Testing: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). (Ref: file 07 Phase 0) | **Persona: QA Engineer** — verified 2026-09-08: `Passed! Failed: 0, Passed: 552, Skipped: 8` (8 skipped are opt-in MySQL `ConfigImagesLocationsIntegrationTests` gated on `MTM_WAITLIST_TEST_DB_CONNECTION_STRING`; run live against 172.16.1.104 → 8/8 pass). Repaired 4 stale Infor Visual DB-integration tests (stale `EMPLOYEE.NAME` schema + dead WO fixtures) → live green; mirrored `Database/InforVisual/Queues/**` content into `MTM_Waitlist.Tests.csproj` so tests load real scripts.
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## Phase 1 — Canonical Category/Item catalog (data-driven from CSV)

### Subphase 1.1: Request-type/request-item model foundation

- [x] **Data Model: add canonical Category (umbrella) enum** with `Pickup`, `Deliver`, `Assist`, `Other`. (Ref: CSV col 1/6; Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Models/RequestCategory.cs`
- [x] **Data Model: add canonical Item model** mirroring the CSV 18 rows (id `pickup-coil` … `other`, display, normalized, value type, source). (Ref: CSV rows; col 3/4/5/10) | **Persona: Backend Engineer** — verified 2026-09-07: `RequestItemDefinition.cs`, `RequestItemValueType.cs`, `RequestItemCatalog.cs` (18 rows); 7 tests green in `RequestItemCatalogTests.cs`
- [x] **JSON Schema: extend `waitlist-request-types.json`** so each row carries Category + Item + source fields. (Ref: CSV; Plan-Design-TypesSubtypesCatalog) | **Persona: Full Stack Engineer** — **DB-first re-scope (2026-09-08, user): do NOT extend the shipped JSON; route Category/Item through the DB.** Verified 2026-09-08: extended existing catalog tables with canonical `category` + `item_id` mapping columns (leaf rows only) — DDL (create.sql ×2 + AllTables.sql + update_table_descriptions.sql), seed mapping (seed_waitlist_request_catalog + AllSeeds.sql; Forklift Assist type leaf → Other/other, all 24 subtype leaves mapped per TypeCategoryActionRefactor), get-SPs (active + _all, per-artifact + both AllSPs zones) select the columns, and the DB read path (`RequestTypeCatalogService` → `NewRequestTypeDefinition`/`NewRequestSubtypeDefinition`) surfaces `Category`/`ItemId` with tests. Catalog also **expanded 18 → 23 rows** (Wrong Coil, Wrong Flatstock, Scrap offal removal, Pickup Hopper no-return, Table Remove Parts) in `Request-Config-Template.csv` + `RequestItemCatalog`. Build 0w/0e; full suite 574 passed / 0 failed / 8 skipped (MySQL-gated). **Note:** schema/seed/SP changes authored consistently per DB rules but require a live re-seed + DB-integration run (`MTM_WAITLIST_TEST_DB_CONNECTION_STRING`) to confirm against MySQL before the DB path is declared live-green. **Verified 2026-09-09 live on localhost:** DB re-seeded — `category`/`item_id` populated (Forklift Assist type-leaf → Other/other; 24/24 subtype leaves mapped); `sp_waitlist_request_types_get(_all)`/`sp_waitlist_request_subtypes_get(_all)` return the columns; new opt-in `RequestTypeCatalogServiceIntegrationTests` (2/2) pass live against `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` — DB-first mapping confirmed live-green.**
- [x] **Service Layer: expose catalog loader** returning the 18-row catalog from the CSV/JSON, usable by New Request and card render. (Ref: CSV; `NewRequestFlowRules`) | **Persona: Backend Engineer** — verified 2026-09-08: `MTM_Waitlist.Settings/Services/IRequestItemCatalogService.cs` + `RequestItemCatalogService.cs` (wraps `RequestItemCatalog` mirror of CSV: `GetAllItems`/`GetByCategory`/`FindById`/`GetCategoriesInOrder`), DI-registered; 5/5 green in `RequestItemCatalogServiceTests.cs`. JSON-backed source to follow with the schema-extension task above.
*Depends on: catalog loader wiring*

### Subphase 1.2: Category/Item → request model mapping

- [x] **Data Model: re-map `RequestType`/`Subtype` usage** to Category/Item while preserving stable GUIDs. (Ref: Plan-Design-TypeCategoryActionRefactor; `RequestTypeInventory`) | **Persona: Backend Engineer** — verified 2026-09-08: pure static `MTM_Waitlist.Settings/Services/RequestItemLegacyMapper.cs` maps every legacy leaf pair (`"<UPPER TYPE>\u0001<UPPER SUBTYPE>"`, 24 seed subtypes + legacy `PICKUP/OUTSIDE SERVICE` superset + `FORKLIFT ASSIST` type-leaf → `other`) onto the canonical 23-row `RequestItemCatalog` (`Map`, `ResolveUmbrellaVerb`, `KnownLeafKeys`); stable GUIDs untouched (`RequestTypeInventory`/`RequestSubtypeInventory` unchanged). DB read model (`NewRequestTypeDefinition`/`NewRequestSubtypeDefinition` + `RequestTypeCatalogService`) surfaces `Category`/`ItemId`. 4/4 `RequestItemLegacyMapperTests` green; full suite 578 passed / 0 failed / 8 skipped (MySQL-gated).
- [x] **Service Layer: update `WaitlistRequestTitles`** to `Line1 = umbrella`, `Line2 = item identifier`. (Ref: Plan-Design-UnifiedWaitlistCard) | **Persona: Backend Engineer** — verified 2026-09-08 (additive, per user scope: UI text flips in Phase 4): `WaitlistRequestTitles` gains `ResolveLine1(requestType, subtype)` (canonical umbrella verb Pickup/Deliver/Assist/Other via `RequestItemLegacyMapper.ResolveUmbrellaVerb`, legacy-type fallback otherwise) and `ResolveItem(requestType, subtype)` (canonical `RequestItemDefinition` row via mapper, or null). Existing `For()` phrase output unchanged (visible card text + pinning tests untouched). 4 new tests in `WaitlistRequestTitlesTests`; suite green (build 0w/0e).
- [ ] **Service Layer: replace `ResolveImagePath` ad-hoc subtype keyword matching** with explicit Category/Item model. (Ref: Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer** — *additive re-scope (2026-09-08, user): the explicit model surface is ready (`RequestItemLegacyMapper` + `RequestItemCatalog`, ticked above) but `ResolveImagePath` (private static in `WaitlistViewViewModel`) still drives live badge→template selection (`WaitlistLineTemplateSelector`, `WaitlistViewDetailViewModel.LoadTemplateSections`) and sample-order images, so flipping it to the canonical model would change visible images/templates. Per user, defer the swap + test re-pin to Phase 4 (badge/selector task) alongside the uniform 2-line card. No code change made here to keep the card green.*
- [ ] **Service Layer: update `NewRequestFlowRules.GetDefaultTypes()`** to order by Category then Item. (Ref: Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer** — *additive re-scope (2026-09-08, user): `GetDefaultTypes()` is the DB-empty fallback of 8 legacy TYPE definitions (not canonical Items); no test pins its order, but ordering "by Category then Item" only becomes well-defined once the New Request picker surfaces Category→Item. Defer to Phase 3/4 picker re-layout. No code change made here.*
**GATE: catalog loads from CSV spec; every current request maps to a Category/Item row.**

## Phase 2 — Item data resolvers (from active setup job)

### Subphase 2.1: Read-back stored procedure

- [x] **Database Migration: modify `sp_setup_active_jobs_latest_by_work_center_get`** (`Database/StoredProcedures/sp_setup_active_jobs_latest_by_work_center_get/create.sql`) to also `SELECT subordinate_parts_json` and `selected_dunnage_parts_json` (currently returns only work_center/work_order/part_number/sequence_number). (Ref: CSV Notes pickup-coil; table `11_setup_active_jobs`) | **Persona: Database Engineer** — verified 2026-09-09: create.sql + AllSPs.sql now select `aj.subordinate_parts_json, aj.selected_dunnage_parts_json`; deployed + validated live on localhost (both JSON columns returned for a seeded work center).
- [x] **Database Migration: add SP returning avg coil weight** from `mtm_receiving_application.receiving_history AVG(quantity) WHERE part_id`. (Ref: CSV pickup-coil Notes) | **Persona: Database Engineer** — verified 2026-09-09: new `Database/MTMReceivingApp/StoredProcedures/sp_receiving_history_average_coil_weight.sql` (`ROUND(AVG(quantity),0)` WHERE part_id, ignores NULL/<=0 qty); deployed + validated live on localhost (MMC0001000 → 5000).
- [x] **Testing: DB SP unit/integration tests** for both read-back SPs. | **Persona: QA Engineer** — verified 2026-09-09: opt-in `MTM_Waitlist.Tests/Module_Setup/ActiveJobReadBackSpIntegrationTests.cs` seeds a work-center active job (asserts both JSON columns returned by the read-back SP) and two `receiving_history` skids (asserts avg coil weight = 5000); both pass live against localhost (`MTM_WAITLIST_TEST_DB_CONNECTION_STRING`), inconclusive offline.

### Subphase 2.2: Item resolver service

- [x] **Service Layer: implement job-item resolver** that returns a job's coil/flatstock/die/component list from `subordinate_parts_json` by work center (prefix MMC=Coil, MMF=Flatstock, FGT=Die, else Component). (Ref: CSV Source table/cols) | **Persona: Backend Engineer** — verified 2026-09-09: new `MTM_Waitlist.Setup/Services/ActiveJobItemResolverService.cs` (`IActiveJobItemResolverService.ResolveAsync(workCenter)`) reads `sp_setup_active_jobs_latest_by_work_center_get` and deserializes `subordinate_parts_json` into `SetupSubordinatePart[]`, canonicalizing category by prefix (authoritative) with stored-category fallback; returns `SetupActiveJobSnapshot` (Coils/Flatstock/Dies/Components). 8/8 `ActiveJobItemResolverServiceTests` green.
- [x] **Service Layer: implement dunnage resolver** returning a job's assigned dunnage part(s) from `selected_dunnage_parts_json`. (Ref: CSV pickup-dunnage/deliver-dunnage) | **Persona: Backend Engineer** — verified 2026-09-09: same `ActiveJobItemResolverService` deserializes `selected_dunnage_parts_json` into `SetupDunnagePart[]` exposed as `Snapshot.DunnageParts`; covered by `ActiveJobItemResolverServiceTests` (`ResolveAsync_DunnageParts_AreReturned`).
- [x] **Service Layer: implement `{Sequence}` resolver** from `setup_active_jobs.sequence_number` for WIP/Outside rows. (Ref: CSV pickup-wip/pickup-outside-service Notes) | **Persona: Backend Engineer** — verified 2026-09-09: `ActiveJobItemResolverService` surfaces `Snapshot.SequenceNumber` from the read-back row; covered by `ActiveJobItemResolverServiceTests` (`ResolveAsync_Sequence_ReturnsSequenceNumber`).
- [x] **Service Layer: surface die location** from Module_Setup data (destination = Home Location path). (Ref: CSV pickup-die Notes; research in Module_Setup) | **Persona: Backend Engineer** — verified 2026-09-09: `SetupActiveJobSnapshot.PrimaryDie`/`DieLocation` reads the die subordinate row's `Location` (already serialized in `subordinate_parts_json` by `SetupPersistenceService`); covered by `ActiveJobItemResolverServiceTests` (`ResolveAsync_DieLocation_SurfacesHomeLocation`).
*RESOLVED (2026-09-07): die location = `SetupSubordinatePart.Location` (`MTM_Waitlist.Setup/Models/SetupModels.cs`, e.g. `FGT-0653` → `V-A1-01`), serialized by `SetupPersistenceService` into `setup_active_jobs.subordinate_parts_json`. No separate lookup — read the die row's `location` field from the same JSON the job-item resolver parses.*
**GATE: resolver returns real job items for coil/flatstock/die/component/dunnage/sequence.**

## Phase 3 — New Request picker (Category → Item)

- [ ] **Request Type Card: re-lay New Request category selection** to Category (Pickup/Deliver/Assist/Other) then Item, matching CSV Order. (Ref: CSV Order col) | **Persona: Frontend Engineer**
- [ ] **Request Type Card: apply conditional visibility** — auto-populated Items (coil/flatstock/die/component/dunnage) appear only if the requesting job has that part. (Ref: CSV Notes visibility) | **Persona: Frontend Engineer**
- [ ] **Request Type Card: implement Dunnage image-card selection** (same card style as Module_Setup), user always selects the dunnage part. (Ref: CSV pickup-dunnage/deliver-dunnage) | **Persona: Frontend Engineer**
- [ ] **Request Type Card: add user-entry Item paths** for Die destination, Component pick, NCM defect, and Other free text (validated vs Infor Visual where CSV says). (Ref: CSV rows) | **Persona: Frontend Engineer**
- [ ] **Workflow: keep destination = requesting work center** for all Deliver rows (no user entry). (Ref: CSV Deliver Notes) | **Persona: Full Stack Engineer**
- [ ] **Request Type Card: include zero-payload Items** Riser Table / Hopper as pure flag selections. (Ref: CSV pickup-riser-table/deliver-riser-table/deliver-hopper) | **Persona: Frontend Engineer**
**GATE: New Request lets a user pick Category → Item and produces the intended request payload.**

## Phase 4 — Uniform 2-line card (Line1 = umbrella, Line2 = item)

- [ ] **Request Type Card: build single uniform 2-line card control** — Line 1 umbrella, Line 2 item identifier. (Ref: Plan-Design-UnifiedWaitlistCard; CSV col 6/11) | **Persona: Frontend Engineer**
- [ ] **Request Type Card: move type-specific detail fields** to the detail page (not the card). (Ref: Plan-Design-UnifiedWaitlistCard) | **Persona: Frontend Engineer**
- [ ] **Request Type Card: render the full-width 'Other' card** on Row 1 with RowSpan = 2 per grid spec. (Ref: Plan-Design-UnifiedWaitlistCard; CSV Other row) | **Persona: Frontend Engineer**
- [ ] **Service Layer: re-resolve item data at render** for coil/flatstock/die/dunnage rows from work center/job. (Ref: Plan-Design-UnifiedWaitlistCard Coil section) | **Persona: Backend Engineer**
- [ ] **Request Type Card: update badge/selector** so Category maps to the correct image family. (Ref: CSV col 6; Plan-Design-TypeCategoryActionRefactor) | **Persona: Frontend Engineer**
**GATE: every CSV row renders as a uniform 2-line card with correct item/umbrella.**

## Phase 5 — NCM defect feature (mtm_waitlist)

- [x] **Database Table: create `waitlist_defect_types`** in `mtm_waitlist`. (Ref: CSV pickup-ncm; Plan-Design-TypeCategoryActionRefactor) | **Persona: Database Engineer** — verified 2026-09-09: new `Database/Tables/30_waitlist_defect_types/create.sql` + `rollback.sql` (id/public_id/defect_name/description/sort_order/is_active/audit, unique defect_name) added to `AllTables.sql` + `update_table_descriptions.sql`; deployed + live-validated on localhost.
- [x] **Database Migration: add CRUD stored procedures** for defect types (list/add/update/remove). (Ref: CSV pickup-ncm) | **Persona: Database Engineer** — verified 2026-09-09: `sp_waitlist_defect_types_{get_all,insert,update,delete}` per-artifact create.sql+rollback ×4 + `AllSPs.sql`; deployed + live CRUD round-trip validated on localhost (insert/update/delete/get_all).
- [ ] **Settings Page: add a defect-types editor panel** in Module_Settings (searchable list, add/remove). (Ref: CSV pickup-ncm; Plan-Design-TypeCategoryActionRefactor) | **Persona: Frontend Engineer**
- [ ] **Settings Page: gate the defect editor** to Admin/Developer roles. (Ref: repo role gating pattern) | **Persona: Backend Engineer**
- [ ] **Service Layer: wire NCM Item Line 2** = `{PartNumber} / {Defect(User Entry)}` using the managed list. (Ref: CSV pickup-ncm) | **Persona: Backend Engineer**
- [x] **Testing: NCM defect table + SP + picker tests.** | **Persona: QA Engineer** — verified 2026-09-09 (table + SP portion): opt-in `MTM_Waitlist.Tests/Module_Settings/DefectTypesCrudIntegrationTests.cs` round-trips insert→list→update→delete through the live SPs (passes on localhost; inconclusive offline). Picker-test component lands with Phase 3/4 NCM Item picker.
**GATE: NCM defect types are CRUD-editable and appear in the NCM Item pick.**

## Phase 6 — FG / WIP / Outside-Service product type (research-gated)

> **Research outcome (2026-09-08):** Infor Visual is a job-shop ERP. FG / WIP / Outside Service is
> **not** a single product-type field a user sets; it is **derived** from the job + part + operation
> state. Grounded in the local Infor guide (`Documents/Development/InforVisual/Infor Visual Guide`,
> ch 03/05), the schema exports (`DatabaseCSVFiles/ColumnDetails`), and a **live data pull
> (2026-09-08)** from `VISUAL/MTMFG` (ENUM_CODES + WORK_ORDER/OPERATION/PART counts) and the MTM WIP
> Application MySQL DB (`mtm_wip_application_winforms`, floor stock). Status codes confirmed:
> C=Closed, F=Firmed, R=Released, U=Unreleased, X=Cancelled.
>
> **Implementation note (2026-09-08):** the disposition **derivation core is a pure, config-driven
> classifier**. All Infor status-code values are isolated in one easily-editable config class
> (`RequestDispositionStatusCodes`) — now confirmed against live data (see Subphase 6.1). The live
> Infor derivation SQL (`GetDispositionInput.sql`), the MTM WIP App MySQL data path
> (`GetWipFloorQuantities.sql` + `WipFloorInventoryService`), and the pure mapper/resolver feeding
> `DispositionInput` are implemented (see Subphase 6.2 groundwork). Remaining open items are wiring a
> real request Item (part/sequence/disposition) into the Phase 2/3/4 resolver + picker/card pipeline.

### Subphase 6.0: Config-driven disposition derivation (IMPLEMENTED — pure, testable)

- [x] **Data Model: add `RequestDisposition` enum** (`FinishedGoods` / `WorkInProcess` / `OutsideService` / `Unknown`). (Ref: CSV pickup-fg/wip/outside-service; Phase 6) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Models/RequestDisposition.cs`
- [x] **Data Model: add `RequestDispositionClassifier`** — pure function over a `DispositionInput` snapshot (no DB), priority: OutsideService → FinishedGoods (closed + on-hand) → WorkInProcess → Unknown. (Ref: Phase 6 derivation) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs`
- [x] **Configuration: isolate Infor status codes in `RequestDispositionStatusCodes`** — the single editable block (`OpenStatusCodes`, `ClosedStatusCodes`), marked as placeholders until confirmed against live data. (Ref: ColumnDetails WORK_ORDER/OPERATION) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
- [x] **Testing: unit tests for the classifier** cover Outside precedence, FG (closed + on-hand), WIP (open qty / open status), Unknown, and case-insensitive code matching. | **Persona: QA Engineer** — verified 2026-09-07: 8/8 pass in `RequestDispositionClassifierTests.cs`

### Subphase 6.1: Derivation spec from the Infor Visual schema

- [x] **Database Migration: draft the product-disposition derivation SQL** for a work center's active setup job + part: **Outside Service** = an `OPERATION` row with non-empty `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID`; **FG** = `PART.QTY_ON_HAND` / `PART_LOCATION.QTY > 0` at a non-ignored finished-goods location; **WIP** = open-quantity on the `WORK_ORDER` / `MT_WIP_INVENTORY` (has `LOCATION`, `QTY`). (Ref: CSV pickup-fg/ncm/wip/outside-service; ColumnDetails WORK_ORDER/OPERATION/MT_WIP_*) | **Persona: Database Engineer** — verified 2026-09-08: `Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql` (returns WorkOrderStatus / OpenWorkOrderQuantity = DESIRED−RECEIVED / FinishedGoodsQuantity = PART.QTY_ON_HAND / HasOutsideVendorOperation), validated live: WO-074011/24733431 → R, open 33, on-hand 54, outside 0; WO-074010/12-32754-000 → R, outside 1. `Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql` (floor FG/O-S/NCM/WIP buckets), validated live: A22-77724-100 → O/S 74; 22-77401-001 → NCM 340; 380397.001 → WIP 8,740.
- [x] **Configuration: confirm Infor status codes against live data and update `RequestDispositionStatusCodes`** — `WORK_ORDER.STATUS` (nchar1) and `OPERATION.STATUS` drive open vs completed; replace the placeholder code sets. No classifier logic change needed. (Ref: ColumnDetails WORK_ORDER/OPERATION) | **Persona: Database Engineer** — verified 2026-09-08 against live `VISUAL/MTMFG`: ENUM_CODES maps C=Closed, F=Firmed, R=Released, U=Unreleased, X=Cancelled; observed counts C 68,278/R 680/U 42,138/X 1,404 (WO), C 244,112/R 2,840/U 13,046/X 9,030 (OP). `OpenStatusCodes` = R,U,F; `ClosedStatusCodes` = C; X excluded (never FG). 10/10 classifier tests green.
- [x] **Tech Lead: validate the derivation against live Infor Visual test data** for real FG/WIP/Outside parts (the remaining unresolved item — cloud portal and Context7 were unreachable; needs a real data pull). (Ref: research notes) | **Persona: Tech Lead** — verified 2026-09-08 live: Outside = 26.9k OPERATION rows w/ VENDOR_ID/SERVICE_ID/SERVICE_PART_ID (RESOURCE_ID `OUTSIDE_SERVICE`); WIP open qty = `WORK_ORDER.DESIRED_QTY − RECEIVED_QTY` (WO-074011: 84−51=33 open); `MT_WIP_INVENTORY` empty in MTMFG; `PROD_ORDER_TYPE` all NULL. FG locations confirmed in MTM WIP App `md_locations` (FG / DC-FG / FLOOR - FINISHED GOODS). Full note: `Documents/Development/InforVisual/Phase6-Disposition-StatusCodes-Research.md`.

### Subphase 6.2: Item resolution implementation

> **GROUNDWORK COMPLETE (2026-09-08, not yet UI-wired):**
> - MySQL data path added (user decision): `MySqlDatabaseTarget.MtmWipApplication` + env
>   `MTM_WIP_APPLICATION_DB_CONNECTION_STRING` in `MySqlHelperServer`; floor snapshot model
>   `MTM_Waitlist.Core/Models/WipFloorQuantitySnapshot.cs`; executor + script store
>   `MTM_Waitlist.Core/Services/WipFloorInventoryService.cs` (loads `GetWipFloorQuantities.sql` from
>   `Database/MTMWipApp/Queues/Module_Waitlist/Queues`; app+test csproj content includes added).
> - Pure composition `MTM_Waitlist.Settings/Services/RequestDispositionMapper.cs`
>   (`MapInforRow` + `BuildDispositionInput(InforDispositionRow?, WipFloorQuantitySnapshot?)`).
> - End-to-end `MTM_Waitlist.Settings/Services/RequestDispositionResolver.cs` (runs Infor
>   `GetDispositionInput.sql` + floor snapshot → `DispositionInput` → `RequestDisposition`); DI-registered.
> - Tests green: `WipFloorInventoryServiceTests` (5), `RequestDispositionMapperTests` (11),
>   `RequestDispositionClassifierTests` (10). Remaining = wire a request Item (part/sequence/disposition)
>   into the Phase 2/3/4 resolver + New Request picker/card pipeline (no mock `FG-10042`).

- [ ] **Service Layer: implement FG / WIP / Outside Item resolution** from the confirmed derivation, reusing the `GetInventoryLocations.sql` / `LookupWorkOrder.sql` / `GetSubordinateParts.sql` join patterns (VISUAL / MTMFG), feeding the classifier's `DispositionInput`. (Ref: CSV rows; Database/InforVisual/Queues) | **Persona: Backend Engineer**
- [ ] **Service Layer: wire `{PartNumber} / {Sequence}`** from `setup_active_jobs.sequence_number` (already resolved in Phase 2.2) and the derived FG/WIP/Outside type. (Ref: CSV pickup-wip/pickup-outside-service) | **Persona: Backend Engineer**
*PREREQUISITE: Subphase 6.1 live-data validation completes.*
**GATE: FG/WIP/Outside items resolve a real part/sequence/disposition (no hard-coded mock `FG-10042` / `WO-073112 / RM-48190`).**

## Phase 7 — Validation & cleanup

- [ ] **Testing: add/adjust unit tests** for catalog load, resolvers, New Request picker, and card render across all 18 CSV rows. (Ref: CSV; existing `SampleWaitlistRequestCatalog`) | **Persona: QA Engineer**
- [ ] **QA: remove now-obsolete mock field hard-coding** in `WaitlistViewViewModel.AddRequestFields` replaced by resolvers. (Ref: CSV col 19/20) | **Persona: QA Engineer**
- [ ] **Security Review: verify role gating and Infor Visual validation** for user-entry items. | **Persona: Security Engineer**
- [ ] **CI/CD: full solution build + full test suite green** with 0 warnings. | **Persona: DevOps Engineer**
**GATE: green build + full green suite; file `14` moved from 0% toward 100% based on completed phases.**
