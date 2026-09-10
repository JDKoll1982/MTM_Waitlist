# Waitlist / Coil Inventory Location Changes (MASTER — validation reference)

> **STATUS (2026-09-05):** This file is the **master requirements reference**. The executable work is split into **per-task checklist prompt files** below (named `{step}-{Phase}-{shortname}.md`, each using the repo checklist workflow with a Task 0 build/test baseline and per-task test sub-tasks in `MTM_Waitlist.Tests`). **Use the per-task files to implement.** Keep this master to validate that nothing is missed, then remove it once coverage is confirmed.

## Per-Task Prompt Files (executable checklists — implement in this order)
| File | Phase | Covers (master tasks) |
|---|---|---|
| `01-100%-Phase1-lifecycle.md` | 1 | identity + lifecycle fields/SPs/audit (1, 15) |
| `02-100%-Phase1-coil.md` | 1 | coil gating/hide/display/up-front + mock (2, 3, 4) |
| `03-100%-Phase1-listdetail.md` | 1 | list status/wait + detail non-blank + empty states (5, 6, 7, 14) |
| `04-100%-Phase1-requester.md` | 1 | cancel-own + My Requests (9, 10) |
| `05-100%-Phase1-locignore.md` | 1 | ignored-locations settings + app-wide apply (11, 12) |
| `06-80%-Phase1-locgrid.md` | 1 | sortable location grid (13) |
| `07-8%-Phase2-fulfill.md` | 2 | handler data/note + Accept/Complete/Release/Edit (16) |
| `08-70%-Phase2-urgency.md` | 2 | max-allotted subtype + urgency/ordering (17, 18) |
| `09-100%-Phase2-alerts.md` | 2 | new-request toggle + deep-link (19) |
| `10-0%-Phase3-analytics.md` | 3 | stock snapshot + Plant Manager analytics screen, plus the Supervisor analytics extension (20, 21) |
| `11-0%-Phase3-admin.md` | 3 | admin cancelled monitor + retention (22, 23) |
| `12-71%-MockMasterData-DbDriven.md` | — | DB-driven mock master data |
| `13-63%-Developer-UI.md` | — | Developer UI |
| `14-57%-Phase2-UnifiedWaitlistCard.md` | 2 | unified waitlist card (earlier `14-*` copies at lower % are retained) |
| `15-0%-UserManagement.md` | — | Settings → Administration → User Management panel. **Standalone workstream — no master task numbers.** |

> The `NN-P%` prefix on each file name is that file's own completion percentage. The master task numbers in
> parentheses refer to this document's task list; `15` has none and is tracked independently.

> **CHECKBOX REQUIREMENT:** Every task below is a checklist item with a `- [ ]` checkbox in front of it. As this prompt is executed, mark each task `- [x]` **only** once it is fully implemented, builds, and is verified. Do not leave a task unchecked when it is done, and do not check one that is incomplete. Implement the tasks **in the order listed** (each order is chosen for dependencies), and only move to the next task after the current one is complete.

This change updates the Waitlist module codebase in the following projects:

- `MTM_Waitlist.Waitlist.Controls`
- `MTM_Waitlist.Waitlist.NewRequest`
- `MTM_Waitlist.Waitlist.View`
- XAML pages/controls in `Module_Waitlist\Views` and `Module_Waitlist\Controls` (compiled into the app)
- The Settings screen (`Module_Settings\Views\SettingsPage.xaml` + `MTM_Waitlist.Settings\ViewModels\SettingsViewModel.cs`)
- New Infor Visual SQL queue script under `Database\InforVisual\Queues\Module`

## Cross-Cutting Rule: Data Source Follows the Mock Toggle

For any task that pulls a coil or an inventory-location list from "the current work center" or "the Infor Visual database", the source depends on the existing `Feature.InforVisualMockData` setting (same routing as other helper flows):

- **When `Feature.InforVisualMockData` is ON** → short-circuit to the app's sample/mock data (`ISampleDataService` / Setup sample catalog) and do NOT hit the live database.
- **When `Feature.InforVisualMockData` is OFF** → query the live Infor Visual database via the helper/SQL-queue path.
Apply this rule to Tasks 2, 3, 4, 12, and 20.

> **MOCK-DATA PARITY (added 2026-09-05):** A review of the mock-data code/JSON found the mock path is not yet lifecycle/feature-parity with the DB work below — the Waitlist mock returns static empty-status `SampleOrder` rows, there is no Waitlist-level sample coil/inventory-location source, session rows hard-code "Current user", and the mock toggles are not seeded to the documented defaults (`InforVisual` ON / `Recv` OFF) while `MySqlHelperServer` gates an `MtmWaitlist` target on `RecvMockData`. Each per-file checklist (`01`-`11`) now carries a **"Mock-data flow coverage (added 2026-09-05)"** section of new steps to close these gaps (see file `01` Subphase 1.5 for the mock-parity foundation). Treat those steps as part of each task's scope.

