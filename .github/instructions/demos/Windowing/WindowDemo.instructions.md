---
description: "Window (Microsoft.UI.Xaml.Window) in Windowing; the WinUI 3 top-level window: content, title, size, backdrop and lifetime events. MTM: MTM_Waitlist › Workstation identity and main window."
applyTo: "src/WinUI3LearningLab.App/Demos/Windowing/WindowDemo.xaml,src/WinUI3LearningLab.App/Demos/Windowing/WindowDemo.xaml.cs"
---

# Window

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Windowing |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Window` |
| Page | `Windowing/WindowDemo` (`WinUI3LearningLab.Demos.Windowing.WindowDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo window` |

## What it teaches

The WinUI 3 top-level window: content, title, size, backdrop and lifetime events.

## How the demo is built

The page is a `UserControl` at `Windowing/WindowDemo.xaml`, paired with `WindowDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `FactsText`, `TitleBox`, `WindowsText`, `LastActionText`.

Wired in XAML: `OnApplyTitleClicked()`, `OnCreateWindowClicked()`, `OnCloseCreatedClicked()`.

**Code-behind.** Handlers `OnApplyTitleClicked()`, `OnCreateWindowClicked()`, `OnCloseCreatedClicked()`. 169 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Workstation identity and main window**

The app knows which workstation it is on from the computer registry table and keeps one window per user session. Title bar, size and restore behaviour are therefore part of the contract, not decoration.

Schema this mirrors: `core_computers_registry, user_active_sessions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Window is not a DependencyObject and has no DataContext — put your view model on the root FrameworkElement instead.
- Closing is cancellable, Closed is not; both fire on the UI thread.
- A Window owns its XamlRoot, which is what dialogs and flyouts need. Without activating the window, ShowAsync fails.
- Creating a second Window is normal in WinUI 3 — it is not a singleton like the UWP CoreWindow.

## Traps recorded in the source

- Leaving the page must never throw because a window was already gone.

## Try this

- Create a second Window from the demo and give it its own content.

## Documentation

- [Microsoft.UI.Xaml.Window](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.window)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Windowing.cs` or the demo source, then regenerate.
