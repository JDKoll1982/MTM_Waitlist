# Low — Notifications — Tapping a new-request toast shows an internal "TODO" dialog

| Field | Value |
| --- | --- |
| **Criticality** | Low today (unreachable while the app is unpackaged — see `defects/Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md`) — **becomes Medium the moment the app is packaged**, because it is the common user path |
| **Feature area** | Notifications — toast activation / deep link |
| **Type** | Shipped placeholder dialog on a user-facing path |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED — closed by `specs/002-truthful-data-and-controls`**. **Closure:** both placeholder dialogs were deleted (`AppNotificationService.OnNotificationInvoked` and `AppNotificationActivationHandler`), the deep-link handling was factored into `MTM_Waitlist.Core/Activation/RequestDeepLinkHandler.cs`, a recognised activation opens the request, and an unrecognised one shows nothing and is recorded for diagnosis. **Remainder owned by Spec `08-notification-delivery-and-packaging`**, which exercises a real delivered notification on both activation paths. **Proof:** `MTM_Waitlist.Tests/Module_Core/Activation/RequestDeepLinkHandlerTests`. |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — deletes both placeholder dialogs, factors the deep-link handling into one shared helper, and logs unrecognised arguments instead of showing anything. Because the path is unreachable until the alert can fire, the *proof* lands with **Spec `08-notification-delivery-and-packaging`**, which exercises both activation paths (§6 of its seed). Seeds: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`, `…/08-notification-delivery-and-packaging.md` |
| **Files** | `MTM_Waitlist.Core/Services/AppNotificationService.cs` (lines 35–42), `MTM_Waitlist.Core/Activation/AppNotificationActivationHandler.cs` (lines 57–61) |

---

## 1. Summary

The deep link itself is implemented and correct: the toast's `launch` argument carries
`action=openrequest&request=<guid>` (`WaitlistRequestLink.Build`), and
`AppNotificationActivationHandler.HandleInternalAsync` parses it and navigates to the request's detail
page. What is **not** implemented is the rest of the theme — both fallback paths still show the
template's placeholder dialog:

```csharp
// AppNotificationService.cs:35–42 — fires when the app is ALREADY RUNNING and the toast is tapped
public void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
{
    _appWindowProvider.MainWindow.DispatcherQueue.TryEnqueue(() =>
    {
        _appWindowProvider.MainWindow.ShowMessageDialogAsync(
            "TODO: Handle notification invocations when your app is already running.", "Notification Invoked");
        _appWindowProvider.MainWindow.BringToFront();
    });
}

// AppNotificationActivationHandler.cs:57–61 — fires for any argument the app does not recognise
_appWindowProvider.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
{
    _appWindowProvider.MainWindow.ShowMessageDialogAsync("TODO: Handle notification activations.", "Notification Activation");
});
```

"While the app is already running" is the **normal** case for a shop-floor app that stays open all
shift, so as soon as notifications can fire, tapping one raises a dialog that says
**"TODO: Handle notification invocations when your app is already running."** — with no navigation to
the request, even though the payload is right there in `args`.

## 2. Impact

- An operator taps "New Waitlist Request" and gets an internal developer placeholder instead of the
  request; the alert is worse than useless because it interrupts without helping.
- Unrecognised arguments (for example the sample actions still shipped in the resource file,
  `action=ToastClick` / `action=Settings`) also surface a placeholder dialog rather than being ignored.
- Both are exactly the kind of string that should never reach a user in a plant-floor application.

## 3. Evidence

| Item | Evidence |
| --- | --- |
| Deep link is built correctly | `MTM_Waitlist.Core/Services/WaitlistRequestLink.cs` — `Build(Guid)` → `action=openrequest&request=<guid>`; `TryParse` validates it. |
| Cold-start activation is handled | `AppNotificationActivationHandler.HandleInternalAsync` → resolves the request id, queues navigation to `MTM_Waitlist.Module_Waitlist.ViewModels.WaitlistViewDetailViewModel` with `requestId.GetHashCode()`, and brings the window forward. |
| While-running invocation is not | `AppNotificationService.OnNotificationInvoked` (lines 35–42) — placeholder dialog only; it also ignores the `AppNotificationActivatedEventArgs.Argument` it is handed. |
| Unrecognised arguments are not | `AppNotificationActivationHandler` lines 57–61 — second placeholder dialog. |
| Sample arguments still shipped | `Strings/en-us/Resources.resw` `AppNotificationSamplePayload` contains `action=ToastClick` and `action=Settings`. |
| Unreachable today | The toast can never fire in the unpackaged build — see the packaged-only gate defect. |

## 4. Root cause

The template's notification plumbing was kept for the cold-start path (which was then properly
implemented) but the in-process invocation handler and the unrecognised-argument fallback were left at
their scaffold state. Nothing forced the issue because the app ships unpackaged, so no one has ever
tapped a toast.

## 5. Fix

1. **Handle the while-running invocation** in `AppNotificationService.OnNotificationInvoked` the same
   way the activation handler does — reuse `WaitlistRequestLink.TryParse(args.Argument, out var id)`
   and navigate (the service already holds `INavigationService` and `IAppWindowProvider`). If the
   argument is unrecognised, **do nothing but log it** (`StartupDebugLog`) and bring the window to the
   front; never show a placeholder dialog.
2. **Delete both TODO dialogs** and the `AppNotificationSamplePayload` resource entry if it has no
   consumer (a repository search shows none).
3. Factor the shared body into one place — a small internal helper (for example
   `TryHandleRequestDeepLink(string arguments)`) called by both the activation handler and the
   invocation handler — so the two paths cannot drift again.
4. Keep the existing behaviour of bringing the window to the front on activation; it is the one part
   that is already right.

## 6. Verification

1. Unit test the shared helper: `action=openrequest&request=<guid>` navigates to the Waitlist detail
   page key with the request's hash code; `action=Settings`, empty, and malformed values perform no
   navigation and no dialog.
2. Manual, on a packaged build (or with the gate temporarily forced true in a scratch build): submit a
   request from a second account, leave the app running, tap the toast, and confirm the request detail
   page opens and no dialog appears. Then repeat with the app closed, to confirm the cold-start path
   still works.
3. Grep the solution for `TODO: Handle notification` → no output.
4. Build `0 Warning(s) 0 Error(s)`; suite `Failed: 0`.

## 7. Related

- `defects/Medium-Settings-NewRequestAlertsCannotFireUnpackaged.md` — why this path is currently
  unreachable, and the decision that will expose it.
- `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs:809` — the only toast producer.
- `FEATURES.md` §9.
