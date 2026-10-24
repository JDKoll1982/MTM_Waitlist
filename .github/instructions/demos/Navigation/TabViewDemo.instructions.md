---
description: "TabView (Microsoft.UI.Xaml.Controls.TabView) in Navigation; browser-style document tabs with drag-to-reorder, close buttons and an add button. MTM: MTM_Waitlist › Settings › sections and modules."
applyTo: "src/WinUI3LearningLab.App/Demos/Navigation/TabViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Navigation/TabViewDemo.xaml.cs"
---

# TabView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Navigation |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.TabView` |
| Page | `Navigation/TabViewDemo` (`WinUI3LearningLab.Demos.Navigation.TabViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo tab-view` |

## What it teaches

Browser-style document tabs with drag-to-reorder, close buttons and an add button.

## How the demo is built

The page is a `UserControl` at `Navigation/TabViewDemo.xaml`, paired with `TabViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 23 element(s).

Named elements the code-behind touches: `Tabs`, `StatusText`, `EventText`.

Wired in XAML: `OnAddTab()`, `OnTabCloseRequested()`, `OnTabItemsChanged()`.

**Code-behind.** Handlers `OnAddTab()`, `OnTabCloseRequested()`, `OnTabItemsChanged()`. 93 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings › sections and modules**

The settings area is a set of sections with a scope — universal, role, personal — and the user moves between them constantly. Navigation state has to survive that, and the current section has to be obvious from across the shop floor.

Schema this mirrors: `config_settings_values, settings_universal, settings_personal`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- TabItems are TabViewItem objects; binding ItemsSource requires TabItemTemplate and you lose some built-in behaviour.
- TabCloseRequested only raises the event — you must remove the item, or the tab stays.
- CanReorderTabs, CanDragTabs and CanCloseTabs are separate switches, and the add button is TabItemsChanged-driven.
- TabView is a document surface, not navigation: for modal section switching use SelectorBar or NavigationView.

## Try this

- Run `WinUI3LearningLab.App.exe --demo tab-view`. The page shows `TabViewDemo.xaml` and `TabViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.TabView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.tabview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Navigation.cs` or the demo source, then regenerate.
