---
description: "RichTextBlock (Microsoft.UI.Xaml.Controls.RichTextBlock) in Text; read-only, richly formatted text built from Paragraph/Inline runs, with selection and… MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/RichTextBlockDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/RichTextBlockDemo.xaml.cs"
---

# RichTextBlock

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.RichTextBlock` |
| Page | `Text/RichTextBlockDemo` (`WinUI3LearningLab.Demos.Text.RichTextBlockDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo rich-text-block` |

## What it teaches

Read-only, richly formatted text built from Paragraph/Inline runs, with selection and overflow support.

## How the demo is built

The page is a `UserControl` at `Text/RichTextBlockDemo.xaml`, paired with `RichTextBlockDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `SourceBlock`, `OverflowBlock`, `OverflowText`.

**Code-behind.** 20 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Blocks is a collection of Paragraph objects — you build content in code or XAML, not with a single Text string.
- OverflowTarget plus HasOverflowContent is how 'read more' truncation works; there is no MaxLines.
- Inline elements (Run, Hyperlink, Bold) are not controls, so they cannot carry automation names directly.

## Try this

- Run `WinUI3LearningLab.App.exe --demo rich-text-block`. The page shows `RichTextBlockDemo.xaml` and `RichTextBlockDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.RichTextBlock](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.richtextblock)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
