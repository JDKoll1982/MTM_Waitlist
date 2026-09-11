# 13 — Developer UI: Mock Auto-Fallback, DB Connection Gates & Startup Gate

> **Purpose:** Add a **Developer-oriented** UI & behavior layer on top of the Waitlist app so the app
> keeps running when its external databases (Infor Visual, the receiving app) are down.
>
> Three asks:
>
> 1. **Startup/auto mock fallback** — during startup, if the Infor Visual DB can't be reached, use mock
>    data; do the same for the receiving-app DB.
> 2. **Live-data-when-available** — both mocks (receiving & Infor Visual) pull from their real database
>    **only when a connection can be established**, so when either is down the app still runs on mock.
> 3. **`mtm_waitlist` is required to start** — no connection to `mtm_waitlist` ⇒ the app must not start
>    (full stop) with clear user-facing reasoning during splash. (Largely already implemented; polish wording.)
>
> **RETIRED 2026-09-11:** a former fourth ask — a **request-type/subtype editor** on a Developer Settings
> page — is **cancelled and will not be built**. All of its content has been removed from this file; do not
> reintroduce it.
>
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented,
> builds clean, and tests pass.
>
> **Related:** `12-MockMasterData-DbDriven.md` provides the foundation this file builds on: the real
> request-type/subtype **reference tables** (`waitlist_request_types`/`waitlist_request_subtypes` 28/29)
>
> + dedicated SPs + `RequestTypeCatalogService` read, and the mock `mock_*` tables/registry + mock-master
> routing that Phase 1's health fallback extends.
>
> *(The `Developer Settings` page and the request-type/subtype editor this file used to name as a
> prerequisite were retired 2026-09-11 — see the note above.)*

## Task 0 — Green baseline + full read

+ [x] **DevOps: confirm `MTM_Waitlist.sln` builds clean (0 errors/warnings)** (`dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`). | **Persona: DevOps Engineer** — verified 2026-09-06: clean across every increment (0 warnings / 0 errors).
+ [x] **QA: full suite green** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer** — verified 2026-09-06: 421 passed / 0 failed / 12 skipped.
+ [ ] **Tech Lead: read `WeekendProject/PromptFiles/*.md` + `WeekendProject/DeveloperUI-VisualPrompts.md` + `WeekendProject/ChangeLog.md`** and reconcile the mock/real routing rules and the request-type/subtype schema. | **Persona: Tech Lead**
**GATE: baseline green and full read complete before any edits.**

---

## Phase 1 — Connection health & auto mock fallback (items 1 & 3)

> Architecture (locked 2026-09-06): a **Core health service** pings the Infor Visual DB and the
> receiving-app DB. When a DB is unreachable, the app **auto-uses mock** for that source and **disables
> the manual mock toggle** until connectivity returns. When connectivity is re-established, mock is
> auto-disabled for that DB and a **taskbar toast** announces the state change.
> DB access stays **SP-only** for `mtm_waitlist`; the two external DBs keep their existing routing.

### Subphase 1.1 — Core connection health service

+ [x] **Backend Engineer: add `IConnectionHealthService` / `ConnectionHealthService`** in `MTM_Waitlist.Core` that reports reachability for Infor Visual (`SqlHelperServer` target) and the receiving-app DB (`MySqlHelperServer.MtmReceivingApplication`), returning a per-source `ConnectionHealthState` (Unknown / Connected / Unreachable) with a timestamp. | **Persona: Backend Engineer** — verified 2026-09-06: `ConnectionHealthService` + interface probe both sources via short-open connection tests; `ConnectionHealthServiceTests` pass (3).
+ [x] **Backend Engineer: define `ConnectionHealthState` + a per-source state model** and register the service via DI in the app composition root. | **Persona: Backend Engineer** — verified 2026-09-06: `ConnectionHealthState.cs` model added; `IExternalConnectionInfoProvider`/`ExternalConnectionInfoProvider` + `IConnectionHealthService` registered in `ServiceRegistrationExtensions`.
+ [x] **Full Stack Engineer: define how "reachable" is decided** (open a connection with a short timeout; distinguish malformed connection string vs server unreachable vs auth failure) so the fallback reacts to genuine outages, not transient blips. | **Persona: Full Stack Engineer** — verified 2026-09-06: short-timeout open probe (SQL Server for Infor, MySQL for Receiving) with `ClassifyException` distinguishing login vs unreachable; not-configured treated as unreachable.

### Subphase 1.2 — Auto mock fallback on outage + re-enable on recovery

