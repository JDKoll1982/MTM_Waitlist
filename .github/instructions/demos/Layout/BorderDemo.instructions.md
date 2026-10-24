---
description: "Border (Microsoft.UI.Xaml.Controls.Border) in Layout; draws a background, border and rounded corners around a single… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/BorderDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/BorderDemo.xaml.cs"
---

# Border

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Border` |
| Page | `Layout/BorderDemo` (`WinUI3LearningLab.Demos.Layout.BorderDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo border` |

## What it teaches

Draws a background, border and rounded corners around a single child. The building block behind every card.

## How the demo is built

The page is a `UserControl` at `Layout/BorderDemo.xaml`, paired with `BorderDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 32 element(s).

Named elements the code-behind touches: `PaddingBox`, `PaddingContent`, `PaddingSlider`, `MarginBox`, `MarginSlider`, `SquareChildBorder`, `RoundedChildBorder`, `RoundedChild`, `RadiusSlider`.

Wired in XAML: `OnPaddingChanged()`, `OnMarginChanged()`, `OnRadiusChanged()`.

**Code-behind.** Handlers `OnPaddingChanged()`, `OnMarginChanged()`, `OnRadiusChanged()`. 47 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- One child only — wrap content in a Panel when you need more.
- Padding is inside the border; Margin is outside it. Mixing them up is the most common spacing bug in WinUI.
- CornerRadius clips only the background and border, not the child: a child Image needs its own clipping to respect the radius.

## Try this

- Run `WinUI3LearningLab.App.exe --demo border`. The page shows `BorderDemo.xaml` and `BorderDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Border](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.border)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
