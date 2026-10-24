---
description: "InfoBadge (Microsoft.UI.Xaml.Controls.InfoBadge) in Status & info; a small attention dot, number or icon that overlays a tile, tab or navigation… MTM: MTM_Waitlist › Waitlist › request state and audit."
applyTo: "src/WinUI3LearningLab.App/Demos/StatusAndInfo/InfoBadgeDemo.xaml,src/WinUI3LearningLab.App/Demos/StatusAndInfo/InfoBadgeDemo.xaml.cs"
---

# InfoBadge

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Status & info |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.InfoBadge` |
| Page | `StatusAndInfo/InfoBadgeDemo` (`WinUI3LearningLab.Demos.StatusAndInfo.InfoBadgeDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo info-badge` |

## What it teaches

A small attention dot, number or icon that overlays a tile, tab or navigation item.

## How the demo is built

The page is a `UserControl` at `StatusAndInfo/InfoBadgeDemo.xaml`, paired with `InfoBadgeDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 51 element(s).

Named elements the code-behind touches: `InboxBadge`, `InboxTileBadge`, `TileBadge`, `UnreadSlider`, `DotToggle`, `BadgeStatus`.

Wired in XAML: `OnUnreadChanged()`, `OnDotToggled()`.

**Code-behind.** Handlers `OnUnreadChanged()`, `OnDotToggled()`. 47 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › request state and audit**

A request moves through states and every transition is audited. The screen has to say what state a request is in, whether something is still running, and what failed — without a modal box per event.

Schema this mirrors: `waitlist_requests_audit, waitlist_defect_types, log_error`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- InfoBadge draws nothing on its own — it is meant to be placed as a child in a Grid over the element it annotates.
- Value drives the number badge; leave it at -1 and set Icon to get a dot or glyph instead.
- Set AutomationProperties.Name, or the badge is invisible to screen readers.

## Try this

- Run `WinUI3LearningLab.App.exe --demo info-badge`. The page shows `InfoBadgeDemo.xaml` and `InfoBadgeDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.InfoBadge](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.infobadge)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/StatusAndInfo.cs` or the demo source, then regenerate.
