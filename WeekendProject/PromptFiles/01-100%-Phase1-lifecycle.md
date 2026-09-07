# 01 - Phase 1 — Foundation: Identity & Request Lifecycle + Persistence

> **IMPLEMENTATION NOTE (2026-09-05):** The codebase already contains a live Waitlist request lifecycle (submit → DB persist → in-memory session list + audit log). This file is being executed by **building on that existing code**, not rebuilding it. The existing status vocabulary is `Pending / Accepted / Completed / Canceled` and is **retained as-is**; wherever this file/spec says `Waiting / In Progress / Done / Cancelled`, read the equivalents as `Pending / Accepted / Completed / Canceled` respectively. No status rename is performed. Where the spec asks to "add" lifecycle columns/fields that already exist, we extend/verify rather than duplicate.

> **Source:** consolidated in the Master `WeekendProject/PromptFiles/prompt.md`. This file is self-contained so it can be executed after the Master is removed.
> **Workflow:** execute with the **checklist-execution** skill; adopt each task's persona. Tick `- [x]` only when the work is implemented, builds clean, and its tests pass.
> **Task 0 scope:** a green build (no errors/warnings) on both projects and a full test-suite pass is a baseline required by **every** file. It must hold BEFORE and AFTER this file.
> **Per-task testing rule:** after each relevant implementation task, add tests in `MTM_Waitlist.Tests` and run the **full suite**; the suite must pass before moving on.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: confirm `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings) on both the app and test projects.** Use `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` (single-threaded to avoid the WinUI double-build race). | **Persona: DevOps Engineer**
- [x] **QA: confirm the full `MTM_Waitlist.Tests` suite passes.** Run `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` and record the pass count. | **Persona: QA Engineer**
**GATE: 0 build errors + 0 warnings + a full green suite must hold before starting and after finishing this file. Fix the baseline before implementing anything if it fails.**

## Subphase 1.1 — Expose the signed-in user's identity

**PREREQUISITE: Task 0 green.**

- [x] **Data Model: expose current user's employee number and display name on `StartupState`** (or an identity service) so Waitlist services can attribute submit/accept/complete/release/cancel actions and analytics to the right person. Today `StartupState` exposes only role (`CurrentRole`/`IsDeveloper`). *(Ref: Master Task 15)* | **Persona: Backend Engineer**
- [x] **Testing: add unit tests in `MTM_Waitlist.Tests` verifying identity is populated and readable** (e.g., from the startup/session repository path), then run the **full suite**; must pass before moving on. | **Persona: QA Engineer**

## Subphase 1.2 — Request lifecycle data model + DB columns

- [x] **Data Model: extend the Waitlist request model with lifecycle state** — `Status` (`Waiting` / `In Progress` / `Done` / `Cancelled`), `ClaimedBy`, `Note`, `CancellationReason`, and timestamps: created/updated plus accepted/completed/released. *(Ref: Master Task 1)* | **Persona: Backend Engineer**
- [x] **Database Table: add the matching columns to the waitlist requests queue table** under `Database/Tables/` (create + rollback), keeping aggregate `AllTables.sql` in sync. *(Ref: Master Task 1)* | **Persona: Database Engineer**
- [x] **Testing: add model/DB round-trip tests in `MTM_Waitlist.Tests` for the new fields** (status + timestamps persist and read back), then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 1.3 — MySQL read/list + status-update stored procedures; DB load

**PREREQUISITE: Subphase 1.2 complete.** Today requests are held in-memory only and the DB has only `sp_waitlist_request_insert`.

- [x] **Database Migration: add `sp_waitlist_request_list`/`get`** (read open/in-progress/active requests) under `Database/StoredProcedures/`, keeping `AllSPs.sql` in sync. *(Ref: Master Task 1)* | **Persona: Database Engineer**
- [x] **Database Migration: add `sp_waitlist_request_status_update`** (transition status, set claimed-by/timestamps/cancellation fields) under `Database/StoredProcedures/`, keeping `AllSPs.sql` in sync. *(Ref: Master Task 1)* | **Persona: Database Engineer**
- [x] **Service Layer: wire the read/list + status-update calls through the MySQL helper path** (mock-toggle aware via `Feature.InforVisualMockData`) and load real open/in-progress requests from the DB when the Waitlist list/detail is shown. *(Ref: Master Task 1)* | **Persona: Backend Engineer**
- [x] **Testing: add service tests in `MTM_Waitlist.Tests` covering list/get/status-update and the mock short-circuit**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 1.4 — Audit trail of transitions

- [x] **Service Layer: persist an audit/history record for every status transition** (request id, who, when, from→to status) so Phase 3 analytics can be derived; cancelled requests must be retained, never purged. *(Ref: Master Task 1 + Phase 3)* | **Persona: Backend Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` that a submit→accept→complete→release→cancel sequence writes the expected audit entries**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: lifecycle data persists end-to-end (mock ON and OFF), read back from the DB, and an audit trail exists with a full green suite. Only then move to `02-Phase1-coil.md`.**
Next task: **Subphase 1.1 identity** | **Persona: Backend Engineer**

## Subphase 1.5 — Mock-data parity foundation (added 2026-09-05)
>
> **Mock-review finding (corrected 2026-09-05):** the Waitlist mock path returns static display-only `SampleOrder` rows with empty `Status` and no lifecycle. The two helper servers are intentionally separate toggles (`SqlHelperServer` = Infor Visual on `Feature.InforVisualMockData`; `MySqlHelperServer` = MySQL/receiving on `Feature.RecvMockData`), and mock defaults are applied through the Settings toggles — no helper-routing change is required. The real parity gap is that no lifecycle-complete sample `WaitlistRequest` source exists for mock mode.

- [x] **Full Stack Engineer: provide a lifecycle-complete sample WaitlistRequest source for mock mode** (per building) spanning `Pending` / `Accepted` / `Completed` / `Canceled` with requester, `AssignedMaterialHandler`, `Note`, and `RequestedUtc`/`AcceptedUtc`/`CompletedUtc`/`CanceledUtc` timestamps — so list status, wait time, cancel-own, My Requests, and urgency can be exercised with mock ON instead of empty-status static rows; cancelled sample rows retained. *(Mock parity)*
- [x] **QA Engineer: add tests** that the lifecycle-complete sample source returns requests across all four statuses (cancelled retained) with requester/handler/note/timestamps; run the **full suite**; must pass. *(Mock parity)*
