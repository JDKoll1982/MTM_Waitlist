# MTM Waitlist — Weekend Project Changelog

> **Audience:** End users / plant-floor operators, supervisors, and admins.
> This changelog is scoped to the **weekend Waitlist project work** (WeekendProject prompt files
> `01`–`12`) — what you can see and do differently in the Waitlist area, written in plain language.
> It follows the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format (newest first).
>
> For the broader, longer-running repo changelog, see the root `CHANGELOG.md`.
>
> **Most recent update:** 2026-09-06

---

## [Unreleased]

> Work landed this weekend but not yet cut into a numbered release. Items are grouped by the type of
> change, newest first.

### Added

- **Waitlist request lifecycle with statuses.** Requests now move through clear statuses —
  **Pending → Accepted → Completed** (or **Canceled**) — instead of being a static list. The status
  is shown on each request card, and every lifecycle step is recorded with the date/time and who did
  it, so you can see the full history of a request.
- **"My Requests" filter in the window header.** A toggle now sits in the top header (just left of the
  building selector). Turn it on to see **only the requests you submitted** ("My Requests"); turn it
  off to see **all** requests ("All Requests"). It appears only while you're on the Waitlist screen.
- **Cancel your own request.** If a request is still waiting and you created it, you can cancel it from
  the detail view with a confirmation. Cancelled requests are kept (with the reason and who cancelled)
  rather than deleted, so nothing is silently lost. You can only cancel requests you created.
- **How long a request has been waiting.** Each request now shows its age / wait time at a glance.
- **Friendly empty states.** The Waitlist list and detail views show a clear "Nothing to show" message
  instead of a blank area when there are no requests or nothing is selected.
- **Notes on a request.** Requests can carry a free-text note alongside the lifecycle data.
- **Press / work-center photo on the request card.** The request detail now shows the work-center/press
  photo as a proper image on the request card (right side), alongside the request-type image.
- **Click-to-enlarge image viewer.** Click either photo (request type or work center/press) on the
  detail page to open a larger in-page view with a close (X) button.
- **Ignored Infor Visual locations settings.** Under **Settings → Operations**, admins/developers can
  manage the list of locations that are hidden from inventory totals and grids (for example `WC`,
  `NCM`, `V-WC`, `NCM-VITS`, `SHIP`), with add/remove controls.

### Changed

- **Coil inventory now shows real pooled weight.** Instead of coil *counts* with friendly rack labels,
  the app now mirrors Infor Visual: one row per part-in-location as a total **weight in pounds**, using
  real Infor location codes (for example three 5,000 lb coils of `MMC0001000` at `V-A0-01` show as one
  row of **15,000 lb**). The location grid column is labeled **"Quantity (lb)"**, and the coil card's
  "Quantity in house" / on-hand figures use these pooled weights.
- **"Requested coil" now shows the actual coil, not the action.** A Coil request (for example
  `Coil · Bring`) previously displayed the action word ("Bring") as the requested coil. It now always
  shows the real coil number on the job (for example `COIL-204`), except for a "Wrong Coil" report which
  keeps its special signal.
- **Average coil weight is pulled from real receiving history.** The "Average coil weight" shown on the
  coil cards is now the average received skid weight from the receiving app's history for that coil
  part (for example `5,000 lb`), instead of a static figure.
- **The New Request confirm screen now shows coil details.** When confirming a **Coil** request, the screen
  shows the current coil's context (requested coil number, quantity in house, coil description, and average coil
  weight) alongside the request summary, so the worker can verify they are requesting the right coil before
  submitting.

### Fixed

- **Work order and employee number now show on the request detail.** The "Work order and request" card on a
  request's detail page previously always read **"Not available"** for both the Work order and the Employee
  number, even though the request carried that data. It now shows the request's real job/work-order reference
  and the requester's employee number (for example, employee **6229**), with "Not available" shown only when a
  request genuinely has no such value.
- **"My Requests" toggle visibility.** Several rounds of fixes so the toggle reliably appears in the
  correct header (the visible `DefaultHeaderTemplate`) and only when the Waitlist screen is active; the
  bordered "card" wrapper was removed so it matches the rest of the header text.

---

## Versioning

This project is still under active development and does not yet follow strict Semantic Versioning; a
numbered, dated version section will be added at the first cut release. Until then all weekend work is
captured under **[Unreleased]** above.

---

## For developers / implementation notes

> The sections below are **internal** and describe the technical, out-of-scope groundwork done alongside
> the user-facing changes above. They are kept here for maintainers and changelog reference, not for
> end users. (Formerly a standalone "Out of Scope Notes" file; merged into this changelog under the
> "for developers" appendix so the user-facing changelog stays readable.)

### Architecture decisions locked in

- **All MySQL access from C# must go through stored procedures** (`ExecuteStoredProcedure*Async`), never
  raw `ExecuteSql*Async` with inline SQL. `MySqlHelperServer` exposes both; new services use SPs.
- **`waitlist_request_types` / `waitlist_request_subtypes` are REAL (non-mock) catalog data.** They are
  NOT mock data and must NOT live in mock-data services/allow-lists. They have dedicated SPs and a
  separate real-catalog read service (`IRequestTypeCatalogService` / `RequestTypeCatalogService`).

### Done — mock-mode polling host app wiring (Workflow 13, 1.3/2.2) — verified 2026-09-06

Closes the last runtime gap in the mock-mode toast/poll path (all Core/backend was already done + tested).

- **`DispatcherPollScheduler`** (`Services/MockMode/DispatcherPollScheduler.cs`, app project) implements
  `IPollScheduler` over a `Microsoft.UI.Xaml.DispatcherTimer` (30 s default interval; Start idempotent). Must be
  created/started on a UI-thread DispatcherQueue.
- **DI:** `IPollScheduler` → `DispatcherPollScheduler` registered as a singleton in `ServiceRegistrationExtensions`
  (right after `IMockModePollingHost`), so the previously-unregistered scheduler dependency is now resolvable and
  `MockModePollingHost` can actually be constructed by DI.
