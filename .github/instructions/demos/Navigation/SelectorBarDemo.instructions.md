---
description: "SelectorBar (Microsoft.UI.Xaml.Controls.SelectorBar) in Navigation; a horizontal row of text (or icon) choices with an animated selection indicator… MTM: MTM_Waitlist › Settings › sections and modules."
applyTo: "src/WinUI3LearningLab.App/Demos/Navigation/SelectorBarDemo.xaml,src/WinUI3LearningLab.App/Demos/Navigation/SelectorBarDemo.xaml.cs"
---

# SelectorBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Navigation |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.SelectorBar` |
| Page | `Navigation/SelectorBarDemo` (`WinUI3LearningLab.Demos.Navigation.SelectorBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo selector-bar` |

## What it teaches

A horizontal row of text (or icon) choices with an animated selection indicator — the modern replacement for a tab strip.

## How the demo is built

The page is a `UserControl` at `Navigation/SelectorBarDemo.xaml`, paired with `SelectorBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 14 element(s).

Named elements the code-behind touches: `Selector`, `LedgerItem`, `InvoicesItem`, `ReconciliationItem`, `PanelTitle`, `PanelBody`, `PanelFact`, `StatusText`.

Wired in XAML: `OnSelectionChanged()`.

**Code-behind.** Handlers `OnSelectionChanged()`. 77 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings › sections and modules**

The settings area is a set of sections with a scope — universal, role, personal — and the user moves between them constantly. Navigation state has to survive that, and the current section has to be obvious from across the shop floor.

Schema this mirrors: `config_settings_values, settings_universal, settings_personal`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- SelectorBarItem is not a general container: use Text, Icon, or both, and set IsSelected for the initial choice.
- SelectionChanged fires with the previously selected item still available, so guard against null on the first load.
- It sizes to its content and does not scroll; overflow needs a ScrollViewer around it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo selector-bar`. The page shows `SelectorBarDemo.xaml` and `SelectorBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.SelectorBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.selectorbar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Navigation.cs` or the demo source, then regenerate.
