# Phase 0 Research: User Management and Permissions

Each entry below is a choice this plan had to make that the spec, the seed and the shipping code together did not
already settle. Where the answer was read out of this repository or out of official documentation, the source is
named. Everything the seed's decisions 1 to 30 already fix is treated as settled and is not repeated here; it is
carried into `data-model.md` and `contracts/`.

## Where the role ladder lives

**Decision**: The rung of each role is a column on the role catalogue, `auth_roles_catalog.role_rank`, seeded with
the pinned values (`developer` 100, `it_department` 90, `plant_manager` 80, `production_lead` 70, `setup_lead` 60,
`material_handler_lead` 50, and `material_handler`, `production` and `setup` at 10). C# reads the catalogue through
`sp_auth_roles_list` and compares nothing itself: `RoleAuthorization` answers `IsAtLeast`, the roles at or above a
rung, and the highest rung a person holds by reading that one list.

**Rationale**: FR-025 requires the rank rule to be enforced where the change is written, so the stored procedure
that writes a user must be able to compare two rungs itself. If the ladder lived only in C# the procedure would
have to be handed the acting person's rank by its caller, and the caller is exactly what the spec says cannot be
trusted. Putting the numbers in the catalogue keeps one definition that both readers read: the procedure compares
`role_rank` columns directly, and C# compares the numbers it read from the same rows. FR-018's "the rung of every
role must be checkable" then has an obvious answer, a row anyone can read, and every rung is asserted by a test: the
eight the shipped seed writes, in the ladder suite (T031), and the renamed role's own rung of 90, in the live proof
that runs the rename and its reversal (T033).

**Alternatives considered**: A rank table in C# only, as the seed's carried-forward requirements describe, was
rejected because it cannot satisfy FR-025 without trusting the caller. A second table holding ranks keyed by role
code was rejected as a second copy of something the catalogue already owns.

## Where the permission declaration and its resolution live

**Decision**: All of it in `MTM_Waitlist.Core`, under `MTM_Waitlist.Module_Core.Permissions`: the fourteen keys as
constants, the registry that declares them, the ladder readers, the role badge, the service that resolves a
permission for the signed-in person, and the user-management service and repository.

**Rationale**: `MTM_Waitlist.Shared` references `MTM_Waitlist.Core`, not the reverse, and `MTM_Waitlist.Mock.Service`
references `MTM_Waitlist.Core` and `MTM_Waitlist.Mock`. Core is therefore the only project that the app, the
Settings, Setup and Waitlist libraries and the separate service host can all see, which is what the seed requires
when it says the service must resolve the same key from the same store rather than keep a copy. Keeping one
implementation also means one place to invalidate when a permission is saved.

**Alternatives considered**: A pure registry in `MTM_Waitlist.Shared` was rejected because it would invert the
existing reference direction between Shared and Core. A copy of the registry in the service project was rejected
outright: two copies of the vocabulary is the defect this feature exists to remove.

## How the baselines satisfy both "one declaration" and "data, not code"

**Decision**: The registry in code declares each permission's key, its label, what it gates, the area it belongs
to, the shipped fallback, and the gate sites that read it. The baselines are **data**: one `role`-scoped row in
`config_settings_values` per permission per role, shipped by `seed_permission_role_baselines`. The two halves are
tied together by a check rather than by construction: every declared key must have a baseline row for every role
the catalogue holds, and every baseline row must name a declared key. That check runs on the permissions page, and
a pair of tests asserts it in both directions.

**This settles a contradiction inside the seed, deliberately.** The seed's section 3.8 states both readings in
different paragraphs: its registry paragraph says the declaration carries "the baseline each role gives its
people", while its database paragraph requires the baselines to be seeded "data, not literals in code". The
resolution taken here is the database paragraph's, on FR-048 and the seed's own "defaults are data" principle,
because a baseline held in code makes "change what a role may do" a code change again, which is the thing this
feature exists to remove. FR-046 was amended to the field list above, FR-075 was amended so the who-holds-this view
reads the seeded baselines the check ties to the declaration rather than the declaration itself, and T016 and T060
were aligned to that wording. It is recorded rather than silent because the seed is otherwise the authority for the
settled decisions.

**Rationale**: FR-048 says the baselines must be data so that what a role may do can be changed without a code
change, which rules the baselines out of code. FR-050 says an unreachable store must yield the shipped fallback
rather than a refusal, which rules the fallback out of the store, because it has to answer when the store cannot be
asked. So the two tiers live in two places by necessity, and FR-047 ("cannot appear without a baseline") is kept by
checking the two against each other rather than by making one of them derived from the other.