## Shared Clarifications

- "A work center's job has a coil" = the active job associated with that work center resolves to a coil (per the data source above). There is currently no coil↔job link in the Waitlist path; coil is gated today by a hard-coded `hasCoilData: true` in `NewRequestJobTypeViewModel`.
- Localization: all new user-facing strings (settings panel labels, list headers, field labels, dialogs, empty states) must use the existing per-file `.resw` localization convention (`"Key".GetLocalized()` / `LocalizeOrDefault`), not literal text.
- Role gating for editing ignored locations = only users with a role **above Material Handler** (i.e. Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer), matching the existing manage-settings role-gate pattern.

## Tasks (implement in order; tick each `[ ]` to `[x]` when done)

Using `Module_Waitlist\Controls\Coil\CoilRequestTypeImageView.xaml` and `Module_Waitlist\Controls\Coil\CoilWaitlistLineView.xaml` as the reference UI, implement the following in this exact order:

- [ ] **1) Request lifecycle: add status/claim/timestamp fields, DB read/update SPs, and an audit trail (foundation).**
  Extend the Waitlist request model + persistence so a request carries lifecycle state that later tasks (5, 6, 7, 8, 9) can rely on. Add a status (`Waiting` / `In Progress` / `Done` / `Cancelled`), `ClaimedBy`, and created/updated timestamps (plus accepted/completed/released timestamps) to the request. Persist new requests through the existing MySQL stored-procedure/queue path (and the mock/sample path when the mock toggle is ON).
  **Close the persistence gap this implies:** today requests are held **in-memory only** and the DB only has `sp_waitlist_request_insert` (no read/list or status-update SP). Add MySQL read/list (`sp_waitlist_request_list`/`get`) and status-update (`sp_waitlist_request_status_update`) stored procedures, and load real open/in-progress requests from the DB when the Waitlist list/detail is shown (mock-toggle aware).
  In particular, **cancelled requests must save to the MySQL database properly** (do not lose/delete them) so they can be monitored later. Every status transition must write an **audit/history record** (who, when, from→to status, request id) so Phase 3 analytics can be derived. Add any DB migration/script needed for the new columns/records. This foundation must be in place before Tasks 5–9 and Phase 3 build on it.

- [ ] **2) Hide Coil during Request Selection when the job has no coil.**
  In `MTM_Waitlist.Waitlist.NewRequest`, when the current Work Center's active job does **not** have a coil, the **Coil** request type must not appear on the UI during Request Selection, and its **sub-choices (Coil subtypes, e.g. Pickup Coil)** must also be hidden/not reachable. Replace the hard-coded eligibility in `NewRequestJobTypeViewModel` (currently passes `hasCoilData: true`) with a real check against the active job's coil availability (respecting the mock-toggle rule).

- [ ] **3) Show the actual coil for the current work center.**
  Review all views in `Module_Waitlist\Views` and make sure the coil that is actually set/loaded for the current work center's active job is what is displayed — not a hard-coded or fabricated value. Currently several coil fields ("Requested coil", "Quantity in house", "Average coil weight", "Coil description") are placeholders (`"Not provided"`, `"Wrong coil"`) in `WaitlistViewViewModel.AddRequestFields`. Wire them to the real active-job coil data (per the mock-toggle rule).

- [ ] **4) Show the current coil up front on the Request screen.**
  On the request/Request-Selection screen (New Request flow), before the worker chooses a request type, clearly show the coil that is currently on/loaded for the selected work center's job (or show "none"). This gives the worker context for what they are requesting. Continue to **hide any request type that is not relevant to the current job** (e.g. keep the Coil type hidden when the job has no coil). Depends on Task 3's real coil data.

- [ ] **5) Waitlist detail page must not show a blank state for Coil requests.**
  Make `Module_Waitlist\Views\WaitlistViewDetailPage.xaml` (and `WaitlistViewDetailViewModel`) show relevant data for the **Coil** request type for **real/live submitted requests**, not only sample rows. Two known root causes to fix:
  - `OnNavigatedTo` currently resolves `Item` only from `SampleDataService.GetSampleOrders`, so live requests return `null` → the page renders empty. Resolve real requests too.
  - Subtype/`ImagePath` routing: `Pickup Coil` currently maps to `pickup_wip.png`, so it renders the WIP template instead of Coil. Correct the image/subtype routing so a Coil request renders Coil sections/data.
  The page should show relevant coil data (e.g. requested coil, quantity in house, coil description, average coil weight, work center/job, work-order info) rather than a blank/empty state.

- [ ] **6) Show request status on the Waitlist list.**
  Show each request's current status (`Waiting` / `In Progress` / `Done` / `Cancelled`) on its Waitlist list card so workers/leads can see at a glance whether it has been handled. Uses the status from Task 1.

