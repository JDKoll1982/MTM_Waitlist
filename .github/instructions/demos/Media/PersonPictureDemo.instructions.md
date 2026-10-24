---
description: "PersonPicture (Microsoft.UI.Xaml.Controls.PersonPicture) in Media; a circular avatar that shows an image, initials, or a generic… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/PersonPictureDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/PersonPictureDemo.xaml.cs"
---

# PersonPicture

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.PersonPicture` |
| Page | `Media/PersonPictureDemo` (`WinUI3LearningLab.Demos.Media.PersonPictureDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo person-picture` |

## What it teaches

A circular avatar that shows an image, initials, or a generic glyph, with an optional badge.

## How the demo is built

The page is a `UserControl` at `Media/PersonPictureDemo.xaml`, paired with `PersonPictureDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 27 element(s).

Named elements the code-behind touches: `InitialsPicture`, `PicturePerson`, `PictureCaption`, `NameBox`, `InitialsText`.

Wired in XAML: `OnNameChanged()`.

**Code-behind.** Handlers `OnLoaded()`, `OnNameChanged()`. 99 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Initials are generated from DisplayName only when ProfilePicture is null.
- BadgeNumber and BadgeGlyph render at the bottom-right; they are not a general-purpose overlay.
- The circle is a fixed aspect control: it uses MinWidth/Width, not Stretch, so give it a size.

## Try this

- Run `WinUI3LearningLab.App.exe --demo person-picture`. The page shows `PersonPictureDemo.xaml` and `PersonPictureDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.PersonPicture](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.personpicture)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
