# 08 - Phase 2 — Urgency: Max-Allotted Time per Subtype + Ordering

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 17, 18). Self-contained. Builds on timestamps (file `01`) and wait time (file `03`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline
- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — urgency readout & ordering)
- Show remaining time on cards (already present via `RemainingTimeText`), color-coded: normal (neutral), near-due (amber), **overdue (red)**. Urgency = **due − now**, where **due = created time + the sub-type's max allotted time**; overdue when negative. The handler/available list orders **most-urgent-first**. The **max allotted time** is a Plant Manager+ settable value **per request sub-type**, edited on the Settings screen as a small grid/list of sub-types with a numeric minutes/seconds control. Persist via `ILocalSettingsService`; provide sensible defaults; localize.

## Subphase 0.1 — Max allotted time per sub-type (Plant Manager+)
**PREREQUISITE: Task 0 green.**
- [x] **Settings Card: add a Plant Manager+ (and above) setting that stores a max allotted time for each request sub-type** (e.g. each Coil subtype, each Pickup subtype), with sensible defaults, persisted via `ILocalSettingsService`. *(Ref: Master Task 17)* | **Persona: Frontend Engineer** — verified 2026-09-06: `UrgencyAllotmentEditorViewModel` (Settings, child VM) loads the real request-type catalog's sub-types (via `IRequestTypeEditorService`), reads each sub-type's minutes from `UrgencySettingsService` (default 30 when unset), and persists each row's minutes on change. A **Max Allotted Time** `Expander` card (Operations category) lists each sub-type with a minutes `NumberBox` (bound to `UrgencyAllotments.Items`). `SettingsViewModel` exposes `UrgencyAllotments` + `IsUrgencyAllotmentsPanelVisible` (folded into the Operations category). Build clean; full suite green (506 passed).
- [x] **Auth Logic: role-gate editing to Plant Manager and above** matching the existing manage-settings pattern. *(Ref: Master Task 17)* | **Persona: Backend Engineer** — verified 2026-09-06: `UrgencyAllotmentEditorViewModel.CanManageUrgencySettings` allows Admin/Developer/Plant Manager; the card's `NumberBox` `IsEnabled` binds to it, and the persist path no-ops for non-managers.
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for defaults, edit/save, and role gating**, then run the **full suite**; must pass. | **Persona: QA Engineer** — verified 2026-09-06: `UrgencySettingsServiceTests` (4: default 30, set/get round-trip, clamp, fallback) + `UrgencyAllotmentEditorViewModelTests` (4: PM+ role gate, load with stored+default minutes, editing persists under the subtype key, empty-catalog status). Full suite green (506 passed).

## Subphase 0.2 — Urgency deadlines + ordering
- [x] **Service Layer: compute due = created + sub-type max allotted; remaining = due − now; overdue when remaining < 0**, aligning with `TargetTimeUtc`/`IsOverdue`/remaining-time fields. *(Ref: Master Task 18)* | **Persona: Backend Engineer** — verified 2026-09-06: the compute is `UrgencyCalculator.Compute` + `UrgencyDeadlineService.ComputeAsync` (resolves subtype max-allotted from `UrgencySettingsService`). `WaitlistRequestService.SubmitAsync` now derives `TargetTimeUtc` (= created + subtype max-allotted) when a draft supplies none (applies to both mock-ON and production; explicit deadlines preserved). `WaitlistRequestServiceTests` +2 (derive ~30-min deadline when absent; preserve explicit deadline). Full suite green (514 passed).
- [ ] **Workflow: order the available/handler list most-urgent-first** (least remaining / overdue first). *(Ref: Master Task 18)* | **Persona: Frontend Engineer** — ordering helper (`UrgencyCalculator.OrderMostUrgentFirst`) exists; wiring it into the Waitlist list VM is the remaining view-layer step.
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` for due/remaining/overdue math and list ordering**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: per-subtype max times settable (PM+), urgency computed correctly, list orders by urgency, full suite green. Move to `09-Phase2-alerts.md`.**
Next task: **Subphase 0.1 max-allotted setting** | **Persona: Frontend Engineer**

## Mock-data flow coverage (added 2026-09-05)
> Mock rows carry only a static remaining-time string (e.g. "00:27"); they expose no `CreatedUtc`/`TargetTimeUtc`, so due/remaining/overdue math and urgency ordering cannot be tested with mock ON.
- [ ] **Service Layer/VM: mock request rows expose requested + target (due) times and apply the per-subtype max-allotted defaults** so remaining/overdue and most-urgent-first ordering work in mock mode (drop reliance on the static sample remaining text). *(Mock parity)* — DATA ALREADY PRESENT 2026-09-06: `SampleWaitlistRequestCatalog` rows already set `RequestedUtc` + `TargetTimeUtc` (incl. an active overdue row), so the mock data carries the times needed; applying per-subtype defaults on submit/load is part of the Subphase 0.2 Service Layer wiring.
- [x] **QA Engineer: add tests** for due/remaining/overdue math and urgency ordering using the mock rows; run the **full suite**. *(Mock parity)* — verified 2026-09-06: `UrgencyMockParityTests` (3) — active sample rows expose RequestedUtc/TargetTimeUtc, an `IsOverdue` row has a past-due target, and `UrgencyCalculator.OrderMostUrgentFirst` orders overdue before on-time over the sample rows. Full suite green (512 passed).

## Mockups (UI references)
See `../Mockups/` for the target UI for this task:
- `../Mockups/waitlist-card-statuses.svg` — remaining-time (urgency) color states
- `../Mockups/settings-allotment-subtypes.svg` — max allotted time per sub-type editor
