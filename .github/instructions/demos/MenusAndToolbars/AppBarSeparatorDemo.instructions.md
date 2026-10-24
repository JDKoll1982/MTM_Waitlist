---
description: "AppBarSeparator (Microsoft.UI.Xaml.Controls.AppBarSeparator) in Menus & toolbars; the thin divider between groups of commands inside a… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarSeparatorDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarSeparatorDemo.xaml.cs"
---

# AppBarSeparator

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.AppBarSeparator` |
| Page | `MenusAndToolbars/AppBarSeparatorDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.AppBarSeparatorDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo appbar-separator` |

## What it teaches

The thin divider between groups of commands inside a CommandBar.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/AppBarSeparatorDemo.xaml`, paired with `AppBarSeparatorDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 23 element(s).

Named elements the code-behind touches: `GroupedBar`, `CrowdSwitch`, `StatusText`.

Wired in XAML: `OnCommandClicked()`, `OnCrowdToggled()`.

**Code-behind.** Handlers `OnCommandClicked()`, `OnCrowdToggled()`. 67 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It renders as a line; if the CommandBar is crowded it can be pushed into the overflow menu, where it disappears.
- Use it sparingly — Fluent guidance is that a CommandBar should read as one group unless the commands are unrelated.

## Try this

- Run `WinUI3LearningLab.App.exe --demo appbar-separator`. The page shows `AppBarSeparatorDemo.xaml` and `AppBarSeparatorDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AppBarSeparator](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.appbarseparator)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
