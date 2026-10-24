---
description: "Custom layouts (NonVirtualizingLayout) (Microsoft.UI.Xaml.Controls.NonVirtualizingLayout) in Layout; a layout describes Measure and Arrange for ItemsRepeater and… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/LayoutPanelDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/LayoutPanelDemo.xaml.cs"
---

# Custom layouts (NonVirtualizingLayout)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.NonVirtualizingLayout` |
| Page | `Layout/LayoutPanelDemo` (`WinUI3LearningLab.Demos.Layout.LayoutPanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo layout-panel` |

## What it teaches

A layout describes Measure and Arrange for ItemsRepeater and ItemsView. Writing one replaces a bespoke Panel.

## How the demo is built

The page is a `UserControl` at `Layout/LayoutPanelDemo.xaml`, paired with `LayoutPanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 14 element(s).

Named elements the code-behind touches: `ColumnsSlider`, `ColumnsText`, `Repeater`.

Wired in XAML: `OnColumnsChanged()`.

**Code-behind.** Handlers `OnColumnsChanged()`. 164 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Derive from NonVirtualizingLayout for small sets, VirtualizingLayout when the child count is large.
- Use the provided LayoutContext (ItemCount, realization rect) instead of walking Children directly.
- InvalidateMeasure is required when your layout depends on something outside the items — otherwise resizing does nothing.
- WinUI has no built-in LayoutPanel; the type in the WinUI Gallery is a sample implementation, which this page reimplements.

## Try this

- Run `WinUI3LearningLab.App.exe --demo layout-panel`. The page shows `LayoutPanelDemo.xaml` and `LayoutPanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.NonVirtualizingLayout](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.nonvirtualizinglayout)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
