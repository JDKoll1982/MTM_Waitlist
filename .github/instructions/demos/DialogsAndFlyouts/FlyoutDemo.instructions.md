---
description: "Flyout (Microsoft.UI.Xaml.Controls.Flyout) in Dialogs & flyouts; a light-dismiss popup anchored to an element, for short contextual… MTM: MTM_WIP_Application_WinForms › Inventory › remove from bin."
applyTo: "src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/FlyoutDemo.xaml,src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/FlyoutDemo.xaml.cs"
---

# Flyout

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Dialogs & flyouts |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Flyout` |
| Page | `DialogsAndFlyouts/FlyoutDemo` (`WinUI3LearningLab.Demos.DialogsAndFlyouts.FlyoutDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo flyout` |

## What it teaches

A light-dismiss popup anchored to an element, for short contextual content that is not a menu.

## How the demo is built

The page is a `UserControl` at `DialogsAndFlyouts/FlyoutDemo.xaml`, paired with `FlyoutDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `AnchorButton`, `AdjustFlyout`, `ReorderBox`, `StatusText`.

Wired in XAML: `OnSubmitClicked()`, `OnCancelClicked()`, `OnShowAtClicked()`.

**Code-behind.** Handlers `OnShowAtClicked()`, `OnSubmitClicked()`, `OnCancelClicked()`. 40 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › remove from bin**

Removing stock is irreversible, so the commit is behind a confirmation that names the part, the bin and the quantity. Transient feedback that does not deserve a decision — a copied part number, a saved filter — belongs in a flyout instead.

Schema this mirrors: `inv_inventory, inv_transaction`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ShowAt(anchor) needs the anchor to be in the visual tree; calling it from a constructor fails.
- Placement controls where it appears, but the flyout flips itself when it would overflow the window.
- Flyout has no built-in result — put a Button in the content and close with the Flyout's Hide().

## Try this

- Run `WinUI3LearningLab.App.exe --demo flyout`. The page shows `FlyoutDemo.xaml` and `FlyoutDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Flyout](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.flyout)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DialogsAndFlyouts.cs` or the demo source, then regenerate.
