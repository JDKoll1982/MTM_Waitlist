---
description: "Gravatar (DevWinUI.Gravatar) in Controls; an avatar rendered deterministically from an identifier — an identicon generated offline, so no image hosting is… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/GravatarDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/GravatarDemo.xaml.cs"
---

# Gravatar

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.Gravatar` |
| Page | `DevWinUI/GravatarDemo` (`WinUI3LearningLab.Demos.DevWinUI.GravatarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo gravatar` |

## What it teaches

An avatar rendered deterministically from an identifier — an identicon generated offline, so no image hosting is needed.

## How the demo is built

The page is a `UserControl` at `DevWinUI/GravatarDemo.xaml`, paired with `GravatarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

Named elements the code-behind touches: `OperatorBox`, `Avatar`, `OfflineNote`, `Initials`, `DirectoryToggle`, `HashText`.

Wired in XAML: `OnOperatorChanged()`, `OnOfflineToggled()`.

**Code-behind.** Handlers `OnOperatorChanged()`, `OnOfflineToggled()`. 68 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In DevWinUI 10.8.0 this renders a generated identicon from Gravatar.Id via GithubGravatarGenerator; it does not fetch gravatar.com. That is good for privacy and offline use, and different from what the name suggests.
- Because the image is derived from the id, the same id always yields the same avatar — do not use a mutable value such as a timestamp as the id.
- Always provide an initials or glyph fallback; never use an avatar as the only identifier of a person.

## Try this

- Run `WinUI3LearningLab.App.exe --demo gravatar`. The page shows `GravatarDemo.xaml` and `GravatarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Gravatar](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
