# 04 - Phase 1 — Requester: Cancel Own Request & "My Requests" View

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 9, 10). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — requester actions & quick view)

- A requester may cancel **their own** request only **before it is handled**. Provide a clear, low-emphasis **Cancel** action on the request card/detail visible to the request's creator when status is `Waiting`. Use a confirmation dialog (`ContentDialog`) before cancelling. Keep the **"My Requests"** view as a filter/section (a toggle or segmented control) over the Waitlist list showing only the signed-in user's submitted requests with their status. Terminology: a requester **Cancel** records a `Cancelled` status; it is distinct from a handler **Release** (file `07`). *(Ref: Master Task 9/10)*

## Subphase 0.1 — Requester can cancel their own request

**PREREQUISITE: Task 0 green + lifecycle/identity (file `01`) available.**

- [x] **Workflow: allow a worker to cancel their own request before it is handled** (creator identity from file `01`; status `Waiting` only); a cancelled request persists to the MySQL DB with status `Cancelled` (via file `01` update path) — never merely removed from memory. *(Ref: Master Task 9)* | **Persona: Full Stack Engineer**
- [x] **Auth Logic: gate the Cancel action to the request's creator and to the `Waiting` state only** (no cancelling others' or in-progress requests). *(Ref: Master Task 9)* | **Persona: Backend Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` covering cancel-own success, cancel-others denied, cancel-after-accept denied, and DB persistence of the cancelled row**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — "My Requests" quick view

- [x] **Workflow: add a "My Requests" quick view (filter/section) of requests the current user submitted**, each showing its status; localize all new strings via the `.resw` convention. *(Ref: Master Task 10)* | **Persona: Frontend Engineer**
- [x] **Testing: add view-model tests in `MTM_Waitlist.Tests` that "My Requests" filters to the signed-in user and shows status**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: requester can cancel own (recorded), others/in-progress blocked, My Requests filter works, localized, full suite green. Move to `05-Phase1-locignore.md`.**
Next task: **Subphase 0.1 requester cancel-own** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Mock/session display rows hard-code `RequestedByName = "Current user"` and carry no distinct requester identity, so cancel-own (creator-only) and the "My Requests" filter have nothing to key off in mock mode.

- [x] **Backend/Full Stack Engineer: attribute mock requests to real requesters** — mock and session rows carry the requester's employee number/name (from the signed-in identity), and the sample set includes requests from other requesters plus the signed-in user, so cancel-own and My Requests are exercisable with mock ON. *(Mock parity)*
- [x] **QA Engineer: add tests** that My Requests filters to the signed-in user and cancel-own is blocked for non-creators in mock; run the **full suite**. *(Mock parity)*

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-card-cancelown.svg` — requester Cancel on their own Waiting request
- `../Mockups/waitlist-list-myrequests.svg` — "My Requests" filter
