# Startup — Living Spec

> [DRAFT] Surface-first draft from existing code — every requirement is observed from the code surface unless tagged otherwise. Review before trusting.

## Purpose

This capability decides whether a workstation is allowed to run the application at all, and who is sitting in front of it. It is the only thing standing between a double-click and the shell, so its failures are the ones operators meet most often and understand least: a machine that is not registered, an account still on its temporary password, a database that cannot be reached, a local settings file that has gone bad. Getting it wrong is expensive in a specific way — a startup that proceeds on a half-verified identity attributes a shop-floor action to the wrong person, and one that blocks for an unrepairable-sounding reason sends an operator to a developer for something they could have fixed themselves.

## Requirements

### Launch runs as an ordered sequence of named checks

Startup SHALL proceed through a fixed sequence of named checks and report each step's label *before* performing it, so an operator watching the splash knows which check is running rather than only that something is happening. Progress reporting SHALL be advisory: a caller that supplies no progress sink gets the same checks in the same order.

#### Scenario: an operator watches startup
- **WHEN** the application launches
- **THEN** each check announces itself in order, from loading settings through to loading data
- **AND** the label names the check rather than being a generic "loading" message

#### Scenario: only the database-dependent part is retried
- **WHEN** startup is re-entered for a database-only retry
- **THEN** the already-completed settings check is not announced again
- **AND** the checks that depend on the database are re-run

### The machine and the operator are identified from authoritative sources

Startup SHALL derive the workstation's identity from its name and a stable hardware address, and the operator's identity and role from the store. A role SHALL NOT be taken from the workstation's own claim, except for the configured developer accounts, which are a deliberate local override.

#### Scenario: an account's role is resolved
- **WHEN** the session store returns the operator's row
- **THEN** the role and the display identity come from that row
- **AND** the identity is exposed to the rest of the application so actions can be attributed to a person

### Entry to the shell is gated on a registered workstation, and an unverifiable workstation is admitted

Once a person is signed in, entry to the shell SHALL be gated on whether the workstation is registered, and
each outcome SHALL carry its own instruction rather than one generic refusal: proceed when registered, register
when the machine is unknown, confirm the display name when the machine has been renamed, and retry when the
store cannot be reached. A workstation whose hardware identity cannot be established SHALL be admitted, because
a check cannot fail closed on a fact it was unable to read.

`[inferred]` The gate is reached from the **sign-in surface**, not from the launch sequence: the launch sequence
consumes the registration verdict already carried on the session snapshot. The gate service itself was read in
full; its caller was located by structure rather than read end to end.

#### Scenario: the workstation is registered
- **WHEN** the gate reports the workstation as registered
- **THEN** the person proceeds into the application
- **AND** no registration step is offered

#### Scenario: the workstation is unknown
- **WHEN** the gate finds no matching workstation
- **THEN** the operator is told to register this computer
- **AND** the registration fields are prepared

#### Scenario: the workstation has been renamed
- **WHEN** the gate matches the hardware identity under a different name
- **THEN** the operator is told the name changed and is asked to confirm the display name
- **AND** this is kept distinct from an unknown workstation, because the machine is already known

#### Scenario: the store cannot be reached
- **WHEN** the gate cannot complete its lookup
- **THEN** the operator is told to check the connection and try again
- **AND** the attempt is retried rather than treated as a refusal

#### Scenario: the hardware identity cannot be read
- **WHEN** no usable hardware address is available
- **THEN** the gate reports that it could not be performed
- **AND** the person is admitted, because an unreadable fact is not a failed check

### A session is valid only until an expiry judged on the server's clock

Session validity SHALL be judged against the store's clock rather than the workstation's, so a mis-set workstation clock can neither extend access nor deny it. When both a locally held token and a store-held session exist, the locally held token is preferred.

#### Scenario: the workstation clock is wrong
- **WHEN** the workstation's clock disagrees with the store's
- **THEN** validity is decided by the store's clock
- **AND** the session is not treated as valid merely because the workstation believes so

#### Scenario: no local token exists
- **WHEN** the workstation holds no token but the store holds a live session
- **THEN** that session is used
- **AND** the source of the session is recorded so the decision can be traced

### A damaged local setting is repaired rather than reported

When startup cannot read local settings, it SHALL attempt a targeted repair of the single damaged entry and retry the read before blocking, so one bad value does not cost the operator every local setting.

#### Scenario: one entry is unreadable
- **WHEN** the settings read fails
- **THEN** that entry alone is reset and the read is retried
- **AND** startup continues if the retry succeeds

#### Scenario: the repair does not help
- **WHEN** the retry also fails
- **THEN** startup blocks
- **AND** the message offers both remedies — try again, or reset all local settings

### Every block states a diagnosis, and startup never proceeds half-configured

Each blocking condition SHALL produce a message specific to its cause, and a blocked startup SHALL NOT continue into the shell. A failure to answer one optional question SHALL NOT be promoted into a block.

#### Scenario: the centralized logging destination is unset
- **WHEN** no destination is configured
- **THEN** startup blocks
- **AND** a developer is told to configure one, while everyone else is told to contact a developer

#### Scenario: startup configuration is malformed
- **WHEN** the startup database connection details are invalid
- **THEN** startup blocks with the configuration named as the cause
- **AND** the operator is pointed at a developer rather than at a retry they cannot succeed at

#### Scenario: an optional lookup cannot be answered
- **WHEN** the store cannot say whether an account still holds its temporary password
- **THEN** startup continues to the ordinary sign-in surface
- **AND** the failure is recorded rather than shown as a block

### Startup reaches exactly one of three destinations

The shell SHALL be entered directly only when identity, session and device registration all hold. Otherwise startup SHALL stop at the sign-in surface, carrying a hint that states what is missing rather than leaving the operator to guess.

