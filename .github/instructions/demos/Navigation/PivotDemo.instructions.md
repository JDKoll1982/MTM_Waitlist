---
description: "Pivot (Microsoft.UI.Xaml.Controls.Pivot) in Navigation; tab-like navigation with a sliding header, historically used for top-level sections on… MTM: MTM_Waitlist › Settings › sections and modules."
applyTo: "src/WinUI3LearningLab.App/Demos/Navigation/PivotDemo.xaml,src/WinUI3LearningLab.App/Demos/Navigation/PivotDemo.xaml.cs"
---

# Pivot

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Navigation |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.Pivot` |
| Page | `Navigation/PivotDemo` (`WinUI3LearningLab.Demos.Navigation.PivotDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo pivot` |

## What it teaches

Tab-like navigation with a sliding header, historically used for top-level sections on phones.

## How the demo is built

The page is a `UserControl` at `Navigation/PivotDemo.xaml`, paired with `PivotDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 24 element(s).

Named elements the code-behind touches: `ShipmentsPivot`, `StatusText`.

Wired in XAML: `OnSelectionChanged()`.

**Code-behind.** Handlers `OnSelectionChanged()`. 31 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings › sections and modules**

The settings area is a set of sections with a scope — universal, role, personal — and the user moves between them constantly. Navigation state has to survive that, and the current section has to be obvious from across the shop floor.

Schema this mirrors: `config_settings_values, settings_universal, settings_personal`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Pivot is effectively frozen in its WinUI 2 form; SelectorBar and TabView are the current options for desktop.
- PivotItem headers are objects, and the header template is shared for all items.
- SelectionChanged gives you the item, but the pivot animates the header only when IsHeaderItemsCarouselEnabled is on.

## Try this

- Run `WinUI3LearningLab.App.exe --demo pivot`. The page shows `PivotDemo.xaml` and `PivotDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Pivot](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.pivot)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Navigation.cs` or the demo source, then regenerate.
