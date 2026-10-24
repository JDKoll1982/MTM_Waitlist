---
description: "Spoiler (DevWinUI.Spoiler) in Controls; inline covered text that reveals on hover or click — hiding a price, a name or a result until asked. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/SpoilerDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/SpoilerDemo.xaml.cs"
---

# Spoiler

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.Spoiler` |
| Page | `DevWinUI/SpoilerDemo` (`WinUI3LearningLab.Demos.DevWinUI.SpoilerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo spoiler` |

## What it teaches

Inline covered text that reveals on hover or click — hiding a price, a name or a result until asked.

## How the demo is built

The page is a `UserControl` at `DevWinUI/SpoilerDemo.xaml`, paired with `SpoilerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 14 element(s).

Named elements the code-behind touches: `PriceSpoiler`, `RevealStatus`.

Wired in XAML: `OnCoverClicked()`, `OnRevealClicked()`.

**Code-behind.** Handlers `OnRevealChanged()`, `OnCoverClicked()`, `OnRevealClicked()`. 33 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Covered text is still in the visual tree; this hides from sight, not from automation or from the DOM-equivalent.
- Never use it to hide sensitive data — it is a reveal effect, not a security boundary.
- Hover-only reveal excludes touch and keyboard users, so keep click and focus working.

## Try this

- Run `WinUI3LearningLab.App.exe --demo spoiler`. The page shows `SpoilerDemo.xaml` and `SpoilerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Spoiler](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
