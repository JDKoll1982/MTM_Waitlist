---
description: "Theme transitions (Microsoft.UI.Xaml.Media.Animation.ThemeTransition) in Motion; preset content transitions (Entrance, ContentThemeTransition… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/ThemeTransitionsDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/ThemeTransitionsDemo.xaml.cs"
---

# Theme transitions

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Media.Animation.ThemeTransition` |
| Page | `Motion/ThemeTransitionsDemo` (`WinUI3LearningLab.Demos.Motion.ThemeTransitionsDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo theme-transitions` |

## What it teaches

Preset content transitions (Entrance, ContentThemeTransition, AddDeleteThemeTransition) for revealing content.

## How the demo is built

The page is a `UserControl` at `Motion/ThemeTransitionsDemo.xaml`, paired with `ThemeTransitionsDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `Stage`, `StatusText`.

Wired in XAML: `OnEntranceClicked()`, `OnContentClicked()`, `OnAddDeleteClicked()`, `OnNoneClicked()`.

**Code-behind.** Handlers `OnEntranceClicked()`, `OnContentClicked()`, `OnAddDeleteClicked()`, `OnNoneClicked()`. 106 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Theme transitions live in ContentTransitions / ChildrenTransitions, not in Storyboards.
- They run on first layout too, so a page can animate in every time it is created — pleasant once, annoying on every keystroke.
- Disable them per element by setting ContentTransitions=null rather than removing the whole collection.

## Try this

- Run `WinUI3LearningLab.App.exe --demo theme-transitions`. The page shows `ThemeTransitionsDemo.xaml` and `ThemeTransitionsDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.Animation.ThemeTransition](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.animation.themetransition)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
