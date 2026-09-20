# Medium — Settings — "New Request Alerts" can never fire in the shipped application

> **Naming note (2026-09-20).** The **Planned fix** row below names specs by their **template** number, from
> `WeekendProject/SpecTemplates/` -- the seeds written before the specs existed -- so `Spec 01-...` is *not*
> `specs/001-...`. Those templates have since shipped under different numbers: `01-truthful-data-and-controls`
> is now **`specs/002-truthful-data-and-controls`** (shipped), `02-handler-fulfilment-and-urgency` is
> **`specs/003-waitlist-handler-fulfilment`** (shipped), and `03-unified-card-and-taxonomy` is
> **`specs/004-unified-card-item-picker`** (in flight). Templates **04-08** have no spec yet. The single copy
> of this map is `WeekendProject/SpecTemplates/00-INDEX.md`. The **Status** row above already uses the real
> `specs/00N` names -- read that one for what actually happened.


| Field | Value |
| --- | --- |
| **Criticality** | Medium |
| **Feature area** | Settings → Operations → New Request Alerts (and the new-request toast it controls) |
| **Type** | Advertised feature that cannot function in the shipped configuration; capability gate reported in prose instead of enforced in the UI |
| **Found** | 2026-09-12, working tree at `7bf6857` |
| **Status** | **FIXED (surface honesty; option 2 of §5 applied) — closed by `specs/002-truthful-data-and-controls`**. **Closure:** `IsNewRequestAlertsAvailable` reports the installation's real capability, the toggle's `IsEnabled` binds to it, the static packaged-app sentence was replaced by a localized reason (`Settings_NewRequestAlerts.Unavailable`), and no preference is written while the capability is false. **Remainder owned by Spec `08-notification-delivery-and-packaging`**, which settles packaging versus an unpackaged alternative and makes the alert fire. **Proof:** `MTM_Waitlist.Tests/Module_Settings/SettingsViewModelTests` (the view model reports the capability false here; no preference is stored while it is false). |
| **Planned fix** | **Spec `01-truthful-data-and-controls`** — gates the toggle on `RuntimeHelper.IsMSIX` (and states the reason) so the surface stops promising a capability this installation lacks; that is option 2 of §5 and is the honest state either way. The delivery decision is **Spec `08-notification-delivery-and-packaging`**, which settles packaging versus an unpackaged alternative and then makes the alert actually fire. Seeds: `WeekendProject/SpecTemplates/01-truthful-data-and-controls.md`, `…/08-notification-delivery-and-packaging.md` |
| **Files** | `MTM_Waitlist.csproj` (line 16), `MTM_Waitlist.Waitlist.View/Services/WaitlistRequestService.cs` (line 809), `MTM_Waitlist.Core/Services/RequestAlertGate.cs`, `NewRequestAlertService.cs`, `NewRequestAlertNotifier.cs`, `AppNotificationService.cs`, `MTM_Waitlist.Settings/Views/SettingsPage.xaml` (lines 954–990) |

---

## 1. Summary

The app ships **unpackaged** — `MTM_Waitlist.csproj` sets `<WindowsPackageType>None</WindowsPackageType>`
(line 16) and the documented deployment copies a self-contained folder into
`%LOCALAPPDATA%\Programs\MTM_Waitlist`. Windows app notifications require package identity, and the
code checks for it at every step:

```csharp
// WaitlistRequestService.cs:809 — the only caller of the notifier
.NotifyNewRequestAsync(request.Id, title, body, RuntimeHelper.IsMSIX)

// RequestAlertGate.cs — the decision
public static bool ShouldShowToast(bool alertsEnabled, bool isPackaged) => alertsEnabled && isPackaged;

// AppNotificationService.Show — the last line of defence
if (!RuntimeHelper.IsMSIX) { return false; }

// NewRequestAlertNotifier — the observable result
StartupDebugLog.Info("NewRequestAlert", "Skipping new-request toast: toggle off or app not packaged.");
```

`RuntimeHelper.IsMSIX` is false in the shipped app (no package identity), so
`alertsEnabled && isPackaged` is always false and the toast is never shown. Meanwhile the Settings
toggle is fully enabled and only *explains* the limitation in body text:

> "Get notified when a new waitlist request is submitted. **Notifications are only shown when the app
> is installed as a packaged app.**"

## 2. Impact

- A user turns the toggle **On**, sees "On", and never receives an alert. Nothing in the app ever
  tells them why, and the app's own debug log line blames "toggle off **or** app not packaged".
