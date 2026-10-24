---
description: "Sound (MediaPlayer) (Windows.Media.Playback.MediaPlayer) in Media; playing audio without a video surface: a MediaPlayer with no… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/SoundDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/SoundDemo.xaml.cs"
---

# Sound (MediaPlayer)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Intermediate |
| API | `Windows.Media.Playback.MediaPlayer` |
| Page | `Media/SoundDemo` (`WinUI3LearningLab.Demos.Media.SoundDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo sound` |

## What it teaches

Playing audio without a video surface: a MediaPlayer with no MediaPlayerElement attached.

## How the demo is built

The page is a `UserControl` at `Media/SoundDemo.xaml`, paired with `SoundDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 16 element(s).

Named elements the code-behind touches: `ErrorBar`, `SourceBox`, `StateText`, `DetailText`, `LogText`.

Wired in XAML: `OnSetSourceClicked()`, `OnPlayClicked()`, `OnPauseClicked()`, `OnStopClicked()`.

**Code-behind.** Handlers `OnSetSourceClicked()`, `OnPlayClicked()`, `OnPauseClicked()`, `OnStopClicked()`, `OnMediaOpened()`, `OnMediaFailed()`, `OnMediaEnded()`, `OnCurrentStateChanged()`, `OnPlaybackStateChanged()`, `OnUnloaded()`, `OnUi()`. 188 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- SoundPlayerElement does not exist in this SDK; use MediaPlayer directly, or MediaPlayerElement with the video area collapsed.
- MediaPlayer opens asynchronously — call Play in MediaOpened, not immediately after setting Source.
- Respect volume and mute when the app loses focus; nothing stops the audio for you.

## Try this

- Run `WinUI3LearningLab.App.exe --demo sound`. The page shows `SoundDemo.xaml` and `SoundDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Windows.Media.Playback.MediaPlayer](https://learn.microsoft.com/windows/apps/develop/media/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
