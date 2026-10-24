---
description: "ParallaxView (Microsoft.UI.Xaml.Controls.ParallaxView) in Motion; moves a background layer at a different rate than the scroll surface, producing depth… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/ParallaxViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/ParallaxViewDemo.xaml.cs"
---

# ParallaxView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.ParallaxView` |
| Page | `Motion/ParallaxViewDemo` (`WinUI3LearningLab.Demos.Motion.ParallaxViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo parallax-view` |

## What it teaches

Moves a background layer at a different rate than the scroll surface, producing depth while scrolling.

## How the demo is built

The page is a `UserControl` at `Motion/ParallaxViewDemo.xaml`, paired with `ParallaxViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 44 element(s).

Named elements the code-behind touches: `ParallaxLayer`, `Scroll`, `ShiftText`, `StatusText`.

Wired in XAML: `OnViewChanged()`, `OnShiftChanged()`, `OnSourceToggled()`.

**Code-behind.** Handlers `OnDemoLoaded()`, `OnShiftChanged()`, `OnSourceToggled()`, `OnViewChanged()`. 81 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ParallaxView needs a Source (the scrolling element) and a Child (the layer to move); without Source it does nothing.
- It sizes its child to the parallax extent, so images get cropped — plan for overscan.
- VerticalShift and HorizontalShift define the travel distance; large values look cheap, small values look like jitter.

## Traps recorded in the source

- ParallaxView does nothing without a Source: the Source is the element whose scrolling it reads.
- IsOn is set during XAML parsing, so the sender is read instead of the generated field.

## Try this

- Run `WinUI3LearningLab.App.exe --demo parallax-view`. The page shows `ParallaxViewDemo.xaml` and `ParallaxViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ParallaxView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.parallaxview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
