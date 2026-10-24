---
description: "ImageCropper (CommunityToolkit.WinUI.UI.Controls.ImageCropper) in Controls; crop, rotate and zoom an image with an interactive overlay… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/ImageCropperDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/ImageCropperDemo.xaml.cs"
---

# ImageCropper

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Advanced |
| API | `CommunityToolkit.WinUI.UI.Controls.ImageCropper` |
| Page | `Toolkit/ImageCropperDemo` (`WinUI3LearningLab.Demos.Toolkit.ImageCropperDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo image-cropper` |

## What it teaches

Crop, rotate and zoom an image with an interactive overlay, and get the cropped bitmap back.

## How the demo is built

The page is a `UserControl` at `Toolkit/ImageCropperDemo.xaml`, paired with `ImageCropperDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 28 element(s).

Named elements the code-behind touches: `Cropper`, `PreviewImage`, `ResultText`, `CropButton`, `RegionText`, `StatusText`.

Wired in XAML: `OnCropClicked()`, `OnCropShapeClicked()`, `OnAspectRatioClicked()`, `OnResetClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnCropClicked()`, `OnCropShapeClicked()`, `OnAspectRatioClicked()`, `OnResetClicked()`. 195 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Crop the bitmap with SaveAsync or the SoftwareBitmap APIs — reading the visual state alone gives you nothing usable.
- AspectRatio is enforced only when AspectRatioMin/Max are set; leaving them unset lets the user pick any shape.
- The source must finish loading before the overlay appears, so wire Source and CroppedImageAsync in that order.

## Try this

- Run `WinUI3LearningLab.App.exe --demo image-cropper`. The page shows `ImageCropperDemo.xaml` and `ImageCropperDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.ImageCropper](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
