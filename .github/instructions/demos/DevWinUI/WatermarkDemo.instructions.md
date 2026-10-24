---
description: "Watermark (DevWinUI.Watermark) in Controls; a tiled or diagonal watermark text layer over content, for draft, void or confidential markings. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/WatermarkDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/WatermarkDemo.xaml.cs"
---

# Watermark

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.Watermark` |
| Page | `DevWinUI/WatermarkDemo` (`WinUI3LearningLab.Demos.DevWinUI.WatermarkDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo watermark` |

## What it teaches

A tiled or diagonal watermark text layer over content, for draft, void or confidential markings.

## How the demo is built

The page is a `UserControl` at `DevWinUI/WatermarkDemo.xaml`, paired with `WatermarkDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `ConfirmText`, `DraftMark`, `HitTestSwitch`.

Wired in XAML: `OnConfirmClicked()`, `OnHitTestToggled()`.

**Code-behind.** Handlers `OnConfirmClicked()`, `OnHitTestToggled()`. 35 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- A watermark is decoration: it must not intercept pointer input or it makes the surface underneath unclickable.
- It does not survive export. If a printed document must carry the mark, the document has to draw it.
- Very light watermarks vanish on low-contrast displays; keep the contrast ratio in mind even for decoration.

## Try this

- Run `WinUI3LearningLab.App.exe --demo watermark`. The page shows `WatermarkDemo.xaml` and `WatermarkDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Watermark](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
