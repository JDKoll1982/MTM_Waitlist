# Feature Specification: Module_Mock — Automatic Infor Visual Read Fallback

**Feature Branch**: `001-module-mock-visual-fallback`
**Created**: 2026-09-09
**Status**: Draft
**Input**: User description: "Introduce Module_Mock — a new, automatic read-fallback for Infor Visual that replaces the app's two legacy 'mock data' systems. Concretely: the app's own databases, plus the floor/WIP and receiving databases, are always live — never mocked; only Infor Visual (external, read-only) can be unreachable, and when it is the app must automatically fall back to a cached copy of the Visual data and show a read-only status indicator; a dedicated cache store holds a mirror of the five Visual read shapes the app consumes, refreshed by a staging + atomic swap so readers never see partial data; a new lightweight service app runs on the database host (auto-start to tray) that performs the scheduled warm refresh, makes periodic backups of all four internal databases (configurable per database), can restore a database from backup in an emergency, and exposes a small network, token-gated API that clients can call to force a refresh; the legacy mock systems are removed; all hard-coded/inline database statements are converted to pre-defined query routines; and the design must be extensible via a documented playbook for adding a new Visual read shape."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Internal application data is always live (Priority: P1)

A setup lead assigns work centers and saves a workstation setup, then immediately looks at the work-center card: the card shows the setup that was just saved. A reviewer works the full waitlist request lifecycle — accepting a request, adding a note, changing status, and cancelling — and every action is persisted and visible to other users and after a restart. Today this is broken whenever the legacy "sample data" setting is enabled: the save is acknowledged but silently discarded, and request lifecycle actions are skipped entirely.

**Why this priority**: This is the reported production defect. Every other capability is less valuable than the application being truthful about its own data. Fixing it restores trust in the product and unblocks the people who run the floor.

**Independent Test**: Enable the legacy sample-data setting, save a workstation setup, and confirm the work-center card reflects the change. Then perform a complete waitlist request lifecycle and confirm every transition survives an application restart and is visible to a second user.

**Acceptance Scenarios**:

1. **Given** the legacy "sample/demo data" setting is enabled, **When** a user saves a workstation setup, **Then** the work-center card displays the newly saved assignment without any manual refresh or restart.
2. **Given** any prior state of the legacy demo setting, **When** a reviewer accepts a request, adds a note, changes status, or cancels it, **Then** the change is persisted to the application's own store and remains after an application restart.
3. **Given** a user opens any screen that reads the application's own, floor/WIP, or receiving stores, **When** those stores are reachable, **Then** only real data is shown and no sample/demo rows are substituted.
4. **Given** the application is running, **When** a user looks for a demo/sample data control in settings, **Then** no such control exists.

---

### User Story 2 - Automatic cached fallback for external reads (Priority: P1)

The external manufacturing system (Infor Visual) becomes unreachable — network blip, maintenance window, or an outage. The user keeps working: work-order lookup, operation sequences, subordinate parts, inventory locations, and request disposition all continue to return results, served silently from the most recent cached copy of that data. When the external system comes back, reads quietly return to live data without a restart.

**Why this priority**: This is the reason the fallback exists. Without it, the app either stops working or lies (the legacy behaviour). It is the second half of the MVP and is independently valuable on top of User Story 1.

**Independent Test**: Make the external source unreachable, then exercise all five read journeys in the application and confirm each returns a complete, correctly shaped result. Restore the source and confirm reads return to live data without restarting the app.

**Acceptance Scenarios**:

1. **Given** the external source is unreachable, **When** a user performs any of the five supported reads, **Then** a complete result set is returned from cached data with no error surfaced and no partial rows.
2. **Given** the external source was unreachable and becomes reachable again, **When** the user repeats a read, **Then** live data is returned without restarting the application.
3. **Given** a read is served from cached data, **When** the result is compared to a live result, **Then** the result has an identical structure and column set so no caller behaves differently.
4. **Given** a fresh installation whose cache holds only baseline seed content and the external source has never been reached, **When** a user performs a supported read, **Then** a usable (seed) result is returned rather than an error.

---

### User Story 3 - The cache stays warm without anyone asking (Priority: P2)

An operator on the database host runs a small always-on service. It starts automatically when the host logs in, sits quietly in the notification area, and refreshes the cached copies from the external system on a schedule. When the external system is unreachable it does not fail loudly — it logs and tries again next cycle. Nobody has to remember to populate the cache.

