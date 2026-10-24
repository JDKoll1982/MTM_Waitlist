---
description: "InfoCard (DevWinUI.InfoCard) in Controls; a titled information panel — a denser sibling of the plain card. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/InfoCardDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/InfoCardDemo.xaml.cs"
---

# InfoCard

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.InfoCard` |
| Page | `DevWinUI/InfoCardDemo` (`WinUI3LearningLab.Demos.DevWinUI.InfoCardDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo info-card` |

## What it teaches

A titled information panel — a denser sibling of the plain card.

## How the demo is built

The page is a `UserControl` at `DevWinUI/InfoCardDemo.xaml`, paired with `InfoCardDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `DunnageCard`, `ToleranceCard`, `ReprintCard`, `ActionText`.

Wired in XAML: `OnReprintClicked()`.

**Code-behind.** Handlers `OnReprintClicked()`. 32 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The default style fixes the card at roughly 350x96, so long text clips. Set Width/Height to double.NaN when you need it to size to content.
- There is no content or action slot; put extra controls beside the card or inside its content area rather than assuming a footer.
- It is a surface, so do not nest it inside another card; use it as the card.
- Reserve it for static explanation. For state that can change, InfoBar already carries severity.

## Traps recorded in the source

- The style fixes Width at 350 and Height at 96; NaN restores auto sizing so a card fills its column.

## Try this

- Run `WinUI3LearningLab.App.exe --demo info-card`. The page shows `InfoCardDemo.xaml` and `InfoCardDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.InfoCard](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
