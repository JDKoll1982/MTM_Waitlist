---
description: "Animation interop (Composition) (Microsoft.UI.Xaml.Hosting.ElementCompositionPreview) in Motion; drops down from XAML to the Windows.UI.Composition visual… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/AnimationInteropDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/AnimationInteropDemo.xaml.cs"
---

# Animation interop (Composition)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Hosting.ElementCompositionPreview` |
| Page | `Motion/AnimationInteropDemo` (`WinUI3LearningLab.Demos.Motion.AnimationInteropDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo animation-interop` |

## What it teaches

Drops down from XAML to the Windows.UI.Composition visual layer to animate properties XAML does not expose.

## How the demo is built

The page is a `UserControl` at `Motion/AnimationInteropDemo.xaml`, paired with `AnimationInteropDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `Target`, `StatusText`.

Wired in XAML: `OnDemoLoaded()`, `OnAnimateClicked()`, `OnInspectClicked()`, `OnForceLayoutClicked()`.

**Code-behind.** Handlers `OnDemoLoaded()`, `OnAnimateClicked()`, `OnInspectClicked()`, `OnForceLayoutClicked()`. 118 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ElementCompositionPreview.GetElementVisual gives you the visual for an element — but XAML owns many of its properties, so animate via the element's Visual with care.
- The compositor runs off the UI thread: use StartAnimation with an expression or keyframe animation instead of a DispatcherTimer loop.
- Getting the visual before the element is loaded returns a detached visual; hook Loaded first.

## Traps recorded in the source

- XAML creates the element's visual during layout. Anything asked for before Loaded is a visual that is not in the composition tree, so animating it moves nothing on screen.
- A Border that was never added to the tree proves the point: a visual exists, it is just not in the tree, so it has no size and nothing renders.

## Try this

- Run `WinUI3LearningLab.App.exe --demo animation-interop`. The page shows `AnimationInteropDemo.xaml` and `AnimationInteropDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Hosting.ElementCompositionPreview](https://learn.microsoft.com/windows/apps/design/motion/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