#### Scenario: everything holds
- **WHEN** the operator is known, the session is valid, and the device is registered
- **THEN** startup goes straight to the waitlist, without a sign-in surface
- **AND** no hint is carried

#### Scenario: the device is not registered
- **WHEN** device registration is authoritative and the device is not registered
- **THEN** the sign-in surface offers the route to request access
- **AND** the hint says the computer is not registered

#### Scenario: the account is on its temporary password
- **WHEN** the account still holds a temporary credential
- **THEN** the sign-in surface asks for that credential like any other sign-in, and the hint says it is temporary and must be replaced
- **AND** the set-a-new-password panel opens only once the credential has been accepted, because knowing a change is due is not proof of identity, and an attempt limit cannot apply to a credential nobody was ever asked for

### The splash offers only the remedies that can work, and retry repeats the phase that failed

Retry SHALL repeat only the phase that failed. Resetting local settings SHALL be offered only when the fault is local, because it cannot repair an unreachable store. The prompt for a missing logging destination SHALL be available to developers, and cancelling it SHALL leave the application running with its actions offered.

#### Scenario: the store was unreachable
- **WHEN** startup blocked because the store could not be reached
- **THEN** retry reports that it is rechecking the connection
- **AND** resetting local settings is not offered

#### Scenario: local settings were damaged
- **WHEN** startup blocked on a local settings fault
- **THEN** retry reports that it is repairing a setting
- **AND** resetting local settings is offered

#### Scenario: a developer cancels the destination prompt
- **WHEN** a developer dismisses the prompt for the logging destination
- **THEN** the application stays open with its actions offered
- **AND** the state does not claim the store was unreachable

### Window and shell state follow the startup phase

A reduced splash window SHALL be shown with navigation hidden while startup runs, and the main shell SHALL be revealed maximized with navigation visible on success. Handing off from splash to sign-in SHALL NOT pass through the main mode.

#### Scenario: startup is running
- **WHEN** the splash is active
- **THEN** navigation is hidden
- **AND** the window is at the configured splash size

#### Scenario: startup succeeds
- **WHEN** startup routes to the shell
- **THEN** the window is maximized and navigation becomes visible
- **AND** any configured transition delay is honoured before the switch

#### Scenario: startup routes to sign-in
- **WHEN** startup routes to the sign-in surface
- **THEN** the sign-in window replaces the splash
- **AND** the main mode is not entered

#### Scenario: the window cannot be maximized
- **WHEN** the window cannot be put into a maximized state
- **THEN** the configured main size is used instead
- **AND** the transition still completes

### The startup sequence runs once per launch

A second entry into the splash surface SHALL NOT start a second sequence, so a re-entrant navigation cannot run two pipelines against one workstation.

#### Scenario: the splash is entered twice
- **WHEN** the splash surface is navigated to again while a sequence is running
- **THEN** the second entry returns without starting anything
- **AND** the first sequence's outcome is the one that stands

### The pictures a screen will need are copied onto the computer while it starts

Startup SHALL refresh the local copy of the pictures the application draws, on the way past the session check and
before the first screen is shown, and SHALL report a line naming that refresh. The refresh SHALL be best effort: a
picture source that cannot be reached SHALL be recorded and SHALL NOT prevent the application from opening or the
remaining checks from running.

`[observed]` Added 2026-09-22 by the picture-storage-and-cache feature (`specs/007-picture-storage-and-cache`).
The step runs between the session check and the loading step, and the capability it serves is described there: the
first screen a person reaches reads its pictures from local disk instead of reaching across the network for each
one.

#### Scenario: the pictures are already on the computer
- **WHEN** the application starts and every source picture already has a copy that has not changed
- **THEN** nothing is copied and nothing is removed
- **AND** the line naming the refresh is reported anyway, so the splash does not appear to skip a step

#### Scenario: a picture source cannot be reached
- **WHEN** a source folder is unavailable while the pictures are being refreshed
- **THEN** the failure is recorded and the existing copies are left in place
- **AND** the application still opens

## Uncovered

Read in full: `StartupCoordinator.cs`, `StartupShellStateService.cs`, `StartupWindowService.cs`, `ComputerGateService.cs`, `StartupRecoveryService.cs`, `SplashViewModel.cs`.

Read in part: `LoginViewModel.cs` — lines 1–75, the device-gate path (424–478), and the sign-in flow located by its members; its credential handling, remembered-credential handling and registration submission (76–423, 479–515) were not read. `StartupSessionRepository.cs` — lines 1–90, which establish by name and purpose the stored procedures the identity, session and password reads are built on; the bodies of those reads (91–392) were not read.

Two things this draft does **not** verify, and a reviewer should weigh both:

- **The presentation side.** The window-and-shell-state requirements state what the view models and
  `StartupShellStateService` do; they do not verify that the XAML honours them. Whether the splash actually
  hides navigation when told, and whether the login window opens on the password-change state, is asserted from
  the view-model side only. Every view and code-behind under `Module_Startup/Views/` is unread.
- **The registration surface.** `StartupRegistrationService.cs`, `ComputerRegistryService.cs` and
  `ModuleDependencyInjectionExtensions.cs` are unread, so what "register this computer" actually does — and
  whether the gate's registration path can succeed — is outside this draft.

The `startup-diagnostics` sibling owns `StartupLogService.cs` and `StartupLogForwarder.cs`; they are therefore
not enumerated here, and nothing above depends on them.

The picture refresh the startup sequence runs is the picture cache's own behaviour, and that capability is not
registered as a living spec. This capability owns only the sequence and the best-effort rule: that the refresh is
attempted in the right place, reports a line, and cannot block the start. What the refresh copies and removes is
tested where it lives, not here.
