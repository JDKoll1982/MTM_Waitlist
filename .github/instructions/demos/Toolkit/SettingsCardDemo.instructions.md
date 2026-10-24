---
description: "SettingsCard / SettingsExpander (CommunityToolkit.WinUI.Controls.SettingsCard) in Controls; the Fluent settings row: icon, title, description and a… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/SettingsCardDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/SettingsCardDemo.xaml.cs"
---

# SettingsCard / SettingsExpander

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Starter |
| API | `CommunityToolkit.WinUI.Controls.SettingsCard` |
| Page | `Toolkit/SettingsCardDemo` (`WinUI3LearningLab.Demos.Toolkit.SettingsCardDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo settings-card` |

## What it teaches

The Fluent settings row: icon, title, description and a trailing control, with SettingsExpander for nested groups.

## How the demo is built

The page is a `UserControl` at `Toolkit/SettingsCardDemo.xaml`, paired with `SettingsCardDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `AutoReorderBox`, `BinBox`, `BarcodeBox`, `CopiesBox`, `StatusText`.

Wired in XAML: `OnSettingChanged()`.

**Code-behind.** Handlers `OnSettingChanged()`. 29 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

The settings screens are rows, not pages: label, description, control on the right. That matches how `settings_universal` and `settings_personal` are scoped and makes the scope obvious.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The trailing control goes in Content; Header, Description and HeaderIcon build the row.
- SettingsExpander's Items are SettingsCard objects — do not put arbitrary content there.
- SettingsCard stretches by default; set HorizontalAlignment on the card, not on its content.

## Try this

- Run `WinUI3LearningLab.App.exe --demo settings-card`. The page shows `SettingsCardDemo.xaml` and `SettingsCardDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.Controls.SettingsCard](https://learn.microsoft.com/dotnet/communitytoolkit/windows/settingscontrols/settingscard)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
