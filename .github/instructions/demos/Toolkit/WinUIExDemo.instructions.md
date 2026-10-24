---
description: "WinUIEx (WinUIEx.WindowEx) in Controls; small, practical window helpers on top of the Windows App SDK: persisted size/position… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/WinUIExDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/WinUIExDemo.xaml.cs"
---

# WinUIEx

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Intermediate |
| API | `WinUIEx.WindowEx` |
| Page | `Toolkit/WinUIExDemo` (`WinUI3LearningLab.Demos.Toolkit.WinUIExDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo winui-ex` |

## What it teaches

Small, practical window helpers on top of the Windows App SDK: persisted size/position, backdrop helpers, DPI utilities.

## How the demo is built

The page is a `UserControl` at `Toolkit/WinUIExDemo.xaml`, paired with `WinUIExDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `FactsText`, `StatusText`.

Wired in XAML: `OnRefreshClicked()`, `OnResizableClicked()`, `OnAlwaysOnTopClicked()`, `OnCenterClicked()`, `OnForegroundClicked()`, `OnRestoreClicked()`.

**Code-behind.** Handlers `OnRefreshClicked()`, `OnResizableClicked()`, `OnAlwaysOnTopClicked()`, `OnCenterClicked()`, `OnForegroundClicked()`, `OnRestoreClicked()`. 184 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- WindowEx derives from Window, so a WinUIEx window is still a plain WinUI 3 window — no lock-in.
- Persistence helpers write to your app data; call the persist method after resizing, not once at start-up.
- Not every helper is safe unpackaged — check the individual API when the app has no package identity.

## Try this

- Run `WinUI3LearningLab.App.exe --demo winui-ex`. The page shows `WinUIExDemo.xaml` and `WinUIExDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [WinUIEx.WindowEx](https://github.com/dotMorten/WinUIEx)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
