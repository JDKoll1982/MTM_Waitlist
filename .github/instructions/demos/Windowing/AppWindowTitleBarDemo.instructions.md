---
description: "AppWindowTitleBar (Microsoft.UI.Windowing.AppWindowTitleBar) in Windowing; fine-grained control of the native title bar: colours, button… MTM: MTM_Waitlist › Workstation identity and main window."
applyTo: "src/WinUI3LearningLab.App/Demos/Windowing/AppWindowTitleBarDemo.xaml,src/WinUI3LearningLab.App/Demos/Windowing/AppWindowTitleBarDemo.xaml.cs"
---

# AppWindowTitleBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Windowing |
| Level | Advanced |
| API | `Microsoft.UI.Windowing.AppWindowTitleBar` |
| Page | `Windowing/AppWindowTitleBarDemo` (`WinUI3LearningLab.Demos.Windowing.AppWindowTitleBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo app-window-title-bar` |

## What it teaches

Fine-grained control of the native title bar: colours, button visibility, height and drag rectangles.

## How the demo is built

The page is a `UserControl` at `Windowing/AppWindowTitleBarDemo.xaml`, paired with `AppWindowTitleBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `FactsText`, `ColourText`, `ColourButton`, `LastActionText`.

Wired in XAML: `OnToggleColoursClicked()`, `OnToggleHeightClicked()`, `OnResetClicked()`, `OnReadBackClicked()`.

**Code-behind.** Handlers `OnReadBackClicked()`, `OnToggleColoursClicked()`, `OnToggleHeightClicked()`, `OnResetClicked()`. 184 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Workstation identity and main window**

The app knows which workstation it is on from the computer registry table and keeps one window per user session. Title bar, size and restore behaviour are therefore part of the contract, not decoration.

Schema this mirrors: `core_computers_registry, user_active_sessions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ExtendsContentIntoTitleBar must be set on the Window for the XAML title bar to replace the native one.
- Custom colours are ignored unless the app opts in by setting the colour properties together — partial sets look half-applied.
- Hit-test rectangles for dragging come from InputNonClientPointerSource in newer builds; the old SetDragRectangles is gone.

## Traps recorded in the source

- Restoring the caption bar is cosmetic; leaving the page must never throw.

## Try this

- Run `WinUI3LearningLab.App.exe --demo app-window-title-bar`. The page shows `AppWindowTitleBarDemo.xaml` and `AppWindowTitleBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Windowing.AppWindowTitleBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindowtitlebar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Windowing.cs` or the demo source, then regenerate.
