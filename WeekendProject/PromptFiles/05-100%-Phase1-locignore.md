# 05 - Phase 1 — Ignored Infor Visual Inventory Locations (Settings + App-wide Apply)

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 11, 12). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## IMPLEMENTATION NOTE (2026-09-05) — app-wide apply dependency on file `06` (locgrid)

The ignored-location defaults (`WC, NCM, V-WC, NCM-VITS, SHIP`) are **plant inventory location codes**. The design (see `Module_Waitlist\Controls\Coil\CoilWaitlistLineView.xaml` "excluding NCM and SHIP locations") applies them to a **per-location coil inventory grid and its summed totals**. Today no such per-location data source exists in code: the coil "Quantity in house" total is a hard-coded `"18 coils"`, and the only current "location" fields (`SetupSubordinatePart.Location`, `SetupDunnagePart.HomeLocation`) are **storage-rack labels** (`Rack A1`, `Kit Shelf 2`, `V-A1-01`) that are unrelated to plant location codes — filtering those by `WC/NCM/SHIP` matches nothing. Therefore Subphase 0.2's "omit from Waitlist/Coil lists+totals" and "Setup location lists" application is deferred to file `06` (locgrid), which introduces the location grid; at that point the new grid and its totals must honor `IIgnoredLocationsService` (the shared read implemented here). This file delivers the foundation: the Settings panel (Subphase 0.1) and the shared ignore-list read (0.2 box 1).

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer** *(verified 2026-09-07: Build succeeded, 0 errors)*
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer** *(verified 2026-09-07: 536 total, 0 failed, 12 skipped)*
**GATE: 0 errors + 0 warnings + full green suite before/after this file. — MET 2026-09-07.**

## UI Guidance (embedded — Settings panel)

- Add a **new section on the existing Settings screen** (`SettingsPage.xaml` + `SettingsViewModel` in `MTM_Waitlist.Settings`). Editing stays in **Settings only** (no inline editing on lists). Use a list/table of saved location codes with an **add** field (auto-uppercased, validated against a simple code pattern) and a **remove** per row. Defaults: `WC, NCM, V-WC, NCM-VITS, SHIP`. Persist via `ILocalSettingsService`. Role-gate editing to roles **above Material Handler** (Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer) using the existing `Can*` manage-settings pattern. Keep Fluent spacing and `Segoe UI Variable`; localize all strings via the `.resw` convention.

## Subphase 0.1 — Settings panel for ignored locations

**PREREQUISITE: Task 0 green.**

- [x] **Settings Card: add an "Ignored Infor Visual inventory locations" section/panel to `SettingsPage` + `SettingsViewModel`** with add/remove of location codes, pre-seeded with defaults `WC, NCM, V-WC, NCM-VITS, SHIP`, persisted via `ILocalSettingsService`. *(Ref: Master Task 11)* | **Persona: Frontend Engineer**
- [x] **Auth Logic: role-gate editing to roles above Material Handler** via a `Can*` property on `SettingsViewModel` matching existing manage-settings gates. *(Ref: Master Task 11)* | **Persona: Backend Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for default seed, add/remove, persistence round-trip, and role gating**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Apply the ignore list app-wide (lists AND totals)

**PREREQUISITE: Subphase 0.1 complete.**

- [x] **Service Layer: expose the ignored-locations list as a shared setting read** so all consumers can filter consistently (Waitlist/Coil, Setup, sample/mock). *(Ref: Master Task 12)* | **Persona: Backend Engineer**
- [x] **Workflow: Waitlist/Coil screens — omit ignored locations from lists and from the "Quantity in house" totals.** *(Ref: Master Task 12)* | **Persona: Full Stack Engineer** *(delivered via file 06 locgrid: `WaitlistInventoryService` + `InventoryLocationFiltering.Apply`, detail-grid sort/totals in `WaitlistViewDetailViewModel`)*
- [x] **Workflow: Setup screen location lists (`SetupSubordinatePart` grids) omit ignored locations.** *(Ref: Master Task 12)* | **Persona: Frontend Engineer** *(2026-09-07: `SetupLookupService` now injects `IIgnoredLocationsService` and filters subordinate parts whose `Location` is ignored, covering mock + backend paths)*
- [x] **Service Layer: honor the ignore rule in mock/sample-data paths and totals** so ON/OFF toggle behavior matches. *(Ref: Master Task 12)* | **Persona: Backend Engineer** *(2026-09-07: `SetupDataCatalog` seeds plant-code rows incl. ignored NCM/SHIP; SetupLookupService applies the same shared ignore read; Waitlist sample path already honors it)*
- [x] **Testing: add tests in `MTM_Waitlist.Tests` verifying ignored locations are excluded from lists and from summed totals across Waitlist, Setup, and sample paths**, then run the **full suite**; must pass. | **Persona: QA Engineer** *(2026-09-07: added `SelectSequenceAsync_OmitsSubordinatePartsAtDefaultIgnoredPlantLocations` + `..._AtCustomIgnoredLocations`; full suite green)*

**GATE: settings panel seeds/persists/role-gates correctly; ignored locations excluded from lists AND totals app-wide; full suite green. — MET 2026-09-07.**
Next task: **Completed — file `05` foundation + Setup/mock apply delivered. Move to `06-Phase1-locgrid.md`.** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Task 12 requires ignored locations (defaults WC, NCM, V-WC, NCM-VITS, SHIP) to be omitted from lists AND summed totals in the sample/mock path too; today the only sample location source (`SetupDataCatalog`) has no concept of the ignored set.

- [x] **Service Layer: expose the ignored-locations read to the sample/mock path** and seed sample location rows that include ignored and non-ignored entries so totals/lists omit ignored in mock ON; apply the same shared ignore rule in `SampleDataService`-driven totals. *(Mock parity)* *(2026-09-07: `SetupDataCatalog.GetSubordinateParts` now seeds NCM/SHIP (ignored) + FLOOR-12 (non-ignored) rows; `SetupLookupService.ExcludeIgnoredLocationsAsync` applies the shared `IIgnoredLocationsService` read; Waitlist sample path (`SampleInventoryLocationCatalog`) already seeded ignored codes and filters via `WaitlistInventoryService`)*
- [x] **QA Engineer: add tests** that mock-ON lists/totals exclude the ignored locations across the Waitlist/Coil and Setup sample paths; run the **full suite**. *(Mock parity)* *(2026-09-07: Setup mock-ON exclusion tests added; Waitlist mock-ON exclusion already covered by `InventoryLocationFilteringTests`/`WaitlistInventoryServiceTests`; full suite green 536/0)*

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/settings-ignoredlocations-panel.svg` — ignored Infor Visual locations settings panel
