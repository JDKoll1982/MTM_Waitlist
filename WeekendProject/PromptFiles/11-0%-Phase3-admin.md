# 11 - Phase 3 — Administration: Cancelled-Request Monitor + Retention

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 22, 23). Self-contained. Builds on retained cancelled records (file `01`/`04`) and analytics data (file `10`).
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline
- [ ] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [ ] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — admin monitor)
- Provide a role-gated (Developer/admin-level) list/tabular view of **cancelled requests over time** (who, when, type/subtype, reason). Keep it a simple sortable table consistent with file `06`'s grid guidance. This is a separate **administrative** view from the Plant Manager operational analytics (file `10`). Register via `PageService`/DI/shell with the role gate; localize.

## Subphase 0.1 — Admin monitor of cancelled requests
**PREREQUISITE: Task 0 green.**
- [ ] **Workflow/VM: add a role-gated (Developer/admin-level) view monitoring cancelled requests** — who cancelled, when, request type/subtype, reason — from retained cancelled records (never purged). *(Ref: Master Task 22)* | **Persona: Full Stack Engineer**
- [ ] **Auth Logic: role-gate the monitor**; register page + view model via `PageService`/DI and add to shell navigation. *(Ref: Master Task 22)* | **Persona: Backend Engineer**
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` that the monitor lists retained cancellations with correct fields and enforces the role gate**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Resolved-request retention & archival
- [ ] **Service Layer: honor `waitlist.resolved_retention_days` (default 90)** — resolved (`Done`/`Cancelled`) requests older than the window are hidden from the active Waitlist list but preserved (not deleted) for analytics/admin. *(Ref: Master Task 23)* | **Persona: Backend Engineer**
- [ ] **Workflow: add active-list filtering so aged resolved requests do not appear**, plus a housekeeping/archival routine; localize UI strings. *(Ref: Master Task 23)* | **Persona: Full Stack Engineer**
- [ ] **Testing: add tests in `MTM_Waitlist.Tests` for retention-window filtering and archival**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: admin monitor lists retained cancellations (role-gated); resolved requests aged past retention are hidden from the active list but preserved; full suite green. Phase 3 complete.**
Next task: **Subphase 0.1 admin monitor** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
> Retention/archival hides resolved rows older than the window from the active list but preserves them. Mock sample rows must include an aged resolved row to verify the retention filter in mock mode.
- [ ] **Full Stack Engineer: include an aged resolved sample request** (`Done`/`Cancelled` older than the retention window) so the active-list retention filter is verifiable with mock ON, and confirm mock rows are preserved (not deleted). *(Mock parity)*
- [ ] **QA Engineer: add tests** that an aged resolved mock row is hidden from the active list but retained; run the **full suite**. *(Mock parity)*

## Mockups (UI references)
See `../Mockups/` for the target UI for this task:
- `../Mockups/admin-cancellations-monitor.svg` — cancelled-requests admin monitor
