---
description: "Connected animation (Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService) in Motion; carries an element visually from one view to the next, so… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/ConnectedAnimationDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/ConnectedAnimationDemo.xaml.cs"
---

# Connected animation

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService` |
| Page | `Motion/ConnectedAnimationDemo` (`WinUI3LearningLab.Demos.Motion.ConnectedAnimationDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo connected-animation` |

## What it teaches

Carries an element visually from one view to the next, so navigation feels continuous.

## How the demo is built

The page is a `UserControl` at `Motion/ConnectedAnimationDemo.xaml`, paired with `ConnectedAnimationDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 18 element(s).

Named elements the code-behind touches: `SourceList`, `DetailPanel`, `DetailRows`, `EmptyDetailText`, `StatusText`.

Wired in XAML: `OnLineSelected()`.

**Code-behind.** Handlers `OnLineSelected()`, `OnAnimationCompleted()`. 153 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Opening a request from the board animates the row into the detail header, so the operator keeps their place in a long queue.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Prepare the animation before navigating and Start it after the destination loads; calling Start too early silently fails.
- The source element must be in the visual tree and visible at PrepareToAnimate time.
- Only one animation per item per navigation — reuse a key and the second call throws.

## Traps recorded in the source

- Only one animation can be live per key: a second PrepareToAnimate with a key that has not been started yet is the documented way to make this API throw.
- The destination has to exist and be laid out, not merely visible, before Start runs.

## Try this

- Run `WinUI3LearningLab.App.exe --demo connected-animation`. The page shows `ConnectedAnimationDemo.xaml` and `ConnectedAnimationDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.Animation.ConnectedAnimationService](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.animation.connectedanimationservice)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
