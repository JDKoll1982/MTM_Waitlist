---
description: "NavigationView (Microsoft.UI.Xaml.Controls.NavigationView) in Navigation; the standard app shell: a collapsible pane of destinations, a header, a… MTM: MTM_Waitlist › Settings › sections and modules."
applyTo: "src/WinUI3LearningLab.App/Demos/Navigation/NavigationViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Navigation/NavigationViewDemo.xaml.cs"
---

# NavigationView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Navigation |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.NavigationView` |
| Page | `Navigation/NavigationViewDemo` (`WinUI3LearningLab.Demos.Navigation.NavigationViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo navigation-view` |

## What it teaches

The standard app shell: a collapsible pane of destinations, a header, a footer and settings.

## How the demo is built

The page is a `UserControl` at `Navigation/NavigationViewDemo.xaml`, paired with `NavigationViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 26 element(s).

Named elements the code-behind touches: `Nav`, `DestinationTitle`, `DestinationBody`, `InvocationText`, `ModeText`.

Wired in XAML: `OnItemInvoked()`.

**Code-behind.** Handlers `OnItemInvoked()`. 70 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings › sections and modules**

The settings area is a set of sections with a scope — universal, role, personal — and the user moves between them constantly. Navigation state has to survive that, and the current section has to be obvious from across the shop floor.

The shell of every MTM app: a left pane of modules (Receiving, Dunnage, Volvo, Settings) with the role from `sys_roles` deciding what is listed.

Schema this mirrors: `config_settings_values, settings_universal, settings_personal`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- NavigationView does not navigate — it raises ItemInvoked/SelectionChanged and you drive a Frame.
- PaneDisplayMode plus the two threshold widths decide when the pane becomes compact; they are the properties that make it adaptive.
- NavigationViewItemHeader and NavigationViewItemSeparator are added to MenuItems like items, but are not selectable.
- Settings is a built-in item; set IsSettingsVisible and handle it separately from the other items.

## Traps recorded in the source

- Deliberately not a ternary: mixing a named tuple with a literal tuple widens the result to (string, string) and the element names are lost, which is a confusing CS1061.

## Try this

- Narrow the window and watch PaneDisplayMode switch to the compact hamburger.

## Documentation

- [Microsoft.UI.Xaml.Controls.NavigationView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.navigationview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Navigation.cs` or the demo source, then regenerate.
