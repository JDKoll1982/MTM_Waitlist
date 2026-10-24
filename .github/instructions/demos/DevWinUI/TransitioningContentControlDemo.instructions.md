---
description: "TransitioningContentControl (DevWinUI.TransitioningContentControl) in Controls; swaps its content with an animation when the bound content changes — a lighter… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/TransitioningContentControlDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/TransitioningContentControlDemo.xaml.cs"
---

# TransitioningContentControl

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.TransitioningContentControl` |
| Page | `DevWinUI/TransitioningContentControlDemo` (`WinUI3LearningLab.Demos.DevWinUI.TransitioningContentControlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo transitioning-content` |

## What it teaches

Swaps its content with an animation when the bound content changes — a lighter alternative to hand-written visual states.

## How the demo is built

The page is a `UserControl` at `DevWinUI/TransitioningContentControlDemo.xaml`, paired with `TransitioningContentControlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 16 element(s).

Named elements the code-behind touches: `ModeBox`, `TransitionBox`, `Switcher`, `ModeNote`.

Wired in XAML: `OnModeChanged()`, `OnTransitionChanged()`.

**Code-behind.** Handlers `OnModeChanged()`, `OnTransitionChanged()`. 124 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It animates a content *replacement*; changing a property on the same content does not trigger it.
- The default transition is short on purpose. A long transition on a frequently changing value is nauseating.
- Screen readers may not announce changed content; announce it yourself when the change matters.

## Try this

- Run `WinUI3LearningLab.App.exe --demo transitioning-content`. The page shows `TransitioningContentControlDemo.xaml` and `TransitioningContentControlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.TransitioningContentControl](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
