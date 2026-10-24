---
description: "TextBlock (Microsoft.UI.Xaml.Controls.TextBlock) in Text; the workhorse for read-only text: styled, wrapped, trimmed, and optionally selectable. MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/TextBlockDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/TextBlockDemo.xaml.cs"
---

# TextBlock

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.TextBlock` |
| Page | `Text/TextBlockDemo` (`WinUI3LearningLab.Demos.Text.TextBlockDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo text-block` |

## What it teaches

The workhorse for read-only text: styled, wrapped, trimmed, and optionally selectable.

## How the demo is built

The page is a `UserControl` at `Text/TextBlockDemo.xaml`, paired with `TextBlockDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `SelectableText`.

**Code-behind.** 12 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- TextWrapping defaults to NoWrap; long strings will be clipped rather than wrapped unless you set Wrap.
- TextTrimming has no effect while TextWrapping is Wrap — pick one behaviour per block.
- IsTextSelectionEnabled turns it into selectable text but also changes hit-testing, which can swallow taps.
- Use theme-aware styles (BodyTextBlockStyle, TitleTextBlockStyle) instead of hard-coded FontSize for Fluent consistency.

## Try this

- Compare TextTrimming=CharacterEllipsis with TextWrapping=Wrap on the same long string.

## Documentation

- [Microsoft.UI.Xaml.Controls.TextBlock](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textblock)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