**Why this priority**: Without scheduled refreshing the fallback decays into stale or empty data; with it, the fallback stays useful. It is P2 because the fallback itself (User Story 2) is still demonstrable with seeded or manually refreshed cache content.

**Independent Test**: Install and run the service on a host, observe a scheduled refresh complete for all supported read shapes, and observe that an unreachable source produces a logged skip rather than a failure or a partial cache update.

**Acceptance Scenarios**:

1. **Given** the service is installed on the host, **When** the host logs in, **Then** the service starts automatically and runs minimized without user interaction.
2. **Given** a configured refresh interval has elapsed, **When** the external source is reachable, **Then** all configured read shapes are refreshed and the service records a successful run per shape.
3. **Given** the external source is unreachable during a scheduled refresh, **When** the cycle runs, **Then** the refresh is skipped and logged, previously cached data remains intact, and the next cycle is attempted on schedule.
4. **Given** a refresh is in progress, **When** a user reads the same cached data concurrently, **Then** the user sees either the complete previous snapshot or the complete new snapshot, never a mixture.

---

### User Story 4 - Read-only fallback status indicator (Priority: P2)

While cached data is being served, the user sees a non-interactive indicator explaining that the external manufacturing system is unreachable and cached data is in use. There is nothing to click and nothing to switch on — the indicator is purely informational and disappears when live reads resume.

**Why this priority**: Users must be able to tell the difference between real and cached data without hunting for a setting, but the application still functions without the indicator, so it ranks below the fallback and the always-live fix.

**Independent Test**: Force the external source offline, confirm the indicator appears and is not interactive, then restore the source and confirm the indicator clears.

**Acceptance Scenarios**:

1. **Given** the external source is unreachable, **When** any screen is in use, **Then** a read-only indicator stating that the external system is unreachable and cached data is in use is visible.
2. **Given** the indicator is displayed, **When** the user interacts with it, **Then** nothing happens — there is no toggle, no dialog, and no mode change.
3. **Given** the external source becomes reachable again, **When** the state is re-evaluated, **Then** the indicator clears.

---

### User Story 5 - On-demand refresh and operational visibility (Priority: P3)

An operator or developer wants fresh cached data now rather than waiting for the next scheduled cycle, and wants to see whether refreshes and backups have been succeeding.

**Why this priority**: A convenience and observability layer over User Story 3. Valuable, but the fallback is already functional with scheduled refresh alone.

**Independent Test**: Request an immediate refresh through the service and observe it complete; request service status and observe per-item last-run results.

**Acceptance Scenarios**:

1. **Given** the service is running, **When** an authorized caller requests an immediate refresh, **Then** all configured read shapes are refreshed and the caller receives an outcome.
2. **Given** the service has run at least one cycle, **When** an authorized caller requests status, **Then** last refresh and last backup outcomes per item are returned.
3. **Given** a caller does not name an application user holding an approved operator role, **When** any request is made, **Then** the request is refused.

---

### User Story 6 - Backups and emergency restore of internal stores (Priority: P3)

An operator configures periodic backups of all four internal stores — the application's own store, the floor/WIP store, the receiving store, and the cached-data store — independently per store (enable/disable, schedule, retention, destination). In an emergency, the operator restores a chosen store from a chosen backup, replacing it entirely after an explicit confirmation.

**Why this priority**: Protective capability for data that is now always live and therefore no longer insulated by demo mode. Important, but not required for the fallback to function.

**Independent Test**: Configure a schedule for one store, observe a restorable artifact produced, corrupt a throwaway store, and restore it from the artifact after confirming the prompt.

**Acceptance Scenarios**:

1. **Given** a backup schedule is configured for a store, **When** the scheduled time passes, **Then** a restorable backup artifact is produced and recorded with its timestamp and location.
2. **Given** per-store settings, **When** an operator disables backups for one store, **Then** the other stores continue to be backed up on their own schedules.
3. **Given** an operator requests a restore, **When** the confirmation is not accepted, **Then** no data is changed.
4. **Given** an operator confirms a restore, **When** the restore completes, **Then** the target store contains exactly the contents of the selected backup and the outcome is recorded.
5. **Given** the backup facility is unavailable on the host, **When** a backup cycle runs, **Then** the condition is reported clearly and no partial or corrupt artifact is recorded as successful.

---

