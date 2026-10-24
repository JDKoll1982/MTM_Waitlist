---
description: "ScrollViewer (Microsoft.UI.Xaml.Controls.ScrollViewer) in Scrolling; the classic scrolling container with the four bar… MTM: MTM_WIP_Application_WinForms › Inventory › stock list and last transactions."
applyTo: "src/WinUI3LearningLab.App/Demos/Scrolling/ScrollViewerDemo.xaml,src/WinUI3LearningLab.App/Demos/Scrolling/ScrollViewerDemo.xaml.cs"
---

# ScrollViewer

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Scrolling |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ScrollViewer` |
| Page | `Scrolling/ScrollViewerDemo` (`WinUI3LearningLab.Demos.Scrolling.ScrollViewerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo scroll-viewer` |

## What it teaches

The classic scrolling container with the four bar visibilities, zoom, and programmatic offsets.

## How the demo is built

The page is a `UserControl` at `Scrolling/ScrollViewerDemo.xaml`, paired with `ScrollViewerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 35 element(s).

Named elements the code-behind touches: `VerticalModeBox`, `VerticalBarBox`, `HorizontalModeBox`, `HorizontalBarBox`, `ClassicScroll`, `BayGrid`, `SettingStatus`.

Wired in XAML: `OnScrollSettingChanged()`, `OnScrollDownClicked()`, `OnScrollRightClicked()`, `OnOriginClicked()`.

**Code-behind.** Handlers `OnScrollSettingChanged()`, `OnScrollDownClicked()`, `OnScrollRightClicked()`, `OnOriginClicked()`. 125 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › stock list and last transactions**

The stock list is tens of thousands of rows on a workstation that also runs the label printer. Realising rows lazily and keeping the header in view is the difference between a usable screen and a frozen one.

The receipt body scrolls while the header and the action footer stay put — the shape of every MTM form.

Schema this mirrors: `inv_inventory, sys_last_10_transactions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- HorizontalScrollMode and HorizontalScrollBarVisibility are independent: the mode can allow scrolling while the bar stays hidden.
- Nesting ScrollViewers in the same direction breaks wheel behaviour and virtualisation — keep one scrolling axis per region.
- Content larger than the viewport without a ScrollViewer is simply clipped; nothing warns you.

## Try this

- Run `WinUI3LearningLab.App.exe --demo scroll-viewer`. The page shows `ScrollViewerDemo.xaml` and `ScrollViewerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ScrollViewer](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.scrollviewer)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Scrolling.cs` or the demo source, then regenerate.
