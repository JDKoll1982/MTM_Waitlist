---
description: "RichEditBox (Microsoft.UI.Xaml.Controls.RichEditBox) in Text; a rich text editor over an ITextDocument, for formatted text, links and custom document… MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/RichEditBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/RichEditBoxDemo.xaml.cs"
---

# RichEditBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.RichEditBox` |
| Page | `Text/RichEditBoxDemo` (`WinUI3LearningLab.Demos.Text.RichEditBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo rich-edit-box` |

## What it teaches

A rich text editor over an ITextDocument, for formatted text, links and custom document formats.

## How the demo is built

The page is a `UserControl` at `Text/RichEditBoxDemo.xaml`, paired with `RichEditBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `Editor`, `StatusText`.

Wired in XAML: `OnBoldClicked()`, `OnItalicClicked()`, `OnBulletsClicked()`, `OnReadPlainClicked()`.

**Code-behind.** Handlers `OnBoldClicked()`, `OnItalicClicked()`, `OnBulletsClicked()`, `OnReadPlainClicked()`. 41 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Content is not a string: use Document.SetText/GetText with TextGetOptions to read and write plain text.
- Document is a Windows.UI.Text.ITextDocument and is better for RTF; saving to .rtf needs a file stream from a picker.
- IsSpellCheckEnabled defaults to true and will underline code samples — turn it off for technical input.

## Try this

- Run `WinUI3LearningLab.App.exe --demo rich-edit-box`. The page shows `RichEditBoxDemo.xaml` and `RichEditBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.RichEditBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.richeditbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
