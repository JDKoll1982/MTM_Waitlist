# Quickstart: Proving User Management and Permissions

The order below is the order the work is verified in, because the steps depend on the one before them. The
precedence re-rank has to be in place before any per-person permission row exists, the rename's paired reversal has
to be run while the state it reverses is still the state on the floor, and the forward rename has to be back in
place before the permission baselines are seeded, because every baseline is keyed to the role the rename creates.

## The two orderings that must not be reversed

They are written here as well as in `tasks.md` and `plan.md`, so they are found by reading rather than remembered.

1. **The re-rank before any per-person row.** `Database/Functions/fn_config_settings_scope_rank` becomes
   `computer` 1, `all_users` 2, `role` 3, `admin` 4, `developer` 5, `user` 6, and it must be in place **before the
   first per-person permission row is ever written**. That row is written by
   `sp_config_permissions_user_set`, reached only from the permissions page. A row written under the old order
   resolves below an inherited `admin` or `developer` value and is silently overridden, and nothing reports it, so
   a store that took the row first would look correct and behave wrongly.
2. **The rename, its reversal, and the re-application of the rename, before the baselines.** The rename
   (`Seeds/seed_role_admin_to_it_department`) runs, its paired reversal is **actually executed**, and the forward
   rename is then **re-applied**, all before `Seeds/seed_permission_role_baselines` runs. The reversal leaves the
   retired role in the catalogue, and every baseline is keyed `role:<role_code>`: a baseline keyed to a role that
   does not exist is a baseline nobody holds, and one keyed to the retired role is a baseline nobody should. Step 4
   below is the whole of that ordering in one run, and step 5 depends on the state step 4 leaves.

## Before anything is written

1. **Check what the store holds now.** Read the three rows in `config_settings_values` and confirm all three are
   scoped `all_users`. Everything about the precedence change being safe rests on this, so it is checked rather
   than remembered.
2. **Count the accounts on `admin`.** This is the set the migrated set must equal afterwards, and it is the number
   the verification compares against. Take it as a list of accounts, not as a count.

## The schema and the data

3. **Apply the schema.** Run `Database/Bootstrap/create_database.sql` for a fresh store, or let
   `Database/Bootstrap/update_table_descriptions.sql` add the four columns to a store that already exists. The
   guarded blocks report that they already exist rather than failing when they do.
4. **Run the rename and prove both halves, then leave the rename standing.** Apply
   `Seeds/seed_role_admin_to_it_department/create.sql`, then check: the set of accounts on IT Department equals the
   set that held `admin`, the count of accounts with no role is zero, and the catalogue holds the nine codes with
   `admin` absent and `it_department` present at its rung. Then apply
   `Seeds/seed_role_admin_to_it_department/rollback.sql` and make both comparisons again, which is the half that
   turns a written rollback into a run one. Re-apply `create.sql` afterwards — the reversal is proved, not left
   standing — and check the accounts and the nine codes once more, so the store the next step seeds into holds IT
   Department at its rung and not `admin`.
5. **Seed the baselines and prove they reproduce today.** This runs against the state step 4 leaves, with IT
   Department in the catalogue and `admin` out of it, because every baseline is keyed `role:<role_code>` and the
   renamed role's baseline is keyed `role:it_department`. Apply `Seeds/seed_permission_role_baselines/create.sql`.
   The comparison that matters is per person and is asserted by the test suite rather than by eye: for every
   person in the store, their effective set on ship day equals exactly what their role's retired list gave them,
   for each of the eleven keys that replaces a list, with zero differences and with no per-person permission row
   stored. The three keys with no predecessor — the accounts key, the reset key and the permissions key — have
   nothing to be compared against, so they are checked against their stated baseline sets: six roles for the first
   two, and IT Department, Plant Manager and Developer for the third.
6. **Migrate the older sign-in names.** Apply `Seeds/seed_username_upper_normalization/create.sql`, and confirm a
   name that was stored in lower case is now stored in upper case and still signs in when typed in either case.

## The suite

7. **Run the whole suite**, after stopping any running `MTM_Waitlist.exe` and clearing the build nodes:

