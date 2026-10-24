---
description: "ContentIsland (Microsoft.UI.Content.ContentIsland) in Clipboard & pickers; the composable unit behind every WinUI window: a tree of… MTM: MTM_Receiving_Application › Receiving › copy, export and file selection."
applyTo: "src/WinUI3LearningLab.App/Demos/SystemServices/ContentIslandDemo.xaml,src/WinUI3LearningLab.App/Demos/SystemServices/ContentIslandDemo.xaml.cs"
---

# ContentIsland

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Clipboard & pickers |
| Level | Advanced |
| API | `Microsoft.UI.Content.ContentIsland` |
| Page | `SystemServices/ContentIslandDemo` (`WinUI3LearningLab.Demos.SystemServices.ContentIslandDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo content-island` |

## What it teaches

The composable unit behind every WinUI window: a tree of content the compositor can host anywhere.

## Availability

Documentation-only page: hosting an island needs a Composition-level host (a second HWND or a ContentSite), which does not fit inside this page's demo slot.

## How the demo is built

The page is a `UserControl` at `SystemServices/ContentIslandDemo.xaml`, paired with `ContentIslandDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 28 element(s).

Named elements the code-behind touches: `ScenarioBox`, `HostCaption`, `SiteCaption`, `IslandCaption`, `XamlCaption`, `ScenarioText`.

Wired in XAML: `OnScenarioChanged()`.

**Code-behind.** Handlers `OnScenarioChanged()`. 54 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › copy, export and file selection**

Clerks paste part lists into emails and export a shift to a file for the planner, then pick the label template to print. These are desktop capabilities with no WinUI equivalent, so the interop rules — window handle, clipboard threading — are the whole lesson.

Schema this mirrors: `dunnage_label_data, view_receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ContentIsland is the modern replacement for XAML islands; WinUI 3 windows are themselves built on a ContentSite + island pair.
- Hosting XAML inside an existing HWND is still done with DesktopWindowXamlSource; islands are for Composition-level hosting.
- The types live in Microsoft.UI.Content and require careful lifetime handling — disposing the island tears down its XAML tree.

## Try this

- Run `WinUI3LearningLab.App.exe --demo content-island`. The page shows `ContentIslandDemo.xaml` and `ContentIslandDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Content.ContentIsland](https://learn.microsoft.com/windows/apps/develop/ui-input/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/SystemServices.cs` or the demo source, then regenerate.
