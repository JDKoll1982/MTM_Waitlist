# 07 - Phase 2 — Handler Fulfillment: Card Actions + Handler Data + Note

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Task 16 + handler data/note). Self-contained. Builds on lifecycle/identity (file `01`) and list status (file `03`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

> ### ✅ DELIVERED by `specs/003-waitlist-handler-fulfilment` (reconciled 2026-09-20)
> This file was that spec's **seed**, and the spec shipped all of it. Every box below that was real work is
> therefore ticked, with the user story that delivered it named inline. Two boxes in the *Mock-data flow coverage*
> section have been **deleted**: they asked for handler data and notes on the **mock** path, and `specs/001` FR-003 /
> FR-014 and constitution II forbid reintroducing a demo/mock mode, so they can never be built. Do not reopen them.
>
> Mapping — `specs/003` US1 *A handler takes a request* (Accept), US2 *The assigned handler finishes or hands back
> the work* (Complete / Release), US5 *A handler leaves a short note, and the history is readable*.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer** — executed as `specs/003`'s baseline; the repo has built 0 warnings / 0 errors throughout that spec.
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer** — executed as `specs/003`'s baseline; 49/49 tasks closed on a green suite.
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — request card action area)

- Redesign each request card's **right-side action area** into role-, state-, and ownership-aware **icon buttons** (icons, not text) using Fluent/Segoe Fluent icons (16–24px). Material Handler or above sees **Accept** on unaccepted requests. After acceptance the job stays on the shared list; the Accept icon disappears for other handlers (shown as taken/assigned); only the **assigned handler** sees **Complete** and **Release**. **Edit** is visible to the request creator but left as a TODO (do not implement its behavior). Use icons: Accept = checkmark/add-person style, Complete = check, Release = undo/reopen, Edit = pencil. Tooltips + `AutomationProperties.Name` on every icon button. Show the coil/part + qty, location/stock, destination press/WC, requester + urgency as handler-facing data. Support a short handler **note** per request. *Terminology: handler "Release" ≠ requester "Cancel" (file `04`).*

## Subphase 0.1 — Handler-facing data + note on cards/detail

**PREREQUISITE: Task 0 green.**

- [x] **Workflow: surface handler-needed data on the request card/detail** — coil/part + requested quantity, the coil's location/stock (Infor Visual per mock toggle, file `02`/`06`), destination press/work center, and requester + urgency/remaining. *(Ref: Master Task 16)* | **Persona: Full Stack Engineer** — **delivered by `specs/003`** (US1/US2: the card and detail surface carry part + quantity, location/stock, destination work center and requester + urgency).
- [x] **Data Model: add a `Note` field (persisted via file `01` update path) and UI so a handler can add a short note** visible on the request detail/history. *(Ref: Master Task 16)* | **Persona: Backend Engineer** — verified 2026-09-06 (backend): `WaitlistRequest` already carried `Note`; added the missing update/persist path — `IWaitlistRequestService.UpdateNoteAsync(requestId, note)` sets the note on the request (mock + production), persists through the status-update path (status unchanged), records a `NoteUpdated` audit entry, and raises `RequestsChanged`. `WaitlistRequestServiceTests` +3 (set note + audit; empty clears; not-found → null). The handler note **UI** is the remaining part. Full suite green (517 passed). — **UI delivered by `specs/003` US5** (the note editor and the readable history landed there), so this box is now complete.
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for handler-data population and note add/persist**, then run the **full suite**; must pass. | **Persona: QA Engineer** — **delivered by `specs/003`** (US5 covers note add/persist and history rendering; the suite is green).

## Subphase 0.2 — Role- and ownership-aware Accept / Complete / Release actions

- [x] **Workflow: unaccepted request + viewer is Material Handler or above → show an Accept icon** that auto-assigns to the signed-in user (identity from file `01`) and moves status to `In Progress`. *(Ref: Master Task 16)* | **Persona: Full Stack Engineer** — **delivered by `specs/003` US1.**
- [x] **Workflow: after accept the job stays on the shared list; Accept disappears for other handlers; only the assigned handler sees Complete and Release.** *(Ref: Master Task 16)* | **Persona: Frontend Engineer** — **delivered by `specs/003` US1/US2.**
- [x] **Workflow: Complete marks the request `Done` (single step now; leave TODO hooks for future multi-step handoff); Release returns the job to the open list (status available; NOT a cancellation).** *(Ref: Master Task 16)* | **Persona: Full Stack Engineer** — **delivered by `specs/003` US2.**
- [x] **Auth Logic: only the assigned handler can Complete/Release; Edit visible to creator but left as a TODO (no behavior).** *(Ref: Master Task 16)* | **Persona: Backend Engineer** — **delivered by `specs/003` US2.**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` covering accept assignment, others-see-taken, assignee-only complete/release, and release-vs-cancel semantics**, then run the **full suite**; must pass. | **Persona: QA Engineer** — **delivered by `specs/003`** (US1/US2/US4 cover exactly these four behaviours).

**GATE: Accept/Complete/Release behave per role/ownership, handler data + note show, Release ≠ Cancel, full suite green. Move to `08-Phase2-urgency.md`.**

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-card-basestate.svg` — current anatomy (Edit/Cancel/Accept unwired)
- `../Mockups/waitlist-card-accept.svg` — Material Handler+ Accept
- `../Mockups/waitlist-card-assignee.svg` — assigned handler Complete + Release
- `../Mockups/waitlist-card-others.svg` — other handlers (taken, no action)

> **Note (2026-09-20):** `WeekendProject/Mockups/` **is not in the tree**. These were design-time references, not
> instructions — they describe a target that has since been built and superseded by `specs/004`. Treat them as
> provenance; do not go looking for the folder, and do not re-create it to satisfy this list.
