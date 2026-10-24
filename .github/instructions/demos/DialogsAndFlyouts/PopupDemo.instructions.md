---
description: "Popup (Microsoft.UI.Xaml.Controls.Primitives.Popup) in Dialogs & flyouts; the primitive host for arbitrary content outside the layout… MTM: MTM_WIP_Application_WinForms › Inventory › remove from bin."
applyTo: "src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/PopupDemo.xaml,src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/PopupDemo.xaml.cs"
---

# Popup

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Dialogs & flyouts |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.Primitives.Popup` |
| Page | `DialogsAndFlyouts/PopupDemo` (`WinUI3LearningLab.Demos.DialogsAndFlyouts.PopupDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo popup` |

## What it teaches

The primitive host for arbitrary content outside the layout tree. Flyout and ContentDialog are built on it.

## How the demo is built

The page is a `UserControl` at `DialogsAndFlyouts/PopupDemo.xaml`, paired with `PopupDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `PopupToggle`, `PopupState`, `SuggestionPopup`.

Wired in XAML: `OnLoaded()`, `OnTogglePopup()`, `OnPopupOpened()`, `OnPopupClosed()`, `OnClosePopup()`.

**Code-behind.** Handlers `OnLoaded()`, `OnTogglePopup()`, `OnPopupOpened()`, `OnPopupClosed()`, `OnClosePopup()`. 66 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › remove from bin**

Removing stock is irreversible, so the commit is behind a confirmation that names the part, the bin and the quantity. Transient feedback that does not deserve a decision — a copied part number, a saved filter — belongs in a flyout instead.

Schema this mirrors: `inv_inventory, inv_transaction`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- A Popup is not in the visual tree of its parent: DataContext, resources and theme do not flow into it automatically.
- Set XamlRoot or the popup will not render on desktop.
- IsLightDismissEnabled handles outside clicks; otherwise you must close it yourself.
- Popup content is not constrained by the window, so clamp sizes with MaxWidth/MaxHeight.

## Traps recorded in the source

- A Popup is not in the layout tree, so it never picks up a XamlRoot by itself.
- TransformToVisual throws once the element is detached; a fixed offset still shows the popup.

## Try this

- Run `WinUI3LearningLab.App.exe --demo popup`. The page shows `PopupDemo.xaml` and `PopupDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Primitives.Popup](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.popup)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DialogsAndFlyouts.cs` or the demo source, then regenerate.