**Alternatives considered**: Baselines in the registry's code table was rejected because it contradicts FR-048 and
would make a role's may-do a code change again. Storing the fallback in the store was rejected because a fallback
that lives in an unreachable store is not a fallback. Generating the seed from the code table at build time was
considered and rejected as machinery with one consumer; the check gives the same guarantee with nothing to run.

## How the shipped baselines are proved to reproduce today's behaviour

**Decision**: A test reads the shipped seed file from the repository and compares it, permission by permission and
role by role, against the retired lists captured as fixtures in the test. The code path is the one already
used by `MTM_Waitlist.Tests/Module_Settings/Services/DestinationQuestionRetirementTests.cs`, which resolves the
repository root with `RepositoryPatternScan.FindRepositoryRoot()` and asserts against
`Database/Seeds/seed_waitlist_request_item_configs/create.sql` and `Database/Seeds/AllSeeds.sql`. The reverse
direction, the effective set per person against a live store, is an opt-in live-database test following the
repository's existing environment-gated pattern.

**Rationale**: SC-001 requires the comparison per person and tolerates zero differences, and §6 gate 7 requires it
provable rather than asserted. The shipped seed is the data that actually ships, it is checked in, and the
repository already polices shipped SQL from the test suite. Reading it introduces no second copy of the baselines.

**Alternatives considered**: Repeating the baselines as a C# table in the test was rejected because a fixture that
restates the thing under test can agree with itself while the shipped data disagrees. Asserting only against a live
database was rejected because that test is environment-gated and would leave the default build unproved.

## Where a permission is written, and why not through the existing upsert

**Decision**: The permission write is a new pair, `sp_config_permissions_user_get` and
`sp_config_permissions_user_set`. The set procedure writes the `config_settings_values` row and its
`config_settings_history` row in one transaction. `sp_config_settings_upsert` is left untouched and is not the
permission write path.

**Rationale**: Read from the shipping code, `sp_config_settings_upsert` derives `scope_key` from `p_scope_type` in a
`CASE` whose final branch is `ELSE 'developer'`, so a scope type it does not recognise silently lands on a
developer scope key rather than failing, and it writes no history row at all. FR-072 requires one history record per
changed permission naming the actor and both values, and the seed requires the row and its history to be written
together, because a reversal reads that history. Teaching the existing procedure a role scope would mean adding a
role-code parameter to a procedure whose single caller passes eleven positional parameters, which is a wider change
than a new pair for a new purpose.

**Alternatives considered**: Extending `sp_config_settings_upsert` with a `p_role_code` parameter was rejected as a
signature change to a shared procedure for the benefit of one caller, and because its missing history write would
still have to be added. Writing the two statements from C# was rejected: constitution III forbids statement text in
application code.

**Residual risk, recorded rather than fixed**: `sp_config_settings_upsert`'s `ELSE 'developer'` branch remains a
trap for any future caller that passes an unrecognised scope type. This plan does not change it, because its only
caller passes a known scope and changing a shared procedure's behaviour is outside this feature's scope. The
comment that explains this is added to the procedure's header so the next reader is not the one who finds it.

## The order the scopes resolve in

**Decision**: `fn_config_settings_scope_rank` becomes `computer` 1, `all_users` 2, `role` 3, `admin` 4,
`developer` 5, `user` 6. The reader keeps taking the highest-ranked applicable row, so a person's own row now beats
their role's baseline, which beats the all-users value.

**Rationale**: FR-053 requires a person's own value to win, and the function read from the database this session
ranks `user` 3, below `admin` 4 and `developer` 5, so today a person's own row loses. `role` must sit above
`all_users` because a role is narrower than everyone, and below `user` because a person is narrower than a role. The
two legacy scopes keep their order relative to each other so nothing that uses them changes meaning. Nothing
observable moves today: the table holds three rows, all three are `all_users`, and `seed_dev_masked_baseline`
confirms it.

**Alternatives considered**: Placing `role` above the two legacy scopes was rejected. There is no case for it, and
it would silently change what an `admin` or `developer` row means for every scoped setting, not just permissions,
since the function is shared with the whole settings system.

## Where the wrong-attempt count is held and what enforces it

