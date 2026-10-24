---
description: "CarouselView2 (DevWinUI.CarouselView2) in Controls; a coverflow-style carousel for a small set of featured items, with keyboard and pointer navigation. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/CarouselViewDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/CarouselViewDemo.xaml.cs"
---

# CarouselView2

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.CarouselView2` |
| Page | `DevWinUI/CarouselViewDemo` (`WinUI3LearningLab.Demos.DevWinUI.CarouselViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo carousel-view` |

## What it teaches

A coverflow-style carousel for a small set of featured items, with keyboard and pointer navigation.

## How the demo is built

The page is a `UserControl` at `DevWinUI/CarouselViewDemo.xaml`, paired with `CarouselViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 18 element(s).

Named elements the code-behind touches: `Featured`, `SelectedText`.

Wired in XAML: `OnCarouselSelectionChanged()`, `OnPreviousClick()`, `OnNextClick()`.

**Code-behind.** Handlers `OnLoaded()`, `OnCarouselSelectionChanged()`, `OnPreviousClick()`, `OnNextClick()`. 60 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It realises every item — this is a highlight control for a handful of items, not a virtualising list.
- CarouselView2 is the current control; the older CarouselView remains for compatibility and differs in its item panel.
- Items are CarouselViewItem objects, so the template is applied per item rather than through ItemTemplate alone.

## Try this

- Run `WinUI3LearningLab.App.exe --demo carousel-view`. The page shows `CarouselViewDemo.xaml` and `CarouselViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.CarouselView2](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
