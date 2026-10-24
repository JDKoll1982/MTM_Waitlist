---
description: "Camera preview (MediaCapture) (Windows.Media.Capture.MediaCapture) in Media; shows a live camera feed by feeding a MediaCapture preview… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/CameraPreviewDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/CameraPreviewDemo.xaml.cs"
---

# Camera preview (MediaCapture)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Advanced |
| API | `Windows.Media.Capture.MediaCapture` |
| Page | `Media/CameraPreviewDemo` (`WinUI3LearningLab.Demos.Media.CameraPreviewDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo camera-preview` |

## What it teaches

Shows a live camera feed by feeding a MediaCapture preview into a MediaPlayerElement.

## Availability

Interactive, but the page does not start the camera until you press Start — this lab is unpackaged, so camera privacy settings decide whether initialisation succeeds.

## How the demo is built

The page is a `UserControl` at `Media/CameraPreviewDemo.xaml`, paired with `CameraPreviewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 12 element(s).

Named elements the code-behind touches: `ErrorBar`, `Preview`, `Placeholder`, `StartButton`, `StopButton`, `StatusText`.

Wired in XAML: `OnStartCameraClicked()`, `OnStopCameraClicked()`.

**Code-behind.** Handlers `OnStartCameraClicked()`, `OnStopCameraClicked()`, `OnUnloaded()`, `OnCaptureFailed()`, `OnUi()`. 137 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- MediaCapture needs camera permission (Settings > Privacy > Camera) and initialises asynchronously — always try/catch.
- WinUI 3 has no CaptureElement: use MediaPlayerElement with a MediaSource created from MediaCapture.
- Stop and Dispose the capture object when the page unloads, or the camera light stays on.

## Try this

- Run `WinUI3LearningLab.App.exe --demo camera-preview`. The page shows `CameraPreviewDemo.xaml` and `CameraPreviewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Windows.Media.Capture.MediaCapture](https://learn.microsoft.com/windows/apps/develop/media/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
