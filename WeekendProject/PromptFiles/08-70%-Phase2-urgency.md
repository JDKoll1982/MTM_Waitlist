# 08 - Phase 2 — Urgency: Max-Allotted Time per Subtype + Ordering

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 17, 18). Self-contained. Builds on timestamps (file `01`) and wait time (file `03`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

> ### ✅ DELIVERED by `specs/003-waitlist-handler-fulfilment` (reconciled 2026-09-20)
> This file was part of that spec's **seed**. Its remaining open boxes are ticked below with the user story that
> delivered them. The whole *Mock-data flow coverage* section has been **deleted**: it asked for urgency math on the
> **mock** path, and `specs/001` FR-003 / FR-014 and constitution II forbid reintroducing a demo/mock mode, so it can
> never be built. Its previously-ticked QA box is void too — it asserted against `SampleWaitlistRequestCatalog`, and
> that catalog was **deleted** by `specs/001` (`RetiredSymbolAuditTests` fails the build if it returns). Do not
> reopen any of it.
>
> Mapping — `specs/003` US3 *The list leads with the most urgent work*.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — urgency readout & ordering)

- Show remaining time on cards (already present via `RemainingTimeText`), color-coded: normal (neutral), near-due (amber), **overdue (red)**. Urgency = **due − now**, where **due = created time + the sub-type's max allotted time**; overdue when negative. The handler/available list orders **most-urgent-first**. The **max allotted time** is a Plant Manager+ settable value **per request sub-type**, edited on the Settings screen as a small grid/list of sub-types with a numeric minutes/seconds control. Persist via `ILocalSettingsService`; provide sensible defaults; localize.

## Subphase 0.1 — Max allotted time per sub-type (Plant Manager+)

**PREREQUISITE: Task 0 green.**

- [x] **Settings Card: add a Plant Manager+ (and above) setting that stores a max allotted time for each request sub-type** (e.g. each Coil subtype, each Pickup subtype), with sensible defaults, persisted via `ILocalSettingsService`. *(Ref: Master Task 17)* | **Persona: Frontend Engineer** — verified 2026-09-06: `UrgencyAllotmentEditorViewModel` (Settings, child VM) loads the real request-type catalog's sub-types (via `IRequestSubtypeNameReadService`), reads each sub-type's minutes from `UrgencySettingsService` (default 30 when unset), and persists each row's minutes on change. A **Max Allotted Time** `Expander` card (Operations category) lists each sub-type with a minutes `NumberBox` (bound to `UrgencyAllotments.Items`). `SettingsViewModel` exposes `UrgencyAllotments` + `IsUrgencyAllotmentsPanelVisible` (folded into the Operations category). Build clean; full suite green (506 passed).
- [x] **Auth Logic: role-gate editing to Plant Manager and above** matching the existing manage-settings pattern. *(Ref: Master Task 17)* | **Persona: Backend Engineer** — verified 2026-09-06: `UrgencyAllotmentEditorViewModel.CanManageUrgencySettings` allows Admin/Developer/Plant Manager; the card's `NumberBox` `IsEnabled` binds to it, and the persist path no-ops for non-managers.
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for defaults, edit/save, and role gating**, then run the **full suite**; must pass. | **Persona: QA Engineer** — verified 2026-09-06: `UrgencySettingsServiceTests` (4: default 30, set/get round-trip, clamp, fallback) + `UrgencyAllotmentEditorViewModelTests` (4: PM+ role gate, load with stored+default minutes, editing persists under the subtype key, empty-catalog status). Full suite green (506 passed).

## Subphase 0.2 — Urgency deadlines + ordering

- [x] **Service Layer: compute due = created + sub-type max allotted; remaining = due − now; overdue when remaining < 0**, aligning with `TargetTimeUtc`/`IsOverdue`/remaining-time fields. *(Ref: Master Task 18)* | **Persona: Backend Engineer** — verified 2026-09-06: the compute is `UrgencyCalculator.Compute` + `UrgencyDeadlineService.ComputeAsync` (resolves subtype max-allotted from `UrgencySettingsService`). `WaitlistRequestService.SubmitAsync` now derives `TargetTimeUtc` (= created + subtype max-allotted) when a draft supplies none (applies to both mock-ON and production; explicit deadlines preserved). `WaitlistRequestServiceTests` +2 (derive ~30-min deadline when absent; preserve explicit deadline). Full suite green (514 passed).
- [x] **Workflow: order the available/handler list most-urgent-first** (least remaining / overdue first). *(Ref: Master Task 18)* | **Persona: Frontend Engineer** — ordering helper (`UrgencyCalculator.OrderMostUrgentFirst`) exists; wiring it into the Waitlist list VM is the remaining view-layer step. — **DELIVERED by `specs/003` US3:** `UrgencyCalculator.OrderBy` defaults to most-urgent and `WaitlistViewViewModel.SortOrder` resolves the remembered key with `WaitlistSortOrder.MostUrgent` as the default, so the list leads with the most urgent work without the user choosing it.
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for due/remaining/overdue math and list ordering**, then run the **full suite**; must pass. | **Persona: QA Engineer** — **delivered by `specs/003` US3** (the ordering default and the due/remaining/overdue math are covered by its tests).

**GATE: per-subtype max times settable (PM+), urgency computed correctly, list orders by urgency, full suite green. Move to `09-Phase2-alerts.md`.**

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-card-statuses.svg` — remaining-time (urgency) color states
- `../Mockups/settings-allotment-subtypes.svg` — max allotted time per sub-type editor

> **Note (2026-09-20):** `WeekendProject/Mockups/` **is not in the tree**. These were design-time references, not
> instructions. Treat them as provenance; do not go looking for the folder, and do not re-create it to satisfy this
> list.
