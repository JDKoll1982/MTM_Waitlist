---
description: "SelectorBarSegmented (DevWinUI.SelectorBarSegmented) in Controls; a connected group of mutually exclusive options, built on SelectorBar — a denser alternative… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/SegmentedDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/SegmentedDemo.xaml.cs"
---

# SelectorBarSegmented

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.SelectorBarSegmented` |
| Page | `DevWinUI/SegmentedDemo` (`WinUI3LearningLab.Demos.DevWinUI.SegmentedDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo segmented` |

## What it teaches

A connected group of mutually exclusive options, built on SelectorBar — a denser alternative to radio buttons for two to four choices.

## How the demo is built

The page is a `UserControl` at `DevWinUI/SegmentedDemo.xaml`, paired with `SegmentedDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 28 element(s).

Named elements the code-behind touches: `ModeBar`, `ModeText`, `ScanPanel`, `ScanStatus`, `ManualPanel`, `ManualPart`, `ManualStatus`, `ImportPanel`, `ImportStatus`, `ResourceWarning`.

Wired in XAML: `OnModeChanged()`, `OnScanClick()`, `OnAddManualClick()`, `OnImportClick()`.

**Code-behind.** Handlers `OnModeChanged()`, `OnScanClick()`, `OnAddManualClick()`, `OnImportClick()`. 99 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- There is no `Segmented` control in DevWinUI 10.8.0; the type is SelectorBarSegmented, and because it derives from SelectorBar it binds SelectorBarItems and raises SelectorBarSelectionChanged.
- Its constructor moves SelectorBarItemHorizontalStyle and SelectorBarItemVerticalStyle out of Application.Current.Resources, so those two keys must be copied into the page or the items render unstyled.
- Below three items, use a ToggleButton; above about five, use a ComboBox.
- Selected index is the state; the visual highlight follows it, so do not style segments individually.

## Try this

- Run `WinUI3LearningLab.App.exe --demo segmented`. The page shows `SegmentedDemo.xaml` and `SegmentedDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.SelectorBarSegmented](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
