---
description: "AppBarToggleButton (Microsoft.UI.Xaml.Controls.AppBarToggleButton) in Menus & toolbars; a toolbar button with on/off state, for modes like bold or… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarToggleButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarToggleButtonDemo.xaml.cs"
---

# AppBarToggleButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.AppBarToggleButton` |
| Page | `MenusAndToolbars/AppBarToggleButtonDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.AppBarToggleButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo appbar-toggle-button` |

## What it teaches

A toolbar button with on/off state, for modes like bold or grid view.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/AppBarToggleButtonDemo.xaml`, paired with `AppBarToggleButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `BoldToggle`, `ItalicToggle`, `PreviewText`, `StateText`.

Wired in XAML: `OnStyleToggled()`.

**Code-behind.** Handlers `OnStyleToggled()`. 33 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsChecked is bool? — the same nullable trap as ToggleButton.
- A toggle in a CommandBar keeps its checked state in the overflow menu, but the user may not find it there.

## Try this

- Run `WinUI3LearningLab.App.exe --demo appbar-toggle-button`. The page shows `AppBarToggleButtonDemo.xaml` and `AppBarToggleButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AppBarToggleButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.appbartogglebutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
