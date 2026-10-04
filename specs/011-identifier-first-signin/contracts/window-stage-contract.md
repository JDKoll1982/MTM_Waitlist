# Contract: One Window, Five Stages

## 1. The window

One window exists for the whole life of the application. It opens centred on the screen it starts on, hosts the
launch stages, and becomes the shell.

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
