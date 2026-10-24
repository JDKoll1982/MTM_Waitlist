---
description: "InfoBar (Microsoft.UI.Xaml.Controls.InfoBar) in Status & info; the standard inline message strip for information, success, warning and error… MTM: MTM_Waitlist › Waitlist › request state and audit."
applyTo: "src/WinUI3LearningLab.App/Demos/StatusAndInfo/InfoBarDemo.xaml,src/WinUI3LearningLab.App/Demos/StatusAndInfo/InfoBarDemo.xaml.cs"
---

# InfoBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Status & info |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.InfoBar` |
| Page | `StatusAndInfo/InfoBarDemo` (`WinUI3LearningLab.Demos.StatusAndInfo.InfoBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo info-bar` |

## What it teaches

The standard inline message strip for information, success, warning and error states.

## How the demo is built

The page is a `UserControl` at `StatusAndInfo/InfoBarDemo.xaml`, paired with `InfoBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `SeverityBox`, `LiveBar`, `ClosableCheck`, `LiveStatus`, `DismissibleBar`, `RestoreStatus`.

Wired in XAML: `OnSeverityChanged()`, `OnActionClicked()`, `OnClosableChanged()`, `OnRestoreClicked()`.

**Code-behind.** Handlers `OnSeverityChanged()`, `OnActionClicked()`, `OnClosableChanged()`, `OnRestoreClicked()`. 68 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › request state and audit**

A request moves through states and every transition is audited. The screen has to say what state a request is in, whether something is still running, and what failed — without a modal box per event.

A quality hold on the receipt is an `InfoBar` at the top of the entry form: it stays visible while the operator works and links to `receiving_quality_holds`, rather than interrupting with a dialog.

Schema this mirrors: `waitlist_requests_audit, waitlist_defect_types, log_error`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsOpen is the visibility: setting Message alone shows nothing.
- IsClosable defaults to true; set it false for errors the user cannot dismiss.
- For actionable messages put a Button in ActionButton rather than overloading the message text.

## Try this

- Cycle Severity and compare the icon and accent colour for each value.

## Documentation

- [Microsoft.UI.Xaml.Controls.InfoBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.infobar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/StatusAndInfo.cs` or the demo source, then regenerate.
