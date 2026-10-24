---
description: "VariableSizedWrapGrid (Microsoft.UI.Xaml.Controls.VariableSizedWrapGrid) in Layout; wraps items into rows, with items able to span several cells… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/VariableSizedWrapGridDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/VariableSizedWrapGridDemo.xaml.cs"
---

# VariableSizedWrapGrid

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.VariableSizedWrapGrid` |
| Page | `Layout/VariableSizedWrapGridDemo` (`WinUI3LearningLab.Demos.Layout.VariableSizedWrapGridDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo variable-sized-wrap-grid` |

## What it teaches

Wraps items into rows, with items able to span several cells — the live-tile style layout.

## How the demo is built

The page is a `UserControl` at `Layout/VariableSizedWrapGridDemo.xaml`, paired with `VariableSizedWrapGridDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `OrientationBox`, `ItemWidthSlider`, `Wrap`, `SpanText`.

Wired in XAML: `OnOrientationChanged()`, `OnItemWidthChanged()`.

**Code-behind.** Handlers `OnOrientationChanged()`, `OnItemWidthChanged()`. 45 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Item size comes from ItemWidth/ItemHeight or the first child's size, not from the children individually.
- Orientation changes the wrap axis, which also changes what RowSpan and ColumnSpan mean.
- It is not virtualising: for large collections use ItemsRepeater with UniformGridLayout instead.

## Try this

- Run `WinUI3LearningLab.App.exe --demo variable-sized-wrap-grid`. The page shows `VariableSizedWrapGridDemo.xaml` and `VariableSizedWrapGridDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.VariableSizedWrapGrid](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.variablesizedwrapgrid)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
