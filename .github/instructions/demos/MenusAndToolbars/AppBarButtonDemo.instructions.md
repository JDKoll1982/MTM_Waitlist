---
description: "AppBarButton (Microsoft.UI.Xaml.Controls.AppBarButton) in Menus & toolbars; a toolbar button with an icon and a label, designed for… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/AppBarButtonDemo.xaml.cs"
---

# AppBarButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.AppBarButton` |
| Page | `MenusAndToolbars/AppBarButtonDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.AppBarButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo appbar-button` |

## What it teaches

A toolbar button with an icon and a label, designed for CommandBar.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/AppBarButtonDemo.xaml`, paired with `AppBarButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `ResultText`.

Wired in XAML: `OnCommandClicked()`.

**Code-behind.** Handlers `OnCommandClicked()`. 19 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Icon and Label are separate: Label is the text under/next to the icon, and it is what accessibility reads.
- Set LabelPosition=Collapsed to keep only the icon, but keep Label non-empty for the tooltip and screen readers.
- Icon accepts IconElement, so FontIcon, SymbolIcon, BitmapIcon and PathIcon all work.

## Try this

- Run `WinUI3LearningLab.App.exe --demo appbar-button`. The page shows `AppBarButtonDemo.xaml` and `AppBarButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AppBarButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.appbarbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
