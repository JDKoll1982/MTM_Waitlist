---
description: "Resources & localization (MRT Core) (Microsoft.Windows.ApplicationModel.Resources.ResourceLoader) in Platform services; MRT Core resolves string and file resources by qualifier… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/ResourcesDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/ResourcesDemo.xaml.cs"
---

# Resources & localization (MRT Core)

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Intermediate |
| API | `Microsoft.Windows.ApplicationModel.Resources.ResourceLoader` |
| Page | `Platform/ResourcesDemo` (`WinUI3LearningLab.Demos.Platform.ResourcesDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo resources-and-localization` |

## What it teaches

MRT Core resolves string and file resources by qualifier (language, scale, contrast) at runtime.

## How the demo is built

The page is a `UserControl` at `Platform/ResourcesDemo.xaml`, paired with `ResourcesDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 22 element(s).

Named elements the code-behind touches: `ResultText`, `LastActionText`.

Wired in XAML: `OnLoadStringClicked()`, `OnMissingKeyClicked()`, `OnDefaultPathClicked()`.

**Code-behind.** Handlers `OnLoadStringClicked()`, `OnMissingKeyClicked()`, `OnDefaultPathClicked()`. 69 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ResourceLoader.GetString throws if the key is missing — provide a default in the default language file rather than catching.
- Strings live in .resw files under Strings/<lang>/Resources.resw; the default file is the fallback for every qualifier.
- Qualifiers apply to files too, which is how scale-100/200 assets are selected without code.
- XAML can use x:Uid to bind text automatically; the loader is for code paths and formatted messages.

## Try this

- Run `WinUI3LearningLab.App.exe --demo resources-and-localization`. The page shows `ResourcesDemo.xaml` and `ResourcesDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.ApplicationModel.Resources.ResourceLoader](https://learn.microsoft.com/windows/apps/windows-app-sdk/mrtcore/mrtcore-overview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
