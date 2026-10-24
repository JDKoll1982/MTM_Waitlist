---
description: "Clipboard (Windows.ApplicationModel.DataTransfer.Clipboard) in Clipboard & pickers; puts text, HTML, bitmaps or storage items on the Windows… MTM: MTM_Receiving_Application › Receiving › copy, export and file selection."
applyTo: "src/WinUI3LearningLab.App/Demos/SystemServices/ClipboardDemo.xaml,src/WinUI3LearningLab.App/Demos/SystemServices/ClipboardDemo.xaml.cs"
---

# Clipboard

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Clipboard & pickers |
| Level | Starter |
| API | `Windows.ApplicationModel.DataTransfer.Clipboard` |
| Page | `SystemServices/ClipboardDemo` (`WinUI3LearningLab.Demos.SystemServices.ClipboardDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo clipboard` |

## What it teaches

Puts text, HTML, bitmaps or storage items on the Windows clipboard and reads them back.

## How the demo is built

The page is a `UserControl` at `SystemServices/ClipboardDemo.xaml`, paired with `ClipboardDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `PayloadBox`, `FormatsText`, `OutputText`, `LastActionText`.

Wired in XAML: `OnSetTextClicked()`, `OnSetHtmlClicked()`, `OnReadTextClicked()`, `OnReadHtmlClicked()`, `OnClearClicked()`.

**Code-behind.** Handlers `OnSetTextClicked()`, `OnSetHtmlClicked()`, `OnReadTextClicked()`, `OnReadHtmlClicked()`, `OnClearClicked()`. 151 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › copy, export and file selection**

Clerks paste part lists into emails and export a shift to a file for the planner, then pick the label template to print. These are desktop capabilities with no WinUI equivalent, so the interop rules — window handle, clipboard threading — are the whole lesson.

Copying a part list out of the grid and pasting it into an email is a daily task, and the clipboard has to be touched on the UI thread.

Schema this mirrors: `dunnage_label_data, view_receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Only one app can hold the clipboard at a time; SetContent fails while another app has it locked, so always catch.
- Reading requires Content.GetTextAsync and a format check — an empty clipboard throws rather than returning null.
- Clipboard content is visible to every process: never put secrets there.

## Try this

- Run `WinUI3LearningLab.App.exe --demo clipboard`. The page shows `ClipboardDemo.xaml` and `ClipboardDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Windows.ApplicationModel.DataTransfer.Clipboard](https://learn.microsoft.com/uwp/api/windows.applicationmodel.datatransfer.clipboard)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/SystemServices.cs` or the demo source, then regenerate.
