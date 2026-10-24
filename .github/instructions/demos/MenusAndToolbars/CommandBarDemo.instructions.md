---
description: "CommandBar (Microsoft.UI.Xaml.Controls.CommandBar) in Menus & toolbars; a container that groups primary commands and moves the rest into an… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/CommandBarDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/CommandBarDemo.xaml.cs"
---

# CommandBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.CommandBar` |
| Page | `MenusAndToolbars/CommandBarDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.CommandBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo command-bar` |

## What it teaches

A container that groups primary commands and moves the rest into an overflow menu automatically.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/CommandBarDemo.xaml`, paired with `CommandBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 29 element(s).

Named elements the code-behind touches: `OrderBar`, `OverflowToggle`, `StatusText`.

Wired in XAML: `OnBarOpened()`, `OnBarClosed()`, `OnCommandClicked()`, `OnOverflowToggleChanged()`.

**Code-behind.** Handlers `OnCommandClicked()`, `OnOverflowToggleChanged()`, `OnBarOpened()`, `OnBarClosed()`. 49 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- PrimaryCommands and SecondaryCommands are separate collections: only SecondaryCommands go to the ellipsis menu.
- Setting DefaultLabelPosition=Right is the modern Fluent layout; Bottom is the legacy default.
- In WinUI 3 CommandBar is not the recommended shell for new apps — CommandBarFlyout, MenuBar or a plain StackPanel of buttons usually fit better.
- IsOpen shows the overflow, which is handy for screenshots and automated UI tests.

## Try this

- Run `WinUI3LearningLab.App.exe --demo command-bar`. The page shows `CommandBarDemo.xaml` and `CommandBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.CommandBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.commandbar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