- **`MockModeToastCoordinator`** (`Services/MockMode/MockModeToastCoordinator.cs`, singleton) starts the
  `IMockModePollingHost` exactly once and subscribes `ModeChanged` → for each notify-worthy `MockModeChange` calls
  `IAppNotificationService.Show(MockModeSummaryProvider.ToastMessage(change))`, naming the affected source. Toasts
  are MSIX-only, so unpackaged apps are a no-op (no DB probes start); idempotent so a recreated shell can call
  `Start()` again.
- **Wired into the shell:** `ShellPage` (DI ctor) takes the coordinator and calls `Start()` from `OnLoaded` — once
  the shell is up and the signed-in role is known.
- Build clean (0/0); full suite green (480 passed). Ticked Workflow 13 Subphase 1.3 in `13-Developer-UI.md`; the
  standalone Security race-condition-guard review (Subphase 2.2) remains open.

### Done — Workflow 09 alert service groundwork (per-user new-request alert) — verified 2026-09-06

Additive backend increment for `09-Phase2-alerts.md` (new-request alert toggle + toast). The pure decision helper
`RequestAlertGate` already existed; this adds the per-user persistence + single-ask service the Settings toggle UI
and the request-submit toast hook will consume.

- **`INewRequestAlertService` / `NewRequestAlertService`** (`MTM_Waitlist.Core`): owns the per-user setting key
  `User.NewRequestAlertsEnabled` (backed by `ILocalSettingsService`, which is per-Windows-user) with **default OFF**
  (`GetEnabledAsync` → absent = false), `SetEnabledAsync` to persist, and
  `ShouldNotifyOnCreatedAsync(requestCreatedSignal, isPackaged)` that folds the stored toggle into `RequestAlertGate`
  (toast only when a request was actually created AND the toggle is ON AND the app is packaged/MSIX).
- DI-registered as a singleton in `ServiceRegistrationExtensions`.
- Tests: `NewRequestAlertServiceTests` (5) — default-off read, stored-value read, persist under key, decision
  signal × toggle × packaged, off-toggle no-op. Build clean; full suite green (485 passed).
- **Subphase 0.1 DONE 2026-09-06 (per-user Settings toggle):** `SettingsViewModel` exposes `NewRequestAlertsEnabled`
  (initialized default-off from the service; persists on change) + `IsNewRequestAlertsPanelVisible` search visibility
  folded into `IsOperationsCategoryVisible`. Added a **New Request Alerts** `Expander`+`SettingsCard`+`ToggleSwitch`
  card to the Operations category in `SettingsPage.xaml` (mirrors the Mock Data card). `SettingsViewModelTests` gained
  3 cases (defaults-off, panel/category search visibility, toggle persists under key). Build clean; full suite green
  (488 passed). File `09-Phase2-alerts.md` Subphase 0.1 + Task-0 boxes ticked.

### Fixed — request detail: Work order / Employee number "Not available" — verified 2026-09-06

In `WaitlistViewDetailViewModel.LoadCoilSections`, the "Work order and request" card hard-coded **"Not available"**
for Work order and Employee number instead of reading the request. `SampleOrder` already carried the requester
employee number, and the underlying request is resolvable via `SampleOrder.RequestId`.

