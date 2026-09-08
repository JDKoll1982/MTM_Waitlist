# 06 - Phase 1 — Sortable Location Grid on the Waitlist Detail Page

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Task 13). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — data grid on detail page)

- Show a **sortable table/grid** on `WaitlistViewDetailPage.xaml` with columns **Part #, Location, Quantity**; sorting is by **clicking a column header** (no separate search box). Use a header row with sort affordance and zebra/alternating rows for readability; format quantity consistently (decimal, thousands separators). Only rows with **on-hand quantity ≥ 1** appear (drop `0`/`0.00` rows). Rows from locations in the ignored-locations setting (file `05`) are omitted. Keep Fluent spacing/typography; localize headers. Source data per the mock toggle: sample/mock when `Feature.InforVisualMockData` is ON, else a new Infor Visual SQL queue script under `Database/InforVisual/Queues/Module`.

## Subphase 0.1 — Infor Visual distinct-inventory query (source)

**PREREQUISITE: Task 0 green.**

- [x] **Database Migration: add a checked-in Infor Visual SQL queue script under `Database/InforVisual/Queues/Module`** returning per-part inventory rows (`Part #`, `Location`, `Quantity`/on-hand) using the `GetSubordinateParts.sql` join pattern as the template. *(Ref: Master Task 13)* | **Persona: Database Engineer**
- [x] **Service Layer: add a helper service that returns the location/part rows**, routing to the new SQL when `Feature.InforVisualMockData` is OFF and to sample/mock data when ON. *(Ref: Master Task 13)* | **Persona: Backend Engineer** *(2026-09-07: `WaitlistInventoryService.GetInventoryLocationsFromBackendAsync` now runs `GetInventoryLocations.sql` via a new shared Core executor `MTM_Waitlist.Core/Services/InforVisualSqlQueryService` + `WaitlistInforVisualSqlScriptStore`, mapping rows to `InventoryLocationRow` via `MapToInventoryLocationRow`; mock ON still routes to `SampleInventoryLocationCatalog`.)*
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for the query/mock routing (rows returned, zero rows handled, mock parity)**, then run the **full suite**; must pass. | **Persona: QA Engineer** *(2026-09-07: `WaitlistInventoryServiceTests` covers mock-ON routing/filter, mock-OFF no-connection → empty, empty-part → empty, plus new `MapToInventoryLocationRow_*` mapping tests. Live SQL row-return validation is gated behind the on-prem DB and excluded from CI; full suite 538/0 green.)*

## Subphase 0.2 — Sortable grid UI on the detail page

- [x] **Workflow: add the sortable Part # / Location / Quantity grid to `WaitlistViewDetailPage`** with column-header sorting. *(Ref: Master Task 13)* | **Persona: Frontend Engineer**
- [x] **Service Layer: filter rows to on-hand quantity ≥ 1 and omit ignored locations** (from file `05`) before display. *(Ref: Master Task 13)* | **Persona: Full Stack Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for ≥1 filtering, ignored-location omission, and sort behavior**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: grid appears on the detail page, is column-sortable, shows only ≥1 non-ignored rows, mock ON/OFF both work, full suite green. — MET 2026-09-07.**
Next task: **Completed — file `06` grid + backend source delivered. Move to `07-Phase2-fulfill.md`.** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Task 13's location grid needs sample rows (part/location/on-hand qty) spanning qty ≥1 vs 0 and ignored vs non-ignored locations to be testable with mock ON; no general sample inventory-location list exists at the Waitlist level.

- [x] **Full Stack Engineer: add a sample inventory-location list** (Part #, Location, On-hand qty) for the Waitlist/New Request work centers that includes ignored-location rows (defaults) and qty-0 rows so the grid's ≥1 filter and ignore rule are exercised with mock ON. *(Mock parity)*
- [x] **QA Engineer: add tests** that the mock grid omits qty 0 and ignored locations and sorts correctly; run the **full suite**. *(Mock parity)*

## Out-of-scope / additional changes (recorded 2026-09-07)
> The following were done while completing this file's Subphase 0.1 work. They are intentionally broader than this file's strict checkboxes and are recorded here for traceability.

- **Shared Infor Visual executor added to Core (architectural debt noted):** A new `MTM_Waitlist.Core/Services/InforVisualSqlQueryService.cs` (namespace `MTM_Waitlist.Module_Core.Services`) was added as the reusable executor that runs `Database/InforVisual/Queues/Module_Waitlist/Queries/*.sql`. It intentionally **duplicates** the logic already in `MTM_Waitlist.Setup/Services/InforVisualSqlQueryService.cs` so Waitlist does not need a `Waitlist.View → Setup` project reference. **Out of scope / not done:** migrating Module_Setup's executor onto the shared Core one (left for a follow-up to avoid churn in Setup's passing tests). The Core executor is registered in `ServiceRegistrationExtensions` Core-services block; Setup keeps its own registration.
- **CI filter broadened (user-directed):** `.github/workflows/build-and-test.yml` changed from `FullyQualifiedName!~InforVisualSqlQueryServiceTests` to `FullyQualifiedName!~InforVisual` so **all** Infor Visual (live on-prem DB) tests are excluded from CI, matching the request to remove Infor Visual tests from CI. Local runs still execute them as `Assert.Inconclusive` (skipped) when no DB is configured.
- **Type-ambiguity fix in Setup tests:** because the new Core executor shares the name `InforVisualSqlQueryService`, `SetupWorkflowServiceTests.CreateService` now fully-qualifies the Setup type (`MTM_Waitlist.Module_Setup.Services.InforVisualSqlQueryService`).
- **App output copy rule:** `MTM_Waitlist.csproj` now copies `Database\InforVisual\Queues\Module_Waitlist\**\*.sql` to output (previously only Module_Setup was copied), so the new script is present at runtime.
- **Environment:** a running `MTM_Waitlist.exe` debug session was stopped to clear output-file locks (MSB3026) so the build gate could be verified clean.

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-detail-locationgrid.svg` — sortable Part # / Location / Quantity grid
