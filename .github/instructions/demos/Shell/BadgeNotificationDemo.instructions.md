---
description: "Badge notifications (Microsoft.Windows.BadgeNotifications.BadgeNotificationManager) in Notifications & jump lists; the numeric or glyph badge on the taskbar icon, driven by… MTM: MTM_Waitlist › Waitlist › notification of assignment."
applyTo: "src/WinUI3LearningLab.App/Demos/Shell/BadgeNotificationDemo.xaml,src/WinUI3LearningLab.App/Demos/Shell/BadgeNotificationDemo.xaml.cs"
---

# Badge notifications

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Notifications & jump lists |
| Level | Advanced |
| API | `Microsoft.Windows.BadgeNotifications.BadgeNotificationManager` |
| Page | `Shell/BadgeNotificationDemo` (`WinUI3LearningLab.Demos.Shell.BadgeNotificationDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo badge-notification` |

## What it teaches

The numeric or glyph badge on the taskbar icon, driven by BadgeNotificationManager.

## Availability

This lab is unpackaged. Badge notifications need an app identity, so the demo calls the API inside a guard and reports the failure it receives instead of pretending to work.

## Package identity

This API needs MSIX package identity. The lab is unpackaged (`WindowsPackageType=None`), so the page probes the real call and reports the exception instead of faking a result. See `.github/instructions/packaging-and-identity.instructions.md`.

## How the demo is built

The page is a `UserControl` at `Shell/BadgeNotificationDemo.xaml`, paired with `BadgeNotificationDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 29 element(s).

Named elements the code-behind touches: `FactsText`, `CountBox`, `GlyphBox`, `LastActionText`.

Wired in XAML: `OnSetCountClicked()`, `OnSetGlyphClicked()`, `OnClearClicked()`.

**Code-behind.** Handlers `OnSetCountClicked()`, `OnSetGlyphClicked()`, `OnClearClicked()`. 100 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › notification of assignment**

An operator needs to know a request was assigned to their lugger while the board is behind another window. A toast is the mechanism; the badge is the persistent count of what is still open. Both depend on the app having an identity, which this lab deliberately does not have.

The taskbar badge is the persistent count of open requests. It is keyed by the app's AUMID, which an unpackaged build does not have — so this page proves the failure instead of faking it.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Badge updates need package identity for the classic path; in an unpackaged app this throws and must be guarded.
- The badge is a per-app identity feature — without registration the call fails silently or with an HRESULT.
- Clear the badge when the user has seen the content; a stale badge trains people to ignore it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo badge-notification`. The page shows `BadgeNotificationDemo.xaml` and `BadgeNotificationDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.BadgeNotifications.BadgeNotificationManager](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.badgenotifications.badgenotificationmanager)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Shell.cs` or the demo source, then regenerate.
