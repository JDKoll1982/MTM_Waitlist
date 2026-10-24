---
description: "App notifications (Microsoft.Windows.AppNotifications.AppNotificationManager) in Notifications & jump lists; toasts from a desktop app, with buttons, inputs and… MTM: MTM_Waitlist › Waitlist › notification of assignment."
applyTo: "src/WinUI3LearningLab.App/Demos/Shell/AppNotificationDemo.xaml,src/WinUI3LearningLab.App/Demos/Shell/AppNotificationDemo.xaml.cs"
---

# App notifications

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Notifications & jump lists |
| Level | Intermediate |
| API | `Microsoft.Windows.AppNotifications.AppNotificationManager` |
| Page | `Shell/AppNotificationDemo` (`WinUI3LearningLab.Demos.Shell.AppNotificationDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo app-notification` |

## What it teaches

Toasts from a desktop app, with buttons, inputs and activation callbacks — the Windows App SDK replacement for toast XML.

## How the demo is built

The page is a `UserControl` at `Shell/AppNotificationDemo.xaml`, paired with `AppNotificationDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `FactsText`, `InvocationText`, `LastActionText`.

Wired in XAML: `OnSendClicked()`, `OnRemoveAllClicked()`, `OnRegisterClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnUnloaded()`, `OnRegisterClicked()`, `OnSendClicked()`, `OnRemoveAllClicked()`, `OnNotificationInvoked()`. 176 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › notification of assignment**

An operator needs to know a request was assigned to their lugger while the board is behind another window. A toast is the mechanism; the badge is the persistent count of what is still open. Both depend on the app having an identity, which this lab deliberately does not have.

A toast tells the operator that a request was assigned to their lugger while the board was behind another window. It needs `AppNotificationManager.Register()` at start-up and works unpackaged.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Call AppNotificationManager.Default.Register() once at start-up, before showing anything.
- Handle NotificationInvoked to learn what the user clicked; activation can also arrive before the window exists.
- Unpackaged apps can show toasts, but the app must be registered first — otherwise nothing appears and no error is raised.
- Respect the user's notification settings and never send a toast for something the app could show inline.

## Traps recorded in the source

- Nothing to unsubscribe from; keep the page quiet on the way out.

## Try this

- Run `WinUI3LearningLab.App.exe --demo app-notification`. The page shows `AppNotificationDemo.xaml` and `AppNotificationDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.AppNotifications.AppNotificationManager](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications.appnotificationmanager)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Shell.cs` or the demo source, then regenerate.
