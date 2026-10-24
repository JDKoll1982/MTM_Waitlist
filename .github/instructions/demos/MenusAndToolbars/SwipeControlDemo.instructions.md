---
description: "SwipeControl (Microsoft.UI.Xaml.Controls.SwipeControl) in Menus & toolbars; touch and pen swipe actions on a list item, with left, right… MTM: MTM_WIP_Application_WinForms › Main form › command bar."
applyTo: "src/WinUI3LearningLab.App/Demos/MenusAndToolbars/SwipeControlDemo.xaml,src/WinUI3LearningLab.App/Demos/MenusAndToolbars/SwipeControlDemo.xaml.cs"
---

# SwipeControl

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Menus & toolbars |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.SwipeControl` |
| Page | `MenusAndToolbars/SwipeControlDemo` (`WinUI3LearningLab.Demos.MenusAndToolbars.SwipeControlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo swipe-control` |

## What it teaches

Touch and pen swipe actions on a list item, with left, right, top and bottom item collections.

## How the demo is built

The page is a `UserControl` at `MenusAndToolbars/SwipeControlDemo.xaml`, paired with `SwipeControlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `PickList`, `StatusText`.

Wired in XAML: `OnPickSelectionChanged()`, `OnRowSwipeLoaded()`, `OnArchiveInvoked()`, `OnArchiveSelectedClicked()`, `OnDeleteSelectedClicked()`.

**Code-behind.** Handlers `OnRowSwipeLoaded()`, `OnArchiveInvoked()`, `OnDeleteInvoked()`, `OnArchiveSelectedClicked()`, `OnDeleteSelectedClicked()`, `OnPickSelectionChanged()`. 116 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Main form › command bar**

The primary actions of the app sit in a command bar, and each one has a per-user keyboard shortcut stored in the database. Role decides which commands are even visible, so overflow behaviour and disabled-versus-hidden are real decisions, not styling.

Schema this mirrors: `sys_shortcuts, usr_user_shortcuts, sys_roles`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- SwipeControl is effectively touch-only — mouse users cannot discover it, so always provide a menu or button too.
- Each direction takes SwipeItems; use Mode=Execute for a single instant action, Reveal for one button, Multiple for a set.
- It must wrap the item content: putting it inside the DataTemplate is the correct place.

## Try this

- Run `WinUI3LearningLab.App.exe --demo swipe-control`. The page shows `SwipeControlDemo.xaml` and `SwipeControlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.SwipeControl](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.swipecontrol)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/MenusAndToolbars.cs` or the demo source, then regenerate.
