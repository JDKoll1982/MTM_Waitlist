---
description: "TitleBar (Microsoft.UI.Xaml.Controls.TitleBar) in Windowing; a XAML title bar with icon, title, subtitle, back button and custom content. MTM: MTM_Waitlist › Workstation identity and main window."
applyTo: "src/WinUI3LearningLab.App/Demos/Windowing/TitleBarDemo.xaml,src/WinUI3LearningLab.App/Demos/Windowing/TitleBarDemo.xaml.cs"
---

# TitleBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Windowing |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.TitleBar` |
| Page | `Windowing/TitleBarDemo` (`WinUI3LearningLab.Demos.Windowing.TitleBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo title-bar` |

## What it teaches

A XAML title bar with icon, title, subtitle, back button and custom content.

## How the demo is built

The page is a `UserControl` at `Windowing/TitleBarDemo.xaml`, paired with `TitleBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `PreviewTitleBar`, `BackToggle`, `PaneToggle`, `StateText`, `LastActionText`.

Wired in XAML: `OnBackRequested()`, `OnPaneToggleRequested()`, `OnBackVisibleToggled()`, `OnPaneVisibleToggled()`, `OnAttachClicked()`, `OnRestoreClicked()`.

**Code-behind.** Handlers `OnBackVisibleToggled()`, `OnPaneVisibleToggled()`, `OnBackRequested()`, `OnPaneToggleRequested()`, `OnAttachClicked()`, `OnRestoreClicked()`. 133 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Workstation identity and main window**

The app knows which workstation it is on from the computer registry table and keeps one window per user session. Title bar, size and restore behaviour are therefore part of the contract, not decoration.

Schema this mirrors: `core_computers_registry, user_active_sessions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Assign it to Window.ExtendsContentIntoTitleBar = true and Window.SetTitleBar(theTitleBar) so dragging and the system menu keep working.
- IsBackButtonVisible and IsPaneToggleButtonVisible are opt-in; the control draws no buttons by default.
- Without SetTitleBar the area is not draggable, which makes the window feel broken.

## Traps recorded in the source

- Leaving the page must not throw, even if the window already went away.

## Try this

- Run `WinUI3LearningLab.App.exe --demo title-bar`. The page shows `TitleBarDemo.xaml` and `TitleBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.TitleBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.titlebar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Windowing.cs` or the demo source, then regenerate.
