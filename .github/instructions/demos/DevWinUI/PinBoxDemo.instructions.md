---
description: "PinBox (DevWinUI.PinBox) in Controls; a segmented PIN / code entry control — the right input for a badge number or a supervisor override code. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/PinBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/PinBoxDemo.xaml.cs"
---

# PinBox

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.PinBox` |
| Page | `DevWinUI/PinBoxDemo` (`WinUI3LearningLab.Demos.DevWinUI.PinBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo pin-box` |

## What it teaches

A segmented PIN / code entry control — the right input for a badge number or a supervisor override code.

## How the demo is built

The page is a `UserControl` at `DevWinUI/PinBoxDemo.xaml`, paired with `PinBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `OverridePin`, `LengthText`, `MatchText`.

Wired in XAML: `OnPasswordChanged()`, `OnClearClick()`.

**Code-behind.** Handlers `OnPasswordChanged()`, `OnClearClick()`. 51 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Supervisor override on the lugger board: six digits, no keyboard required, cleared after every attempt whether it succeeded or not.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- PinBox is a presentation control: it reports the entered characters, it does not verify them. Never compare the PIN in the view.
- Its length is fixed by the control's configured character count, so a variable-length code needs a TextBox instead.
- Password-box rules still apply: do not log the value and do not put it in the clipboard.

## Traps recorded in the source

- Hardcoded only so the demo has something to compare against. Real verification belongs to the service that owns the override; a view that compares a PIN is a view that leaks it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo pin-box`. The page shows `PinBoxDemo.xaml` and `PinBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.PinBox](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
