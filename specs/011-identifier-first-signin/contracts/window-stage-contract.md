# Contract: One Window, Five Stages

## 1. The window

One window exists for the whole life of the application. It opens centred on the screen it starts on, hosts the
launch stages, and becomes the shell.

**Scope — what this rule governs, and what it does not.** This is the application's *stage host*: the window the
launch opens, sizes, re-centres and maximises, and the one window a person drives the whole workflow from. It is not
a cap on the number of `Window` objects the process may create. One diagnostic surface is deliberately a window of
its own: the developer control inspector (`Ctrl+Alt+Right-Click` on a tracked control) opens
`ControlInspectorDetailWindow`. It is not a stage — nothing selects it, it hosts none of the five stages, it is not
in the shell's back stack, and it is closed rather than navigated away from. It is a window because a hovered control
is often inside a `ContentDialog`, and a `ContentDialog` owns its `XamlRoot` and blocks it, so a page navigated to
behind the dialog can never be seen. A window has no `XamlRoot` to be blocked by, so the details appear over the
dialog that raised them.

## 2. Stage selection

`App.ShowLaunchSurface(LaunchOutcome)` is the only place a stage is chosen.

| `LaunchOutcome` | Stage | Navigation panel |
|---|---|---|
| `MainScreens` | `Shell` | Shown |
| `SignIn` | `SignIn` | Hidden |
| `MachineSetup` | `MachineSetup` | Hidden |
| `Blocked` | `Blocked` | Hidden |
| `Ended` | process ends | not applicable |

The splash stage is shown while the launch runs, before any outcome. The shell is reachable only from
`MainScreens` (FR-022, FR-043).

## 3. Size and position

- The window resizes between the big phases.
- It re-centres after each resize the launch makes. A resize the person performs is not re-centred.
- It maximises on the move to `Shell` (FR-019).

## 4. Assumptions prohibited

Nothing in the application may assume a signed-in person before the hand-over (FR-023). The navigation panel is
absent during every stage except `Shell`, so any screen that assumed a signed-in person would fail visibly.
