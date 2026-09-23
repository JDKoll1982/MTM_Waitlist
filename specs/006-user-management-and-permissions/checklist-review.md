# Review — 006 User Management and Permissions

Recorded 2026-09-22 by `/speckit.superspec.review`, scope `specs/006-user-management-and-permissions`.
Method: built-in review protocol (spec compliance, edge cases, constitution, code quality, test coverage).
The superpowers `requesting-code-review` skill was **not** used: `.specify/superpowers.yml` reports it detected at
`~/.agents/skills/requesting-code-review/SKILL.md`, but no `.agents` skill root exists on this profile, so the
fallback protocol ran. Findings are reported at confidence 80 or above; below that they are suppressed.

Artifacts reviewed: `user-management-and-permissions.spec.md` (117 FRs, 9 stories, 20 SCs, 25 edge cases),
`plan.md`, `tasks.md` (all 76 tasks ticked), `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, the
implementation across `MTM_Waitlist.*` and `Module_*`, the new `Database/**` artifacts, and `MTM_Waitlist.Tests/**`.

## Verdict

The feature is substantially built and unusually well documented: 86 of 96 acceptance scenarios are covered by an
implementation **and** a named test, every artifact ships its paired rollback, no inline SQL survives, no caller
supplies its own rank, and the permission composition is performed in one place. The findings below cluster in three
places rather than spread evenly: **unhandled store failures on three paths**, **a handful of spec contradictions**,
and **a tail of tests that assert the test's own constants**.

## Critical

### C1 — A store failure during sign-in throws instead of reporting (confidence 90)

`MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs` — `SignInAsync` (`:286`) awaits
`_startupSessionRepository.CheckCredentialsAsync(...)` (`:294`) with no handler. Inside, the retried read rethrows
after its budget (`StartupSessionRepository.cs:532`) and, more importantly,
`RecordTemporaryCredentialAttemptAsync` (`:314-349`) writes the attempt counter on its **own second connection** with
no retry and no catch. `SignInAsync` is a `[RelayCommand]`, so the generated `AsyncRelayCommand` rethrows the fault on
the UI context: the failure is an unhandled exception, not a sentence on the screen.

*Failure path:* the network drops between the credential read and the counter write → `MySqlException` escapes both
methods → the sign-in window goes down. The counter write happens exactly on the path an operator hits when they have
mistyped a PIN, so the exposure is on the ordinary path, not an exotic one.

*Fix:* wrap `SignInAsync`'s body in `try`/`catch` and route the failure to `LoginHint`, and make the counter write
non-fatal — record the failure, keep the verdict, let the next attempt re-read the count.

### C2 — The undo "restore anyway" path can crash the permissions page (confidence 85)

`MTM_Waitlist.Settings/ViewModels/PermissionsViewModel.cs` — `SaveAsync` (`:298`), `UndoAsync` (`:363`) and
`ConfirmRestoreAsync` (`:395`) are `try`/`finally` with **no catch**. `ConfirmRestoreAsync` calls
`ReverseLastSaveAsync(..., restoreDespiteMovedValue: true)`, which reads the current values through
`PermissionAdministrationService.GetForPersonAsync`, whose catch-all logs and **rethrows**
(`MTM_Waitlist.Core/Services/PermissionAdministrationService.cs:107`). Nothing between that and the command catches,
so a store outage at the moment the reader answers the moved-value prompt ends the process.

*Fix:* either move the moved-value read inside `ReverseLastSaveAsync`'s error mapping so it returns
`PermissionChangeResult.Failed(StoreUnavailable, …)`, or add the same `catch (Exception)` the other three Settings
view models carry and set `MessageText`.

### C3 — A person can reset their own password, which the spec forbids twice (confidence 95)

`user-management-and-permissions.spec.md:583-584` (FR-028) and `:869` (Out of Scope) both state there must be no way
for a person to reset their own password. `ResetPasswordAsync`
(`MTM_Waitlist.Settings/ViewModels/EditUserViewModel.cs:410-425`) guards on `IsEditable`, `CanResetPassword`,
`HasUnsavedChanges` and `IsSaving` — never on `IsSelfAccount`, although that property exists (`:168`) and is used for
deactivation and rename. The view binds the Reset button to `CanResetPassword` alone
(`Module_Settings/Views/EditUserPage.xaml:128-132`), and the store refuses only a target that *outranks* the actor
(`Database/StoredProcedures/sp_user_management_reset_password/create.sql:84`) — on one's own account the ranks are
equal, so it is allowed. `permission.admin.reset_password` ships to six roles, so most administrators can do this.

*Impact:* a spec MUST NOT that the code does, and a self-lockout route — reset your own account, and the only way
back in is the PIN you were just shown. No test asserts the claim (the only nearby test checks the *sign-in* page has
no reset control).

*Fix:* refuse the action when `IsSelfAccount`, and hide the control on the same condition, mirroring deactivate and
rename; add the case to `EditUserViewModelTests`.

### C4 — A save or reset against an account that does not exist reports success (confidence 88)

`MTM_Waitlist.Core/Services/UserManagementRepository.cs:242-275` — the doc comment says the method "read[s] the
affected-row count", but `:269` calls `ExecuteNonQueryAsync` and discards the result, then returns
`UserManagementResult.Succeeded()`. `sp_user_management_update` has no existence check, and
`auth_user_management_audit` carries no foreign key on `target_user_id`. So a `p_user_id` that matches nothing
updates zero rows, writes an orphan audit row, commits, and reports success; for the reset path the operator is shown
a one-time credential window for a person who is not there.

*Fix:* read the returned count and treat zero as a typed "no such account" refusal, or `SIGNAL` a `mtm_user_not_found`
token from the procedures in the style the other refusals already use; correct the doc comment either way.

## Important

### I1 — "Remember me" writes the password to disk in plaintext, including one just set from a PIN (confidence 92)

`MTM_Waitlist.Startup/ViewModels/LoginViewModel.cs:471` persists the credential verbatim through
`LocalSettingsService` (`MTM_Waitlist.Settings/Services/LocalSettingsService.cs:163`), which writes a JSON file under
`%LOCALAPPDATA%` with `File.WriteAllTextAsync`. `:375` passes the freshly chosen `NewPassword` into the same path, so
the password that replaced a one-time PIN lands in a readable file. The constitution's Security constraint requires
credentials not to be stored unguarded and requires DPAPI (`ProtectedData`, `CurrentUser`) where a secret is at rest;
sign-out clears the value, but clearing is not the same as never having written it in the clear. The PIN path honours
the "readable only while the window is open" contract; the password it creates does not.

*Fix:* protect the remembered value with `ProtectedData.Protect(CurrentUser)` and unprotect on read, or keep the
remembered credential in memory for the session only; at minimum never remember a password set on the forced-change
panel.

### I2 — The PIN's hash and salt leave the store, so the attempt limit is not the real control (confidence 85)

`sp_auth_credentials_check` returns `password_hash` and `password_salt`, and the comparison happens on the client
(`MTM_Waitlist.Startup/Services/StartupSessionRepository.cs`, `PasswordSecretHasher.Verify`). The comparison is
correct (PBKDF2-SHA256, 100 000 iterations, fixed-time compare), but a four-digit space is 10 000 candidates —
minutes of CPU for anyone holding one captured sign-in. The design's stated strength for a PIN is the five-attempt
limit, and this makes that limit bypassable by anything that observes the transport.

*Fix:* move the comparison into the store — the procedure takes the presented secret, compares inside, returns only
the verdict and identity. If that is out of scope here, record it as a known weakness instead of describing the
attempt limit as the credential's protection.

### I3 — The rank rule fails open on the person's page and fails closed on the permissions page (confidence 88)

`MTM_Waitlist.Settings/ViewModels/EditUserViewModel.cs:335-337` computes
`readerRole is not null && personRole is not null && IsAbove(...)`, so an unreadable role means **editable**; the
comment directly above says the honest answer is that the account cannot be acted on. `PermissionsViewModel` answers
the same question the other way (`catalogue.Count > 0`). The store is not a backstop when the target's role is
missing: `sp_user_management_update`'s rank lookup is an `INNER JOIN`, so a role not in the catalogue leaves
`v_target_rank` at its default 0 and never trips `mtm_target_outranks_actor`.

*Fix:* treat an unresolvable role as locked on both screens, and refuse a target whose role cannot be resolved inside
the procedures rather than defaulting its rank to 0.

### I4 — A failed role-catalogue read is cached as an empty catalogue for the life of the process (confidence 90)

`MTM_Waitlist.Core/Services/RoleCatalogService.cs:38` caches with `_cachedRoles ??= await ReadRolesAsync(...)` while
`:76` returns `Array.Empty<RoleCatalogEntry>()` on failure, and nothing in production calls `Invalidate()`. A
momentary outage is therefore served as "the plant has no roles" for the rest of the session: the create form's role
picker is empty, and — with I3 — the person's page presents outranking accounts as editable.

*Fix:* cache only successful reads; return the empty list without caching it so the next screen retries.

### I5 — Nothing stops an administrator locking themselves out by role change or by their own permission row (confidence 85)

`sp_user_management_update` guards self-deactivation and self-rename but not a change to the actor's own role code,
and `sp_config_permissions_user_set` accepts `p_user_id = p_actor_user_id` with no self-guard; `EditUserViewModel`
offers Save on the reader's own account and the role picker includes their own role. The feature protects two
self-destructive actions and misses the two that are equally final — demote yourself and there is no in-application
way back.

*Fix:* refuse a self role change that would drop the actor below the rung needed to administer users, and refuse a
permission change that would remove the actor's own `admin.users` or `admin.permissions`.

### I6 — The display-name bound is discarded before it reaches the store (confidence 85)

`UserManagementService` bounds the derived display name, and the comment at
`MTM_Waitlist.Core/Services/UserManagementRepository.cs:152-154` and `:186` explains that the value "travels nowhere"
because the procedure derives it. The procedure does exactly that —
`sp_user_management_create/create.sql:87` and `sp_user_management_update/create.sql:90` set
`CONCAT(v_first_name, ' ', v_last_name)` with **no truncation** into a `VARCHAR(256)` column
(`:71` and `:68`). Two 128-character name parts derive 257 characters, so the guard that exists to prevent that
failure is not in the path that would hit it.

*Fix:* pass the bounded value as a parameter, or truncate inside the procedure; delete whichever copy is not the real
one.

### I7 — The wrong-attempt counter is a read-then-write with no transaction or row lock (confidence 82)

`MTM_Waitlist.Startup/Services/StartupSessionRepository.cs` reads the count, compares in C#, and writes the increment
in a separate call on a separate connection. Two concurrent attempts against one account read the same value and the
increments collapse; the account row is shared by every workstation, so a scripted attack from several seats against
one temporary account can buy more than five tries.

*Fix:* one procedure that locks the row (`SELECT … FOR UPDATE` inside a transaction) and returns the verdict.

### I8 — Save and reset read store columns by ordinal (confidence 85)

`MTM_Waitlist.Core/Services/UserManagementRepository.cs` addresses a 12-column and a 9-column result positionally
(`reader.GetInt64(0)` … `ReadString(reader, 8)`), and `StartupSessionRepository` does the same (`reader.GetBoolean(5)`,
`reader.GetValue(8)`). The procedures' own headers record that "the calling code's ordinals moved in the same change"
— that is the hazard, not the mitigation. One inserted column in `sp_user_management_get` shifts `role_rank`,
`is_active` and the attempt count into each other without throwing.

*Fix:* read by name with `reader.GetOrdinal(...)`, which `ServiceOperatorRoleResolver`, `PermissionAdministrationService`
and `RoleCatalogService` already do.

### I9 — The undo path can merge two saves made in the same second (confidence 82)

`sp_config_permissions_user_set` stamps `changed_utc` with `UTC_TIMESTAMP()` into a `DATETIME` column with no
fractional seconds, and `sp_config_permissions_reversal_get` groups on `MAX(changed_utc)`. A second save (or a save
and its undo) by the same person inside the same second is grouped with the first, so an undo restores the union —
a silent wrong write to somebody's permissions.

*Fix:* use `DATETIME(6)` and `UTC_TIMESTAMP(6)`, or record a save group id and group on that.

### I10 — The live role-rename test can strand the store in the retired-role state (confidence 85)

`MTM_Waitlist.Tests/Module_Settings/Services/RoleRenameMigrationTests.cs:96` executes the shipped
`seed_role_admin_to_it_department/create.sql` and `rollback.sql` against the **live** store, which re-points every
real account's assignment. `TestCleanupAsync` (`:86`) removes only its own fixture row, and nothing restores the
catalogue in a `finally`. A run that fails between the reversal and the re-apply leaves the store holding the retired
`admin` role with no `it_department` — the state the seeded permission baselines are not keyed to, and the state the
parity, badge and service-operator suites all read. The fixture sign-in name is fixed rather than per-run, so two
concurrent runs also collide.

*Fix:* re-run `create.sql` in cleanup so the store always ends renamed, and fail loudly if the ending catalogue is
wrong; make the fixture name unique per run.

### I11 — The live case the suite claims is not covered (confidence 90)

`tasks.md` T074 says the suite "must cover the eleven live cases §6 gate 2 names", and `quickstart.md` itself admits
**UI-thread collection safety** is "stated as a gap". No test in the project installs a `SynchronizationContext`
(zero matches), so the eligibility claim in `tasks.md` overstates what the suite proves. The other ten cases do have
owners, and the four skipped tests are genuine skips (`Assert.Inconclusive`) belonging to an earlier feature.

*Fix:* correct T074's sentence, or add the case.

### I12 — A helper-seam bypass is not recorded where the constitution requires it (confidence 85)

`UserManagementRepository.cs:259,377` and `PermissionAdministrationService.cs:455-489` open their own
`MySqlConnection` rather than using the `IMySqlHelperServer` seam that `Database/Database-Ruleset.md:156` describes,
and one write path (`UserManagementRepository.cs:269`) does not read the affected-row count (see C4). Constitution III's
letter is met and the rationale is genuinely documented in code and in the decision journal, but `plan.md:131` states
"No principle is violated, so there is no Complexity Tracking table", while the Review rule requires a deviation to be
recorded there before merge.

*Fix:* add the Complexity Tracking entry with the rationale already written in the code comments.

## Suggestions

| # | Confidence | What |
|---|---|---|
| S1 | 95 | `MTM_Waitlist.Core/Permissions/PermissionKeys.cs:4` says "The fourteen permission keys" above **fifteen** constants; `PermissionRegistry.cs:71` repeats it; `tasks.md` T016 and the quickstart's permissions-page walk say fourteen. The tests are right; the prose is not. The fifteenth (`permission.settings.storage_paths`) belongs to the picture work, so the fix is the count, not the key. |
| S2 | 95 | `.specify/superpowers.yml` records `requesting-code-review` as detected, but no `.agents/skills` root exists on this profile, so the project's declared "enhanced mode" is unavailable. Correct the record, or reinstall the skills. |
| S3 | 90 | `plan.md:124` states "no new converter or resource key is introduced"; `MTM_Waitlist.Settings/Converters/HexColorToBrushConverter.cs` is new and used at `UserManagementPage.xaml:288` (correctly registered in `App.xaml:23`). The self-assessment line is wrong, not the code. |
| S4 | 90 | The spec is named `user-management-and-permissions.spec.md`, not `spec.md`; `.github/instructions/spec-kit.instructions.md` says `spec.md` is mandatory and other features ship it. |
| S5 | 88 | `MTM_Waitlist.Settings/Models/IssuedCredential.cs` is a `record`, so its generated `ToString()` prints the PIN. Nothing stringifies it today; the first interpolation anywhere silently breaks the "logged nowhere" contract. Override `ToString()` to omit it. |
| S6 | 88 | Three tests compare the declaration to a surface derived from the declaration, so a dropped key passes: `PermissionsViewModelTests.cs:49,56-59`, `PermissionHoldersViewModelTests.cs:192-198`. The real guard is `PermissionRegistryTests.cs:71`. |
| S7 | 88 | `MTM_Waitlist.Tests/Module_Settings/Services/RoleRenameMigrationTests.cs` aside, `RoleAuthorizationTests.cs` never reads a shipped artifact — `Ladder()` (`:199`) builds entries from the test's own constant, so a seed that lost a rung passes the whole file. `RoleLadderSeedTests.cs:41` shows the stronger pattern. |
| S8 | 85 | `sp_config_permissions_feature_holders_get` filters the role join by `hr.role_rank = (SELECT MAX(...))`, which matches every role a person holds at their top rung; the three worker roles share rank 10, so a person can be listed twice. Pick one deterministically as `sp_user_management_list` does. |
| S9 | 85 | `UserManagementViewModel.cs:513` and `PermissionHoldersViewModel.cs:154` start fire-and-forget loads with no cancellation, so the visible list can settle on the result of an older search term and exceptions outside the inner catch are unobserved. |
| S10 | 85 | `UserManagementRepository.cs:297,339` compose the provider's `ex.Message` into operator-facing text. Keep it in the log. |
| S11 | 85 | The generic write path passes `UserManagementOutcomeKind.InvalidInput` as its fallback kind (`UserManagementRepository.cs:242`), so an outage is reported as a validation problem to callers that switch on `Kind`. |
| S12 | 85 | `permission.admin.reset_password` alone grants nothing — `ResetPasswordAsync` requires `IsEditable`, which requires `permission.admin.users`. Every seeded role has both, so nothing breaks today, but the key can be granted to a role and change nothing. Gate the reset on its own key or state the conjunction in the declaration. |
| S13 | 85 | `Module_Settings/Views/SettingsPage.xaml.20260917-124630.bak` sits in a Views folder and is not covered by `.gitignore`. |
| S14 | 82 | `PermissionRegistry.Resolve` (`:221`) tries to strip the `"permission."` namespace from a resource key that begins `"Permission_"`, so the fallback label reads "Permission requests handle". |
| S15 | 82 | `Database/StoredProcedures/sp_config_permissions_feature_holders_get` and `sp_user_management_get` return a person's role through a join that can yield more than one row for a multi-role holder; only the list procedure pins the answer. |

## Edge cases

Of the spec's 25 edge-case bullets, 15 have both an implementation and a named test. The uncovered ones, in
confidence order: the last holder of the top role has no stated recovery route anywhere in code or resources (no
implementation found); a reset of a deactivated account is written to preserve `is_active` but no test asserts it;
overrides surviving a role change, two people changing one account, the reversal-under-rank refusal, and the
lost-PIN route all have implementation or design but no test. SC-007's store-and-log search is performed manually in
`quickstart.md` rather than by a test. SC-011/012 (scaling and no-freeze) have only the weak proxy of `IsBusy` being
false after a load.

## Checked and found clean

No caller-supplied rank or entitlement anywhere in `Database/**`; no string-built or dynamic SQL, no `PREPARE`; no
role **name** compared for access (every gate reads `role_code`); no PIN, hash, salt or connection string logged, and
the PIN is not stored, audited or echoed (both credential audit rows carry NULL/NULL); the one-time PIN really does
reach sign-in through the same hasher, so the two paths cannot diverge; the attempt limit itself is correct — read
before comparison, cleared only by a success or a fresh reset, held on the account row so it survives a restart, and
not movable for an ordinary account; permission composition is performed once and in one order on both the gate and
the page; the request-handling refusal exists at the action (`WaitlistRequestService`) and not only on the card; the
save path is atomic, sends `from` for every entry, writes nothing for an empty set, and `SIGNAL`s stable tokens; no
`.Result`/`.Wait()`/`async void` was added on the new paths; every 006 schema artifact ships `create.sql` and
`rollback.sql`; every compound write procedure carries `DELIMITER`, an exit handler and an explicit transaction; the
master lists are updated; `CHANGELOG.md`, `FEATURES.md` and `Database/Database-Ruleset.md` were updated in the same
change.

## Not verifiable from this review

Principle VI (a clean build and a green suite) was not re-run here; the last recorded run is 1387 total, 1383 passed,
4 skipped, 0 failed, with 0 warnings. The server's `sql_mode` was not established, so I6's failure mode (refusal
versus silent truncation) is unresolved. Principle IV's lookups cannot be proved from artifacts, only evidenced.