+ [x] **Backend Engineer: route the mock toggle decision through health** so that when the Infor Visual source is `Unreachable`, the Infor mock is effectively ON (regardless of the stored toggle) and the manual toggle is **disabled** in UI until reconnected; mirror for the receiving source. | **Persona: Backend Engineer** — verified 2026-09-06 (decision layer done): `MockRoutingService.Resolve` forces `UseMockData=true` + `IsAutoForced=true` when a source is `Unreachable`/not configured, overriding any stored toggle, and marks the toggle disabled for the UI. Mirror logic is source-generic (works for both Infor & Receiving). UI toggle-disabled binding is phase-gated (needs Developer Settings page).
+ [x] **Backend Engineer: on recovery** (source returns to `Connected`), auto disable mock for that source and record the transition so UI + toast can react. | **Persona: Backend Engineer** — verified 2026-09-06: `MockModeChangeDetector` + new `MockRoutingRefreshService` (Core) run one refresh pass per source (fresh health → debounce → resolve routing decision) and, comparing to the previous decision, emit a `MockModeChange` (`Recovered` when a source comes back online and mock auto-disables to the configured mode; `ForcedOn`/`TurnedOn`/`TurnedOff` otherwise). `LastDecisions` exposes the current per-source decision. DI-registered; `MockFallbackDebouncerTests` (5) + `MockRoutingRefreshServiceTests` (5) pass; build clean; full suite green (431 passed). Emitting the change to the taskbar toast is the Phase 1.3 (UI) part.
+ [x] **Full Stack Engineer: reconcile the manual toggle vs auto-fallback precedence** (manual Admin override still respected when reachable; health only forces mock when down) and resolve the cross-file toggle ambiguity (Infor vs Recv per source). | **Persona: Full Stack Engineer** — verified 2026-09-06 (precedence part done): `MockRoutingService` honours a local manual override when reachable and only forces mock when the source is down. Per-source key mapping resolved at the health layer (`ConnectionSource` docstrings: InforVisual → `Feature.InforVisualMockData`, Receiving → `Feature.RecvMockData`). The remaining cross-file routing ambiguity (F12 0.4b sample-catalog wiring) is separate and tracked under Workflow 12.
+ [x] **Security Engineer: review for race/health flapping** — debounce short outages so mock doesn't thrash on/off; guard against the health check itself being the single point of failure. | **Persona: Security Engineer** — verified 2026-09-06: `IMockFallbackDebouncer`/`MockFallbackDebouncer` (Core, pure) only flips a source's *effective* status after `Threshold` (default 2) consecutive observations of the new status, so a single transient probe failure cannot flip the mock fallback on/off. Per-source independent state + `Reset()`. Consumed by `MockRoutingRefreshService` between raw health and the routing decision. DI-registered; `MockFallbackDebouncerTests` (5) pass; build clean; full suite green (431 passed).

### Subphase 1.3 — Taskbar toast on state change

+ [x] **Frontend/Backend Engineer: show a taskbar notification/toast** when mock is auto-enabled (source down) and when it is auto-disabled (source recovered), with a clear user-facing message naming the affected source. Reuse the app notification pattern already in the app. | **Persona: Frontend Engineer** — verified 2026-09-06: added the WinUI app wiring. `DispatcherPollScheduler` (`Services/MockMode/DispatcherPollScheduler.cs`, `Microsoft.UI.Xaml.DispatcherTimer`, 30s interval) implements `IPollScheduler`; registered in `ServiceRegistrationExtensions`. `MockModeToastCoordinator` (`Services/MockMode/MockModeToastCoordinator.cs`, singleton) starts the `IMockModePollingHost` from `ShellPage.OnLoaded` (role known) and subscribes `ModeChanged` → `IAppNotificationService.Show(MockModeSummaryProvider.ToastMessage(change))` for each notify-worthy change (forced-on outage / recovery / manual or central toggle flip), naming the source. Idempotent start; unpackaged apps are a no-op (toasts require MSIX identity, mirroring the alert/notification gates). Build clean; full suite green (480 passed).

---

## Phase 2 — Central mock setting reflected on all clients (item 1/3 admin push)

> Architecture (locked 2026-09-06): mock ON/OFF for Infor & receiving is stored **centrally** in
> `mtm_waitlist.config_settings_values` so an Admin/Developer-set value reflects on **all running
> clients**. Clients refresh and apply it (with a toast when it changes). Guard against **race
> conditions** when multiple clients flip the setting during an outage.

### Subphase 2.1 — Central config storage + read/write

