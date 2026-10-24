---
description: "CopyButton (DevWinUI.CopyButton) in Controls; a button that plays the copy-confirmation visual — useful as a ready-made affordance, but verify what version… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/CopyButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/CopyButtonDemo.xaml.cs"
---

# CopyButton

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.CopyButton` |
| Page | `DevWinUI/CopyButtonDemo` (`WinUI3LearningLab.Demos.DevWinUI.CopyButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo copy-button` |

## What it teaches

A button that plays the copy-confirmation visual — useful as a ready-made affordance, but verify what version you have first.

## How the demo is built

The page is a `UserControl` at `DevWinUI/CopyButtonDemo.xaml`, paired with `CopyButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 23 element(s).

Named elements the code-behind touches: `PartValue`, `OrderValue`, `CopyStatus`.

Wired in XAML: `OnCopyPartClicked()`, `OnCopyOrderClicked()`.

**Code-behind.** Handlers `OnCopyPartClicked()`, `OnCopyOrderClicked()`. 35 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In DevWinUI 10.8.0 the control does NOT put anything on the clipboard: it only runs the confirmation storyboard, so the page has to call Clipboard.SetContent itself. Assuming otherwise ships a button that lies.
- It copies text to the shared clipboard — never bind it to a secret.
- Clipboard access can fail when another process holds it; surface that instead of showing the success animation.

## Try this

- Run `WinUI3LearningLab.App.exe --demo copy-button`. The page shows `CopyButtonDemo.xaml` and `CopyButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.CopyButton](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
