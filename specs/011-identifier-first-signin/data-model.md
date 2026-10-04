# Phase 1 Data Model: Identifier-First Sign-In and a Faster Launch

This feature introduces no new table for sign-in: the store already holds the profile, its `employee_identifier`
column and its credential, and the plant-system read is a mirror like every other external read. What the model
describes is the shapes the application works with, plus the one new set of database artifacts.

## Identifier

The value a person types first.

- **Value**: the text exactly as typed, trimmed of surrounding whitespace.
- **Kind**: `EmployeeNumber` when the value is exactly four digits, otherwise `UserName`.
- **Validation**: a blank value is rejected with the field's hint (FR-015). `12 3`, `12345`, `123a` and `123` are
  user names, never employee numbers (FR-002).
- **Uniqueness**: an employee number is deliberately not unique. `johnk` and `jkoll` may both carry `6229`.

## IdentifierSignInResult

What the first stage produces after the identifier is resolved.

- **Kind**: `ResolvedInStore` | `KnownToPlantSystemOnly` | `Unknown` | `NotActive`.
- **SignInName**: the account's user name, present for `ResolvedInStore`.
- **EmployeeNumber**: the four digits, present for an employee-number identifier.
- **FirstName**, **LastName**: present for `KnownToPlantSystemOnly`, taken from the plant system.
- **Reason**: a plain-language refusal for `Unknown` and `NotActive`.
- **Transitions**: the sign-in stage moves to the credential stage on `ResolvedInStore`, to the set-up panel on
  `KnownToPlantSystemOnly`, and stays open, showing the reason, on `Unknown` or `NotActive`.

**Rules** (FR-003 to FR-006, FR-012, FR-013, and the clarifications):

- The store is consulted first, by `employee_identifier` when the identifier is an employee number and by user
  name otherwise.
- An inactive store account short-circuits to `NotActive`; the plant system is not consulted and no set-up is
  offered, so no duplicate account can be created for a number the store already holds.
- When the store does not hold the number, the plant system is read through the cached-fallback path. An
  unreachable plant system whose mirror has never held the number yields `Unknown` with the "cannot be completed
  right now" reason, never a guessed name (FR-013).

## SetUpRequest

The state the sign-in surface holds while the set-up panel is open.

- **EmployeeNumber**: the identifier that produced `KnownToPlantSystemOnly`.
- **FirstName**, **LastName**: prefilled from the plant system and editable; the edited values are what the
  account is created with (FR-007).
- **Pin**, **ConfirmPin**: held as local values only, never as properties of the screen, and passed as one
  argument to the creation call (FR-008).
- **Validation**: at least four characters, no upper bound, both values identical, both non-blank.
- **Transitions**: `Open` → `Creating` → `Created` (the account exists) or `Failed` (nothing created). Leaving
  the panel from `Open` creates nothing and returns to the identifier stage (FR-024a).

## AccountSetup

What one creation call writes, atomically.

- **Profile**: display name (from the confirmed first and last name), `employee_identifier`, active.
- **Credential**: the chosen PIN's hash and salt, not temporary (FR-009, FR-045).
- **RankAssignment**: the lowest rank, read inside the procedure from the store's own rank list (FR-010).
- **Validation**: a failure anywhere leaves no profile, no credential and no rank (SC-011). The record is
  written by one stored procedure, `sp_core_account_setup_create` (FR-034).

## EmployeeByNumberRecord

The sixth plant-system read shape's row.

- **EmployeeNumber** (input and output), **FirstName**, **LastName**.
- **Mirror**: `visual_employee_by_number_result`, with its `_stage` twin and `_prev` twin, read by
  `sp_visual_employee_by_number_get` and refreshed by `sp_visual_employee_by_number_refresh` (FR-032).
- **Fallback parity**: the mirror's row has the same shape as the live read, so a caller cannot tell which
  answered (FR-032).

## LaunchBudget

- **Duration**: 45 seconds (FR-024).
- **Relationship**: one budget for one launch, linked to every step's own token. It never replaces a step's
  stated maximum (FR-025).
- **Transition**: `Running` → `Reached` (the walk ends at `Blocked` with a cause naming the budget) or
  `Completed` (the walk reaches the shell or a stated stop first).

## LaunchStepGroup

How the catalogue expresses concurrency.

- **Id**, **Order**, **Members** (step ids).
- **Rule**: members start together and the group ends when every member has ended. Each member still runs under
  its own stated maximum through the existing runner, so the per-step bound and the one-line-per-operation rule
  are unchanged (FR-026, FR-027, FR-031).
- Groups in this change: `{judge-session, check-computer-against-store, check-temporary-credential}` and
  `{read-local-settings, read-hardware-identity, store-reachability}`.

## LaunchStage

The phases the one window passes through.

- **Kind**: `Splash` | `SignIn` | `MachineSetup` | `Blocked` | `Shell`.
- **Rules**: the navigation panel is hidden for every kind except `Shell` (FR-018); the window maximises on the
  move to `Shell` (FR-019) and re-centres after each resize the launch makes (FR-020); the window opens centred
  (FR-021); `Shell` is reached only from `MainScreens` (FR-022, FR-043).
- **Transition**: driven by `App.ShowLaunchSurface(LaunchOutcome)`, which is the only place a stage is chosen.