- Anyone who provisioned this feature (and anyone reading `WeekendProject/ChangeLog.Simple.md`
  feature 18, marked ✅) believes the alerts work.
- The settings surface is honest in prose but dishonest in behaviour: the control's state implies a
  capability the build does not have.

## 3. Evidence

| Claim | Evidence |
| --- | --- |
| The app is unpackaged | `MTM_Waitlist.csproj:16` `<WindowsPackageType>None</WindowsPackageType>`; `Properties/PublishProfiles/win-x64-selfcontained.pubxml`; `README.md` deploy section. |
| Package identity is the gate | `MTM_Waitlist.Core/Helpers/RuntimeHelper.cs:11–16` `IsMSIX` via `GetCurrentPackageFullName`. |
| The gate is consulted on the live path | `WaitlistRequestService.cs:809` passes `RuntimeHelper.IsMSIX` into `INewRequestAlertNotifier.NotifyNewRequestAsync`. |
| The gate fails closed | `RequestAlertGate.ShouldShowToast` / `ShouldNotifyOnCreated`; `AppNotificationService.Initialize` (line 28) and `Show` (line 47) both no-op when `IsMSIX` is false. |
| The UI does not reflect it | `Module_Settings/Views/SettingsPage.xaml:954–990` — the `ToggleSwitch` binds only `IsOn="{x:Bind ViewModel.NewRequestAlertsEnabled, Mode=TwoWay}"`; no `IsEnabled` or visibility gate, and the explanation is static text. |
| Tests pass anyway | `RequestAlertGateTests` asserts the `isPackaged: false` branch returns false — i.e. the tests *encode* the failure rather than protecting the feature. |

## 4. Root cause

The feature was built for a packaged (MSIX) deployment; the product then chose unpackaged,
self-contained deployment. The notification path was made crash-safe for that choice (the "FIX:"
comments in `AppNotificationService`) but the user-facing surface was never revisited, so the toggle
still offers a capability the deployment cannot provide.

## 5. Fix — one of three, and the choice is a product decision

1. **Package the app** (`Package.appxmanifest` + `Package.appinstaller` already exist in the repo as
   an unfilled template, and `EnableMsixTooling` is already true). Then the alerts work as designed —
   but this contradicts the current per-user, self-contained deploy decision and needs a signing and
   update story.
2. **Gate the UI on the capability** — the smallest correct change: bind the toggle's `IsEnabled`
   (and the card's description) to `RuntimeHelper.IsMSIX`, and replace the static explanation with a
   localized "not available in this installation" message sourced from the resource file. Then the
   setting never claims a capability the build lacks.
3. **Implement an unpackaged equivalent** — for example a tray/`Shell_NotifyIcon` balloon or an
   in-app prompt. Only worth it if the alert is genuinely wanted before packaging; it is a new
   feature, not a fix.

Until one is chosen, option 2 should at least be applied, because it costs one binding and removes a
false promise from the settings screen.

**Also correct:** `WeekendProject/ChangeLog.Simple.md` feature 18 (✅ "New Request Alerts toggle +
toast + deep link") and `WeekendProject/ChangeLog.md` should record the packaged-only caveat.

## 6. Verification

1. Unit test: `RequestAlertGate`/`NewRequestAlertNotifier` behaviour is already covered; add an
   assertion that the *settings view model* reports the capability
   (e.g. `IsNewRequestAlertsAvailable` false when `RuntimeHelper.IsMSIX` is false) — this is the new
   contract the UI binds to.
2. Manual (unpackaged Debug build): open Settings → Operations → New Request Alerts and confirm the
   toggle is disabled/annotated as unavailable, and that no toast is claimed.
3. If option 1 is chosen instead: install the MSIX, enable the toggle, submit a request from a second
   account, confirm the toast appears and the tap routes to the request via
   `AppNotificationActivationHandler` — and fix `defects/Closed-Partial-Low-Notifications-ToastActivationShowsTodoDialog.md`
   first, because tapping a toast while the app is running currently shows a "TODO" dialog.
4. Suite `Failed: 0`; build `0 Warning(s) 0 Error(s)`.

## 7. Related

- `defects/Closed-Partial-Low-Notifications-ToastActivationShowsTodoDialog.md` — the placeholder dialog on the
  toast-activation path.
- `.github/instructions/packaging.instructions.md` — packaging rules for this repo;
  `Package.appxmanifest` / `Package.appinstaller`.
- `FEATURES.md` §5, §8, §9.
