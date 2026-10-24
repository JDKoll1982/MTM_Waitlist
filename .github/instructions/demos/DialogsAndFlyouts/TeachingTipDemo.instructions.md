---
description: "TeachingTip (Microsoft.UI.Xaml.Controls.TeachingTip) in Dialogs & flyouts; a coach-mark pointing at a target, for onboarding and… MTM: MTM_WIP_Application_WinForms › Inventory › remove from bin."
applyTo: "src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/TeachingTipDemo.xaml,src/WinUI3LearningLab.App/Demos/DialogsAndFlyouts/TeachingTipDemo.xaml.cs"
---

# TeachingTip

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Dialogs & flyouts |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.TeachingTip` |
| Page | `DialogsAndFlyouts/TeachingTipDemo` (`WinUI3LearningLab.Demos.DialogsAndFlyouts.TeachingTipDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo teaching-tip` |

## What it teaches

A coach-mark pointing at a target, for onboarding and one-time explanations.

## How the demo is built

The page is a `UserControl` at `DialogsAndFlyouts/TeachingTipDemo.xaml`, paired with `TeachingTipDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 18 element(s).

Named elements the code-behind touches: `ReceiveButton`, `SlotButton`, `TourLog`, `ReceiveTip`, `SlotTip`.

Wired in XAML: `OnReceiveClicked()`, `OnSlotClicked()`, `OnStartTourClicked()`, `OnStopTourClicked()`, `OnReceiveTipClosed()`, `OnSlotTipClosed()`.

**Code-behind.** Handlers `OnStartTourClicked()`, `OnStopTourClicked()`, `OnReceiveTipClosed()`, `OnSlotTipClosed()`, `OnReceiveClicked()`, `OnSlotClicked()`. 68 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › remove from bin**

Removing stock is irreversible, so the commit is behind a confirmation that names the part, the bin and the quantity. Transient feedback that does not deserve a decision — a copied part number, a saved filter — belongs in a flyout instead.

The first time an operator opens the lugger board, a teaching tip points at the assignment column. It is shown once per user and never blocks the board.

Schema this mirrors: `inv_inventory, inv_transaction`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Target must reference an element that is already loaded, and the tip needs a XamlRoot.
- IsOpen is the control: set it true to show, and handle Closed to advance your tour.
- Without a Target it renders as a centred educational pane, which is a different pattern entirely.

## Traps recorded in the source

- Target must be an element that is already in the tree, so it is wired here and not in markup.

## Try this

- Run `WinUI3LearningLab.App.exe --demo teaching-tip`. The page shows `TeachingTipDemo.xaml` and `TeachingTipDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.TeachingTip](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.teachingtip)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DialogsAndFlyouts.cs` or the demo source, then regenerate.
