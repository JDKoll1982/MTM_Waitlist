---
description: "MenuBar (Microsoft.UI.Xaml.Controls.MenuBar) in Menus & toolbars; the classic horizontal menu bar (File / Edit / View) with keyboard… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/MenuBarDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/MenuBarDemo.xaml.cs"
---

# MenuBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.MenuBar` |
| Page | `MenusAndToolbars/MenuBarDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.MenuBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo menu-bar` |

## What it teaches

The classic horizontal menu bar (File / Edit / View) with keyboard accelerators and sub-menus.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/MenuBarDemo.xaml`, paired with `MenuBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 43 element(s).

Named elements the code-behind touches: `StatusText`.

Wired in XAML: `OnMenuItemClicked()`, `OnViewToggleClicked()`.

**Code-behind.** Handlers `OnMenuItemClicked()`, `OnViewToggleClicked()`. 25 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- MenuBarItem holds MenuFlyoutItem, MenuFlyoutSubItem and separators — it is not a generic content container.
- Keyboard accelerators on items give you Alt+key mnemonics; access keys use AccessKey on the label.
- On narrow windows the bar does not collapse into a hamburger by itself; you handle that in your shell.

## Try this

- Run `WinUI3LearningLab.App.exe --demo menu-bar`. The page shows `MenuBarDemo.xaml` and `MenuBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.MenuBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.menubar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
