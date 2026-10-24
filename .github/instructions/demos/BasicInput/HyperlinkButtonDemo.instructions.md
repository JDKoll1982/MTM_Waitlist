---
description: "HyperlinkButton (Microsoft.UI.Xaml.Controls.HyperlinkButton) in Basic input; a text link that navigates or acts. Use it inline in prose or as… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/HyperlinkButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/HyperlinkButtonDemo.xaml.cs"
---

# HyperlinkButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.HyperlinkButton` |
| Page | `BasicInput/HyperlinkButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.HyperlinkButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo hyperlink-button` |

## What it teaches

A text link that navigates or acts. Use it inline in prose or as a lightweight secondary action.

## How the demo is built

The page is a `UserControl` at `BasicInput/HyperlinkButtonDemo.xaml`, paired with `HyperlinkButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `ActionLink`, `StatusText`.

Wired in XAML: `OnActionLinkClicked()`.

**Code-behind.** Handlers `OnActionLinkClicked()`. 21 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

The audit panel links out to the work instruction for a defect code. It opens in the browser, so it stays a link and not a navigation event inside the app.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- NavigateUri launches the system handler; it does not embed a browser. WebView2 is the control for in-app web content.
- The visual is a hyperlink, but it is still a Button: Click works and keyboard focus is a tab stop.
- Long links do not wrap by default because Content is an object; put a TextBlock with TextWrapping in Content.

## Try this

- Run `WinUI3LearningLab.App.exe --demo hyperlink-button`. The page shows `HyperlinkButtonDemo.xaml` and `HyperlinkButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.HyperlinkButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.hyperlinkbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
