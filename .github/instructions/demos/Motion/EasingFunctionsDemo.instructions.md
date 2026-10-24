---
description: "Easing functions (Microsoft.UI.Xaml.Media.Animation.EasingFunctionBase) in Motion; timing curves that turn linear animations into motion that feels… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/EasingFunctionsDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/EasingFunctionsDemo.xaml.cs"
---

# Easing functions

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Media.Animation.EasingFunctionBase` |
| Page | `Motion/EasingFunctionsDemo` (`WinUI3LearningLab.Demos.Motion.EasingFunctionsDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo easing-functions` |

## What it teaches

Timing curves that turn linear animations into motion that feels physical: Quadratic, Cubic, Back, Bounce, Elastic, Circle.

## How the demo is built

The page is a `UserControl` at `Motion/EasingFunctionsDemo.xaml`, paired with `EasingFunctionsDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `EasingBox`, `ModeBox`, `DurationText`, `DurationSlider`, `Track`, `Traveller`, `StatusText`.

Wired in XAML: `OnSettingChanged()`, `OnDurationChanged()`, `OnRunClicked()`, `OnResetClicked()`.

**Code-behind.** Handlers `OnDemoLoaded()`, `OnSettingChanged()`, `OnDurationChanged()`, `OnRunClicked()`, `OnResetClicked()`. 128 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- EasingMode (EaseIn, EaseOut, EaseInOut) changes the whole feel — Fluent prefers EaseOut for entrances.
- Easing functions apply to key frames and animations, not to Storyboard.Begin itself.
- Very long durations with Back or Elastic look cartoonish; keep UI transitions under ~300 ms.

## Traps recorded in the source

- The storyboard targets the transform, not the Border: a TranslateX on a CompositeTransform is an independent animation, so it does not force a layout pass per frame.

## Try this

- Run `WinUI3LearningLab.App.exe --demo easing-functions`. The page shows `EasingFunctionsDemo.xaml` and `EasingFunctionsDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.Animation.EasingFunctionBase](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.animation.easingfunctionbase)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
