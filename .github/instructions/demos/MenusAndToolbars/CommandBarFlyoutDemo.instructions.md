---
description: "CommandBarFlyout (Microsoft.UI.Xaml.Controls.CommandBarFlyout) in Menus & toolbars; a flyout that holds commands, used for contextual actions on… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/CommandBarFlyoutDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/CommandBarFlyoutDemo.xaml.cs"
---

# CommandBarFlyout

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.CommandBarFlyout` |
| Page | `MenusAndToolbars/CommandBarFlyoutDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.CommandBarFlyoutDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo command-bar-flyout` |

## What it teaches

A flyout that holds commands, used for contextual actions on an item (right-click menus with icons and labels).

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/CommandBarFlyoutDemo.xaml`, paired with `CommandBarFlyoutDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `OrderCard`, `ResultText`.

Wired in XAML: `OnCardRightTapped()`, `OnShowCommandsClicked()`.

**Code-behind.** Handlers `OnCardRightTapped()`, `OnShowCommandsClicked()`, `OnFlyoutCommand()`. 69 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- PrimaryCommands hold the always-visible buttons; SecondaryCommands appears behind the ellipsis.
- ShowAt with a placement and a target rectangle is how you anchor it at the pointer for a context menu.
- Commands are AppBarButton objects, so Icon and Label follow the same rules as a CommandBar.

## Try this

- Run `WinUI3LearningLab.App.exe --demo command-bar-flyout`. The page shows `CommandBarFlyoutDemo.xaml` and `CommandBarFlyoutDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.CommandBarFlyout](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.commandbarflyout)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