### User Story 7 - Extend to a new external read shape (Priority: P3)

A maintainer needs to add a sixth external read shape. They follow a published playbook — capture the read, add its cached counterpart, add the named query routines, register it for refresh, route the caller through the live-then-cached pattern, and add verification — without redesigning anything or changing existing shapes.

**Why this priority**: Extensibility protects the investment; the five initial shapes are what matters for delivery.

**Independent Test**: Hand the playbook to a maintainer who has not worked on the cache; they add a read shape end-to-end and demonstrate the fallback for it.

**Acceptance Scenarios**:

1. **Given** the playbook is published, **When** a maintainer adds a read shape, **Then** each required step is enumerated in order with the artifact it produces.
2. **Given** a new read shape is added following the playbook, **When** the external source is unreachable, **Then** the new read falls back to cached data exactly like the existing five.
3. **Given** a new read shape is added, **When** existing shapes are exercised, **Then** their behaviour is unchanged.

---

### Edge Cases

- **Cold cache on a fresh install**: The cache has never been populated and the external source is unreachable — reads must return baseline seed content rather than an error.
- **Stale cache**: The cached copy is older than the configured refresh interval — the system remains usable and the read-only indicator shows the age of the cached data so the user/operator can judge freshness.
- **Interrupted refresh**: A refresh fails midway — readers must never observe a partially populated or empty cache; the previous complete snapshot remains in effect.
- **Flapping source**: The external source alternates between reachable and unreachable — repeated switches must not thrash the UI, duplicate refreshes, or leave the indicator in a wrong state.
- **Service not running**: The service is stopped or crashed — the application must continue to operate on whatever cached data exists, with no hard dependency on the service.
- **Internal store unreachable**: An internal store (application, floor/WIP, or receiving) is genuinely unavailable — the system retries automatically with a short backoff and, if still failing, shows a clear unavailable/error state with retry and guidance; no sample data is substituted and there is no fallback for these stores.
- **Long-running read during a refresh**: A user read spans a refresh boundary — it must return a single consistent snapshot.
- **Unconfirmed restore**: A restore is requested but not confirmed — no data changes.
- **Restore interrupted**: A restore fails partway — the operator must be able to distinguish the failed state and recover from a backup.
- **Unauthorized network request**: A refresh/status request arrives that names no application user, names an unknown or inactive one, or names one whose role is not an approved operator role — it is refused, with the same response for all three cases, and logged without leaking secrets. Restore is not exposed on the network at all.
- **Backup facility missing**: The host lacks the backup tooling — reported, no false success.
- **Source schema drift**: The external read returns an unexpected column set — the refresh for that shape must be marked failed and must not corrupt the last good cached snapshot.
- **New read shape half-added**: A maintainer adds a cached shape but not the refresh registration — the gap must be detectable (documented in the playbook and covered by verification).
- **Reachable source returns no rows**: The external source is reachable but the read legitimately has no results — this is a valid empty result and MUST NOT be replaced by cached data (fallback applies only to unreachability).
- **Cache unavailable**: The cached-data store is unreachable — if the external source is reachable, live reads continue unaffected; if both are unavailable, the affected screen surfaces the unavailable/error state from FR-021.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application's own store, the floor/WIP store, and the receiving store MUST always be read from and written to live. No user-facing or hidden setting may short-circuit reads or writes to these stores.
- **FR-002**: The system MUST detect when the external manufacturing source is unreachable and MUST automatically serve each supported external read from a cached copy.
- **FR-003**: Fallback MUST be automatic and MUST NOT require any user action; the system MUST NOT expose a manual demo/mock mode.
- **FR-004**: A result served from a cached copy MUST have an identical structure and content shape to the same read served live, so callers cannot distinguish the source.
- **FR-005**: The system MUST display a read-only, non-interactive indicator while cached data is being served, and MUST clear it when live reads resume.
- **FR-006**: Cached copies MUST be updatable from the external source, and an update MUST become visible to readers only as a complete snapshot; readers MUST never observe a partially updated cache.
- **FR-007**: A service MUST run on the database host, start automatically at logon, and continue running minimized with no user interaction.
- **FR-008**: The service MUST refresh supported external reads on a configurable schedule, MUST skip and log cycles when the external source is unreachable, and MUST leave the last good cached snapshot intact.
- **FR-009**: The service MUST produce periodic backups of all four internal stores, configurable independently per store (enablement, schedule, retention, destination).
- **FR-010**: The service MUST support restoring a selected store from a selected backup as a full replacement, performed from the service on the database host, and MUST require an explicit confirmation before making any change.
- **FR-011**: The service MUST expose a network-reachable, operator-gated interface that supports requesting an immediate refresh and retrieving status; the application and operators MAY request an immediate refresh through it; requests that do not name an application user holding an approved operator role MUST be refused.
- **FR-012**: The service MUST allow an operator to configure and persist the refresh interval, per-store backup settings, its network endpoint, and external-source connection details.
- **FR-013**: The service MUST record and surface, per item, the outcome and timestamp of the last refresh and the last backup, and MUST report clearly when required backup tooling is unavailable.
- **FR-014**: The system MUST remove the legacy sample/demo data systems in full — the in-application sample catalogs and their contract, the demo toggles and their keys, the mock routing/auto-force/monitoring stack and its toasts, the settings "use demo data" control, the database-backed demo master tables with their query routines, seeds, and registry entries — together with the tests that exercised them.
- **FR-015**: Every data operation against the application's stores MUST be performed through pre-defined, centrally managed query routines. No inline or hard-coded statement text may remain embedded in application code.
- **FR-016**: Adding a new external read shape MUST be possible by following a published, ordered playbook, and MUST NOT require changes to the fallback contract for existing shapes.
- **FR-017**: The system MUST return baseline seed content for a supported read when the cache has never been populated, so a fresh installation is usable before the external source is ever reached.
- **FR-018**: Request disposition and floor/WIP stock MUST continue to come from their real sources; floor/WIP and receiving data MUST NOT be cached as part of this feature.
- **FR-019**: Coil availability MUST be determined from a real source rather than assumed, and request-type definitions MUST have a single authoritative source.
- **FR-020**: The system MUST support a new external read shape being added without downtime for the application, so that already-deployed clients continue to serve existing shapes.
- **FR-021**: When an internal store (application, floor/WIP, or receiving) is unavailable, the system MUST retry automatically with a bounded backoff (up to three attempts, with delays of approximately 1 s, 2 s, and 4 s) and, if the retries still fail, MUST present a clear unavailable/error state on the screen that initiated the read, including a manual retry action and guidance; it MUST NOT substitute sample data and MUST NOT rely on a persistent empty-state banner.
- **FR-022**: While cached data is being served, the read-only indicator MUST include the age of the cached data (the time of the last successful refresh) so users can judge freshness; the system MUST NOT refuse to serve cached data based on its age.
- **FR-023**: Operator authorization MUST gate only the network refresh/status interface. Restore MUST be host-only and MUST NOT be exposed, routed, or otherwise reachable over the network.
- **FR-024**: The system MUST prefer live external data whenever the external source is reachable and MUST serve from the cached copy only while that source is unreachable; a reachable source that legitimately returns no rows MUST NOT be replaced by cached data.
- **FR-025**: The application MUST remain fully functional when the service is not running, relying only on the cached copy's current contents; refresh scheduling MUST be owned by the service, and the application MUST NOT perform its own scheduled refresh.
- **FR-026**: The service MUST authorize a network caller by resolving the caller's user name against the application's own user and role store, MUST fail closed when that resolution is impossible, and MUST NOT disclose which of "unknown user", "no role" or "role not approved" applied.
- **FR-027**: The cached-data store MUST be a dedicated store, separate from the application's own store, and MUST never be used as an authoritative source for internal application data (only as the external-read fallback).
- **FR-028**: Project documentation MUST be updated to describe this module and to remove references to the retired sample/demo systems, including the published playbook for adding a new external read shape.

