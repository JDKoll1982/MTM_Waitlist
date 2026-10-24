---
description: "WebView2 (Microsoft.UI.Xaml.Controls.WebView2) in Media; embeds the Edge Chromium engine for HTML content, with script interop in… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/WebView2Demo.xaml,src/WinUI3LearningLab.App/Demos/Media/WebView2Demo.xaml.cs"
---

# WebView2

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.WebView2` |
| Page | `Media/WebView2Demo` (`WinUI3LearningLab.Demos.Media.WebView2Demo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo web-view2` |

## What it teaches

Embeds the Edge Chromium engine for HTML content, with script interop in both directions.

## How the demo is built

The page is a `UserControl` at `Media/WebView2Demo.xaml`, paired with `WebView2Demo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `ErrorBar`, `Browser`, `Placeholder`, `ScriptButton`, `ReloadButton`, `StatusText`, `ScriptText`.

Wired in XAML: `OnRunScriptClicked()`, `OnReloadClicked()`.

**Code-behind.** Handlers `OnLoaded()`, `OnRunScriptClicked()`, `OnReloadClicked()`. 116 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The WebView2 Runtime must be installed; EnsureCoreWebView2Async throws if it is missing.
- CoreWebView2 is null until initialization completes — do not touch it in the constructor.
- Use ExecuteScriptAsync and WebMessageReceived for interop; there is no synchronous script call.
- For local HTML use SetVirtualHostNameToFolderMapping with a ms-appx-web or https URL — file:// navigation is restricted.

## Try this

- Run `WinUI3LearningLab.App.exe --demo web-view2`. The page shows `WebView2Demo.xaml` and `WebView2Demo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.WebView2](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.webview2)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
