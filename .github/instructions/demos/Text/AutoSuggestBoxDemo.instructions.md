---
description: "AutoSuggestBox (Microsoft.UI.Xaml.Controls.AutoSuggestBox) in Text; a search box that suggests as you type and can submit a free-form query. It is the… MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/AutoSuggestBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/AutoSuggestBoxDemo.xaml.cs"
---

# AutoSuggestBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.AutoSuggestBox` |
| Page | `Text/AutoSuggestBoxDemo` (`WinUI3LearningLab.Demos.Text.AutoSuggestBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo auto-suggest-box` |

## What it teaches

A search box that suggests as you type and can submit a free-form query. It is the canonical search affordance in WinUI.

## How the demo is built

The page is a `UserControl` at `Text/AutoSuggestBoxDemo.xaml`, paired with `AutoSuggestBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 5 element(s).

Named elements the code-behind touches: `FruitBox`, `StatusText`.

Wired in XAML: `OnTextChanged()`, `OnSuggestionChosen()`, `OnQuerySubmitted()`.

**Code-behind.** Handlers `OnTextChanged()`, `OnSuggestionChosen()`, `OnQuerySubmitted()`. 46 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Part number entry searches `md_part_ids` as the operator types, with the scanner wedge bypassing the suggestion list once a full code arrives.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- TextChanged fires with a Reason: only UserInput means the user typed, so filter on it before refreshing suggestions.
- QuerySubmitted carries both the typed text and the chosen suggestion — handle both cases or the second one silently does nothing.
- SuggestionChosen fires before the text is replaced; set Text in that handler when you want to display a friendly label.
- Setting ItemsSource by hand makes the box open the suggestion list; to keep it closed, update items without assigning a new list.

## Try this

- Handle SuggestionChosen to store the key but keep the display name in Text.

## Documentation

- [Microsoft.UI.Xaml.Controls.AutoSuggestBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.autosuggestbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
