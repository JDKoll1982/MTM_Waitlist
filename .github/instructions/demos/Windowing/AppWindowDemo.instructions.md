---
description: "AppWindow (Microsoft.UI.Windowing.AppWindow) in Windowing; the window as the shell sees it: size, position, presenter (full screen, compact… MTM: MTM_Waitlist › Workstation identity and main window."
applyTo: "src/WinUI3LearningLab.App/Demos/Windowing/AppWindowDemo.xaml,src/WinUI3LearningLab.App/Demos/Windowing/AppWindowDemo.xaml.cs"
---

# AppWindow

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Windowing |
| Level | Intermediate |
| API | `Microsoft.UI.Windowing.AppWindow` |
| Page | `Windowing/AppWindowDemo` (`WinUI3LearningLab.Demos.Windowing.AppWindowDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo app-window` |

## What it teaches

The window as the shell sees it: size, position, presenter (full screen, compact overlay) and title bar.

## How the demo is built

The page is a `UserControl` at `Windowing/AppWindowDemo.xaml`, paired with `AppWindowDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 24 element(s).

Named elements the code-behind touches: `FactsText`, `PresenterText`, `ScaleText`, `MaximizeToggle`, `LastActionText`.

Wired in XAML: `OnResizeClicked()`, `OnMoveAndResizeClicked()`, `OnCenterClicked()`, `OnRefreshClicked()`, `OnRestoreSizeClicked()`, `OnMaximizeToggled()`.

**Code-behind.** Handlers `OnRefreshClicked()`, `OnResizeClicked()`, `OnRestoreSizeClicked()`, `OnMoveAndResizeClicked()`, `OnCenterClicked()`, `OnMaximizeToggled()`. 195 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Workstation identity and main window**

The app knows which workstation it is on from the computer registry table and keeps one window per user session. Title bar, size and restore behaviour are therefore part of the contract, not decoration.

The main window is restored to the size and position the workstation last used, keyed by the row in `core_computers_registry`.

Schema this mirrors: `core_computers_registry, user_active_sessions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Get it from the Window: Window.AppWindow in WinUI 3 1.4+, or WindowNative.GetWindowHandle + WindowIdFromWindow on older code.
- Resize takes physical pixels, not DIPs — on a 150% display a 800 px request looks smaller than you expect.
- A presenter change is asynchronous in effect; read AppWindow.Presenter after the change, not before.

## Try this

- Run `WinUI3LearningLab.App.exe --demo app-window`. The page shows `AppWindowDemo.xaml` and `AppWindowDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Windowing.AppWindow](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.appwindow)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Windowing.cs` or the demo source, then regenerate.
