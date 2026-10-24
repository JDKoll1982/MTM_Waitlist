---
description: "GridView (Microsoft.UI.Xaml.Controls.GridView) in Collections; a horizontally-flowing, virtualising item view for tile and thumbnail layouts. MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/GridViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/GridViewDemo.xaml.cs"
---

# GridView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.GridView` |
| Page | `Collections/GridViewDemo` (`WinUI3LearningLab.Demos.Collections.GridViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo grid-view` |

## What it teaches

A horizontally-flowing, virtualising item view for tile and thumbnail layouts.

## How the demo is built

The page is a `UserControl` at `Collections/GridViewDemo.xaml`, paired with `GridViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 22 element(s).

Named elements the code-behind touches: `AreaGrid`, `MultiSwitch`, `SelectionText`.

Wired in XAML: `OnAreaSelectionChanged()`, `OnSelectionModeToggled()`.

**Code-behind.** Handlers `OnSelectionModeToggled()`, `OnAreaSelectionChanged()`. 50 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- GridView scrolls horizontally by default; put it inside a vertically scrolling parent only with an explicit height.
- ItemsPanel controls the layout — ItemsWrapGrid for rows that wrap, ItemsStackPanel for a single row.
- Selection is single by default; SelectionMode=Multiple with IsItemClickEnabled changes the interaction model completely.

## Try this

- Run `WinUI3LearningLab.App.exe --demo grid-view`. The page shows `GridViewDemo.xaml` and `GridViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.GridView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.gridview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
