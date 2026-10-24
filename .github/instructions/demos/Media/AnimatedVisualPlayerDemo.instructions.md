---
description: "AnimatedVisualPlayer (Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer) in Media; plays an IAnimatedVisualSource — Lottie JSON compiled to… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/AnimatedVisualPlayerDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/AnimatedVisualPlayerDemo.xaml.cs"
---

# AnimatedVisualPlayer

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer` |
| Page | `Media/AnimatedVisualPlayerDemo` (`WinUI3LearningLab.Demos.Media.AnimatedVisualPlayerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo animated-visual-player` |

## What it teaches

Plays an IAnimatedVisualSource — Lottie JSON compiled to code, or one of the built-in animated icon sources.

## How the demo is built

The page is a `UserControl` at `Media/AnimatedVisualPlayerDemo.xaml`, paired with `AnimatedVisualPlayerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 18 element(s).

Named elements the code-behind touches: `Player`, `DurationText`, `StateText`.

Wired in XAML: `OnPlayClicked()`, `OnPauseClicked()`, `OnResumeClicked()`, `OnPlayLoopedClicked()`, `OnStopClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnPlayClicked()`, `OnPlayLoopedClicked()`, `OnPauseClicked()`, `OnResumeClicked()`, `OnStopClicked()`. 69 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Source must be an IAnimatedVisualSource; there is no path to a .json file at runtime in WinUI 3.
- PlayAsync(from, to, looped) returns a Task you can await; Pause and Resume act on the same player.
- Set AutoPlay false when the animation is a one-shot, or it restarts every time the page is shown.

## Try this

- Run `WinUI3LearningLab.App.exe --demo animated-visual-player`. The page shows `AnimatedVisualPlayerDemo.xaml` and `AnimatedVisualPlayerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedvisualplayer)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
