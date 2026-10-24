---
description: "JumpList (Microsoft.UI.StartScreen.JumpList) in Notifications & jump lists; the right-click menu of the taskbar button, with recent files and… MTM: MTM_Waitlist › Waitlist › notification of assignment."
applyTo: "src/WinUI3LearningLab.App/Demos/Shell/JumpListDemo.xaml,src/WinUI3LearningLab.App/Demos/Shell/JumpListDemo.xaml.cs"
---

# JumpList

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Notifications & jump lists |
| Level | Advanced |
| API | `Microsoft.UI.StartScreen.JumpList` |
| Page | `Shell/JumpListDemo` (`WinUI3LearningLab.Demos.Shell.JumpListDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo jump-list` |

## What it teaches

The right-click menu of the taskbar button, with recent files and custom tasks.

## Availability

This lab is unpackaged. The JumpList API needs an app identity, so the demo explains the requirement and shows the exact call an MSIX app would make.

## Package identity

This API needs MSIX package identity. The lab is unpackaged (`WindowsPackageType=None`), so the page probes the real call and reports the exception instead of faking a result. See `.github/instructions/packaging-and-identity.instructions.md`.

## How the demo is built

The page is a `UserControl` at `Shell/JumpListDemo.xaml`, paired with `JumpListDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 22 element(s).

Named elements the code-behind touches: `FactsText`, `ItemList`, `ResultText`, `LastActionText`.

Wired in XAML: `OnLoadClicked()`, `OnBuildItemsClicked()`, `OnSaveClicked()`.

**Code-behind.** Handlers `OnLoadClicked()`, `OnBuildItemsClicked()`, `OnSaveClicked()`. 113 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › notification of assignment**

An operator needs to know a request was assigned to their lugger while the board is behind another window. A toast is the mechanism; the badge is the persistent count of what is still open. Both depend on the app having an identity, which this lab deliberately does not have.

A jump list would put the three most recent work orders on the taskbar button. Building the items works without identity; attaching them to the shell does not.

Schema this mirrors: `waitlist_requests_queue, waitlist_requests_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- JumpList requires the app to have an identity and a registered AUMID; unpackaged invocation fails.
- Items are JumpListItem objects with Arguments, so activation arrives as command-line arguments you must parse.
- Load the list at start-up and Save it only when it changed — writing on every launch is wasteful.

## Try this

- Run `WinUI3LearningLab.App.exe --demo jump-list`. The page shows `JumpListDemo.xaml` and `JumpListDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.StartScreen.JumpList](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.startscreen.jumplist)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Shell.cs` or the demo source, then regenerate.
