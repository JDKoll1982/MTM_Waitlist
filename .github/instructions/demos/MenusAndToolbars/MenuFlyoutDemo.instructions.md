---
description: "MenuFlyout (Microsoft.UI.Xaml.Controls.MenuFlyout) in Menus & toolbars; the standard context / drop-down menu, with icons, toggles… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/MenuFlyoutDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/MenuFlyoutDemo.xaml.cs"
---

# MenuFlyout

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.MenuFlyout` |
| Page | `MenusAndToolbars/MenuFlyoutDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.MenuFlyoutDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo menu-flyout` |

## What it teaches

The standard context / drop-down menu, with icons, toggles, sub-menus and accelerators.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/MenuFlyoutDemo.xaml`, paired with `MenuFlyoutDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 36 element(s).

Named elements the code-behind touches: `OrderMenuButton`, `OrderMenu`, `VoidMenuItem`, `OrderCard`, `StatusText`.

Wired in XAML: `OnShowMenuClicked()`, `OnMenuAction()`, `OnMenuToggle()`, `OnPriorityChanged()`, `OnTargetRightTapped()`.

**Code-behind.** Handlers `OnShowMenuClicked()`, `OnTargetRightTapped()`, `OnMenuAction()`, `OnMenuToggle()`, `OnPriorityChanged()`. 63 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ShowAt(target, FlyoutShowOptions) lets you position at the pointer; ShowAt(target) anchors to the element's edge.
- MenuFlyoutItem supports Command and CommandParameter, but it does not disable itself when CanExecute is false — bind IsEnabled as well.
- Attaching one MenuFlyout instance to many elements is fine; it is recycled and its DataContext is the target's.

## Try this

- Run `WinUI3LearningLab.App.exe --demo menu-flyout`. The page shows `MenuFlyoutDemo.xaml` and `MenuFlyoutDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.MenuFlyout](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.menuflyout)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
