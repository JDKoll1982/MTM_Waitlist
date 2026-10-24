---
description: "ContentDialog (Microsoft.UI.Xaml.Controls.ContentDialog) in Dialogs & flyouts; the modal dialog for confirmations and short flows. It dims… MTM: MTM_WIP_Application_WinForms › Inventory › remove from bin."
applyTo: "src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/ContentDialogDemo.xaml,src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/ContentDialogDemo.xaml.cs"
---

# ContentDialog

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Dialogs & flyouts |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ContentDialog` |
| Page | `DialogsAndFlyouts/ContentDialogDemo` (`WinUI3LearningLab.Demos.DialogsAndFlyouts.ContentDialogDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo content-dialog` |

## What it teaches

The modal dialog for confirmations and short flows. It dims the app, traps focus, and returns a ContentDialogResult.

## How the demo is built

The page is a `UserControl` at `DialogsAndFlyouts/ContentDialogDemo.xaml`, paired with `ContentDialogDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 16 element(s).

Named elements the code-behind touches: `DialogButtons`, `ResultLog`.

Wired in XAML: `OnPrimaryCloseClicked()`, `OnThreeButtonClicked()`, `OnCloseOnlyClicked()`, `OnSequenceClicked()`.

**Code-behind.** Handlers `OnPrimaryCloseClicked()`, `OnThreeButtonClicked()`, `OnCloseOnlyClicked()`, `OnSequenceClicked()`. 156 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › remove from bin**

Removing stock is irreversible, so the commit is behind a confirmation that names the part, the bin and the quantity. Transient feedback that does not deserve a decision — a copied part number, a saved filter — belongs in a flyout instead.

Removing stock from a bin is committed only after a dialog that repeats the part, the bin and the quantity — the same data the `inv_transaction` row will record.

Schema this mirrors: `inv_inventory, inv_transaction`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In WinUI 3 a ContentDialog needs a XamlRoot (set XamlRoot = this.XamlRoot) or ShowAsync throws.
- Await ShowAsync directly — do not add ConfigureAwait(false); it must resume on the UI thread.
- Only one dialog may be open per XamlRoot; a second ShowAsync fails with an 'already open' error.
- Set DefaultButton and CloseButtonText so Escape and Enter do something predictable.

## Try this

- Set PrimaryButtonText, SecondaryButtonText and CloseButtonText and compare the returned enum values.

## Documentation

- [Microsoft.UI.Xaml.Controls.ContentDialog](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentdialog)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DialogsAndFlyouts.cs` or the demo source, then regenerate.
