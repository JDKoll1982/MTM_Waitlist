---
description: "InAppNotification (CommunityToolkit.WinUI.UI.Controls.InAppNotification) in Controls; a non-modal notification banner inside your window — the… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/InAppNotificationDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/InAppNotificationDemo.xaml.cs"
---

# InAppNotification

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Intermediate |
| API | `CommunityToolkit.WinUI.UI.Controls.InAppNotification` |
| Page | `Toolkit/InAppNotificationDemo` (`WinUI3LearningLab.Demos.Toolkit.InAppNotificationDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo in-app-notification` |

## What it teaches

A non-modal notification banner inside your window — the right choice for messages that must not interrupt.

## How the demo is built

The page is a `UserControl` at `Toolkit/InAppNotificationDemo.xaml`, paired with `InAppNotificationDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 22 element(s).

Named elements the code-behind touches: `StackSwitch`, `ClosedText`, `StatusText`, `Banner`.

Wired in XAML: `OnInfoClicked()`, `OnSuccessClicked()`, `OnWarningClicked()`, `OnDismissClicked()`, `OnStackModeToggled()`, `OnBannerClosed()`.

**Code-behind.** Handlers `OnInfoClicked()`, `OnSuccessClicked()`, `OnWarningClicked()`, `OnDismissClicked()`, `OnStackModeToggled()`, `OnBannerClosed()`. 100 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Show is explicit; there is no queueing unless you manage it, so two calls overwrite the first message.
- It positions itself relative to its parent — put it in a top-level Grid so it can overlay everything.
- Closing raises events with a reason; use that to distinguish a timeout from a user dismissal.

## Try this

- Run `WinUI3LearningLab.App.exe --demo in-app-notification`. The page shows `InAppNotificationDemo.xaml` and `InAppNotificationDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.InAppNotification](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
