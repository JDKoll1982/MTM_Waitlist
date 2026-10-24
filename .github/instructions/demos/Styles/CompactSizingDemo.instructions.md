---
description: "Compact sizing (Concept only) in Styles & brushes; fluent has a compact density for data-dense desktop layouts, applied by overriding the… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/CompactSizingDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/CompactSizingDemo.xaml.cs"
---

# Compact sizing

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Concept only` |
| Page | `Styles/CompactSizingDemo` (`WinUI3LearningLab.Demos.Styles.CompactSizingDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo compact-sizing` |

## What it teaches

Fluent has a compact density for data-dense desktop layouts, applied by overriding the control size resources.

## Availability

Interactive, but scoped: this page swaps a small set of size resources at runtime so you can see the effect. A production app would merge the full compact resource dictionary.

## How the demo is built

The page is a `UserControl` at `Styles/CompactSizingDemo.xaml`, paired with `CompactSizingDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 7 element(s).

Named elements the code-behind touches: `DensitySwitch`, `FormHost`, `MeasureText`.

Wired in XAML: `OnDensityToggled()`.

**Code-behind.** Handlers `OnLoaded()`, `OnDensityToggled()`. 106 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- There is no single CompactMode switch in WinUI 3: compact sizing is a resource dictionary of control height and padding overrides.
- Override the resources in Application.Resources so every control picks them up, then re-evaluate layouts that hard-code heights.
- Mixing compact and standard controls in one view reads as misalignment — pick one density per surface.

## Try this

- Run `WinUI3LearningLab.App.exe --demo compact-sizing`. The page shows `CompactSizingDemo.xaml` and `CompactSizingDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Concept only](https://learn.microsoft.com/windows/apps/design/style/spacing)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
