---
description: "App data & settings (Microsoft.Windows.Storage.ApplicationData) in Platform services; per-user, per-app storage for settings and files, with… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/AppDataDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/AppDataDemo.xaml.cs"
---

# App data & settings

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Intermediate |
| API | `Microsoft.Windows.Storage.ApplicationData` |
| Page | `Platform/AppDataDemo` (`WinUI3LearningLab.Demos.Platform.AppDataDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo app-data` |

## What it teaches

Per-user, per-app storage for settings and files, with roaming and local scopes.

## Availability

This lab is unpackaged, so ApplicationData is unavailable. The demo uses a JSON file under %LOCALAPPDATA% and shows the equivalent packaged code.

## Package identity

This API needs MSIX package identity. The lab is unpackaged (`WindowsPackageType=None`), so the page probes the real call and reports the exception instead of faking a result. See `.github/instructions/packaging-and-identity.instructions.md`.

## How the demo is built

The page is a `UserControl` at `Platform/AppDataDemo.xaml`, paired with `AppDataDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 26 element(s).

Named elements the code-behind touches: `IdentityText`, `NoteBox`, `PathText`, `ContentText`, `LastActionText`.

Wired in XAML: `OnSaveClicked()`, `OnLoadClicked()`.

**Code-behind.** Handlers `OnSaveClicked()`, `OnLoadClicked()`. 114 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In the Windows App SDK the type is Microsoft.Windows.Storage.ApplicationData and it needs package identity; unpackaged apps must use a path of their own.
- LocalSettings has a 8 KB per-value limit and a total budget — do not store blobs there.
- Settings values are limited to the WinRT property types; serialize anything else to JSON.

## Try this

- Run `WinUI3LearningLab.App.exe --demo app-data`. The page shows `AppDataDemo.xaml` and `AppDataDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.Storage.ApplicationData](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.applicationdata)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