- [ ] **7) Show how long each request has been waiting.**
  Display how long each request has been waiting (age based on its created timestamp from Task 1) on the Waitlist list, so leads can prioritize the oldest requests first.

- [ ] **8) Allow a handler to claim/take a request.**
  Let a handler "claim/take" a request (moves it to `In Progress`, records `ClaimedBy` via Task 1) so others see it is being handled and do not double-handle it. Show the claim state on the list card.

- [ ] **9) Allow a worker to cancel their own request.**
  Let a worker cancel their own request before it has been handled/picked up. A cancelled request must **persist to the MySQL database with status `Cancelled`** (per Task 1) so it can be monitored by the future admin analytics panel; do not merely remove it from the in-memory list.

- [ ] **10) Add a "My Requests" quick view.**
  Add a quick view (filter/section) of requests the current user submitted, showing each one's status (uses Task 1 data). Localize all new strings.

- [ ] **11) New Settings panel: ignored Infor Visual inventory locations.**
  Create a new settings section/panel for adding and removing **ignored Infor Visual inventory locations**. Add it as a **new section on the existing Settings screen** (`SettingsPage.xaml` + `SettingsViewModel` in `MTM_Waitlist.Settings`), following the existing section pattern and persisting through `ILocalSettingsService`.
  - Editing stays in **Settings only** (no inline editing on lists).
  - Default ignored locations include: **WC, NCM, V-WC, NCM-VITS, SHIP**.
  - Support add/remove of location codes; persist the saved list.
  - Role-gate editing to roles **above Material Handler** (see Shared Clarifications).
  - Localize all new strings.

- [ ] **12) Apply the ignore list app-wide.**
  Update all current lists that display locations across the app to **ignore / not show / not include in total counts** any location saved in the panel from Task 11. This includes (at minimum):
  - Waitlist / Coil screens — the "Quantity in house" totals and any coil-related location display paths in the Waitlist module.
  - Setup screen location lists (e.g. `SetupSubordinatePart` part/location grids in `MTM_Waitlist.Setup`).
  - Mock/sample-data-driven lists and totals — the ignore rule must be honored in the mock/sample path too.

- [ ] **13) Location list on the Waitlist detail page.**
  In `Module_Waitlist\Views\WaitlistViewDetailPage.xaml`, show a **list of inventory rows** present in the Infor Visual database (source per the mock-toggle rule), presented as a **sortable table/grid with columns: Part #, Location, Quantity** (sort by clicking a column header; no separate search box needed). Requirements:
  - Show only rows with **on-hand quantity of 1 or more** — drop rows where the quantity is `0`/`0.00` (e.g. if location `V-A0-00` only holds `MMC0001000 @ 0.00`, do not show that location/row).
  - **Ignore** any location saved in the settings panel from Task 11 (defaults WC, NCM, V-WC, NCM-VITS, SHIP).
  - Source the list from a new Infor Visual query (a checked-in SQL queue script under `Database\InforVisual\Queues\Module`, run through the helper path) when the mock toggle is OFF, or from sample/mock data when it is ON.
  - Localize all new UI strings for this list.

- [ ] **14) Friendly empty/blank states.**
  Across the affected screens (Waitlist detail page, the location list from Task 13, and the "My Requests" view from Task 10), when there is no data to show (no coil found, no matching locations, no requests), show a helpful localized message and, where appropriate, a refresh action — instead of a blank page/area that looks like a bug.

## Phase 2 — Material Handler Workflow & Analytics (builds on Phase 1 Tasks 1–14; continue in order)

This phase adds the view/actions for the person who **fulfills** waitlist requests (the Material Handler and above), plus the manager-side urgency rules and waitlist analytics. Terminology note: a worker/requester **Cancel** (Task 9) records a cancellation; a handler **Release** (Task 16) does **not** — it puts the job back on the open list. Keep the two distinct and never call the handler action "Cancel".

- [ ] **15) Expose the signed-in user's identity to services (foundation).**
  Accepting a job must auto-assign it to the signed-in user. Today `StartupState` only exposes role (`CurrentRole`/`IsDeveloper`). Expose the current user's employee number and name through `StartupState` (or an identity service) so the Waitlist services (and later analytics) can attribute accept/complete/release actions and request submissions to the right person.