### Key Entities *(include if feature involves data)*

- **Internal data store**: One of the four always-live stores — the application's own store, the floor/WIP store, the receiving store, and the cached-data store. Backed up independently; never subject to fallback.
- **External manufacturing source**: The read-only external system the application consumes; the only source that may be unreachable and therefore the only one with a cached counterpart. Never backed up or written to by this feature.
- **External read shape**: A distinct result the application consumes from the external source. Five are in initial scope: work-order lookup, operation sequence, subordinate parts, inventory locations, and disposition input. Each is defined by its name, required inputs, returned columns, source query, refresh schedule, and cached counterpart — this definition is the unit of extensibility.
- **Cached copy**: The stored counterpart of one read shape, holding the read's inputs and outputs plus freshness metadata, and updated only as a complete replacement.
- **Refresh run record**: The outcome of one refresh attempt for one read shape — start, finish, success/skip/failure, and any error detail.
- **Service configuration**: Operator-editable settings — refresh interval, per-store backup settings, network endpoint, and external-source connection details.
- **Backup artifact**: A restorable copy of one internal store, with the store it belongs to, its creation time, location, size, and whether it is retained.
- **Read status state**: Whether reads are currently live or served from cache, the last successful refresh per shape, and the age of the cached data.
- **Approved operator role**: One of the application roles permitted to call the service's network interface, resolved from the application's own user and role store rather than from a shared secret.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A workstation setup saved while the legacy demo setting is enabled appears on the work-center card immediately — 0 occurrences of the "save acknowledged but card unchanged" defect across a 20-run regression.
- **SC-002**: Waitlist request lifecycle actions (accept, note, status change, cancel) persist with 0 lost updates across a 50-operation mixed lifecycle test, and every action survives an application restart.
- **SC-003**: With the external source unreachable, 100% of the five supported read journeys complete successfully from cached data — no user-visible error and no partial result across 50 attempts.
- **SC-004**: Results served from cache are structurally identical to live results for all five read shapes, verified by direct comparison of 100% of the shapes.
- **SC-005**: 0 partial-cache observations when 100 refresh cycles run concurrently with continuous reads.
- **SC-006**: Users can tell the difference between live and cached data: 100% of test participants correctly identify that cached data is in use when the indicator is visible, and no participant reports being able to change a data mode.
- **SC-007**: The cache is refreshed at least once per configured interval, with ≥95% of scheduled cycles succeeding over a 30-day observation window, and every skipped cycle attributable to a logged external-source outage.
- **SC-008**: 100% of scheduled backup windows over a 30-day window produce a restorable artifact for every store with backups enabled.
- **SC-009**: A database restore, from request to verified replacement, completes in under 15 minutes in 100% of drills, and 0 restores execute without the confirmation step.
- **SC-010**: Unauthorized network requests to the service are refused 100% of the time, and no secret material appears in any log or displayed status.
- **SC-011**: The application remains fully functional with the service stopped — 0 hard failures caused by the service being absent.
- **SC-012**: A maintainer who has not previously worked on the cache can add a sixth read shape end-to-end within one working day following only the published playbook.
- **SC-013**: Automated audit finds 0 references to the retired demo/sample systems and 0 inline database statement text in application code.
- **SC-014**: Support requests attributable to "my changes were not saved" fall to 0 in the release following deployment.
- **SC-015**: The full automated build and test suite pass with 0 errors and 0 warnings after the change, and 0 tests reference the retired demo/sample systems.
- **SC-016**: A documentation review finds 0 stale references to the retired sample/demo systems in the project documentation, and the new module and its extension playbook are documented.