- Added `ResolveRequest(SampleOrder)` (via `IWaitlistRequestService.GetRequest(RequestId)`), plus
  `WorkOrderText(...)` (a "Work order" field if present, else the request's `ActiveSetupJobId`) and
  `EmployeeNumberText(...)` (requester employee number, else `"Not available"`).
- The coil detail card now shows the real values. `WaitlistViewDetailViewModelTests` +1 (Work order = active job
  id, Employee number = requester). Build clean; full suite green (518 passed).

### Done — New Request confirm screen: coil details — verified 2026-09-06

The New Request **confirm** (`NewRequestSummaryPage`) previously showed only generic Work Center / Request Type /
Subtype / Details, with no coil context.

- `NewRequestSummaryViewModel` now injects the registered singleton `ICoilAvailabilityService` and, on
  `OnNavigatedTo` for a **Coil** request type, resolves the current coil for the work center
  (`GetCoilForJobAsync(state.WorkCenter)`, mock-toggle aware). New observables: `IsCoilVisible`, `CoilNumber`,
  `CoilQuantityOnHand`, `CoilDescription`, `CoilAverageWeight` (guarded to only show when a coil is present and
  has a number — avoids the blank-field live-mode case).
- `NewRequestSummaryPage.xaml` gains a "Coil details" card (visible only for Coil requests) reusing the standard
  coil labels: Requested coil / Quantity in house / Coil description / Average coil weight.
- Build clean; full suite green (519 passed). Note: coil details are resolved from the work center at confirm time
  (not stored on the flow state), so it reflects the current job's coil, consistent with the existing job-changed
  re-validation on submit.

### Done — mock-mode new requests persist to the database — verified 2026-09-06

Waitlist requests are REAL `mtm_waitlist` data; the Infor Visual / receiving mock toggles only short-circuit external
lookups. Previously `WaitlistRequestService.SubmitAsync` returned early in mock mode and only stored the request
in-session, so a new request added while a mock toggle was ON was never written to the database and was lost on
restart.

- Removed the mock-only short-circuit in `SubmitAsync` so a new request is persisted through
  `sp_waitlist_request_insert` whenever a helper server is configured — regardless of the mock toggles — while still
  being stored in-session (audit + notify unchanged). mtm_waitlist is hard-required, so a failed insert returns
  `PersistenceFailure` in both mock and live paths.
- `WaitlistRequestServiceTests` +1 (`SubmitAsync_MockOn_PersistsNewRequestToDatabase`). Build clean; full suite green
  (519 passed).

### Done — Workflow 07 note update path (backend) — verified 2026-09-06

Backend increment for `07-Phase2-fulfill.md` Subphase 0.1 (handler note). `WaitlistRequest` already carried a `Note`
field; this adds the missing update/persist path so a handler can set a short note on a request.

- **`IWaitlistRequestService.UpdateNoteAsync(requestId, note)`** (`WaitlistRequestService`): sets the note on the
  request (mock + production), persists via the status-update SP (status unchanged), records a `NoteUpdated` audit
  entry, and raises `RequestsChanged`. Returns the updated request, or null when not found. Whitespace-only clears.
- `WaitlistRequestServiceTests` +3 (set note + audit; empty clears; not-found → null). Build clean; full suite green
  (517 passed). File `07` Subphase 0.1 "Data Model" box ticked.
- Still to do (07): handler note UI, handler-facing data on cards/detail, and the Accept/Complete/Release actions
  (core status-machine + UI work, needs the running app).

### Done — Workflow 08 urgency settings service groundwork (max allotted per subtype) — verified 2026-09-06

Backend increment for `08-Phase2-urgency.md` Subphase 0.1 (per-subtype max-allotted time). The urgency math helper
`UrgencyCalculator` already existed; this adds the persisted per-subtype setting the Settings card and the urgency
computation consume.

- **`IUrgencySettingsService` / `UrgencySettingsService`** (`MTM_Waitlist.Core`): stores each sub-type's max-allotted
  time as its own scalar int local-settings key (`Urgency.MaxAllottedMinutes.<subtype>`, default `DefaultMinutes`
  = 30 when unset). `GetMaxAllottedAsync(subtype)` → `TimeSpan` with default fallback; `SetMaxAllottedAsync(subtype,
  minutes)` clamps to 1..1440. Safe for every local-settings store (primitive int keys). DI-registered as a
  singleton.
- Tests: `UrgencySettingsServiceTests` (4) — default 30 min when unset, per-subtype set/get round-trip, clamps to
  positive minimum, unconfigured subtype falls back to default. Build clean; full suite green (502 passed).
- **Subphase 0.1 UI DONE 2026-09-06:** `UrgencyAllotmentItem` (Settings/Models, observable row: SubtypeName +
  minutes) + `UrgencyAllotmentEditorViewModel` (Settings child VM, mirrors `ComputerManagementViewModel`) that loads
  the real request-type catalog's sub-types (via `IRequestTypeEditorService`), reads minutes from
  `UrgencySettingsService`, persists on change, and gates editing to Plant Manager+ (`CanManageUrgencySettings`).
  `SettingsViewModel` exposes `UrgencyAllotments` + `IsUrgencyAllotmentsPanelVisible`. Added a role-gated **Max
  Allotted Time** `Expander` card (Operations category) listing each sub-type with a minutes `NumberBox`.
  `UrgencyAllotmentEditorViewModelTests` (4) pass; full suite green (506 passed). File `08` Subphase 0.1 boxes ticked.
- **Subphase 0.2 compute foundation DONE 2026-09-06:** `IUrgencyDeadlineService`/`UrgencyDeadlineService` (Core)
  resolves a request sub-type's max-allotted (via `UrgencySettingsService`) and runs `UrgencyCalculator.Compute`
  → the "due = created + subtype max allotted" rule as one call (`ComputeAsync(created, subtype, now)`).
  `UrgencyDeadlineServiceTests` (3) pass; full suite green (509 passed). DI-registered.
- **Mock-parity data confirmed + QA tests added 2026-09-06:** `SampleWaitlistRequestCatalog` rows already set
  `RequestedUtc` + `TargetTimeUtc` (incl. an active overdue row), so urgency works mock-ON.
  `UrgencyMockParityTests` (3) verify active sample rows expose the times, the flagged-overdue row has a past-due
  target, and most-urgent-first ordering works over the sample rows. Full suite green (512 passed). File `08`
  mock-parity QA box ticked.
- **Subphase 0.2 Service Layer WIRED 2026-09-06:** `WaitlistRequestService.SubmitAsync` now derives `TargetTimeUtc`
  (= created + sub-type max-allotted via `UrgencyDeadlineService`) when a draft supplies none — applied to both
  mock-ON and production submissions; explicit deadlines are preserved. `WaitlistRequestServiceTests` +2. Full suite
  green (514 passed). File `08` Subphase 0.2 "Service Layer" box ticked.
- Still to do (Subphase 0.2): order the handler/available list most-urgent-first (view-layer wiring in the Waitlist
  module; `UrgencyCalculator.OrderMostUrgentFirst` helper is ready).

### Done — Workflow 09 deep-link contract + toast engine (Subphase 0.2 groundwork) — verified 2026-09-06The engine the Subphase 0.2 submit-hook and activation handler consume, added as tested Core groundwork (toasts are
MSIX-only so nothing fires unpackaged; the actual submit-path + activation wiring is the remaining runtime part).

- **`WaitlistRequestLink`** (Core, pure static): the deep-link argument contract for a new-request toast —
  `Build(Guid)` → `action=openrequest&request=<guid>` (carried on the toast `<toast launch="...">` attribute);
  `TryParse(string?, out Guid)` parses it back for the activation handler. `WaitlistRequestLinkTests` (4) pass.
- **`INewRequestAlertNotifier` / `NewRequestAlertNotifier`** (Core): `NotifyNewRequestAsync(requestId, title, body,
  isPackaged)` folds the toggle+packaged decision through `INewRequestAlertService`/`RequestAlertGate`, then builds the
  toast XML (`BuildToastXml`, escaping title/body and embedding the deep-link launch argument via
  `SecurityElement.Escape`) and shows it via `IAppNotificationService`. `NewRequestAlertNotifierTests` (4) pass
  (toggle-on+packaged shows a payload containing the deep link + text; toggle-off and unpackaged show nothing;
  `BuildToastXml` escaping). DI-registered as a singleton. Build clean; full suite green (496 passed).
- **Subphase 0.2 Service Layer WIRED 2026-09-06:** `WaitlistRequestService.SubmitAsync` (single create path, mock
  ON + production) now calls `INewRequestAlertNotifier.NotifyNewRequestAsync(request.Id, title, body,
  RuntimeHelper.IsMSIX)` before returning `Success`, so a newly submitted request fires the toast when the per-user
  toggle is ON and the app is packaged (unpackaged/OFF = no-op). Optional `INewRequestAlertNotifier?` ctor param
  (default null) keeps all existing service constructions compiling; localized `NewRequestAlert_Title` /
  `NewRequestAlert_Body` keys added to `Resources.resw`. `WaitlistRequestServiceTests` +2 (mock-ON success invokes
  the notifier with the request id; validation failure does not). Build clean; full suite green (498 passed). File
  `09` Subphase 0.2 "Service Layer" + mock-parity "Backend" boxes ticked.
