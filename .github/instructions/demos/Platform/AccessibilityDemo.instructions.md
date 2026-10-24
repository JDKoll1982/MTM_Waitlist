---
description: "Accessibility & automation (Microsoft.UI.Xaml.Automation.AutomationProperties) in Platform services; names, help text, landmark roles and live regions that make… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/AccessibilityDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/AccessibilityDemo.xaml.cs"
---

# Accessibility & automation

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Automation.AutomationProperties` |
| Page | `Platform/AccessibilityDemo` (`WinUI3LearningLab.Demos.Platform.AccessibilityDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo accessibility` |

## What it teaches

Names, help text, landmark roles and live regions that make a WinUI app usable with a screen reader.

## How the demo is built

The page is a `UserControl` at `Platform/AccessibilityDemo.xaml`, paired with `AccessibilityDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 36 element(s).

Named elements the code-behind touches: `LiveText`, `Dial`, `DialText`, `FocusText`, `LastActionText`.

Wired in XAML: `OnApproveClicked()`, `OnRejectClicked()`, `OnDialGotFocus()`, `OnDialLostFocus()`, `OnDialKeyDown()`, `OnOrderClicked()`.

**Code-behind.** Handlers `OnApproveClicked()`, `OnRejectClicked()`, `OnDialKeyDown()`, `OnDialGotFocus()`, `OnDialLostFocus()`, `OnOrderClicked()`. 64 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- A control with an icon and no text needs AutomationProperties.Name, or it is announced as 'button'.
- AutomationProperties.LiveSetting only works on elements that also have an automation peer — set it on the TextBlock that changes.
- Keyboard support is part of accessibility: every interactive element must be reachable by Tab and usable with Space/Enter.
- Contrast is measured against the effective background — a semi-transparent card over a backdrop changes what 'passes' means.

## Try this

- Run `WinUI3LearningLab.App.exe --demo accessibility`. The page shows `AccessibilityDemo.xaml` and `AccessibilityDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Automation.AutomationProperties](https://learn.microsoft.com/windows/apps/design/accessibility/accessibility)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
