---
description: "Page transitions (Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition) in Motion; the animation a Frame plays when navigating, chosen per… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/PageTransitionsDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/PageTransitionsDemo.xaml.cs"
---

# Page transitions

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition` |
| Page | `Motion/PageTransitionsDemo` (`WinUI3LearningLab.Demos.Motion.PageTransitionsDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo page-transitions` |

## What it teaches

The animation a Frame plays when navigating, chosen per navigation with NavigationTransitionInfo.

## How the demo is built

The page is a `UserControl` at `Motion/PageTransitionsDemo.xaml`, paired with `PageTransitionsDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 12 element(s).

Named elements the code-behind touches: `Host`, `StatusText`.

Wired in XAML: `OnEntranceClicked()`, `OnDrillInClicked()`, `OnRepositionClicked()`, `OnSuppressClicked()`, `OnDescribeClicked()`.

**Code-behind.** Handlers `OnEntranceClicked()`, `OnDrillInClicked()`, `OnRepositionClicked()`, `OnSuppressClicked()`, `OnDescribeClicked()`. 123 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Pass the transition to Frame.Navigate(type, parameter, transitionInfo); the Frame's default is slide.
- SuppressNavigationTransitionInfo is the escape hatch for a jump-cut, which a shell uses for deep links.
- Frame.Navigate needs a Page type the XAML metadata knows about — in practice a page with its own .xaml file. Navigating to a Page subclass built only in code kills the process with an access violation instead of throwing, so it cannot be guarded.
- The theme transitions (Entrance, DrillIn, Reposition) animate a content swap; they are not substitutes for a Frame navigation.

## Traps recorded in the source

- Constructing these is safe; it is handing them to a Frame that requires real page metadata.

## Try this

- Run `WinUI3LearningLab.App.exe --demo page-transitions`. The page shows `PageTransitionsDemo.xaml` and `PageTransitionsDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.Animation.NavigationThemeTransition](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.animation.navigationthemetransition)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
