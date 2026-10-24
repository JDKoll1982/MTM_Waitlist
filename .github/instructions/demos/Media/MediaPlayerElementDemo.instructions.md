---
description: "MediaPlayerElement (Microsoft.UI.Xaml.Controls.MediaPlayerElement) in Media; video and audio playback with built-in transport controls… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/MediaPlayerElementDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/MediaPlayerElementDemo.xaml.cs"
---

# MediaPlayerElement

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.MediaPlayerElement` |
| Page | `Media/MediaPlayerElementDemo` (`WinUI3LearningLab.Demos.Media.MediaPlayerElementDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo media-player-element` |

## What it teaches

Video and audio playback with built-in transport controls, over a Windows.Media.Playback.MediaPlayer.

## How the demo is built

The page is a `UserControl` at `Media/MediaPlayerElementDemo.xaml`, paired with `MediaPlayerElementDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `Element`, `VolumeSlider`, `MuteCheck`, `DisposeButton`, `StateText`, `LogText`.

Wired in XAML: `OnVolumeSliderChanged()`, `OnMuteChanged()`, `OnPlayClicked()`, `OnPauseClicked()`, `OnDisposeClicked()`.

**Code-behind.** Handlers `OnVolumeSliderChanged()`, `OnPlayerVolumeChanged()`, `OnMuteChanged()`, `OnPlayClicked()`, `OnPauseClicked()`, `OnDisposeClicked()`, `OnMediaOpened()`, `OnMediaFailed()`, `OnCurrentStateChanged()`, `OnPlaybackStateChanged()`, `OnUnloaded()`, `OnUi()`. 155 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Set Source (a MediaSource) rather than AutoPlay blindly — autoplay in a lab or test run is hostile.
- AreTransportControlsEnabled adds the standard bar; strip it with IsCompact when you provide your own controls.
- MediaPlayer lives on a background thread and can outlive the element: call Dispose on unload.

## Try this

- Run `WinUI3LearningLab.App.exe --demo media-player-element`. The page shows `MediaPlayerElementDemo.xaml` and `MediaPlayerElementDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.MediaPlayerElement](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mediaplayerelement)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