- [ ] **16) Role- and ownership-aware request card actions: Accept / Complete / Release (with Edit-for-creator kept as a TODO).**
  On the shared Waitlist list, redesign each request card's right-side action area (today a checkbox and/or uncommanded Edit/Cancel/Accept glyphs) into role-, state-, and ownership-aware **icon** buttons (icons, not text):
  - **Unaccepted** request + the viewer is **Material Handler or above** → show an **Accept** icon. Accepting auto-assigns to the signed-in user (from Task 15) and moves status to `In Progress` (persisted per Task 1).
  - After a handler accepts, the job **stays on the shared list**; the **Accept button is no longer shown to any other handler** (they see it as taken/assigned).
  - **Only the assigned handler** sees **Complete** and **Release** on that job.
  - **Complete** = mark the request `Done`/`Completed` (single step for now — leave TODO hooks for a future multi-step picked-up→delivered→closed handoff).
  - **Release** = the assigned handler puts the job back on the open list (status back to available; **NOT** recorded as a cancellation).
  - **Edit** is visible to the request's creator/requester; wire its visibility now but leave its behavior as a **TODO for future implementation** (do not fully implement Edit).
  - **Handler-facing data:** the card/detail must show what the handler needs to do the job (per the mock-toggle rule) — coil/part + requested quantity, the coil's location/stock (Infor Visual lookup), the destination press/work center, and the requester + urgency/remaining time.
  - **Request note:** allow a handler to add a short note to a request (add a `Note` field to the model + UI on the card/detail); notes are visible on the request detail/history.
  Non-handler / non-owner viewers see no action buttons. This supersedes/refines Phase 1 Task 8's claim UI.

- [ ] **17) Max allotted time per request sub-type (Plant Manager+ settable).**
  Add a manager-manageable setting (role-gated to Plant Manager and above, consistent with existing manage-settings gates) that stores a **max allotted time for each request sub-type** (e.g. each Coil subtype, each Pickup subtype). Persist via `ILocalSettingsService`; provide sensible defaults; localize strings. Used by the urgency rule in Task 18.

- [ ] **18) Urgency deadlines and handler list ordering.**
  For each request, derive its **due time = created time + that sub-type's max allotted time** (from Task 17); **remaining time = due − now**; **overdue** when remaining is negative. Use/align this with the existing `TargetTimeUtc` / `IsOverdue` / remaining-time display fields. Order the available/handler list by urgency (most urgent first — least remaining / overdue first), building on Phase 1 Tasks 1 (timestamps) and 7 (wait time).

- [ ] **19) New-request alert (per-user toggle, off by default) + deep-link.**
  Add a per-user Settings toggle (off by default) such as "alert me when a new waitlist request is ready." When ON and the app is packaged (`RuntimeHelper.IsMSIX`), show an app notification/toast on a newly submitted request via the existing `IAppNotificationService`; when unpackaged (or the toggle is OFF), no-op (no external notification).
  Also wire **notification deep-linking**: tapping a notification opens that request's detail page (extend `AppNotificationActivationHandler`, currently a TODO placeholder, to navigate to the request).

- [ ] **20) Stock-shortage snapshot at request creation (analytics scenario A).**
  When a part/coil-bearing request is created, quietly record a snapshot of that coil/part's **on-hand quantity and location** at that moment (from the Infor Visual lookup path per the mock-toggle rule; no handler/requester action required). Persist the snapshot so analytics can answer "how often were requests placed while on-hand was below the requested amount" → which coils/locations chronically run short.

- [ ] **21) In-app Waitlist analytics screen (Plant Manager+).**
  Add a role-gated (Plant Manager and above) analytics screen presenting waitlist-flow analytics from Phase 1 Task 1's lifecycle data and Task 20's snapshots:
  - Volumes by request type and over time.
  - Fill speed / average wait time.
  - On-time vs overdue.
  - Cancellations (requester cancellations) and reasons.
  - Handler workload/performance.
  - Stock-shortage metrics (from scenario A).
  Follow the existing analytics/report page patterns: create the page + view model, register via `PageService` + DI, add to shell navigation, apply the role gate, and localize all strings.

## Phase 3 — Waitlist Analytics & Administration (builds on Phase 1 audit + Task 20 snapshots; continue in order)

This phase turns the lifecycle data (Phase 1 audit records), stock snapshots (Task 20), and retained cancelled records into manager/admin reporting, and keeps resolved data from cluttering the live list.

- [ ] **22) Admin monitoring of cancelled requests.**
  Add a role-gated (Developer / admin-level) view that **monitors cancelled requests over time** — who cancelled, when, request type/subtype, and reason — using the retained cancelled records from Phase 1 (never purged). This is distinct from the Plant Manager operational analytics in Task 21. Register the page + view model via `PageService`/DI/shell with the role gate and localize all strings.

- [ ] **23) Resolved-request retention & archival.**
  Honor the existing `waitlist.resolved_retention_days` setting (default 90): resolved (`Done` / `Cancelled`) requests older than the retention window are **hidden from the active Waitlist list** but **preserved** (not deleted) so analytics/admin (Tasks 21–22) can still report on them. Add the active-list filtering plus a housekeeping/archival routine, localized where it touches the UI.
