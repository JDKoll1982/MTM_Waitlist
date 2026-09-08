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

- [ ] **CI/CD: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. (Ref: file 07 Phase 0) | **Persona: DevOps Engineer**
- [ ] **Testing: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). (Ref: file 07 Phase 0) | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## Phase 1 — Canonical Category/Item catalog (data-driven from CSV)

### Subphase 1.1: Request-type/request-item model foundation

- [x] **Data Model: add canonical Category (umbrella) enum** with `Pickup`, `Deliver`, `Assist`, `Other`. (Ref: CSV col 1/6; Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Models/RequestCategory.cs`
- [x] **Data Model: add canonical Item model** mirroring the CSV 18 rows (id `pickup-coil` … `other`, display, normalized, value type, source). (Ref: CSV rows; col 3/4/5/10) | **Persona: Backend Engineer** — verified 2026-09-07: `RequestItemDefinition.cs`, `RequestItemValueType.cs`, `RequestItemCatalog.cs` (18 rows); 7 tests green in `RequestItemCatalogTests.cs`
- [ ] **JSON Schema: extend `waitlist-request-types.json`** so each row carries Category + Item + source fields. (Ref: CSV; Plan-Design-TypesSubtypesCatalog) | **Persona: Full Stack Engineer**
- [ ] **Service Layer: expose catalog loader** returning the 18-row catalog from the CSV/JSON, usable by New Request and card render. (Ref: CSV; `NewRequestFlowRules`) | **Persona: Backend Engineer**
*Depends on: catalog loader wiring*

### Subphase 1.2: Category/Item → request model mapping

- [ ] **Data Model: re-map `RequestType`/`Subtype` usage** to Category/Item while preserving stable GUIDs. (Ref: Plan-Design-TypeCategoryActionRefactor; `RequestTypeInventory`) | **Persona: Backend Engineer**
- [ ] **Service Layer: update `WaitlistRequestTitles`** to `Line1 = umbrella`, `Line2 = item identifier`. (Ref: Plan-Design-UnifiedWaitlistCard) | **Persona: Backend Engineer**
- [ ] **Service Layer: replace `ResolveImagePath` ad-hoc subtype keyword matching** with explicit Category/Item model. (Ref: Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer**
- [ ] **Service Layer: update `NewRequestFlowRules.GetDefaultTypes()`** to order by Category then Item. (Ref: Plan-Design-TypeCategoryActionRefactor) | **Persona: Backend Engineer**
**GATE: catalog loads from CSV spec; every current request maps to a Category/Item row.**

## Phase 2 — Item data resolvers (from active setup job)

### Subphase 2.1: Read-back stored procedure

- [ ] **Database Migration: modify `sp_setup_active_jobs_latest_by_work_center_get`** (`Database/StoredProcedures/sp_setup_active_jobs_latest_by_work_center_get/create.sql`) to also `SELECT subordinate_parts_json` and `selected_dunnage_parts_json` (currently returns only work_center/work_order/part_number/sequence_number). (Ref: CSV Notes pickup-coil; table `11_setup_active_jobs`) | **Persona: Database Engineer**
- [ ] **Database Migration: add SP returning avg coil weight** from `mtm_receiving_application.receiving_history AVG(quantity) WHERE part_id`. (Ref: CSV pickup-coil Notes) | **Persona: Database Engineer**
- [ ] **Testing: DB SP unit/integration tests** for both read-back SPs. | **Persona: QA Engineer**

### Subphase 2.2: Item resolver service

- [ ] **Service Layer: implement job-item resolver** that returns a job's coil/flatstock/die/component list from `subordinate_parts_json` by work center (prefix MMC=Coil, MMF=Flatstock, FGT=Die, else Component). (Ref: CSV Source table/cols) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement dunnage resolver** returning a job's assigned dunnage part(s) from `selected_dunnage_parts_json`. (Ref: CSV pickup-dunnage/deliver-dunnage) | **Persona: Backend Engineer**
- [ ] **Service Layer: implement `{Sequence}` resolver** from `setup_active_jobs.sequence_number` for WIP/Outside rows. (Ref: CSV pickup-wip/pickup-outside-service Notes) | **Persona: Backend Engineer**
- [ ] **Service Layer: surface die location** from Module_Setup data (destination = Home Location path). (Ref: CSV pickup-die Notes; research in Module_Setup) | **Persona: Backend Engineer**
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

- [ ] **Database Table: create `waitlist_defect_types`** in `mtm_waitlist`. (Ref: CSV pickup-ncm; Plan-Design-TypeCategoryActionRefactor) | **Persona: Database Engineer**
- [ ] **Database Migration: add CRUD stored procedures** for defect types (list/add/update/remove). (Ref: CSV pickup-ncm) | **Persona: Database Engineer**
- [ ] **Settings Page: add a defect-types editor panel** in Module_Settings (searchable list, add/remove). (Ref: CSV pickup-ncm; Plan-Design-TypeCategoryActionRefactor) | **Persona: Frontend Engineer**
- [ ] **Settings Page: gate the defect editor** to Admin/Developer roles. (Ref: repo role gating pattern) | **Persona: Backend Engineer**
- [ ] **Service Layer: wire NCM Item Line 2** = `{PartNumber} / {Defect(User Entry)}` using the managed list. (Ref: CSV pickup-ncm) | **Persona: Backend Engineer**
- [ ] **Testing: NCM defect table + SP + picker tests.** | **Persona: QA Engineer**
**GATE: NCM defect types are CRUD-editable and appear in the NCM Item pick.**

## Phase 6 — FG / WIP / Outside-Service product type (research-gated)

> **Research outcome (2026-09-07):** Infor Visual is a job-shop ERP. FG / WIP / Outside Service is **not** a single product-type field a user sets; it is **derived** from the job + part + operation state. Grounded in the local Infor guide (`Documents/Development/InforVisual/Infor Visual Guide`, ch 03/05) and the schema exports (`DatabaseCSVFiles/ColumnDetails`). Context7 + the Infor cloud portal were unreachable (login-gated / 404), so the remaining step needs live test data.
>
> **Implementation note (2026-09-07):** the disposition **derivation core is implemented as a pure,
> config-driven classifier** so it stays stable while the Infor status codes are unknown. All Infor
> status-code values are isolated in one easily-editable config class; when the real codes are confirmed
> you edit only that file — no logic change. The remaining open items are the live Infor SQL script, the
> exact status-code confirmation, and wiring the resolver to the job-item pipeline.

### Subphase 6.0: Config-driven disposition derivation (IMPLEMENTED — pure, testable)

- [x] **Data Model: add `RequestDisposition` enum** (`FinishedGoods` / `WorkInProcess` / `OutsideService` / `Unknown`). (Ref: CSV pickup-fg/wip/outside-service; Phase 6) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Models/RequestDisposition.cs`
- [x] **Data Model: add `RequestDispositionClassifier`** — pure function over a `DispositionInput` snapshot (no DB), priority: OutsideService → FinishedGoods (closed + on-hand) → WorkInProcess → Unknown. (Ref: Phase 6 derivation) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Services/RequestDispositionClassifier.cs`
- [x] **Configuration: isolate Infor status codes in `RequestDispositionStatusCodes`** — the single editable block (`OpenStatusCodes`, `ClosedStatusCodes`), marked as placeholders until confirmed against live data. (Ref: ColumnDetails WORK_ORDER/OPERATION) | **Persona: Backend Engineer** — verified 2026-09-07: `MTM_Waitlist.Settings/Services/RequestDispositionStatusCodes.cs`
- [x] **Testing: unit tests for the classifier** cover Outside precedence, FG (closed + on-hand), WIP (open qty / open status), Unknown, and case-insensitive code matching. | **Persona: QA Engineer** — verified 2026-09-07: 8/8 pass in `RequestDispositionClassifierTests.cs`

### Subphase 6.1: Derivation spec from the Infor Visual schema

- [ ] **Database Migration: draft the product-disposition derivation SQL** for a work center's active setup job + part: **Outside Service** = an `OPERATION` row with non-empty `VENDOR_ID` / `SERVICE_ID` / `SERVICE_PART_ID`; **FG** = `PART.QTY_ON_HAND` / `PART_LOCATION.QTY > 0` at a non-ignored finished-goods location; **WIP** = open-quantity on the `WORK_ORDER` / `MT_WIP_INVENTORY` (has `LOCATION`, `QTY`). (Ref: CSV pickup-fg/ncm/wip/outside-service; ColumnDetails WORK_ORDER/OPERATION/MT_WIP_*) | **Persona: Database Engineer**
- [ ] **Configuration: confirm Infor status codes against live data and update `RequestDispositionStatusCodes`** — `WORK_ORDER.STATUS` (nchar1) and `OPERATION.STATUS` drive open vs completed; replace the placeholder code sets. No classifier logic change needed. (Ref: ColumnDetails WORK_ORDER/OPERATION) | **Persona: Database Engineer**
- [ ] **Tech Lead: validate the derivation against live Infor Visual test data** for real FG/WIP/Outside parts (the remaining unresolved item — cloud portal and Context7 were unreachable; needs a real data pull). (Ref: research notes) | **Persona: Tech Lead**

### Subphase 6.2: Item resolution implementation

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