- **Subphase 0.2 deep-link WIRED 2026-09-06:** `AppNotificationActivationHandler.HandleInternalAsync` now parses
  the `AppNotificationActivatedEventArgs.Argument` with `WaitlistRequestLink.TryParse` and, for an
  `action=openrequest&request=<guid>` toast tap, enqueues (low priority, after the shell/nav frame is up) a
  navigation to the Waitlist request detail page (by view-model full-name key, param = `requestId.GetHashCode()`)
  and brings the window forward; unrecognized arguments keep the placeholder fallback. Build clean; full suite
  green (498 passed). **Workflow 09 (`09-Phase2-alerts.md`) is now COMPLETE (all boxes ticked).** Known residual:
  fully rendering the request on a cold-start deep-link still depends on the detail page's existing lookup contract
  (request loaded in the selected building's in-memory set).

### Developer-UI groundwork (Workflow 13, Phase 1) — verified 2026-09-06

Early, self-contained backend increment for `13-Developer-UI.md` (auto mock-fallback on DB outage):

- **Connection-health service** added to Core:
  - `ConnectionHealthState.cs` (models `ConnectionSource` = Infor Visual / Receiving, `ConnectionHealthStatus`
    = Unknown / Connected / Unreachable, and a timestamped snapshot). `mtm_waitlist` is intentionally excluded —
    it is hard-required for startup.
  - `IConnectionHealthService` / `ConnectionHealthService` probe each external source with a short-open
    connection test (SQL Server for Infor Visual, MySQL for Receiving), cache a last-known snapshot, and
    return user-safe failure reasons (never raw credentials).
  - `IExternalConnectionInfoProvider` / app `ExternalConnectionInfoProvider` resolve the Infor + Receiving
    connection strings by mirroring the existing `InforVisualSqlQueryService` (config/env) and `MySqlHelperServer`
    (env/config) resolution. Registered in `ServiceRegistrationExtensions`.
  - Tests: `ConnectionHealthServiceTests` (3) — unconfigured→Unreachable, Unknown-before-check, CheckAll both sources.
  - This is the probe half of the F13 Phase-1 fallback; the auto-enable/disable-mock routing + toast wiring
    (F13 Subphases 1.2/1.3) and central config (F13 Phase 2) are still to do.

### Bug fix — mock registry SP name (2026-09-06)

- `MockMasterDataService` referenced `sp_mock_master_tables_get`, but the generated registry SP is
  `sp_mock_master_tables_registry_get`. Fixed both the `GetMasterTablesAsync` call and the
  `ResolveGetProcedure("mock_master_tables_registry")` mapping. Verified the registry SP returns the 7 mock
  tables.

### Done — mock-master read/write editor service (Workflow 12, Subphase 0.4) — verified 2026-09-06

- **`MockMasterDataService`** now exposes the full Developer-editor backend over SP-only access:
  - Registry enumeration (`sp_mock_master_tables_registry_get`).
  - Per-table row reads (`sp_mock_<table>_get`).
  - `GetTableColumnsAsync` (via `sp_mock_master_table_columns_get`) + `MockMasterColumnDefinition` model.
  - Table-driven `AddTableRowAsync` / `UpdateTableRowAsync` / `DeleteTableRowAsync` dispatching to the exact
    per-table insert/update/delete SPs, using editable-column contracts confirmed against the generated SP
    signatures. Real (non-mock) request tables remain excluded.
  - Verified: `MockMasterDataServiceTests` (9) pass; **live round-trip** on `mock_requesters`
    (insert → read → update → delete) confirmed; full suite green (390 passed).

### Done — central mock configuration service (Workflow 13, Phase 2.1) — verified 2026-09-06

- **`MockSettingState`** model + **`IMockConfigurationService` / `MockConfigurationService`** (Core) that read
  (`sp_config_settings_get_effective`) and upsert (`sp_config_settings_upsert`, all_users scope, bool) the
  centrally-stored `Feature.InforVisualMockData` / `Feature.RecvMockData` settings per `ConnectionSource`.
  DI-registered; role-gating delegated to callers. `MockConfigurationServiceTests` (3) pass; full suite green
  (393 passed). Client polling/refresh + race handling (Phase 2.2) still to do.

### Done — effective mock routing decision layer (Workflow 13, Phase 1.2/2.2 groundwork) — verified 2026-09-06

- **`MockRoutingDecision`** (model: `Source`, `UseMockData`, `IsAutoForced`, `Reason`/`MockRoutingReason`,
  `ReasonText`) + **`IMockRoutingService` / `MockRoutingService`** (Core, deliberately pure/no deps). Centralizes
  the single deterministic precedence rule all callers (helper-server routing, Developer-UI toggle state,
  health auto-fallback) should share:
  1. Source unreachable / not configured → mock **forced on**, `IsAutoForced=true` (toggle disabled).
  2. Reachable + local manual override set → override wins.
  3. No override + central shared setting present → central governs.
  4. Reachable + nothing configured → live data (off).
  `Unknown` health is treated as reachable (prefer configured settings until a probe fails). DI-registered.
  `MockRoutingServiceTests` (7) pass; build clean; full suite green (400 passed). This is additive groundwork —
  no live helper-server behaviour was rewired, so it is safe until the F12 0.4b / P1b wiring that consumes it.

### Done — mock routing coordinator (Workflow 13, Phase 1.2/2.2 orchestration) — verified 2026-09-06

- **`MockRoutingCoordinator`** (+ `IMockRoutingCoordinator`, Core) composes the four inputs into one async
  answer: `GetEffectiveDecisionAsync(source, refreshHealth)` fetches the last-known (or fresh) health snapshot,
  the central shared setting, and the per-machine local override, then runs `MockRoutingService.Resolve`.
  `GetEffectiveDecisionsAsync()` returns both sources. Callers ask one question instead of wiring the pieces.
- **`MockSettingKeys.For(source)`** extracted as the single source of truth for per-source keys
  (`Feature.InforVisualMockData` / `Feature.RecvMockData`); `MockConfigurationService` now delegates to it, so
  the local-override read and the central store can never drift apart. DI-registered.
  `MockRoutingCoordinatorTests` (4) pass; build clean; full suite green (404 passed). Still additive — not yet
  wired into helper-server routing or the UI.

### Done — mock-mode change detector (Workflow 13, Phase 1.2/2.2 notification signal) — verified 2026-09-06

- **`MockModeChange`** model + **`MockModeChangeDetector`** (pure static, Core) classify the transition between
  two consecutive routing decisions for a source — `TurnedOn`, `TurnedOff`, `ForcedOn`, `Recovered`,
  `StillForced`, `None` — with a `ShouldNotify` flag and user-safe `Message`. This is the exact signal the
  P1b toast and the P2.2 "recovery re-enable" logic need. First-observation baseline yields `None` (no toast on
  startup). `MockModeChangeDetectorTests` (8) pass; build clean; full suite green (412 passed). Still additive —
  a consumer (toast/UI) has yet to subscribe.

### Done — real-catalog editor backend (Workflow 13, Phase 4.2) — verified 2026-09-06

- **Two admin reads** — `sp_waitlist_request_types_get_all` + `sp_waitlist_request_subtypes_get_all` return ALL
  real (non-mock) catalog rows INCLUDING inactive ones so an editor can view/re-activate them (the wizard
  `*_get` SPs filter `is_active = 1`). New `Database/StoredProcedures/sp_waitlist_request_{types,subtypes}_get_all/{create,rollback}.sql`,
  appended to `AllSPs.sql`, applied + verified live (8 types / 24 subtypes). No explicit `sort_order` column
  yet — display order is read order by id (schema follow-up tracked).
- **`IRequestTypeEditorService` / `RequestTypeEditorService`** (`MTM_Waitlist.Core`) + editor DTOs
  `RequestTypeEditorItem` / `RequestSubtypeEditorItem`: `GetCatalogAsync` (types grouped with subtypes, incl.
  inactive) via `_get_all`; `AddSubtypeAsync` / `UpdateSubtypeAsync` / `DeleteSubtypeAsync` / `UpdateTypeAsync`
  dispatch to the dedicated real-catalog SPs (JSON grid fields serialized, byte bools). DI-registered; kept
  deliberately separate from all mock code. `RequestTypeEditorServiceTests` (5) pass; build clean; full suite
  green (417 passed). This is the backend the Developer Settings editor UI (Subphase 4.3) will consume.

### Done — control-type reuse validation (Workflow 13, Phase 4.2) — verified 2026-09-06

- **`RequestCatalogControlValidation`** (Core, pure/static): `CollectAvailableControlTypes(catalog)` returns the
  distinct image-view control types present across types+subtypes (data-driven dropdown source; the editor
  "reuses all existing controls across any type"); `Validate(catalog, availableControls)` returns
  `ControlAvailabilityIssue`s flagging rows whose control is empty or not in the available allow-list (error
  gating). No runtime reflection required (unit-testable); consumers may inject the app's authoritative
  compiled-control allow-list. `RequestCatalogControlValidationTests` (4) pass; build clean; full suite green
  (421 passed). UI dropdown binding + live compiled-control allow-list is the remaining (runtime/UI) part.

### Done — debounce guard + client refresh service (Workflow 13, Phase 1.2 recovery/debounce + Phase 2.2 poll core) — verified 2026-09-06

- **`IMockFallbackDebouncer` / `MockFallbackDebouncer`** (Core, pure): a source's *effective* reachability status
  only changes after `Threshold` (default 2) consecutive observations of the new status, so a single transient
  probe failure can't flip mock fallback on/off (anti-flapping). Per-source independent state + `Reset()`.
  `MockFallbackDebouncerTests` (5) pass.
- **`IMockRoutingRefreshService` / `MockRoutingRefreshService`** (Core): runs one refresh pass per external
  source — fresh health (`CheckAsync`) → debounce → central config + local override → routing decision — then,
  compared to the prior decision, emits a `MockModeChange` (`Recovered` on reconnection, `ForcedOn`/`TurnedOn`/
  `TurnedOff` otherwise) so the toast/UI can react. `LastDecisions` exposes the current per-source decision.
  This is the testable polling/apply core the Phase 2.2 client timer will drive. `MockRoutingRefreshServiceTests`
  (5) pass. Both DI-registered; build clean; full suite green (431 passed). The UI timer + toast subscription
  (Phase 1.3 / Phase 2.2 timer) is the remaining runtime part.

### Done — unified Developer-editable table catalog (Workflow 12, Subphase 0.6 expose real tables) — verified 2026-09-06

- **`DeveloperEditableCatalogService`** (+ `IDeveloperEditableCatalogService`, Core) returns the single list the
  Developer-settings editor dropdown binds to: the `mock_*` tables (enumerated from `mock_master_tables_registry`
  via `MockMasterDataService`) **plus** the two real request tables (`waitlist_request_types` /
  `waitlist_request_subtypes`). Each `DeveloperEditableTable` carries a `DeveloperTableSourceKind` (Mock vs
  RealCatalog) so the editor routes reads/writes to `MockMasterDataService` (per-table SPs) or
  `RequestTypeEditorService` (dedicated real-catalog SPs). DI-registered; `DeveloperEditableCatalogServiceTests`
  (3) pass; build clean; full suite green (437 passed). Grid editor UI still to build (Subphase 0.5/0.6).

### Done — client mock-mode monitor (Workflow 13, Phase 2.2 poll/apply core) — verified 2026-09-06

- **`IMockRoutingMonitorService` / `MockRoutingMonitorService`** (Core): wraps `MockRoutingRefreshService`,
  raises a `MockModeChanged` event for every notify-worthy `MockModeChange` returned by a refresh, exposes
  `LastChanges`, and forwards `ResetBaseline()` to the refresh service (which gained a proper `Reset()`).
  Timer-decoupled so it is unit-testable; a WinUI `DispatcherTimer` host calls `RunOnceAsync()` and subscribes
  to the event to fire a toast. DI-registered; `MockRoutingMonitorServiceTests` (3) pass; build clean; full
  suite green (437 passed). Timer host + toast rendering remain the Phase 1.3/app wiring.

### Done — role gate + central-config QA suite (Workflow 13, Phase 2.2) — verified 2026-09-06

- **`IDeveloperAccessGuard` / `DeveloperAccessGuard`** (Core): centralizes role-based authorization for the
  Developer Settings page, mock-master editing, real-catalog editing, and central mock-config changes. Roles are
  normalized case-insensitive strings (default `developer`/`admin`/`administrator`; constructor-injectable).
  Pure + unit-testable; this is the shared gate the Developer UI and write paths will call.
  `DeveloperAccessGuardTests` (4) pass.
- **`MockModeQaTests`** (Core tests): QA for the central mock-config path — per-source key mapping + all_users/bool
  scope on read/write, role gating denying operator roles for config/catalog edits, a changed central value being
  applied on the next client refresh with a single `TurnedOn` event, and last-writer-wins concurrent upserts on
  the same key. `DeveloperAccessGuard` is DI-registered; build clean; full suite green (445 passed).

### Done — editor validation helpers (error gating for Developer editors) — verified 2026-09-06

- **`RequestCatalogEditValidator`** (Core, pure): client-side field rules for the real request-type/subtype
  editor (name/control required, prompt required when text input required, min ≥ 0, max ≥ min, no blank grid
  labels) plus `FindDuplicateSubtypeName` to keep the DB unique constraint satisfied when editing. This is the
  testable error-gating slice the Phase 4.3 wizard's cancel/save validation uses.
  `RequestCatalogEditValidatorTests` (5) pass.
- **`MockMasterRowEditValidator`** (Core, pure): validates a Developer-edited `mock_*` row before it is written —
  rejects values for audit/unknown columns and flags missing/blank required (non-nullable, non-audit) business
  columns. Underpins the mock-master grid editor's error gating (F12 0.5). `MockMasterRowEditValidatorTests`
  (5) pass. Build clean; full suite green (455 passed).

### Done — mock-mode status/toast text + polling host (Workflow 13, Phase 1.3/2.2 surfaces) — verified 2026-09-06

- **`MockModeSummaryProvider`** (Core, pure): consistent user-facing text for the F13 1.3 toasts and the
  Developer status card — `SourceDisplayName` (Infor Visual / Receiving), `Describe(decision)` (forced-on /
  mock / live one-liners), and `ToastMessage(change)` naming the affected source per change kind.
  `MockModeSummaryProviderTests` (4) pass.
- **`IPollScheduler` + `IMockModePollingHost` / `MockModePollingHost`** (Core): timer-agnostic polling host that
  drives `MockRoutingMonitorService.RunOnceAsync()` on each scheduler tick and forwards `ModeChanged`
  (`event`) + `LastChanges`. Tests use a fake scheduler; the WinUI app implements `IPollScheduler` with a
  `DispatcherTimer`. DI-registered. `MockModePollingHostTests` (2) pass. Build clean; full suite green (461
  passed). Remaining app wiring: implement `IPollScheduler` over a `DispatcherTimer` and subscribe `ModeChanged`
  to render the toast.

### Done — urgency math + resolved-retention helpers (Workflow 08 / 11 backend) — verified 2026-09-06

- **`UrgencyCalculator`** + `UrgencyState` (Core, pure): `Compute(createdUtc, maxAllotted, now)` → due / remaining /
  overdue (`RemainingLabel` = "Overdue" or "N min"); `OrderMostUrgentFirst` orders overdue-first then least
  remaining. This is the file-08 due/remaining/overdue math + most-urgent-first ordering backend.
  `UrgencyCalculatorTests` (4) pass.
- **`ResolvedRetentionFilter`** (Core, pure): `IsWithinRetention` / `IsAgedOut` / `RetentionCutoff` over
  `waitlist.resolved_retention_days` (default 90) — a resolved request within the window stays on the active list;
  older is hidden but retained (never purged). This is the file-11 retention-window filtering backend.
  `ResolvedRetentionFilterTests` (5) pass. Build clean; full suite green (470 passed). UI/DB wiring (settings card,
  list filtering/archival routine) remains.

### Done — request alert gate + request action policy (Workflow 07/09 decision helpers) — verified 2026-09-06

- **`RequestAlertGate`** (Core, pure): the file-09 new-request alert decision — a toast only fires when the request
  was created AND the per-user toggle is ON AND the app is packaged (MSIX). OFF/unpackaged = no-op.
  `RequestAlertGateTests` (2) pass.
- **`RequestActionPolicy`** (Core, pure): file-07 role-/ownership-aware card actions — `CanViewerAccept`,
  `IsAssignedToViewer`, `CanViewerCompleteOrRelease` (only the assigned handler, not when done), `CanViewerEdit`
  (creator, not when done), plus `IsAvailable`/`IsTaken`/`IsDone` over case-insensitive status strings.
  `RequestActionPolicyTests` (5) pass. Build clean; full suite green (477 passed). These are additive decision
  helpers for the card UI + note/accept wiring; the actual service/VM/UI + notification rendering remain.

### Done — single "Mock Data" toggle (master key + keep both keys in sync) — verified 2026-09-06

Decision: keep both existing mock keys in place but treat **`Feature.InforVisualMockData` as the master/visual
key**, keep `Feature.RecvMockData` equal to it, and surface **ONE "Mock Data" toggle** in Settings (all mock data
reads from the mock tables when ON). The F13 auto-fallback is kept (already fully implemented).

- **`IMockToggleService` / `MockToggleService`** (Core): `GetEffectiveAsync()` reads the master key; `SetAsync(value)`
  writes BOTH keys to the same value so every existing per-key reader sees the same effective mock-data setting.
  DI-registered. `MockToggleServiceTests` (3) pass.
- **`SettingsViewModel`**: replaced the two toggle properties (`UseRecvMockData`/`UseInforVisualMockData`) with one
  `UseMockData` driven by `MockToggleService` (writes both keys on change). Constructor injects the service.
- **`SettingsPage.xaml`**: the two mock toggle cards are now a single "Mock Data" toggle bound to `UseMockData`.
- Build clean (0/0); full suite green (480 passed). The waitlist service readers already check the individual keys,
  which now stay equal so the single toggle drives them all.

### Done — DB + SP layer (verified against live `mtm_waitlist`)

- **Real request-type catalog tables** `waitlist_request_types` (28) / `waitlist_request_subtypes` (29)
  under `Database/Tables/…` (`create.sql` + `rollback.sql`), applied live; `AllTables.sql` +
  `update_table_descriptions.sql` updated.
- Seeded 1:1 from `Assets/Config/waitlist-request-types.json`
  (`Database/Seeds/seed_waitlist_request_catalog` + `AllSeeds.sql`). Live DB: **8 request types / 24
  subtypes**, using the JSON `id` GUIDs (== `RequestTypeInventory`/`RequestSubtypeInventory` StableIds).
- **Real-catalog SPs** (dedicated, non-mock): `sp_waitlist_request_types_get/_insert/_update/_delete` and
  `sp_waitlist_request_subtypes_get/_insert/_update/_delete` — applied live + appended to `AllSPs.sql`.
- **Mock per-table SPs**, applied live + appended to `AllSPs.sql` (generated via `tools/gen_mock_sp.ps1`):
  per mock table (`mock_master_tables_registry`, `mock_parts`, `mock_work_orders`, `mock_work_centers`,
  `mock_locations`, `mock_requesters`, `mock_inventory_locations`, `mock_request_types`) a
  `sp_<table>_get/_insert/_update/_delete`. Note: `mock_inventory_locations` has no `is_active` column.
- **Waitlist lifecycle DB work** (files 01/04): `waitlist_requests_audit` table (19) +
  `sp_waitlist_request_audit_insert`, plus read/list + status-update SPs, seed data, and the audit trail
  persisted on every lifecycle event.
- **Mock-master tables + registry** (workflow 12): `mock_master_tables_registry` + 7 mock tables (20–27),
  each with `create.sql` + `rollback.sql`; `mock_master_tables_registry` carries UI-readable names +
  plain-language descriptions for the Developer settings dropdown. Seeded via
  `Database/Seeds/seed_mock_master_default/` + `AllSeeds.sql`.
- Added `sp_mock_master_table_columns_get` (SP-based schema introspection for the editor grid).

### Done — C# service layer

- **`IRequestTypeCatalogService` / `RequestTypeCatalogService`** (`MTM_Waitlist.Waitlist.NewRequest`) —
  reads the REAL catalog through the two get SPs and maps rows into `NewRequestTypeDefinition`/
  `NewRequestSubtypeDefinition` (the shape the old JSON parser produced).
- **`NewRequestFlowService.LoadRequestTypesAsync()`** now reads from the DB catalog service;
  `NewRequestFlowRules.GetDefaultTypes()` is the fallback only. Registered in `AddWaitlistNewRequestServices`.
- **`MockMasterDataService` / `IMockMasterDataService`** (Core) — clean **read-only, mock-only** service
  routing through per-table get SPs; excludes the real request tables.
- **Waitlist lifecycle / requester / identity / coil / location work** (files 01–06): signed-in user's
  employee number/name surfaced into the session; cancel-own + My Requests paths; lifecycle-parity mock
  data (`SampleWaitlistRequestCatalog` etc.); coil availability service; location-ignore service;
  inventory location filtering + sortable detail grid; pooled-weight coil representation; average coil
  weight service reading the receiving DB.
- Removed the orphaned `MockMasterColumnDefinition` model (only used by a reverted inline-SQL CRUD draft).

### Earlier out-of-scope work (pre-Workflow-12; from the Waitlist UI/data session)

- **Header "My Requests" filter** (Shell header): toggle + dynamic "My Requests"/"All Requests" label in
  the visible header, left of the building selector; bordered card wrapper removed; shows only while the
  Waitlist list is active. `ViewModels/ShellViewModel.cs`, `Module_Core/Views/ShellPage.xaml`.
- **Request-detail work-center image + lightbox**: moved the press image out of a faint full-page
  background into the request hero card (right side) and made both hero images click-to-enlarge with an
  in-page lightbox (`ImageViewerOverlay`) + close button. `WaitlistViewDetailPage.xaml`/`.cs`.
- **Requested-coil correctness**: Coil branch no longer treats the action subtype as the requested coil;
  always reports the job's actual coil, with a regression test (`WaitlistRequestServiceTests`).
- **Coil quantity = pooled per-location weight**: Infor pooled-weight semantics across
  `SampleInventoryLocationCatalog`, `InventoryLocationRow`, coil on-hand strings, `WaitlistViewViewModel`,
  `SampleDataService`, the SQL header, and the detail grid ("Quantity (lb)").
- **Average coil weight from the receiving app DB (cross-repo)**: `IAverageCoilWeightService` /
  `AverageCoilWeightService` computing `AVG(quantity)` over
  `mtm_receiving_application.receiving_history` by `part_id` (`RecvMockData`-routed, with
  `SampleAverageCoilWeightCatalog` for mock ON); wired into Waitlist list + detail coil cards; plus seed
  `Database/MTMReceivingApp/Seeds/seed_receiving_history_coil_weights/`. NOTE: predates the Workflow-12
  "SP-only" rule and uses inline `ExecuteSqlQueryAsync` against the external receiving DB — revisit to
  route through an SP if the SP-only rule covers the receiving DB.

### Still to do (from the checklist)

Remaining open checklist items across the Weekend Project prompt files (01–13). Grouped by file, newest
concept first; the per-file prompt documents hold the exact wording/personas/artifacts.

## Waitlist core — files 05 / 06 (location ignore + location grid)

- **05** Apply the ignored-location rule app-wide: omit ignored locations from Waitlist/Coil lists and from
  "Quantity in house" totals, and from the Setup screen location lists (`SetupSubordinatePart` grids).
- **05 (mock parity)** Honor the ignore rule in mock/sample-data paths and totals; expose the ignored-locations
  read to the sample/mock path and seed sample rows that include ignored + non-ignored entries; QA tests that
  mock-ON lists/totals exclude ignored locations across Waitlist/Coil and Setup.
- **06** Add the location/part-row helper service (routes to SQL when `Feature.InforVisualMockData` OFF, sample/
  mock when ON) + query/mock-routing tests (rows returned, zero rows, mock parity).

## Phase 2 — files 07 / 08 / 09 (fulfill, urgency, alerts)

- **07** Handler-needed data on card/detail (coil/part + qty, coil location/stock, destination press/work center,
  requester + urgency); a persisted `Note` field + UI (via file 01 update path).
- **07** Accept / Complete / Release: unaccepted + handler-or-above shows **Accept** (auto-assigns to the signed-in
  user → `In Progress`); after accept the job stays shared, Accept hides for others, only the assigned handler sees
  Complete/Release; Complete → `Done` (single-step; TODO hooks left for future multi-step), Release returns to the
  open list (not a cancellation). Edit-for-creator kept as a TODO. Role/ownership tests + mock parity for data/note.
  (Ownership/role decision done: `RequestActionPolicy`; the card UI + service/VM wiring remain.)
- **08** Max-allotted-time per request sub-type (Plant Manager+ settable, persisted, sensible defaults); urgency math
  (due = created + max allotted; remaining = due − now; overdue when < 0) aligned with `TargetTimeUtc`/`IsOverdue`;
  order the handler list most-urgent-first. Tests for defaults/edit/role-gate and due/remaining/overdue + ordering;
  mock rows expose requested + target times so urgency works mock-ON. (Urgency math + ordering done: `UrgencyCalculator`;
  the Plant Manager+ settings card + wiring the VM/list to it remain.)
- **09** Per-user "alert me on new waitlist requests" toggle (default OFF, persisted per user); when ON + packaged,
  toast on a newly submitted request via `IAppNotificationService` (OFF/unpackaged = no-op); deep-link the toast to
  the request detail (extend `AppNotificationActivationHandler`). Tests for decision (toggle × packaged) + routing;
  mock-ON still triggers the request-created signal. (Decision done: `RequestAlertGate`; the toggle UI + toast
  render + activation routing remain.)

## Phase 3 — files 10 / 11 (analytics, admin)

- **10** Stock-shortage snapshot recorder (`IStockSnapshotRecorder` in Core; recorder + model/service in
  `MTM_Waitlist.Analytics`) quietly snapshotted at request creation (Infor per mock toggle); sample-data mock branch;
  tests that creation writes a snapshot (mock ON/OFF).
- **10** Role-gated (Plant Manager+) **analytics screen** (`AnalyticsViewModel` + `AnalyticsPage` + metrics service):
  volumes by type & over time, fill speed / average wait, on-time vs overdue, cancellations + reasons, handler
  workload/performance, stock-shortage metrics. Tests for metric computation + role gating.
- **11** Role-gated (Developer/admin) **cancelled-request monitor** (who/when/type/reason from retained records,
  never purged) + nav/registration; `waitlist.resolved_retention_days` (default 90) — aged resolved (`Done`/
  `Cancelled`) requests hidden from the active list but preserved for analytics/admin, with a housekeeping/archival
  routine. Tests for the monitor/role gate and retention-window filtering/archival; mock parity (aged resolved row).
  (Retention math done: `ResolvedRetentionFilter`; the active-list filtering/archival routine + monitor UI remain.)

## Workflow 12 — mock master data (Developer-editable)

- **0.4 routing** Route the C# sample catalogs (`SampleDataService`, `SampleJobCoilCatalog`,
  `SampleInventoryLocationCatalog`, `SampleWaitlistRequestCatalog`, `SetupDataCatalog`,
  `SampleAverageCoilWeightCatalog`) to the `mock_*` tables when the relevant mock toggle is ON (InforVisual for
  Infor/coil/inventory flows; Recv for receiving/flatstock). (Real `waitlist_request_types`/`subtypes` stay out of
  mock code.) NOTE: blocked on the routing-toggle partition decision — see "Cross-file review findings" below.
