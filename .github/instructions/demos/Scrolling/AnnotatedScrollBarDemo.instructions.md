---
description: "AnnotatedScrollBar (Microsoft.UI.Xaml.Controls.AnnotatedScrollBar) in Scrolling; a scroll bar that also shows labels or marks at meaningful… MTM: MTM_WIP_Application_WinForms › Inventory › stock list and last transactions."
applyTo: "src/WinUI3LearningLab.App/Demos/Scrolling/AnnotatedScrollBarDemo.xaml,src/WinUI3LearningLab.App/Demos/Scrolling/AnnotatedScrollBarDemo.xaml.cs"
---

# AnnotatedScrollBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Scrolling |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.AnnotatedScrollBar` |
| Page | `Scrolling/AnnotatedScrollBarDemo` (`WinUI3LearningLab.Demos.Scrolling.AnnotatedScrollBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo annotated-scroll-bar` |

## What it teaches

A scroll bar that also shows labels or marks at meaningful offsets, like chapter names in a document.

## How the demo is built

The page is a `UserControl` at `Scrolling/AnnotatedScrollBarDemo.xaml`, paired with `AnnotatedScrollBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 39 element(s).

Named elements the code-behind touches: `ChapterScroll`, `DocumentPanel`, `Chapter1`, `Chapter2`, `Chapter3`, `Chapter4`, `Chapter5`, `Chapter6`, `ChapterBar`, `ControllerStatus`, `ScrollingStatus`, `ViewStatus`.

Wired in XAML: `OnViewChanged()`, `OnAnnotatedScrolling()`.

**Code-behind.** Handlers `OnLoaded()`, `OnDocumentSizeChanged()`, `OnAnnotatedScrolling()`, `OnViewChanged()`. 70 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › stock list and last transactions**

The stock list is tens of thousands of rows on a workstation that also runs the label printer. Realising rows lazily and keeping the header in view is the difference between a usable screen and a frozen one.

Schema this mirrors: `inv_inventory, sys_last_10_transactions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It is a free-standing control — the API expects you to wire Scrolling event and Value to a ScrollView yourself.
- Labels come from a collection of AnnotatedScrollBarLabel objects, each tied to an offset value.
- It only exists in newer Windows App SDK releases; check the version before copying this into an older app.

## Traps recorded in the source

- AnnotatedScrollBar.ScrollController is read-only in Windows App SDK 2.3.1: the bar picks up the controller from the ScrollView it is placed beside, so there is nothing to assign here.

## Try this

- Run `WinUI3LearningLab.App.exe --demo annotated-scroll-bar`. The page shows `AnnotatedScrollBarDemo.xaml` and `AnnotatedScrollBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AnnotatedScrollBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.annotatedscrollbar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Scrolling.cs` or the demo source, then regenerate.
