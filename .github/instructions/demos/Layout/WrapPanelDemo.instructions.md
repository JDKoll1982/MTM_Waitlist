---
description: "WrapPanel (Community Toolkit) (CommunityToolkit.WinUI.UI.Controls.WrapPanel) in Layout; wraps children onto new lines based on available width — the… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/WrapPanelDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/WrapPanelDemo.xaml.cs"
---

# WrapPanel (Community Toolkit)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `CommunityToolkit.WinUI.UI.Controls.WrapPanel` |
| Page | `Layout/WrapPanelDemo` (`WinUI3LearningLab.Demos.Layout.WrapPanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo wrap-panel` |

## What it teaches

Wraps children onto new lines based on available width — the thing StackPanel and Grid cannot do declaratively.

## How the demo is built

The page is a `UserControl` at `Layout/WrapPanelDemo.xaml`, paired with `WrapPanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 26 element(s).

Named elements the code-behind touches: `ItemWidthSlider`, `WrapItems`, `WrapStatus`.

Wired in XAML: `OnItemWidthChanged()`.

**Code-behind.** Handlers `OnItemWidthChanged()`. 39 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Comes from CommunityToolkit.WinUI.UI.Controls, so the page needs xmlns:toolkit="using:CommunityToolkit.WinUI.UI.Controls".
- ItemWidth is fixed-width mode; leave it unset to size each child naturally.
- It is not a virtualising panel — large collections belong in ItemsRepeater.

## Try this

- Run `WinUI3LearningLab.App.exe --demo wrap-panel`. The page shows `WrapPanelDemo.xaml` and `WrapPanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.WrapPanel](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