- **0.5** Role-gated **Developer Settings page** (new main-nav item above Settings) + registry-dropdown mock editor
  grid (add/edit/delete per mock table); move existing developer settings cards onto it.
- **0.6 (UI)** Extend the Developer editor so the real request-type/subtype tables are selectable and their rows are
  editable in the grid; retire the JSON at runtime. (Backend already done: `RequestTypeEditorService` +
  `_get_all` admin reads.)

## Workflow 13 — Developer UI

- **1.3** (DONE — see "Done — mock-mode polling host app wiring" below) Taskbar toast when mock is auto-enabled
  (source down) / auto-disabled (source recovered), naming the source.
- **2.2** Poll/apply core + the app-side timer host are DONE (see "Done — mock-mode polling host app wiring"
  below): `MockRoutingMonitorService` + `MockModePollingHost` (over the new `DispatcherPollScheduler`) + central
  upsert core + a started host subscribed to `ModeChanged` to fire the toast. The standalone **Security/Backend
  race-condition guard** review (single source of truth; optimistic/last-writer-wins on the row) remains open —
  concurrent-upsert last-writer-wins is already covered by QA tests, but a dedicated guard design/review is not.
- **3.1** Polish the splash messaging when `mtm_waitlist` is unreachable (clear, non-technical message + Retry + a
  Cancel that exits) + QA verify of the hard gate.