## Assumptions

- The four internal stores (application, floor/WIP, receiving, and the cache store) reside on one database host that is effectively always available; high availability of that host is out of scope.
- The external manufacturing source is read-only to the application, permanently; the application never writes to it.
- Only the five listed external read shapes require caching in this release; floor/WIP stock and receiving data are read live and are explicitly not cached.
- A fresh cache is expected to contain baseline seed content so the application is usable before the first successful refresh.
- Data retention for cached copies follows the configured refresh interval (each refresh replaces the prior snapshot); no long history of cached snapshots is required.
- Backup retention defaults are reasonable per-store defaults chosen at implementation time and are operator-configurable.
- Required backup tooling is available on the database host; when absent it is reported rather than worked around.
- Client applications and the service are on the same network, and a single shared credential is an acceptable access control model because the service runs on a restricted host and no role/permission system is in scope.
- The service is a single instance on the host; multi-host deployment, clustering, and failover are out of scope.
- The read-only status indicator is informational only and lives within the existing application shell; no new navigation or user-configurable preference is introduced.
- Retiring the legacy demo systems is a breaking removal — no compatibility shim or transition period is required, and any internal tooling that depended on them is out of scope.
- Existing external read behaviours (such as filtering inventory to on-hand quantities and ignoring configured locations) are preserved unchanged by the fallback.
- The service host has network access to the external manufacturing source, which is required to refresh the cache.
- The shared credential is stored securely and is never displayed in the settings UI or written to logs.
- Verified by the accompanying checklist: `checklists/requirements.md`.
