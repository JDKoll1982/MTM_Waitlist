---
description: "Barcode (DevWinUI.Base.Barcode) in Controls; renders a scannable barcode symbol from data — the on-screen equivalent of a printed receiving label. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/BarcodeDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/BarcodeDemo.xaml.cs"
---

# Barcode

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Advanced |
| API | `DevWinUI.Base.Barcode` |
| Page | `DevWinUI/BarcodeDemo` (`WinUI3LearningLab.Demos.DevWinUI.BarcodeDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo barcode` |

## What it teaches

Renders a scannable barcode symbol from data — the on-screen equivalent of a printed receiving label.

## How the demo is built

The page is a `UserControl` at `DevWinUI/BarcodeDemo.xaml`, paired with `BarcodeDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `PartBox`, `TypeBox`, `SymbolImage`, `HumanReadable`, `StatusText`.

Wired in XAML: `OnSelectionChanged()`.

**Code-behind.** Handlers `OnLoaded()`, `OnUnloaded()`, `OnSelectionChanged()`. 66 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Barcode is a non-visual model in DevWinUI.Base, not a control. It reaches the screen only through an extension such as ToPng(barcode, options), which you then wrap in a BitmapImage.
- It is a code renderer, not a camera scanner. Reading a barcode needs a capture device and a decoder.
- Quiet zones are part of the standard: leave margin around the symbol or a real scanner will fail on perfectly valid data.

## Try this

- Run `WinUI3LearningLab.App.exe --demo barcode`. The page shows `BarcodeDemo.xaml` and `BarcodeDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Base.Barcode](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
