---
description: "Drag and drop (Windows.ApplicationModel.DataTransfer.DataPackage) in Platform services; drag data between your controls and other apps using… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/DragDropDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/DragDropDemo.xaml.cs"
---

# Drag and drop

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Intermediate |
| API | `Windows.ApplicationModel.DataTransfer.DataPackage` |
| Page | `Platform/DragDropDemo` (`WinUI3LearningLab.Demos.Platform.DragDropDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo drag-drop` |

## What it teaches

Drag data between your controls and other apps using DataPackage, DragStarting and Drop.

## How the demo is built

The page is a `UserControl` at `Platform/DragDropDemo.xaml`, paired with `DragDropDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `SourceList`, `DropZone`, `DropZoneHint`, `DropZoneContent`, `UnassignedList`, `AssignedList`, `LastActionText`.

Wired in XAML: `OnSourceDragStarting()`, `OnZoneDragOver()`, `OnZoneDragLeave()`, `OnZoneDrop()`, `OnUnassignedDragStarting()`, `OnAssignedDragOver()`, `OnAssignedDrop()`.

**Code-behind.** Handlers `OnSourceDragStarting()`, `OnZoneDragOver()`, `OnZoneDragLeave()`, `OnZoneDrop()`, `OnUnassignedDragStarting()`, `OnAssignedDragOver()`, `OnAssignedDrop()`. 159 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Set CanDrag and handle DragStarting, then Drop and DragOver (set AcceptedOperation) — DragOver is what makes the cursor show 'copy'.
- With AllowDrop, you must check DataView.Contains(format) before reading; reading an absent format throws.
- Drag and drop across elevation levels is blocked by UIPI, which surprises people testing as administrator.

## Try this

- Run `WinUI3LearningLab.App.exe --demo drag-drop`. The page shows `DragDropDemo.xaml` and `DragDropDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Windows.ApplicationModel.DataTransfer.DataPackage](https://learn.microsoft.com/windows/apps/design/input/drag-and-drop)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
