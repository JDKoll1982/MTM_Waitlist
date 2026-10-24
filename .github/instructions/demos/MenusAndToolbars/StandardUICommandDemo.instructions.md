---
description: "StandardUICommand (Microsoft.UI.Xaml.Input.StandardUICommand) in Menus & toolbars; a ready-made ICommand with the right icon, label… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/StandardUICommandDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/StandardUICommandDemo.xaml.cs"
---

# StandardUICommand

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Input.StandardUICommand` |
| Page | `MenusAndToolbars/StandardUICommandDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.StandardUICommandDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo standard-ui-command` |

## What it teaches

A ready-made ICommand with the right icon, label, accelerator and accessible name for a common action.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/StandardUICommandDemo.xaml`, paired with `StandardUICommandDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 18 element(s).

Named elements the code-behind touches: `OrderNote`, `CopyButton`, `CutButton`, `PasteButton`, `SaveButton`, `DeleteButton`, `SelectionText`, `ActionText`, `CommandInfo`.

Wired in XAML: `OnNoteTextChanged()`, `OnNoteSelectionChanged()`, `OnSelectWordClicked()`.

**Code-behind.** Handlers `OnNoteTextChanged()`, `OnNoteSelectionChanged()`, `OnSelectWordClicked()`. 150 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The Kind enum decides everything (icon, label, shortcut); overriding Label or Icon is possible but loses the standard look.
- It is an ICommand, so it can be assigned to Button.Command, AppBarButton.Command, or a KeyboardAccelerator.
- StandardUICommand kinds cover copy, cut, paste, delete, save, undo and a handful more — not arbitrary verbs.

## Try this

- Run `WinUI3LearningLab.App.exe --demo standard-ui-command`. The page shows `StandardUICommandDemo.xaml` and `StandardUICommandDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Input.StandardUICommand](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.standarduicommand)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
