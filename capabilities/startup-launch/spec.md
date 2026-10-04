# startup-launch

**What this capability is.** Opening the application: the launch surface that says what the application is doing
while it starts, the machine setup a computer needs before it may be used, the sign-in that identifies both the
person and the machine, and the stop that names its cause and offers only remedies that could remove it.

**Where it lives.** `MTM_Waitlist.Startup/**` for the launch itself, its steps, its services and its view models,
and `Module_Startup/Views/**` for the surfaces it shows. The logging module (`MTM_Waitlist.Logging/**`) is not
part of this capability: it is usable by the shell, Settings and the module libraries that never reference the
launch, and it has its living spec to come.

**Re-adopted 2026-09-27 by feature 010, task T163**, replacing the two capabilities `startup` and
`startup-diagnostics`, whose matched code the same feature deleted. The old split existed because the file-based
log path was a concern of its own; that path is gone, and the rebuilt surface is one capability.

---

## Requirement: The launch names every piece of work before it performs it

The launch surface shows one line per operation, in the order the operations run, and the line appears before the
work it describes. The count the surface shows is derived from the sequence rather than stored, so a change to the
sequence cannot leave a stale total.

#### Scenario: A stalled launch is attributable to a named line
- **WHEN** the launch begins a piece of work
- **THEN** the surface names that work before it begins, so the last line the person is reading is the work that
  is stalled

#### Scenario: Every individual operation holds its own line
- **WHEN** the launch performs an individual operation
- **THEN** that operation appears as its own line, rather than a group of operations standing behind one

---

## Requirement: Every wait on the launch path has a stated maximum

No wait on the launch path is unbounded. Each step declares its maximum, the ceiling for any single wait is thirty
seconds, and the two best-effort steps are bounded more tightly because neither may hold the launch.

#### Scenario: A step that runs past its maximum is reported rather than waited on
- **WHEN** a step passes the maximum it declares
- **THEN** the launch records the step as failed, naming the bound it passed, and does not keep waiting on it

#### Scenario: A best-effort step cannot stop the launch
- **WHEN** the picture refresh or the external-system priming fails, throws or overruns
- **THEN** the failure is a line on the feed and the launch carries on

---

## Requirement: The launch ends at exactly one of five places

A launch ends at the main screens, at sign-in, at machine setup, at a stated stop, or with the process ended and
the reason stated first. There is no sixth outcome and no way to reach the shell that the launch did not choose.

#### Scenario: A stopped launch states a cause specific to it
- **WHEN** a step fails
- **THEN** the launch ends at a stated stop carrying that step's own diagnosis, and the shell is not shown

#### Scenario: The shell is reached only through the launch
- **WHEN** the launch ends at the main screens
- **THEN** the host is handed that outcome through the one event it listens on, and no other route to the shell
  exists

---

## Requirement: A machine with no configuration cannot reach the main screens

The pipeline, not a screen, is what refuses to continue while this machine is unconfigured. A configuration that
was removed, revoked or cannot be read is treated as unconfigured at the next check.

#### Scenario: Setup appears before any operator signs in
- **WHEN** the launch reads this machine's configuration and finds it incomplete
- **THEN** the launch ends at machine setup, before anybody signs in

#### Scenario: Every abort route out of setup ends the process with its reason stated
- **WHEN** the person closes, cancels, presses Escape or Alt+F4, or declines the setup sign-in
- **THEN** the reason is stated first and the process ends, so the gate is never reported as a crash

---

## Requirement: Signing in identifies both the person and the machine

Sign-in decides whether the store recognises the person, whether their session still stands and whether this
machine is one the store knows. Session validity is judged on the store's clock. An account on a temporary
credential is allowed five attempts and is then required to set a new password.

#### Scenario: A refused temporary credential stays refused across a restart
- **WHEN** five attempts against a temporary credential have failed
- **THEN** a sixth attempt is refused even with the correct value, and a restarted application refuses it too

#### Scenario: A saved new PIN is followed by the application starting again on it
- **WHEN** a person on a temporary credential has chosen a new PIN and the store has accepted it
- **THEN** the launch ends with that reason stated, a replacement application is started, and the person signs in
  with the PIN they chose

#### Scenario: A replacement that could not be started leaves the person where they are
- **WHEN** the application cannot be started again after a new PIN has been saved
- **THEN** the running application carries on to the main screens rather than ending, and the new PIN is still the
  one in the store

#### Scenario: An unreadable key file falls back to the ordinary form
- **WHEN** what is needed to honour a remembered sign-in cannot be read
- **THEN** the ordinary sign-in form is offered, the fault is recorded, and nothing is kept on the machine in its
  place

#### Scenario: A fact that could not be read is not a failed check
- **WHEN** this machine's hardware identity cannot be read
- **THEN** the person is admitted, because the machine is identified by name alone

---

## Requirement: A stop offers only remedies that could remove its cause

Retry repeats the failed work and what follows it, and nothing before it. Restore Defaults names exactly what it
will reset before it acts and touches only this machine's configuration, never a person, a role or a permission.
A fault that can be repaired without the person's involvement is repaired without asking.

#### Scenario: A store outage offers no reset
- **WHEN** the launch stopped because the store could not be reached
- **THEN** resetting is not among the actions offered, because a reset could not remove an outage

#### Scenario: A reset previews what it will touch
- **WHEN** resetting is offered and the person chooses it
- **THEN** they are told exactly what will be reset, and only this machine's configuration is affected

---

## Requirement: A person's preferences follow the person

Theme, the order of the waitlist, new-request alerts, which requests have been seen and whether parts without
pictures are listed are held against the person in the store, so they are already there on another computer. The
only thing left on a computer is what it needs to reach the store, and the local copy of the pictures.

#### Scenario: A preference set on one computer is present on another
- **WHEN** a person sets a preference and later signs in on a different computer
- **THEN** the preference is already there, because it was never held on the first computer

#### Scenario: The plant-wide locations list is readable by everyone and changeable by two roles
- **WHEN** somebody without `IT Department` or `Developer` authority opens the locations-to-hide list
- **THEN** they can read it and the write is refused where it happens, with the reason stated
