---
description: "TokenizingTextBox (CommunityToolkit.WinUI.UI.Controls.TokenizingTextBox) in Controls; a text box that turns entries into removable tokens — the… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/TokenizingTextBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/TokenizingTextBoxDemo.xaml.cs"
---

# TokenizingTextBox

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Advanced |
| API | `CommunityToolkit.WinUI.UI.Controls.TokenizingTextBox` |
| Page | `Toolkit/TokenizingTextBoxDemo` (`WinUI3LearningLab.Demos.Toolkit.TokenizingTextBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo tokenizing-text-box` |

## What it teaches

A text box that turns entries into removable tokens — the control behind tag and recipient inputs.

## How the demo is built

The page is a `UserControl` at `Toolkit/TokenizingTextBoxDemo.xaml`, paired with `TokenizingTextBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `Recipients`, `CountText`, `SuggestionText`, `StatusText`.

Wired in XAML: `OnTextChanged()`, `OnTokenItemAdding()`, `OnTokenItemAdded()`, `OnTokenItemRemoved()`, `OnAddTokenClicked()`, `OnAddDuplicateClicked()`, `OnClearClicked()`, `OnListSuggestionsClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnTextChanged()`, `OnTokenItemAdding()`, `OnTokenItemAdded()`, `OnTokenItemRemoved()`, `OnAddTokenClicked()`, `OnAddDuplicateClicked()`, `OnClearClicked()`, `OnListSuggestionsClicked()`. 181 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- TokenizingTextBox does NOT filter its own suggestions — SuggestedItemsSource is shown exactly as given. Filtering against the typed text is your job, in the AutoSuggestBox-style TextChanged handler.
- TextChanged is typed as TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs>, so the sender parameter is an AutoSuggestBox, not the TokenizingTextBox.
- Filter on AutoSuggestBoxTextChangedEventArgs.Reason == UserInput, or resetting the list re-enters the handler.
- TokenItemAdding fires with the typed text: mark it Handled and add your own object when you need validation. Free text that never matches a suggestion still arrives there, which is why rejection belongs in that handler.
- DisplayMemberPath decides what the token shows; without it the item's ToString is used.
- The tokens are items in ItemsSource, not child controls — read the collection, not the visual tree.

## Traps recorded in the source

- Keeps the token readable if a container style ever drops the generated template.

## Try this

- Run `WinUI3LearningLab.App.exe --demo tokenizing-text-box`. The page shows `TokenizingTextBoxDemo.xaml` and `TokenizingTextBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.TokenizingTextBox](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
