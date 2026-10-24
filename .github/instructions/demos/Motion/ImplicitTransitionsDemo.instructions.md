---
description: "Implicit and reposition transitions (Microsoft.UI.Xaml.Media.Animation.Transition) in Motion; transitions you attach to an element so layout changes… MTM: MTM_Waitlist › Waitlist › board updates."
applyTo: "src/WinUI3LearningLab.App/Demos/Motion/ImplicitTransitionsDemo.xaml,src/WinUI3LearningLab.App/Demos/Motion/ImplicitTransitionsDemo.xaml.cs"
---

# Implicit and reposition transitions

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Motion |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Media.Animation.Transition` |
| Page | `Motion/ImplicitTransitionsDemo` (`WinUI3LearningLab.Demos.Motion.ImplicitTransitionsDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo implicit-transitions` |

## What it teaches

Transitions you attach to an element so layout changes animate themselves — no Storyboard, no code.

## How the demo is built

The page is a `UserControl` at `Motion/ImplicitTransitionsDemo.xaml`, paired with `ImplicitTransitionsDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `TransitionsSwitch`, `TileGrid`, `StatusText`.

Wired in XAML: `OnAddClicked()`, `OnRemoveClicked()`, `OnTransitionsToggled()`.

**Code-behind.** Handlers `OnAddClicked()`, `OnRemoveClicked()`, `OnTransitionsToggled()`. 163 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › board updates**

When a request is assigned or a lugger moves, the board refreshes underneath the operator. Motion is how that change is noticed without a sound or a dialog — and why a transition that runs forever in a background tab is a defect.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- UIElement.Transitions holds transitions for that element (Position, Reposition, FadeIn); Panel.ChildrenTransitions animates children being added or removed.
- Transitions only run when the property changes through layout, so toggling Visibility does not animate — use a theme transition for that.
- A transition that is already running is not restarted; toggling faster than the duration collapses into one animation.

## Traps recorded in the source

- Both collections matter and they are separate: ChildrenTransitions belongs to the panel, Transitions belongs to each child.
- Children is a UIElementCollection; Grid.SetRow/SetColumn take FrameworkElement.

## Try this

- Run `WinUI3LearningLab.App.exe --demo implicit-transitions`. The page shows `ImplicitTransitionsDemo.xaml` and `ImplicitTransitionsDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.Animation.Transition](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.animation.transition)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Motion.cs` or the demo source, then regenerate.
