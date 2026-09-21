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

## What to do when something fails

- A gate that refuses a person it should admit is almost always one of two things: the re-rank did not ship before
  the first per-person row was written, or a gate is still reading a role name. The registry check names the first
  case, and the no-gate-reads-its-own-role-list check names the second.
- A permission that appears to save and then reverts is the `from`/`to` guard in the change-set procedure working
  as intended: somebody else changed that row, and the page is supposed to say so rather than overwrite it.
- A retired role name still deciding something is what the extended retired-symbol audit is for; extend its pattern
  set rather than adding a second scan.
