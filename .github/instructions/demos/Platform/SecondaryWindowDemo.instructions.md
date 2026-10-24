---
description: "Secondary windows (Microsoft.UI.Xaml.Window) in Platform services; WinUI 3 apps can open as many windows as they need; each one has its own… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/SecondaryWindowDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/SecondaryWindowDemo.xaml.cs"
---

# Secondary windows

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Window` |
| Page | `Platform/SecondaryWindowDemo` (`WinUI3LearningLab.Demos.Platform.SecondaryWindowDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo secondary-window` |

## What it teaches

WinUI 3 apps can open as many windows as they need; each one has its own content, XamlRoot and AppWindow.

## How the demo is built

The page is a `UserControl` at `Platform/SecondaryWindowDemo.xaml`, paired with `SecondaryWindowDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `WindowListText`, `LastActionText`.

Wired in XAML: `OnCreateClicked()`, `OnActivateNewestClicked()`, `OnCloseAllClicked()`.

**Code-behind.** Handlers `OnCreateClicked()`, `OnActivateNewestClicked()`, `OnCloseAllClicked()`. 138 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Each Window needs its own root element and its own DataContext; there is no shared Application-level content.
- Closing the last window does not always end the process — track windows if your app must exit with the last one.
- Dialogs and flyouts must be shown from the window whose XamlRoot they belong to.

## Traps recorded in the source

- A window may already be gone; closing it again must not throw out of the page.

## Try this

- Run `WinUI3LearningLab.App.exe --demo secondary-window`. The page shows `SecondaryWindowDemo.xaml` and `SecondaryWindowDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Window](https://learn.microsoft.com/windows/apps/windows-app-sdk/windowing/windowing-overview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
