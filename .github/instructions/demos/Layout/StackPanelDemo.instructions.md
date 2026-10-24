---
description: "StackPanel (Microsoft.UI.Xaml.Controls.StackPanel) in Layout; stacks children in one direction. The simplest layout… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/StackPanelDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/StackPanelDemo.xaml.cs"
---

# StackPanel

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.StackPanel` |
| Page | `Layout/StackPanelDemo` (`WinUI3LearningLab.Demos.Layout.StackPanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo stack-panel` |

## What it teaches

Stacks children in one direction. The simplest layout primitive and the wrong tool for two-dimensional layouts.

## How the demo is built

The page is a `UserControl` at `Layout/StackPanelDemo.xaml`, paired with `StackPanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 37 element(s).

Named elements the code-behind touches: `StackStarRow`, `StackNestedScroll`, `GridStarRow`, `GridNestedScroll`, `MeasureText`.

**Code-behind.** Handlers `OnNestedScrollSizeChanged()`. 63 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In a vertical StackPanel children are measured with infinite height, so star-sized rows and ScrollViewers inside misbehave.
- Spacing is the supported way to gap children; margins add to the gap at both ends of the stack.
- For rows that should share height use a Grid.

## Try this

- Run `WinUI3LearningLab.App.exe --demo stack-panel`. The page shows `StackPanelDemo.xaml` and `StackPanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.StackPanel](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.stackpanel)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
