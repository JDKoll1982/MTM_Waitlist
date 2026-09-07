# 09 - Phase 2 — Alerts: New-Request Notification Toggle + Deep-Link

> **Source:** Master `WeekendProject/PromptFiles/prompt.md` (Task 19). Self-contained.
> **Workflow:** checklist-execution skill; adopt each task's persona. Tick `- [x]` only when implemented, builds clean, and tests pass.
> **Task 0 scope:** green build + full suite pass before/after this file.

## Subphase 0 — Task 0: Green build & full-suite baseline

- [x] **DevOps: `MTM_Waitlist.sln` builds clean (0 errors, 0 warnings)** via `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false`. | **Persona: DevOps Engineer**
- [x] **QA: full `MTM_Waitlist.Tests` suite passes** (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`). | **Persona: QA Engineer**
**GATE: 0 errors + 0 warnings + full green suite before/after this file.**

## UI Guidance (embedded — per-user toggle & toast)

- Add a per-user Settings toggle, **off by default**: "alert me when a new waitlist request is ready." Show it as a standard WinUI `ToggleSwitch` with a short description. Notifications are delivered as an app toast **only when packaged** (`RuntimeHelper.IsMSIX`); unpackaged = no-op fallback. Tapping a toast **deep-links** to that request's detail page (extend `AppNotificationActivationHandler`, currently a TODO placeholder, to navigate to the request). Localize toggle text via `.resw`.

## Subphase 0.1 — Per-user toggle (off by default)

**PREREQUISITE: Task 0 green.**

- [x] **Settings Card: add a per-user "alert me on new waitlist requests" toggle (default OFF)** on the Settings screen, persisted per user via `ILocalSettingsService`. *(Ref: Master Task 19)* | **Persona: Frontend Engineer** — verified 2026-09-06: `INewRequestAlertService`/`NewRequestAlertService` (Core) owns key `User.NewRequestAlertsEnabled` (default OFF, persisted via `ILocalSettingsService`, per-Windows-user). `SettingsViewModel` exposes `NewRequestAlertsEnabled` (init default-off, persists on change via the service) + `IsNewRequestAlertsPanelVisible` search visibility folded into `IsOperationsCategoryVisible`. A **New Request Alerts** `Expander`+`SettingsCard`+`ToggleSwitch` card was added to the Operations category in `SettingsPage.xaml` (mirrors the Mock Data card). Build clean; full suite green (488 passed).
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for default-off, toggle persist/read, and per-user scope**, then run the **full suite**; must pass. | **Persona: QA Engineer** — verified 2026-09-06: `NewRequestAlertServiceTests` (5: default-off read, stored read, persist under key, signal×toggle×packaged decision, off-toggle no-op) + `SettingsViewModelTests` additions (3: defaults-off, panel/category search visibility, toggle persists under key). Full suite green (488 passed).

## Subphase 0.2 — Toast delivery + deep-link

- [x] **Service Layer: when the toggle is ON and the app is packaged, show an app notification/toast on a newly submitted request** via `IAppNotificationService`; unpackaged or OFF = no-op. *(Ref: Master Task 19)* | **Persona: Backend Engineer** — verified 2026-09-06: `WaitlistRequestService.SubmitAsync` (the single create path for BOTH mock-ON and production) now calls `INewRequestAlertNotifier.NotifyNewRequestAsync(request.Id, title, body, RuntimeHelper.IsMSIX)` before returning `Success`. The notifier folds the request-created signal × per-user toggle × packaged through `RequestAlertGate`/`NewRequestAlertService`, builds the deep-link toast XML, and shows it via `IAppNotificationService` (unpackaged/OFF = no-op; failures never break the submit). Added optional `INewRequestAlertNotifier?` ctor param (default null) so existing constructions compile; localized title/body keys `NewRequestAlert_Title`/`NewRequestAlert_Body` added to `Resources.resw`. Build clean; full suite green (498 passed).
- [x] **Workflow: wire notification deep-linking** so tapping a toast opens that request's detail page (extend `AppNotificationActivationHandler` to navigate by request id). *(Ref: Master Task 19)* | **Persona: Full Stack Engineer** — verified 2026-09-06: `AppNotificationActivationHandler.HandleInternalAsync` now reads the `AppNotificationActivatedEventArgs.Argument`, parses it with `WaitlistRequestLink.TryParse` (`action=openrequest&request=<guid>`), and — when it matches — enqueues (low priority, after the shell/nav frame is up) a navigation to the Waitlist request detail page (by view-model full-name key, param = `requestId.GetHashCode()`, matching how the detail resolves a request) and brings the window to front. Unrecognized arguments keep the original placeholder fallback. Build clean; full suite green (498 passed). NOTE: full request rendering on a cold start still depends on the request being loaded into the selected building's in-memory set (existing detail lookup contract) — that is the known detail-resolution gap, not the toast routing.
- [x] **Testing: add tests in `MTM_Waitlist.Tests` for notification decision (toggle × packaged) and activation routing**, then run the **full suite**; must pass. | **Persona: QA Engineer** — verified 2026-09-06: decision (toggle × packaged × signal) is covered by `NewRequestAlertNotifierTests` (4) + `NewRequestAlertServiceTests` (5) + `RequestAlertGateTests` (2); the deep-link argument routing inputs are covered by `WaitlistRequestLinkTests` (4: build round-trip, wrong-action reject, missing/invalid request reject). The navigation/activation handler itself is app-lifecycle (build-verified); its routing inputs are unit-covered. Full suite green (498 passed).

**GATE: toggle is per-user/off-by-default; toast fires only when ON + packaged; tap deep-links to the request; full suite green. Move to `10-Phase3-analytics.md`.**
Next task: **Subphase 0.1 per-user toggle** | **Persona: Frontend Engineer**

## Mock-data flow coverage (added 2026-09-05)
>
> The new-request alert decision is (toggle × packaged). Confirm the mock path still honors `RuntimeHelper.IsMSIX` and does not surface notifications unpackaged; no extra sample data is required, but a mock-ON submitted request should still raise the request-created signal consumed by the alert decision.

- [x] **Backend Engineer: confirm/cover the alert decision with mock ON** — a mock-ON new request still triggers the request-created signal and the toggle × packaged decision path (toast only when ON + packaged); no-op otherwise. *(Mock parity)* — verified 2026-09-06: mock-ON `SubmitAsync` flows through the same notifier call as production (`WaitlistRequestServiceTests.SubmitAsync_MockOn_InvokesNewRequestAlertNotifier` proves the notifier is invoked with the created request id on mock-ON success; `SubmitAsync_ValidationFailure_DoesNotInvokeNotifier` proves no signal on failure).
- [x] **QA Engineer: add tests** for the alert decision (toggle × packaged) with a mock-ON request; run the **full suite**. *(Mock parity)* — verified 2026-09-06: `WaitlistRequestServiceTests.SubmitAsync_MockOn_InvokesNewRequestAlertNotifier` proves a mock-ON submit raises the request-created signal to the notifier; the toast decision (toggle × packaged) is covered by `NewRequestAlertNotifierTests`. Full suite green (498 passed).

## Mockups (UI references)

See `../Mockups/` for the target UI for this task:

- `../Mockups/settings-notifications-toggle.svg` — per-user new-request alert toggle (off default)
- `../Mockups/notifications-request-toast.svg` — toast + deep-link to the request
