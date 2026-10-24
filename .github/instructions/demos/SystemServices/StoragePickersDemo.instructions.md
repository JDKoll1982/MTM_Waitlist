---
description: "Storage pickers (Microsoft.Windows.Storage.Pickers.FileOpenPicker) in Clipboard & pickers; the modern file and folder pickers that take a WindowId, so… MTM: MTM_Receiving_Application › Receiving › copy, export and file selection."
applyTo: "src/WinUI3LearningLab.App/Demos/SystemServices/StoragePickersDemo.xaml,src/WinUI3LearningLab.App/Demos/SystemServices/StoragePickersDemo.xaml.cs"
---

# Storage pickers

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Clipboard & pickers |
| Level | Intermediate |
| API | `Microsoft.Windows.Storage.Pickers.FileOpenPicker` |
| Page | `SystemServices/StoragePickersDemo` (`WinUI3LearningLab.Demos.SystemServices.StoragePickersDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo storage-pickers` |

## What it teaches

The modern file and folder pickers that take a WindowId, so they work in unpackaged desktop apps.

## How the demo is built

The page is a `UserControl` at `SystemServices/StoragePickersDemo.xaml`, paired with `StoragePickersDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `WindowText`, `ResultText`, `LastActionText`.

Wired in XAML: `OnPickSingleFileClicked()`, `OnPickMultipleFilesClicked()`, `OnPickFolderClicked()`.

**Code-behind.** Handlers `OnPickSingleFileClicked()`, `OnPickMultipleFilesClicked()`, `OnPickFolderClicked()`. 141 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › copy, export and file selection**

Clerks paste part lists into emails and export a shift to a file for the planner, then pick the label template to print. These are desktop capabilities with no WinUI equivalent, so the interop rules — window handle, clipboard threading — are the whole lesson.

Picking the label template PDF and the shift export folder: the `Microsoft.Windows.Storage.Pickers` types take the window in the constructor, so no interop step and no `0x80070005`.

Schema this mirrors: `dunnage_label_data, view_receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- This is not Windows.Storage.Pickers.FileOpenPicker: the older type needs InitializeWithWindow(ref hwnd) to work in a desktop app.
- PickAsync returns a PickFileResult with a Path, not a StorageFile, in the modern API.
- Pickers are modal to the window you pass — pass the correct WindowId or the dialog appears behind your window.

## Try this

- Run `WinUI3LearningLab.App.exe --demo storage-pickers`. The page shows `StoragePickersDemo.xaml` and `StoragePickersDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.Storage.Pickers.FileOpenPicker](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.fileopenpicker)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/SystemServices.cs` or the demo source, then regenerate.