**Decision**: A column on `core_users_profiles` beside `require_password_change`, exactly as decision 14 says. The
count is read by `sp_auth_credentials_check`, in the same row the caller already uses to judge the credential, and
it is written by one new procedure, `sp_auth_temporary_credential_attempt_record`, which either raises the count or
clears it in a single statement. `StartupSessionRepository.CheckCredentialsAsync` calls that procedure, so the
count and the judgement that produced it are recorded by the same code path.

**Rationale**: FR-037 requires the count to survive closing the application, so it cannot be in memory. FR-039
requires only a successful sign-in or a fresh reset to clear it, so the clear belongs with the two things that
cause it. Putting the increment in a single stored-procedure statement rather than reading a count and writing it
back from the client removes a lost update between two attempts and between a fast double press; the repository is
where the credential has already been judged, so no screen can forget to record the failure.

**Alternatives considered**: Counting in the sign-in view model was rejected because a second caller of the
credential check would then bypass the guard entirely. A read-modify-write from C# was rejected for the lost-update
reason above. A time-based expiry was excluded by FR-038.

## How a dismissal of the PIN window is refused

**Decision**: The PIN window is a `ContentDialog` carrying the PIN, the person it is for, the sign-in name, when it
was issued, who issued it and the instruction to change it at first sign-in, with one close button and a print
button. The dialog tracks whether the close came from its own close button, and cancels any other close in its
`Closing` handler. Printing routes through the existing `IReportPrintService` and `PrintableReport`, and never
closes the window.

**Rationale**: FR-034 requires the window to be closed deliberately and not by clicking outside it or by Escape.
Microsoft Learn's dialog guidance states that Escape, the system back button and gamepad B all take the close
button's path, and `ContentDialogClosingEventArgs.Cancel` is the documented way to stop a close; cancelling every
close that did not come from the dialog's own button therefore covers all of them, and does not depend on which
input produced the dismissal. A flyout was rejected because the same documentation lists a tap outside and Escape
as its normal light-dismiss actions, which is exactly what FR-034 forbids. Printing through
`IReportPrintService` follows the repository's own rule that printing goes through the operating system's path with
no PDF engine of its own.
Sources: Microsoft Learn, "Dialog controls"
(`https://learn.microsoft.com/windows/apps/develop/ui/controls/dialogs-and-flyouts/dialogs`),
`ContentDialog.Closing` and `ContentDialogClosingEventArgs.Cancel`
(`https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentdialog.closing`),
and "Flyouts" (`https://learn.microsoft.com/windows/apps/develop/ui/controls/dialogs-and-flyouts/flyouts`).

## What the two Administration entries are, and how they are shown

**Decision**: Each entry is a `wct:SettingsCard` with `IsClickEnabled="True"` bound to a navigation command, given a
short description and an action icon, wrapped in `x:Load` and carrying an `x:Name` because `x:Load` requires one.

**Rationale**: The Context7 documentation for the Windows Community Toolkit states that `SettingsCard` becomes a
clickable element with `IsClickEnabled`, and that it is intended for navigation to a detail page, with `ActionIcon`
and `IsActionIconVisible` controlling the affordance. That is the row shape the Settings page already uses for its
in-place panels, so the two new rows match what a reader already recognises while behaving as doorways, which is
what decision 21 requires.
Source: Windows Community Toolkit, `SettingsCard` sample documentation
(`https://github.com/communitytoolkit/windows/blob/main/components/SettingsControls/samples/SettingsCard.md`).

## Where the role code reaches the screens

**Decision**: `sp_auth_credentials_check`, `sp_auth_user_row_get` and `sp_auth_password_reset_required_get` each
gain a `role_code` column alongside the display name they already return. `StartupState` gains `CurrentRoleCode`
and `CurrentUserId`, and **keeps** `CurrentRole` as the display name. Every gate, the badge lookup and the rank
comparison read the code; only text shown to a person reads the name. `StartupState.IsDeveloper` stops comparing
the display name and compares the code.

**Rationale**: FR-105 requires the badge to be keyed on the role code, and FR-054 requires no gate to decide from a
role name. The code confirms the cause: every one of the retired lists the specification's Context counts compares
`auth_roles_catalog.role_name`, which is exactly why a display-name rename would ripple through every gate.
Adding the code rather than replacing the
name is deliberate, because `CurrentRole` is displayed in several places and re-pointing it in place would silently
change what those screens show. `IsDeveloper` is not one of those lists, but it is the same defect and it is the
thirteenth gate site the same Context counts: a gate deciding access from a display name.

**Alternatives considered**: Replacing `CurrentRole` with the code was rejected for the silent display change.
Deriving the code in C# from the name was rejected because it keeps the dependency that is being removed, and would
have to be maintained whenever a name changes.

