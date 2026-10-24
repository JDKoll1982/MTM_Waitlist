---
description: "ToolTip (Microsoft.UI.Xaml.Controls.ToolTip) in Status & info; the hover/focus hint attached with ToolTipService.SetToolTip; it is a content… MTM: MTM_Waitlist › Waitlist › request state and audit."
applyTo: "src/WinUI3LearningLab.App/Demos/StatusAndInfo/ToolTipDemo.xaml,src/WinUI3LearningLab.App/Demos/StatusAndInfo/ToolTipDemo.xaml.cs"
---

# ToolTip

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Status & info |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ToolTip` |
| Page | `StatusAndInfo/ToolTipDemo` (`WinUI3LearningLab.Demos.StatusAndInfo.ToolTipDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo tool-tip` |

## What it teaches

The hover/focus hint attached with ToolTipService.SetToolTip; it is a content control, not a popup you open.

## How the demo is built

The page is a `UserControl` at `StatusAndInfo/ToolTipDemo.xaml`, paired with `ToolTipDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `CodeTipButton`, `PlacementAnchor`, `PlacedTip`, `AutomationOnlyButton`, `ToolTipStatus`.

Wired in XAML: `OnInspectToolTip()`.

**Code-behind.** Handlers `OnInspectToolTip()`. 27 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › request state and audit**

A request moves through states and every transition is audited. The screen has to say what state a request is in, whether something is still running, and what failed — without a modal box per event.

Schema this mirrors: `waitlist_requests_audit, waitlist_defect_types, log_error`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Attach it with the ToolTipService attached property — creating a ToolTip object alone never shows it.
- Tooltips are not a place for essential information: touch users never see them and screen readers read the automation name instead.
- Placement and PlacementTarget let you move the tip, but a tooltip that needs moving usually means the layout is wrong.

## Try this

- Run `WinUI3LearningLab.App.exe --demo tool-tip`. The page shows `ToolTipDemo.xaml` and `ToolTipDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ToolTip](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.tooltip)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/StatusAndInfo.cs` or the demo source, then regenerate.
