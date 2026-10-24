---
description: "ItemsView (Microsoft.UI.Xaml.Controls.ItemsView) in Collections; the modern composable item view: you supply the layout, the item template and… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/ItemsViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/ItemsViewDemo.xaml.cs"
---

# ItemsView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.ItemsView` |
| Page | `Collections/ItemsViewDemo` (`WinUI3LearningLab.Demos.Collections.ItemsViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo items-view` |

## What it teaches

The modern composable item view: you supply the layout, the item template and the interaction model.

## How the demo is built

The page is a `UserControl` at `Collections/ItemsViewDemo.xaml`, paired with `ItemsViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `WorkList`, `LayoutSwitch`, `SelectionText`, `LayoutStateText`.

Wired in XAML: `OnSelectionChanged()`, `OnItemInvoked()`, `OnLayoutToggled()`.

**Code-behind.** Handlers `OnSelectionChanged()`, `OnItemInvoked()`, `OnLayoutToggled()`. 73 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ItemsView needs an ItemsView.Layout — it has no default layout, so an empty ItemsView shows nothing.
- Selection is opt-in through SelectionMode and supports single, multiple and extended modes with a SelectionModel.
- This is the replacement path for GridView/ListView when you need full control over layout and selection.

## Try this

- Run `WinUI3LearningLab.App.exe --demo items-view`. The page shows `ItemsViewDemo.xaml` and `ItemsViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ItemsView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.itemsview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
