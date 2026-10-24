---
description: "ListView (Microsoft.UI.Xaml.Controls.ListView) in Collections; the default vertical, virtualising list with selection, grouping, headers and item… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/ListViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/ListViewDemo.xaml.cs"
---

# ListView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ListView` |
| Page | `Collections/ListViewDemo` (`WinUI3LearningLab.Demos.Collections.ListViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo list-view` |

## What it teaches

The default vertical, virtualising list with selection, grouping, headers and item click support.

## How the demo is built

The page is a `UserControl` at `Collections/ListViewDemo.xaml`, paired with `ListViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `ModeBox`, `RequestList`, `SelectionText`.

Wired in XAML: `OnModeChanged()`, `OnSelectionChanged()`.

**Code-behind.** Handlers `OnSelectionChanged()`, `OnModeChanged()`. 61 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ItemTemplate defines visuals but the container is ListViewItem — style through ItemContainerStyle, not the template alone.
- IsItemClickEnabled=False still fires SelectionChanged; pick one interaction model and disable the other.
- ItemClick gives you the clicked item; SelectionChanged gives you the whole selection — mixing them double-handles clicks.
- Nested scrollable controls inside items break virtualisation and scrolling; give them a fixed height or avoid them.

## Try this

- Set SelectionMode=Multiple and read SelectedItems.Count in SelectionChanged.

## Documentation

- [Microsoft.UI.Xaml.Controls.ListView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.listview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