## How a sign-in name is normalised

**Decision**: The sign-in name is stored and compared in upper case. `core_users_profiles.username_normalized`
keeps its name and its column, the callers that normalise stop lower-casing and start upper-casing, and
`seed_username_upper_normalization` migrates rows still holding a lower-case name. The column's collation stays
`utf8mb4_unicode_ci`, so a comparison remains case-insensitive whatever case a caller passes, and the
`sp_auth_credentials_check` and `sp_auth_user_row_get` header comments that document the lower-case contract are
corrected.

**Rationale**: FR-002 requires the upper-case rule and FR-003 requires the older rows to be migrated. Normalising
in the caller and not in every procedure keeps the unique key usable and keeps the rule in one place. The migration
must be guarded rather than blunt: two rows cannot collapse onto one unique key, so the migration reports a
collision instead of failing partway.

**Alternatives considered**: Wrapping `UPPER()` around the comparison inside each procedure was rejected: it
defeats the unique index and spreads the rule across every read instead of keeping it in the callers that already
normalise.

## The role rename, and how far its reversal is exact

**Decision**: `seed_role_admin_to_it_department` removes the `admin` row, inserts `it_department` with `role_rank`
90, and repoints every assignment from the removed row to the new one, in one transaction. The rung is written
explicitly: `role_rank` is `NOT NULL DEFAULT 0`, so an insert that left it out would rank the renamed top role below
the worker roles at 10, and the rank rule would then refuse it the accounts it exists to own. The paired
`rollback.sql` inserts `admin` again carrying the rung the removed row held — the column's default, because the
shipped seed deliberately leaves the retired role unranked — repoints every assignment currently on `it_department`
back to it, and removes `it_department`. The rollback states, in its own header and in the implementation checklist,
that it is exact while no account has been given IT Department since the migration. Because the reversal therefore
leaves the retired role in the catalogue, the run that proves it (T033) re-applies the forward change once the
reversal has been checked, so the store the baselines are seeded into holds `it_department` and not `admin`.

**The shipped developer seed stays out of the rename.** `seed_dev_masked_baseline` adds `material_handler_lead` and
sets the rungs for the roles it already holds, and leaves `admin` and `it_department` entirely to this migration
(T027). A shipped seed that also inserted `it_department` would put two seeds in charge of the same rows with no
stated precedence, would pre-empt the repoint this migration performs, and would destroy the only reason the
reversal is exact — that `it_department` did not exist before the migration. Restricting the shipped seed is
preferred to stating an application order between two seeds, because the ordering only has to be remembered by a
human, and because the captured `admin` baseline (T001) and the live comparison (T033) are both sound only while
this migration is the sole writer of those rows.

**Rationale**: The owner's decision 7 and FR-011 and FR-012 describe the retired role being removed and replaced
with the accounts moved onto the new one, so this is implemented as written rather than in a near-enough form. The
reversal is exact because `it_department` did not exist before the migration, so every assignment to it at rollback
time must be a migrated one; that is the reason the window holds and it is stated rather than assumed. The
transaction is possible at all because all three steps are row operations: MySQL cannot roll back a schema change
but it does roll these back together, so the interrupted state in which an account holds no role cannot exist.

**Alternatives considered**: Renaming the catalogue row in place, which needs no repointing and is exactly
reversible with one statement in each direction, was considered and rejected because the owner's decision and the
requirements call for removal and replacement and the accounts moved across. It is recorded here because it is the
simpler mechanism, and because a later change that has no such wording to honour should prefer it.

## Where the user list's remembered filter is stored

**Decision**: One row in `config_settings_values` under the key `admin.users.list_filter`, `value_type` `text`, with
`scope_type` `user` and `scope_key` `user:<id>`, holding the search text and the chosen role code as one small JSON
object.

**Rationale**: Decision 25 settles that the preference is remembered per person in the store rather than on the
workstation, and the table's unique key is `(setting_key, scope_key)`, so one person's search text and role filter
have to share one row or one of them has nowhere to go. Two keys would be two reads and would allow a half-saved
filter. A JSON payload in the existing text column needs no new column and no new value type.

**Alternatives considered**: Two separate keys was rejected for the partial-save reason. Reusing the existing
`LocalSettingsService` was rejected because the preference belongs to the person and not to the terminal, and
`LocalSettingsService` is per machine and per profile. A new table was rejected because the seed verified that no
new storage is needed.
