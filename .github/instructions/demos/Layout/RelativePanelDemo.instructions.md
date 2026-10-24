---
description: "RelativePanel (Microsoft.UI.Xaml.Controls.RelativePanel) in Layout; positions children relative to each other and to the panel… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/RelativePanelDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/RelativePanelDemo.xaml.cs"
---

# RelativePanel

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.RelativePanel` |
| Page | `Layout/RelativePanelDemo` (`WinUI3LearningLab.Demos.Layout.RelativePanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo relative-panel` |

## What it teaches

Positions children relative to each other and to the panel, without explicit rows and columns.

## How the demo is built

The page is a `UserControl` at `Layout/RelativePanelDemo.xaml`, paired with `RelativePanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 16 element(s).

Named elements the code-behind touches: `Card`, `IconTile`, `TitleText`, `SubtitleText`, `StatusPill`, `FooterText`, `ActionButton`, `SubtitleToggle`, `ActionToggle`, `ConstraintText`.

Wired in XAML: `OnSubtitleConstraintToggled()`, `OnActionConstraintToggled()`.

**Code-behind.** Handlers `OnSubtitleConstraintToggled()`, `OnActionConstraintToggled()`. 75 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Constraints are attached properties such as RightOf, Below and AlignLeftWithPanel.
- Conflicting constraints are resolved silently — the panel does not throw, it just picks an order.
- RelativePanel does not wrap or scroll; it is best for small forms with clear relationships.

## Try this

- Run `WinUI3LearningLab.App.exe --demo relative-panel`. The page shows `RelativePanelDemo.xaml` and `RelativePanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.RelativePanel](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.relativepanel)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
