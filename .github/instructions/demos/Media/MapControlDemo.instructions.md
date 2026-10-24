---
description: "MapControl (Microsoft.UI.Xaml.Controls.MapControl) in Media; an embedded map with layers, landmarks and 3D views, backed by Azure… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/MapControlDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/MapControlDemo.xaml.cs"
---

# MapControl

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.MapControl` |
| Page | `Media/MapControlDemo` (`WinUI3LearningLab.Demos.Media.MapControlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo map-control` |

## What it teaches

An embedded map with layers, landmarks and 3D views, backed by Azure Maps.

## How the demo is built

The page is a `UserControl` at `Media/MapControlDemo.xaml`, paired with `MapControlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 14 element(s).

Named elements the code-behind touches: `ErrorBar`, `MapHost`, `PlaceholderText`, `StatusText`, `ZoomSlider`, `TokenStatusText`.

Wired in XAML: `OnZoomChanged()`, `OnSetTokenClicked()`, `OnClearTokenClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnMapServiceError()`, `OnSetTokenClicked()`, `OnClearTokenClicked()`, `OnZoomChanged()`. 104 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Nothing renders until MapServiceToken is set — a map with no token is a blank grey square, not an error.
- MapElementsLayer is how you add pins and shapes; Children of the layer take MapIcon and MapPolygon objects.
- The control needs network access; offline it silently shows the last tiles or nothing.

## Try this

- Run `WinUI3LearningLab.App.exe --demo map-control`. The page shows `MapControlDemo.xaml` and `MapControlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.MapControl](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mapcontrol)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
