# 02 - Phase 1 — Coil Correctness: Gating, Real Display, Up-Front Coil

> **Source:** consolidated in the Master `WeekendProject/PromptFiles/prompt.md` (Tasks 2, 3, 4). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build (both projects) + full suite pass required before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: confirm `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** with `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: confirm the full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before and after this file.**

## UI Guidance (embedded for this task — Request Selection / Job Type screen)

- Use the New Request wizard's **Request Selection (Job Type)** step (`NewRequestJobTypePage` + `NewRequestJobTypeViewModel`), a grid of `NewRequestOptionItem` tiles (image + name). Follow the media-card look already used by `CoilRequestTypeImageView` / work-center cards: full-bleed image, rounded `CornerRadius`, readable label. Respect Fluent spacing (8px grid; 12px between cards), `Segoe UI Variable` type ramp, and set `AutomationProperties.Name` per tile (accessibility). Keep request-type tiles hidden entirely (do not show a disabled/explained Coil) when the job has no coil. *(Ref: Master Task 2/4; repo card conventions)*

## Subphase 0.1 — Real coil-availability source (mock toggle)

**PREREQUISITE: Task 0 green.**

- [x] **Service Layer: add a real coil-availability check for the active job** replacing the hard-coded `hasCoilData: true` in `NewRequestJobTypeViewModel`; route via `Feature.InforVisualMockData` (mock/sample when ON, live Infor Visual job data when OFF). *(Ref: Master Task 2 + Cross-Cutting Rule)* | **Persona: Backend Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for has-coil true/false under both mock states**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Hide Coil (and sub-choices) when the job has no coil

- [x] **Workflow: hide the `Coil` request type tile and its sub-choices (e.g. Pickup Coil) during Request Selection when the active job has no coil.** *(Ref: Master Task 2)* | **Persona: Frontend Engineer**
- [x] **Testing: add view-model tests in `MTM_Waitlist.Tests` verifying the Coil tile (and coil subtypes) are absent when `hasCoilData=false`**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.3 — Show the actual coil for the current work center

- [x] **Service Layer/VM: wire coil fields (`Requested coil`, `Quantity in house`, `Average coil weight`, `Coil description`) to the real active-job coil data** instead of the placeholder `"Not provided"`/`"Wrong coil"` strings in `WaitlistViewViewModel.AddRequestFields`. *(Ref: Master Task 3)* | **Persona: Full Stack Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` asserting coil fields resolve from real/sample data and are never the placeholder sentinels**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.4 — Show the current coil up front on the Request screen

- [x] **Workflow: on the Request/Request-Selection screen, clearly show the coil currently on the selected work center's job (or "none") before the worker chooses a request type**, while continuing to hide request types irrelevant to the job. *(Ref: Master Task 4)* | **Persona: Full Stack Engineer**
- [x] **Testing: add view-model tests in `MTM_Waitlist.Tests` for the up-front coil readout (coil present vs none)**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: Coil is hidden when absent, the real coil is shown when present, the up-front readout works, mock ON/OFF both behave, full suite green. Move to `03-Phase1-listdetail.md`.**
Next task: **Subphase 0.1 coil-availability source** | **Persona: Backend Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> The mock path has no Waitlist-level "does this active job have a coil" answer or real coil field values; coil cards fall back to the `"Not provided"` / `"Wrong coil"` sentinels, and the only coil-like sample lives in the Setup-scoped `SetupDataCatalog.GetSubordinateParts`.

- [x] **Backend Engineer: add a Waitlist/New-Request mock coil source** (mock ON) that reports the active job's coil availability (`hasCoil` true/false) and real coil fields (requested coil, quantity in house, coil description, average coil weight) for the selected work center/job, so `NewRequestJobTypeViewModel` and the coil fields never emit `"Not provided"`/`"Wrong coil"` in mock mode. *(Mock parity)*
- [x] **QA Engineer: add tests** that mock-ON yields hasCoil true and false jobs and populated coil fields (never the sentinel strings); run the **full suite**. *(Mock parity)*

## Coil quantity semantics — pooled weight per location (added 2026-09-06)
>
> Infor Visual reports coil stock as **ONE pooled row per unique part-in-location**: a location
> appears once and pools all transactions for that part into a single total on-hand **weight
> (lb)**. Locations are **Infor location codes** (e.g. `V-A0-01`), not storage labels.
>
> Example: three coils of `MMC0001000` @ 5,000 lb in `V-A0-01` display as one row
> `MMC0001000 | V-A0-01 | 15000`, never as three per-coil lines.
>
> Consequences applied in code/mock (mock ON): the Waitlist detail "Inventory by Location"
> grid rows (`SampleInventoryLocationCatalog`) are pooled per-location weights with Infor codes;
> the coil card "Quantity in house" / New-Request banner "on hand" figure is the pooled on-hand
> **weight (lb)** total across non-ignored locations (not a coil count); and "Average coil
> weight" is the per-coil weight (lb). UI column is "Quantity (lb)".

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/newrequest-selection-requesttypes.svg` — Job Type (Request Selection) tiles, baseline
- `../Mockups/newrequest-selection-coilhidden.svg` — Coil tile hidden when the job has no coil
- `../Mockups/newrequest-selection-coilbanner.svg` — current-coil banner shown up front