- **4.3** Role-gated **Developer Settings page** (main nav above Settings; move Mock Data / Image Location / Computers
  cards), the Request-Type master list, the type edit view, the 4-step guided wizard modal (Next/Back/Cancel, step
  indicator, final Save, cancel-confirm on unsaved changes, persist once at final Save), and the per-control card —
  all backed by the completed Phase 4.2 services/validators.

## Task-0 baseline (unticked in files 05–13)

- Each prompt file opens with DevOps "builds clean" + QA "full suite passes" baseline boxes; many remain `[ ]`
  even though the build/tests have been verified across sessions. They can be ticked on the next clean
  `dotnet build` + `dotnet test` pass.

### Cross-file review findings — mock gaps to consider

- **No mock waitlist-request lifecycle master table** (files 01/03/04/07/11); today `SampleWaitlistRequestCatalog`
  (C#) provides these — consider a `mock_*` table + grid.
- Seed **coil-less jobs** (file 02) so Coil-gating is exercisable on mock.
- Represent **ignored-location codes** and **qty-0 + pooled-weight** inventory rows in the mock seed (05/06).
- **Routing toggle ambiguity (Subphase 0.4):** file 01 gates the waitlist lifecycle read on
  `Feature.InforVisualMockData`, while `prompt.md` ties the MySQL `mtm_waitlist` target to
  `Feature.RecvMockData`; `mock_parts` holds coil (`MMC`, Infor) + flatstock (`MMF`, Recv) rows. Decide a
  partition rule before routing.
- **Analytics (file 10)** should consume the mock inventory/location source at a Core-shareable level.

### Generator helper

- `tools/gen_mock_sp.ps1` regenerates the per-table mock SP `create.sql`/`rollback.sql` set (dev helper,
  not runtime code). Real-catalog SPs were authored by hand (`types_delete` is a single multi-table
  `DELETE` so `AllSPs.sql` stays `;`-delimited).
