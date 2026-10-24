---
description: "TableView (not in this SDK) (Concept only) in Collections; the WinUI Gallery samples a TableView control that is not part of the Windows App SDK… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/TableViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/TableViewDemo.xaml.cs"
---

# TableView (not in this SDK)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Intermediate |
| API | `Concept only` |
| Page | `Collections/TableViewDemo` (`WinUI3LearningLab.Demos.Collections.TableViewDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo table-view` |

## What it teaches

The WinUI Gallery samples a TableView control that is not part of the Windows App SDK 2.3.1 public surface.

## Availability

Documentation-only page: TableView is not present in Windows App SDK 2.3.1. The page shows the supported alternatives.

## How the demo is built

The page is a `UserControl` at `Collections/TableViewDemo.xaml`, paired with `TableViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 36 element(s).

Named elements the code-behind touches: `LineList`, `FooterText`.

**Code-behind.** 31 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Microsoft.UI.Xaml.Controls.TableView does not exist in Windows App SDK 2.3.1 — verified against the reference assembly, not documentation.
- Use ListView with a fixed Grid column layout, or the Community Toolkit DataGrid, for tabular data.

## Try this

- Run `WinUI3LearningLab.App.exe --demo table-view`. The page shows `TableViewDemo.xaml` and `TableViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Concept only](https://learn.microsoft.com/windows/apps/design/controls/lists-and-grids)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
