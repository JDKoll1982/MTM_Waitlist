---
description: "TextBox (Microsoft.UI.Xaml.Controls.TextBox) in Text; single-line or multi-line plain text input, with headers, placeholders, validation and input scopes. MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/TextBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/TextBoxDemo.xaml.cs"
---

# TextBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.TextBox` |
| Page | `Text/TextBoxDemo` (`WinUI3LearningLab.Demos.Text.TextBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo text-box` |

## What it teaches

Single-line or multi-line plain text input, with headers, placeholders, validation and input scopes.

## How the demo is built

The page is a `UserControl` at `Text/TextBoxDemo.xaml`, paired with `TextBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `SingleLine`, `ValidatedBox`, `StatusText`.

Wired in XAML: `OnTextChanged()`, `OnValidatedKeyDown()`.

**Code-behind.** Handlers `OnTextChanged()`, `OnValidatedKeyDown()`. 33 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- TextChanged fires per keystroke; TextChanging fires before the text is committed and cannot be cancelled either.
- AcceptsReturn makes it multi-line but does not enable wrapping — set TextWrapping as well.
- Use InputScope (EmailSmtpAddress, Number, Search) to get the right on-screen keyboard on touch devices.
- Description helps screen readers and also renders the small helper line under the box.

## Try this

- Add MaxLength and observe that typing stops silently at the limit.

## Documentation

- [Microsoft.UI.Xaml.Controls.TextBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