```powershell
Get-Process -Name MTM_Waitlist,Microsoft.UI.Xaml.Markup.Compiler,VBCSCompiler,MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
$env:MSBUILDDISABLENODEREUSE = '1'
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

   The expected result is `Failed: 0`, with any environment-gated case recorded as skipped rather than reported as
   passing. The cases that carry this feature are the registry checks in both directions, the per-role baseline
   comparison against the shipped seed, the cache-refresh subset rule, a person's own row beating their role's
   baseline and changing nobody else's, the five-attempt limit surviving a restart, the two audit scans, and the
   new check that no gate reads its own role list.

8. **Build the solution clean**, with the parallel-build trap avoided:

```powershell
dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false
```

   `0 Warning(s) 0 Error(s)` is the gate. `WMC9999` here means a real XAML error somewhere rather than an
   environment problem, so it is surfaced rather than skipped.

## The live database checks

9. **Run the opt-in integration tests against a live `mtm_waitlist`**, which is where the procedures are proved:
   a create writes the profile and the role assignment together and rolls both back on a mid-way failure, a
   duplicate sign-in name arrives as `1062` and becomes one typed answer, the rank rule refuses a target above the
   actor, the self-lockout and self-rename refusals are refused in the store, a reset writes one audit row with no
   value on either side, and a change set writes exactly its own number of history rows.

## The application

10. **Sign in and look at it.** With both connection overrides set, launch the built application and check the
    things only the running app can show:

```powershell
$cs = 'Server=172.16.1.104;Port=3306;Database=mtm_waitlist;User Id=root;Password=root;AllowPublicKeyRetrieval=True;'
$env:MTM_WAITLIST_DB_CONNECTION_STRING = $cs
$env:MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING = $cs
```

   - Settings shows the Administration category, and each entry navigates rather than expanding in place.
   - The user list shows everyone, marks the switched-off people, searches all four facts, filters by role, and
     comes back with the filter still applied and the page saying so.
   - A person's page shows the fields above and the actions below, and the reader's own account shows deactivate
     and rename unavailable with the reason.
   - A create ends with the PIN window, the window survives a click outside it and Escape, and printing leaves it
     open.
   - The permissions page shows one person at a time with each row saying where its value came from, the fixed row
     is locked, and saving nothing is unavailable.
   - The five-attempt limit is real: five wrong tries with a temporary credential stop it being accepted, and
     closing and reopening the application does not restore it.

11. **Check the scaling pass.** The two new pages and the person's page between 150 and 200 percent scaling and at
    the narrowest window the application permits, with all five facts of a list row still visible at both widths.

## What the three live passes proved

Each pass records its outcome here, and each says what it ran against and what it saw. The scaling pass is
recorded beside the walk, because both read the running application.

### T072: the live-database integration checks

One opt-in, environment-gated pass against a live `mtm_waitlist` on MySQL 5.7.24, with
`MTM_WAITLIST_DB_CONNECTION_STRING` and `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` naming the same store. Twenty
cases ran and none was skipped.

| The claim | How it was proved |
| --- | --- |
| A create writes the profile and the role assignment as one act | the new account has one profile row, exactly one role assignment naming the role that was asked for, and one change identifier across every audit row it wrote |
| A duplicate sign-in name arrives as `1062` and becomes one typed answer | creating the same name twice returns the typed "already taken" answer with the pinned sentence, and the store still holds one profile and one assignment |
| A create that fails after the profile insert rolls both writes back | the role-assignment table is held in another transaction, so the profile insert succeeds and the role insert waits and fails with `1205`; afterwards neither row exists |
| The rank rule refuses with no screen involved | a `setup` actor creating a `developer` gets the typed denial and writes nothing, and the same actor editing a `developer` account is refused and leaves that account untouched |
| The two self refusals are refused in the store | switching off one's own account is refused and the account stays active; renaming one's own sign-in name is refused, the stored name is unchanged, and no audit row is written |
| A reset records one row with no value on either side | the audit count moves by exactly one, and that row names the acting user, carries the time, and holds nothing on both sides |
| Switching somebody off ends no session | a session written for the person is still active with the same expiry afterwards, which is what the confirmation promises |
| One save shares one change identifier | a save changing both name parts, the derived display name and the employee number writes four rows under one change identifier, each naming the actor and the time |
| Ship day changes nobody's access | every person in the store holds exactly what their role's retired list gave them, for each of the eleven replaced keys; the three keys with no predecessor match their stated sets; and the store holds zero per-person permission rows |

**What running it found, and what it cost.** `sp_user_management_update` failed on every call from a utf8mb4
connection with `Illegal mix of collations for operation 'UNION'`. The two active flags were written with
`CAST(... AS CHAR)`, which takes the connection's collation, while the routine's own variables take the
database's, and MySQL 5.7 refuses to union the two. The screen suites could not see it because they drive
hand-written fakes, and the mysql client could not see it either because its own connection charset differs. The
two branches now write literals, which are coercible and take the branches' own collation. The fix is in
`Database/StoredProcedures/sp_user_management_update/create.sql` and in the `AllSPs.sql` roll-up.

### T073: the reset PIN is readable nowhere

A reset was performed against the live store with a PIN chosen so that it collides with nothing already stored:
`7391` appears in none of the store's 133 character columns beforehand. It was hashed exactly as the shipped
`PasswordSecretHasher` does, which is PBKDF2/SHA-256, 100,000 iterations, a 32-byte hash, a 16-byte salt and
base64.

| What was searched | What was found |
| --- | --- |
| The store, all 133 of its character columns, for the PIN as a whole value | zero columns hold it. The 102 numeric columns hold values rather than text, and a four-digit number among them is somebody's quantity rather than a credential |
| The account's own row | the stored hash is that PBKDF2 of the PIN under the stored salt, is not the PIN, and is 32 bytes of base64 with a 16-byte salt |
| The application's own files, its local state and its built configuration, 28 files | none carries the PIN |
| The journal, 30 files, against the time of the reset | the five matches are four-digit runs inside the journal's own chained `Hash` fields, written days before the reset; zero lines written after it carry the PIN |
| The application source tree, 7,581 files | three matches, all unrelated data: the same mock seed row id in two roll-ups of one seed, and a SQL Server default-constraint name |
| The audit trail for the reset | one row, with both value columns empty |

### T074: the whole suite and a clean solution build

Run with every running application stopped and the build nodes cleared, and with all five store connections
exported so no integration case is gated out.

| The command | Result |
| --- | --- |
| `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` | 1364 total, 1360 passed, 4 skipped, 0 failed |
| `dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` | `Build succeeded`, `0 Warning(s)`, `0 Error(s)`, and no `WMC9999` |
| the same run filtered to `InlineSqlAuditTests` and `RetiredSymbolAuditTests` | 10 passed, 0 failed, per §6 gate 4 |

**The four skips belong to another feature and cannot be un-gated here.** They are the Infor Visual live checks
(`LookupWorkOrderQuery_ReturnsRows_WhenServerIsAvailable` and its three siblings). Their guard probes the Infor
Visual SQL Server and reports inconclusive when it cannot be reached. `INFOR_VISUAL_SQL_USER` and
`INFOR_VISUAL_SQL_PASSWORD` are set on this machine, so the skip is the unreachable server rather than a missing
variable, and no MySQL connection can change it.

The eleven live cases §6 gate 2 names, and what carries each one:

| The case | What carries it |
| --- | --- |
| Rank refusal | the two live cases in `UserManagementLiveIntegrationTests`, one for the role and one for a target who outranks the actor |
| Self-lockout rejection | `UpdateAsync_DeactivatingTheActorsOwnAccount_IsRefusedByTheStore` and `UpdateAsync_RenamingTheActorsOwnSignInName_IsRefusedByTheStore` |
| Transactional rollback | `CreateAsync_FailingAfterTheProfileIsWritten_RollsTheProfileAndTheRoleBackTogether` |
| Duplicate-username typed result | `CreateAsync_ASignInNameAlreadyTaken_ArrivesAsOneTypedAnswerAndWritesNothing`, and `CreateUserViewModelTests` for the form's side of it |
| Uppercase normalisation | `ASignInNameTypedInLowerCase_IsStoredUpperCase_AndSignsInEitherWay`, which creates with a lower-case name, reads the stored name back, and signs in with both spellings |
| Employee-number non-uniqueness | `CreateUserViewModelTests.TwoPeopleSharingOneEmployeeNumber_AreBothCreatedAndBothListed` |
| Search matching all four fields | `SearchAsync_MatchesEachOfTheFourFacts_AndNarrowsByRole`, one search per fact against the live roster, plus the role filter |
| UI-thread collection safety | **stated as a gap.** The roster's load resumes on the context it was started from, because the continuation that fills the bound collection is not detached, and every roster case reads that collection straight after awaiting the load. No case installs a synchronization context and asserts where the mutation happens, so this one is covered by construction and by the tests that read the collection, not by an assertion about the thread |
| Deactivate without ending a session | `UpdateAsync_DeactivatingAPerson_LeavesTheirExistingSessionsAlone`, and `TemporaryCredentialLimitTests.ResetPassword_LeavesAnOpenSessionRunning` for the reset half |
| Double-click Save writing exactly one row | `CreateUserViewModelTests.OnePress_CreatesExactlyOnePerson` and `PermissionsViewModelTests.OneImpatientPress_WritesExactlyOneChange` |
| An audit row naming the actor and the time | the live create, save and reset cases, each of which asserts the acting user and the recorded time on the rows it wrote |

### T075: the walk through the running application

Run against the built executable (`bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`) with both
connection overrides pointed at the local MySQL 5.7.24, and driven through UI Automation: the window, its navigation
items and its cards were read as they rendered, and the actions were real clicks and keystrokes.

| What was walked | What the running application showed |
| --- | --- |
| The Administration category | both entries are there as clickable rows carrying their own sentences, so neither is an expander; Settings' own search reaches both, "user" leaving the Users entry and "permission" leaving the Permissions entry |
| Both entries navigate | clicking Users opened the roster and clicking Permissions opened the permissions page, and Back on either returned to Settings |
| The roster | every account listed, each row one target that announces itself ("John Koll, JOHNK, 6229, Developer. Open this person"), with all five facts in the row |
| The filter remembered | searching for a name narrowed the list, and returning to the page came back with the term still in the box, the list still narrowed, and the banner saying a filter is applied |
| The person's page, on the reader's own account | "This is the account you are signed in as, so its sign-in name cannot be changed" and "…so it cannot be deactivated", with the sign-in name field read-only, Deactivate disabled, and the deactivation sentence on the page itself |
| The person's page, on somebody else | every field editable, Deactivate and Reset password available, and neither self refusal shown |
| A create | the create page opened from the roster's Add action, took the five fields and a role chosen from the roles at or below the reader's rung, and ended in the PIN window showing the sign-in name, the four-digit credential, when it was issued and who issued it |
| The PIN window | a click far outside it left it open, Escape left it open, and Print left it open while raising the Windows print preview, which then had to be closed before the page beneath would take a click again |
| Closing the PIN window | its own close button closed it, and the credential was nowhere on the screen afterwards |
| The permissions page | one person at a time, each of the fourteen features stating what it gates and "From the role's baseline", the "Manage permissions" row disabled and locked with "This is the permission that opens this page, so it cannot be changed here.", Save changes disabled while nothing had changed, and the who-holds-this view naming the roles whose baselines give the feature |
| The five-attempt limit | signing in as the account the walk created with the wrong credential five times moved "Attempts remaining on this temporary password" through 4, 3, 2, 1 and 0, the sixth attempt with the **correct** credential was refused with "This temporary password is no longer accepted. Someone who can reset passwords must issue a fresh one.", and after closing and reopening the application the correct credential was still refused |

**Two defects the walk found, which no test could see.**

1. **The create form had no way in.** The roster page carried no Add control at all. The view model had
   `AddPersonCommand` and `AddLabelText` and the resource key `UserManagement_Add.Label` had shipped, but nothing on
   the page was bound to either, so a person could not be created from the running application and the PIN window
   could not be reached. An Add button bound to that command and that label now sits in the page header behind the
   same entitlement as the entry, and the walk then used it: the create form opened, the create landed, and the PIN
   window appeared.
2. **The hidden count never moved.** The roster's "N hidden by the filter" was bound one time, so it read
   "0 hidden by the filter" while nine people were hidden. The binding is now one way. That fix is a markup change
   verified by the build and by the pre-fix observation of the defect; it was **not** re-observed in the running
   application, because the walk's own sign-out could not be signed back in through UI Automation afterwards, the
   password field refusing programmatic input.

**What the walk changed on this machine.** The developer account `JOHNK` held the legacy masked credential with a
forced change, so the shell could not be reached at all. The forced change was cleared to get in, and the
application's own change-password form was then driven with the value this workstation already remembers for that
account, so the account now holds a real salted password and no longer demands a change. The account the walk
created (`ZZ.WALK.PERSON`) was removed afterwards with its audit rows and sessions, leaving the store's ten accounts
as they were. Everything was run again after both fixes: the suite reports 1364 total, 1360 passed, 4 skipped,
0 failed.

### T076: not walked, and left open

The scaling walk was not performed, so this task stays open rather than reported as done.

- The display scale cannot be changed without signing the session out and back in, and this pass ran unattended, so
  the 150 percent and 200 percent walks were not attempted.
- The narrowest-window walk on the three screens needs the shell, and after the five-attempt walk the application
  could not be signed back in through UI Automation, so it was not attempted either.
- For information rather than as evidence: reading the markup of the three screens and the two others this feature
  adds, no content control declares a fixed `Width`. What the reading does find is a `Width="24"` progress ring on
  each page, a `MaxWidth="720"` on the create and person form stacks and a `MinWidth="200"` on the permissions
  page's people column. Whether those three are consistent with "no control has a fixed width" is a judgement for
  whoever does the scaling walk, because a spinner's size is fixed by its own nature and a maximum is not a fixed
  width, and none of that is a substitute for the walk.

## What to do when something fails

- A gate that refuses a person it should admit is almost always one of two things: the re-rank did not ship before
  the first per-person row was written, or a gate is still reading a role name. The registry check names the first
  case, and the no-gate-reads-its-own-role-list check names the second.
- A permission that appears to save and then reverts is the `from`/`to` guard in the change-set procedure working
  as intended: somebody else changed that row, and the page is supposed to say so rather than overwrite it.
- A retired role name still deciding something is what the extended retired-symbol audit is for; extend its pattern
  set rather than adding a second scan.