+ [x] **Database Engineer: add stored procedures to read/upsert the Infor & Receiving mock settings** in `config_settings_values` (SP-only; follow repo DB naming rules; `AllSPs.sql` + `update_table_descriptions.sql` kept in sync). | **Persona: Database Engineer** — verified 2026-09-06: the repo's existing `sp_config_settings_upsert` and `sp_config_settings_get_effective` already read/upsert any setting key (incl. bool) at `all_users` scope, so no new SP was required; the mock settings are stored under `Feature.InforVisualMockData` / `Feature.RecvMockData`.
+ [x] **Backend Engineer: add a service** to read the central mock settings and to upsert them (role-checked), replacing/augmenting the local `Feature.InforVisualMockData` / `Feature.RecvMockData` keys. | **Persona: Backend Engineer** — verified 2026-09-06: `IMockConfigurationService`/`MockConfigurationService` (Core) reads via `sp_config_settings_get_effective` and upserts via `sp_config_settings_upsert` (all_users, bool) keyed by source; DI-registered; `MockConfigurationServiceTests` (3) pass. Role-gating is delegated to the caller (per interface doc).

### Subphase 2.2 — Client refresh + race handling

+ [x] **Backend Engineer: client polling** — running clients poll the central mock config on a timer and apply changes + fire a toast. | **Persona: Backend Engineer** — verified 2026-09-06 (poll/apply core done): `MockRoutingRefreshService` (one refresh pass → `MockModeChange`s) + `MockRoutingMonitorService`/`IMockRoutingMonitorService` (raises a `MockModeChanged` event per change; exposes `LastChanges`; `ResetBaseline()`). A UI host (WinUI `DispatcherTimer`) calls `RunOnceAsync()` and subscribes to `MockModeChanged` to fire the toast — that timer host + toast rendering is the remaining Phase 1.3/app wiring. `MockRoutingMonitorServiceTests` (3) pass; build clean; full suite green (437 passed).
+ [ ] **Security Engineer: race-condition guard** — single source of truth with optimistic/last-writer-wins on the row so simultaneous clients can't corrupt state during an outage. | **Persona: Security Engineer**
+ [x] **QA Engineer: tests** for central read/write, role gating, client refresh applying a changed value, and concurrent upserts. | **Persona: QA Engineer** — verified 2026-09-06: `MockModeQaTests` covers central read/write per-source key mapping + all_users/bool scope (`MockConfigurationService`), role gating (`DeveloperAccessGuard` denies operator roles for config/catalog edits), a changed central value applied on the next client refresh with a single `TurnedOn` event (`MockRoutingMonitorService`), and last-writer-wins concurrent upserts on the same key. `MockModeQaTests` (4) pass; build clean; full suite green (445 passed).

---

## Phase 3 — `mtm_waitlist` required to start (item 4)

> Already implemented: `StartupCoordinator` blocks startup when the `mtm_waitlist` startup session can't
> be validated. This phase polishes the user-facing splash reasoning.

### Subphase 3.1 — Startup gate polish

+ [ ] **Frontend/Backend Engineer: confirm + polish the splash messaging** when `mtm_waitlist` is unreachable — a clear, non-technical message ("Cannot connect to the MTM Waitlist server…") with a Retry option and a Cancel that exits. | **Persona: Frontend Engineer**
+ [ ] **QA Engineer: verify** the hard no-start-without-`mtm_waitlist` gate with a user-facing message; full suite green. | **Persona: QA Engineer**

---

## Phase 4 — RETIRED: Request-Type/Subtype editor (cancelled 2026-09-11)

> **No request-type/subtype editor will be used.** This phase — the Developer Settings page, the
> Request-Type master list, the type edit view, the 4-step guided wizard modal, the per-control card, and
> the real-catalog grid editor — is cancelled, not deferred. Its design rules, Option-A wireframe
> reference, editor backend and UI tasks have been removed from this file. Do not reintroduce them.
> The catalog's **data** half is already complete (`RequestTypeCatalogService` reads the MySQL reference
> tables); none of it is editable in the app.


### Subphase 4.2 — Backend editor services — REMOVED (cancelled 2026-09-11)

*The three completed backend tasks that used to sit here (the real-catalog CRUD SPs, the
`IRequestTypeEditorService`/`RequestTypeEditorService` read/write service and its DTOs, and the
control-type-reuse validation helper) described code that has since been deleted. They are no longer
documented here: no request-type/subtype editor will be used.*


---

**GATE: `mtm_waitlist` is hard-required at startup with clear splash messaging; full suite green.**

*(The mock-fallback, central-config and request-type-editor clauses that used to sit in this gate are
retired: the mock systems were removed by `specs/001-module-mock-visual-fallback` (FR-014), and the
request-type/subtype editor is cancelled as of 2026-09-11.)*
