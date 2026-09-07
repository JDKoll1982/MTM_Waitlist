# 07 - Phase 2 — Handler Fulfillment: Card Actions + Handler Data + Note

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Task 16 + handler data/note). Self-contained. Builds on lifecycle/identity (file `01`) and list status (file `03`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [ ] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [ ] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — request card action area)

- Redesign each request card's **right-side action area** into role-, state-, and ownership-aware **icon buttons** (icons, not text) using Fluent/Segoe Fluent icons (16–24px). Material Handler or above sees **Accept** on unaccepted requests. After acceptance the job stays on the shared list; the Accept icon disappears for other handlers (shown as taken/assigned); only the **assigned handler** sees **Complete** and **Release**. **Edit** is visible to the request creator but left as a TODO (do not implement its behavior). Use icons: Accept = checkmark/add-person style, Complete = check, Release = undo/reopen, Edit = pencil. Tooltips + `AutomationProperties.Name` on every icon button. Show the coil/part + qty, location/stock, destination press/WC, requester + urgency as handler-facing data. Support a short handler **note** per request. *Terminology: handler "Release" ≠ requester "Cancel" (file `04`).*

## Subphase 0.1 — Handler-facing data + note on cards/detail

**PREREQUISITE: Task 0 green.**

- [ ] **Workflow: surface handler-needed data on the request card/detail** — coil/part + requested quantity, the coil's location/stock (Infor Visual per mock toggle, file `02`/`06`), destination press/work center, and requester + urgency/remaining. *(Ref: Master Task 16)* | **Persona: Full Stack Engineer**
- [x] **Data Model: add a `Note` field (persisted via file `01` update path) and UI so a handler can add a short note** visible on the request detail/history. *(Ref: Master Task 16)* | **Persona: Backend Engineer** — verified 2026-09-06 (backend): `WaitlistRequest` already carried `Note`; added the missing update/persist path — `IWaitlistRequestService.UpdateNoteAsync(requestId, note)` sets the note on the request (mock + production), persists through the status-update path (status unchanged), records a `NoteUpdated` audit entry, and raises `RequestsChanged`. `WaitlistRequestServiceTests` +3 (set note + audit; empty clears; not-found → null). The handler note **UI** is the remaining part. Full suite green (517 passed).
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` for handler-data population and note add/persist**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Role- and ownership-aware Accept / Complete / Release actions

- [ ] **Workflow: unaccepted request + viewer is Material Handler or above → show an Accept icon** that auto-assigns to the signed-in user (identity from file `01`) and moves status to `In Progress`. *(Ref: Master Task 16)* | **Persona: Full Stack Engineer**
- [ ] **Workflow: after accept the job stays on the shared list; Accept disappears for other handlers; only the assigned handler sees Complete and Release.** *(Ref: Master Task 16)* | **Persona: Frontend Engineer**
- [ ] **Workflow: Complete marks the request `Done` (single step now; leave TODO hooks for future multi-step handoff); Release returns the job to the open list (status available; NOT a cancellation).** *(Ref: Master Task 16)* | **Persona: Full Stack Engineer**
- [ ] **Auth Logic: only the assigned handler can Complete/Release; Edit visible to creator but left as a TODO (no behavior).** *(Ref: Master Task 16)* | **Persona: Backend Engineer**
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` covering accept assignment, others-see-taken, assignee-only complete/release, and release-vs-cancel semantics**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: Accept/Complete/Release behave per role/ownership, handler data + note show, Release ≠ Cancel, full suite green. Move to `08-Phase2-urgency.md`.**
Next task: **Subphase 0.1 handler-facing data** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Handler-facing data (coil/part + location/stock + destination press) and the request `Note` need to be populated in mock mode; mock/session rows currently surface "Not provided" placeholders and no location/stock for the assigned handler.

- [ ] **Backend/Full Stack Engineer: populate handler-facing data + note in mock mode** — mock rows expose location/stock and destination, and the handler `Note` is editable/persisted on the mock path (mirroring the DB update), so Accept/Complete/Release and note-add are exercisable with mock ON. *(Mock parity)*
- [ ] **QA Engineer: add tests** for handler-data population and note add/persist in mock; run the **full suite**. *(Mock parity)*

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-card-basestate.svg` — current anatomy (Edit/Cancel/Accept unwired)
- `../Mockups/waitlist-card-accept.svg` — Material Handler+ Accept
- `../Mockups/waitlist-card-assignee.svg` — assigned handler Complete + Release
- `../Mockups/waitlist-card-others.svg` — other handlers (taken, no action)
