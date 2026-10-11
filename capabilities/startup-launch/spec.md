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

#### Scenario: Nothing opens the plant's shared key file
- **WHEN** a launch runs and a person signs in
- **THEN** no key file is read, copied or kept, and nothing about the sign-in is left on the machine

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

---

## Requirement: Sign-in asks who a person is before it asks for their PIN

Feature 011. The first stage takes a user name, or exactly four digits for an employee number. An employee number
is resolved in the store first, by `employee_identifier`; only a number the store does not hold is put to the
plant system through the sixth read shape, `employee_by_number`. Everything behind the credential stage — the
attempt count, the session check, the machine gate, the temporary-credential limit and the new-PIN write — is
unchanged.

#### Scenario: An employee number the store holds reaches the PIN stage
- **WHEN** a person types exactly four digits and the store holds that `employee_identifier` on an active account
- **THEN** the second stage asks for their PIN, signing in with the account's own sign-in name

#### Scenario: An employee number the store holds on an inactive account is refused
- **WHEN** the store holds the number but the account is not active
- **THEN** the person is told the employee is not active, no set-up is offered, and no duplicate account is created
  even when the plant system also knows the number

#### Scenario: Nobody holds the identifier
- **WHEN** neither the store nor the plant system holds what was typed
- **THEN** the person is refused in plain words and the form stays open and usable

#### Scenario: The plant system cannot be reached and has never held the number
- **WHEN** the plant system is unreachable and the mirror has never held that employee
- **THEN** the set-up states it cannot be completed right now and no name is invented

#### Scenario: Two people legitimately share an employee number
- **WHEN** more than one account carries the number
- **THEN** the store answers with the active account first, by its own ordering, and no rank or name is decided in
  the application

---

## Requirement: A person the plant system knows sets themselves up

Feature 011. A number the store does not hold but the plant system does opens a set-up panel: it states the person
is not set up for the waitlist yet, shows the plant system's first and last name pre-filled and editable, and asks
for a PIN twice under the existing rules (at least four characters, no upper bound, both values typed the same
way and passed together as one argument). The chosen PIN is the credential from creation, so the account never
enters the temporary-credential state; the lowest rank is read inside the creation procedure from the store's own
rank list, and the panel says a higher permission level needs IT. Leaving creates nothing.

#### Scenario: A person only the plant system knows is created and signs in straight away
- **WHEN** they confirm their name, choose a PIN twice and submit
- **THEN** the profile, the credential and the rank are written in one stored-procedure call, and they sign in on
  that PIN without ever seeing a temporary-credential prompt

#### Scenario: The set-up write fails part-way
- **WHEN** the creation call cannot complete
- **THEN** nothing is written — no credential without a rank and no rank without a credential — and the reason is
  stated

#### Scenario: The form is completable from the keyboard
- **WHEN** Enter is pressed on a stage
- **THEN** it submits exactly as that stage's button does, on the set-up panel it moves to the next field and
  submits only at the last, and a blank required field shows its hint rather than doing nothing

---

## Requirement: One window, and a launch that overlaps its independent work

Feature 011. The splash, sign-in, machine-setup and blocked surfaces are stages of the one window, which hosts the
launch, hides its navigation panel until the hand-over, resizes between the big phases, re-centres after each
resize it makes, opens centred on the screen it starts on, and maximises when it becomes the shell. The shell
remains reachable only from the `MainScreens` outcome, and nothing in the application assumes a signed-in person
before the hand-over. A launch-wide budget of 45 seconds sits **in addition to** each step's own stated maximum
and never replaces it. Independent work overlaps: the three session checks run together, the local reads run
alongside the first store contact, and the picture copies start at the beginning. The external-system verdict
stays last before the hand-over. The surface renders every open line at once, each written before the work it
describes. No launch path refreshes the cached plant data.

#### Scenario: A launch passes its budget
- **WHEN** a bounded step has not itself failed and the walk passes the launch-wide budget
- **THEN** the launch stops at a stated stop whose cause names the budget, and each step's own maximum still
  applied independently

#### Scenario: Several operations are in flight at once
- **WHEN** concurrent work is running
- **THEN** the surface shows each operation as its own line, so a stall is still attributable to a named line

#### Scenario: The application never refreshes the mirror
- **WHEN** any launch runs
- **THEN** no request that refreshes the cached plant data is made, because refreshing belongs to
  `MTM_Waitlist.Mock.Service` alone

**Accepted risk (recorded, not fixed).** Two-stage sign-in reveals whether an identifier exists. This is accepted
for an internal application. Hiding the navigation panel means nothing in the shell may assume a signed-in person
before the hand-over. Re-centring on resize is only smooth if resizes happen at the big phases.

### Sign-in offers the last six people who signed in on this computer

Sign-in offers the six people who most recently signed in on this computer. Choosing one fills in the user name
and nothing else, so the PIN is still required and picking a name signs nobody in.

#### Scenario: Somebody picks a person offered beside the form
- **WHEN** a person picks one of the offered names
- **THEN** the user name is filled in, the PIN stage opens with it, and the person is not signed in until the
  PIN is given

#### Scenario: The store cannot be read while somebody signs in
- **WHEN** the offer cannot be read
- **THEN** the form is shown on its own and the sign-in still works

### The offer of previous people is shown whole beside the sign-in form

The offer sits on the right-hand side of the form, separated from it by a divider, and the stage is sized so no
name in it is cut off.

#### Scenario: The offer is shown
- **WHEN** the sign-in stage shows six people
- **THEN** all six are visible, with none cut off by the window

### Machine setup reads the computer's name from the computer

The name a computer reports for itself is read from the computer and shown on the machine-setup stage. A person
cannot type it.

#### Scenario: The machine-setup stage opens
- **WHEN** an authorised person reaches the setup stage
- **THEN** the name the computer reports is shown and there is no field to type one

### Machine setup asks only for where the computer stands

Where a computer stands is the only value a person supplies at setup, and a computer that supplies nothing else
is configured.

#### Scenario: A computer with no record is set up
- **WHEN** an authorised person gives the location and saves
- **THEN** the computer is configured by the name it reports and that location, and no shared folder is captured

### What a computer stored before the location existed is read as its location

A record written before the location existed keeps working, so no computer is set up again to gain one.

#### Scenario: A computer whose record predates the change starts the application
- **WHEN** a computer whose record was written before the change starts the application
- **THEN** it is configured, and the value it stored is read as the location it stands in
