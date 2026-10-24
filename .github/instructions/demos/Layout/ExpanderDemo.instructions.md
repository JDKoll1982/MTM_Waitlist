---
description: "Expander (Microsoft.UI.Xaml.Controls.Expander) in Layout; a collapsible section with a header, for progressively… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/ExpanderDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/ExpanderDemo.xaml.cs"
---

# Expander

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Expander` |
| Page | `Layout/ExpanderDemo` (`WinUI3LearningLab.Demos.Layout.ExpanderDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo expander` |

## What it teaches

A collapsible section with a header, for progressively disclosing detail.

## How the demo is built

The page is a `UserControl` at `Layout/ExpanderDemo.xaml`, paired with `ExpanderDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 36 element(s).

Named elements the code-behind touches: `HiddenNotesBox`, `HiddenNotesStatus`, `HiddenStatus`.

Wired in XAML: `OnHiddenNotesChanged()`, `OnReadHiddenClicked()`.

**Code-behind.** Handlers `OnHiddenNotesChanged()`, `OnReadHiddenClicked()`. 44 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Content is hidden but still created: collapsed content stays in the visual tree and keeps running animations.
- HorizontalContentAlignment does not stretch by default, so wide content is left-aligned unless you set it.
- For long lists use the Expander per item sparingly — every expanded item is fully realised.

## Try this

- Run `WinUI3LearningLab.App.exe --demo expander`. The page shows `ExpanderDemo.xaml` and `ExpanderDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Expander](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.expander)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
