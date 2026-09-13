# Contract — Notification activation

**Feature**: `002-truthful-data-and-controls` | **Spec**: [../spec.md](../spec.md) | Requirements: FR-022, FR-023, FR-024

Both entry points are covered: `MTM_Waitlist.Core/Activation/AppNotificationActivationHandler.cs` (the
activation path) and `MTM_Waitlist.Core/Services/AppNotificationService.cs` `OnNotificationInvoked` (the
while-running path). They MUST NOT drift apart — one shared helper does the parsing and the navigation.

---

## C1 — Shared handler

```
TryHandleRequestDeepLink(string? arguments) -> bool
```

1. Parse with the existing `MTM_Waitlist.Core/Services/WaitlistRequestLink.cs` (`TryParse` accepts
   `action=openrequest&request=<guid>`; `Build(Guid)` produces it).
2. On success: navigate to the waitlist detail page for that request and bring the window forward; return
   `true`.
3. On failure: record the event for diagnosis, bring the window forward, return `false`.

Both callers invoke this helper; neither contains its own parse or its own fallback UI.

## C2 — Outcome matrix

| Situation | Required outcome |
| --- | --- |
| App running, arguments map to a request | The named request's detail page is shown, the window is brought forward, and **no dialog of any kind** appears |
| App running, arguments do not map | Nothing visible is shown; the event is recorded for diagnosis; the window is brought forward |
| App not running (cold start), arguments map | Unchanged from today's verified behaviour |
| App not running, arguments do not map | Nothing visible; the event is recorded; **no new placeholder path** is introduced by removing the old one |
| Any state, repeated activation | Same outcome each time; no dialog accumulates; no navigation stack growth beyond one detail page |

## C3 — Forbidden

- `ContentDialog` / `MessageBox` / any developer-facing surface on either activation path.
- Ignoring the `AppNotificationActivatedEventArgs.Argument` on the while-running path (today's defect: the
  handler is handed the argument and discards it).
- A `TODO` marker in the notification path (the scan gate covers the notification placeholder marker).
- Registering notifications, changing packaging, or delivering a new-request toast — that is a later
  specification's scope, and this feature only makes the existing paths honest.

## C4 — Diagnostics

The unrecognised case is recorded through the existing diagnostic seam (`StartupDebugLog` and, where
present, the service log) with the raw arguments at debug level. It is a diagnostic record, not a
user-facing message: the screen stays silent by design.

<!-- token-budget: compacted (level=medium) on 2026-09-12T23:51:51Z; original at notification-activation-contract.full.md -->
