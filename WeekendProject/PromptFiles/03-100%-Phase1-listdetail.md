# 03 - Phase 1 — Waitlist List & Detail: Status, Wait Time, Non-Blank Detail

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Tasks 5, 6, 7, 14). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — Waitlist list cards & detail page)

- List cards use the per-type line views wrapping `WaitlistLineCardView`; show a **status badge**, the request title, requester, requesting press, remaining/urgency, and 5 field rows. Keep Fluent spacing (8px grid; 12px between cards), `Segoe UI Variable` type ramp, and readable color contrast. Status colors (theme-aware): `Waiting` neutral/accent blue, `In Progress` amber, `Done` green, `Cancelled` grey — use accessible tinted pills, not harsh fills. Detail page renders hero (work-center image, title, requested-by, press, remaining time) + type-based template sections in rounded cards. Empty states must be friendly messages with a refresh action, never a blank area. *(Ref: Master Task 6/7/14)*

> **VERIFIED CARD ANATOMY (2026-09-06, user-approved; DO NOT REGRESS):** The shared `WaitlistLineCardView.xaml` shell is laid out as a spreadsheet-style grid: (a) type/subtype **image is a FIXED 96×96 centered square** (do NOT stretch it full card height); (b) **title sits on its own top row**; (c) below it a content row splits into a **4-row label/value metadata grid** (Requested by / Press / Remaining time / Waiting, labels left, values right, aligned) on the left and the **per-type detail-field grid** on the right; (d) the **action buttons (Edit/Cancel/Accept) are vertically centered** on the right; (e) the **status pill is COMPACT** (fixed ~36px height), below the buttons, spanning the combined button width — do NOT make it fill remaining card height. Each per-type `*WaitlistLineView.xaml` `DetailsContent` renders its fields as a **2×3 four-column label/value grid** (`Auto/*/Auto/*` row pairs: Field0|Field1, Field2|Field3, Field4 spanning col1-3) — Coil uses the SAME grid pattern as the other types (do NOT swap it back to a single `ItemsControl` column). Overdue "Remaining time" is bold red. *(Ref: Master Task 6/7/14)*

## Subphase 0.1 — Detail page must not be blank for real Coil requests

**PREREQUISITE: Task 0 green + lifecycle data (file `01`) is available.**

- [x] **Service Layer/VM: `WaitlistViewDetailViewModel.OnNavigatedTo` must resolve real/live submitted requests**, not only `SampleDataService.GetSampleOrders` rows (live rows currently return null → blank page). *(Ref: Master Task 5)* | **Persona: Full Stack Engineer**
- [x] **Workflow: fix subtype/`ImagePath` routing so a Coil request renders Coil sections/data** (`Pickup Coil` currently maps to `pickup_wip.png` and renders WIP). *(Ref: Master Task 5)* | **Persona: Full Stack Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` that a real submitted Coil request resolves an Item and produces non-empty Coil template sections**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.2 — Show request status on the Waitlist list

- [x] **Workflow: show each request's status (`Waiting`/`In Progress`/`Done`/`Cancelled`) on its list card** as a badge/pill (see UI guidance), from the lifecycle status added in file `01`. *(Ref: Master Task 6)* | **Persona: Frontend Engineer**
- [x] **Testing: add view tests in `MTM_Waitlist.Tests` that each status renders the correct badge state**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.3 — Show how long each request has been waiting

- [x] **Workflow: display how long each request has been waiting (age from its created timestamp)** on the list so leads can prioritize the oldest. *(Ref: Master Task 7)* | **Persona: Full Stack Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for age formatting/ordering correctness**, then run the **full suite**; must pass. | **Persona: QA Engineer**

## Subphase 0.4 — Friendly empty/blank states

- [x] **Workflow: across the Waitlist detail page, and any list in this scope, show a helpful localized message (and a refresh action where appropriate) instead of a blank area** when no data/coil is found. *(Ref: Master Task 14)* | **Persona: Frontend Engineer**
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for empty-state message selection**, then run the **full suite**; must pass. | **Persona: QA Engineer**

**GATE: real Coil detail renders non-blank, status + wait time show, empty states are friendly, full suite green. Move to `04-Phase1-requester.md`.**
Next task: **Subphase 0.1 real-request resolution** | **Persona: Full Stack Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> Mock sample rows are static display `SampleOrder`s with empty `Status` and no `CreatedUtc`, so the status badge (Task 6) and wait-time/age (Task 7) cannot render for mock rows, and a mock-submitted request opens a blank detail because the detail resolves only from `GetSampleOrders(...)`.

- [x] **Full Stack Engineer: make the mock list data lifecycle-capable** — sample rows (and mock-submitted requests) expose `Status` and a `CreatedUtc` so the status badge and wait-time render with mock ON; provide sample rows across `Pending`/`Accepted`/`Completed`/`Canceled`. *(Mock parity)*
- [x] **QA Engineer: add tests** that a mock-ON list renders the status badge and computed wait time, and that a real/mock submitted request resolves a non-blank detail Item; run the **full suite**. *(Mock parity)*

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/waitlist-card-basestate.svg` — current request-card anatomy (WaitlistLineCardView)
- `../Mockups/waitlist-card-statuses.svg` — status chip + remaining-time colors
- `../Mockups/waitlist-detail-coilfields.svg` — non-blank Coil detail page
- `../Mockups/waitlist-detail-emptystate.svg` — friendly empty state
