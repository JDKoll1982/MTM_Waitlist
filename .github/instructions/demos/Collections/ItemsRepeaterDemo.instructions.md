---
description: "ItemsRepeater (Microsoft.UI.Xaml.Controls.ItemsRepeater) in Collections; a layout-only repeater: it repeats a template but has no scrolling… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/ItemsRepeaterDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/ItemsRepeaterDemo.xaml.cs"
---

# ItemsRepeater

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.ItemsRepeater` |
| Page | `Collections/ItemsRepeaterDemo` (`WinUI3LearningLab.Demos.Collections.ItemsRepeaterDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo items-repeater` |

## What it teaches

A layout-only repeater: it repeats a template but has no scrolling, selection or chrome. You compose those yourself.

## How the demo is built

The page is a `UserControl` at `Collections/ItemsRepeaterDemo.xaml`, paired with `ItemsRepeaterDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

Named elements the code-behind touches: `LayoutText`, `Repeater`.

Wired in XAML: `OnStackLayoutClick()`, `OnUniformLayoutClick()`.

**Code-behind.** Handlers `OnStackLayoutClick()`, `OnUniformLayoutClick()`. 55 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- There is no ScrollViewer inside — host it in a ScrollView, which is what enables virtualisation.
- ItemsSource accepts plain IEnumerable, but for large or changing data use an observable collection or ItemsSourceView.
- It does not create item containers, so ItemContainerStyle and container-based styling do not apply.
- Layout is mandatory for anything beyond stacking: assign StackLayout, UniformGridLayout or a custom VirtualizingLayout.

## Try this

- Swap StackLayout for UniformGridLayout and watch the items reflow without touching the template.

## Documentation

- [Microsoft.UI.Xaml.Controls.ItemsRepeater](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.itemsrepeater)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
