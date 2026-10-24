---
description: "PasswordBox (Microsoft.UI.Xaml.Controls.PasswordBox) in Text; a text field that masks input and exposes the value only through Password (a string you cannot… MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/PasswordBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/PasswordBoxDemo.xaml.cs"
---

# PasswordBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.PasswordBox` |
| Page | `Text/PasswordBoxDemo` (`WinUI3LearningLab.Demos.Text.PasswordBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo password-box` |

## What it teaches

A text field that masks input and exposes the value only through Password (a string you cannot bind by default).

## How the demo is built

The page is a `UserControl` at `Text/PasswordBoxDemo.xaml`, paired with `PasswordBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 6 element(s).

Named elements the code-behind touches: `Password`, `StatusText`.

Wired in XAML: `OnPasswordChanged()`, `OnSignInClicked()`.

**Code-behind.** Handlers `OnPasswordChanged()`, `OnSignInClicked()`. 26 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

The supervisor override prompt on the WIP form takes a PIN, and the reveal button exists so the operator can check a mistyped digit before it counts against the audit trail in `auth_user_management_audit`.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Password is deliberately not a dependency property, so it cannot be data bound — read it in the submit handler.
- PasswordRevealMode controls the reveal button; Peek shows it only while pressed.
- PasswordChanged fires per keystroke; do not log or store the value from there.

## Traps recorded in the source

- Deliberately no echo of the value: a password must not land in the UI or a log.

## Try this

- Run `WinUI3LearningLab.App.exe --demo password-box`. The page shows `PasswordBoxDemo.xaml` and `PasswordBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.PasswordBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.passwordbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
