# Removal proof

This file is the output of **T033** (prove by automation that no launch path reaches the shell, the sign-in form
or a store read) and **T034** (run the full suite and the schema validator against a live store, compare against
the Phase 1 baseline, and account for every deleted test). Both are Phase 2 / Phase 3 tasks of `tasks.md`, and
both were blocked until the old startup surface had actually been removed.

`baseline.md` is the comparison point: it recorded the build, the suite and all three launch windows *before* the
removal. This file records what the tree looks like after it.

## The state this was taken against

| What | Value |
| --- | --- |
| Branch | `010-startup-rebuild-removal` |
| Commit | `5a6e3fa` ("Remove the old startup surface and prove the removal") |
| Working tree | 61 entries dirty: the 59 that belong to the owner, `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`, which this unit extended, and this file. No other file was touched. |
| Configuration | `Debug`, `Platform=x64` |
| Artifact driven | `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`, built from this tree at 15:53 local |

---

## T033 — the launch path reaches nothing but the placeholder

### What was run

The built executable was launched the way a person launches it, with **no connection overrides**: neither
`MTM_WAITLIST_DB_CONNECTION_STRING` nor `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` was set for the process. Its
top-level windows were then read through UI Automation, and the placeholder's own Close button was invoked with
`InvokePattern`.

### The windows

| Window | Automation name | Size | State | Text | Buttons |
| --- | --- | --- | --- | --- | --- |
| Placeholder | `MTM Waitlist` | 2580x1023, restore 2580x1023 | normal (`showCmd` 1) | 1 text element | 1 `Close` button |

Exactly **one** top-level window belonged to the process. Its single text element read:

```text
Startup was removed from this build. This window is a placeholder until the rebuilt launch pipeline lands.
```

Its single button reported automation id `StartupPlaceholder_CloseButton`. There were **no edit fields and no list
items anywhere in the window**, which is the structural reason the sign-in form cannot be reached from here: the
form was made of them, and none exists.

### The absence of the two removed destinations

Each marker below was counted across every named automation element of every window the process owned.

| Marker | Count |
| --- | --- |
| `Waitlist for` (the shell header) | 0 |
| `Sign In` / `Sign in` / `Sign in to continue` | 0 |
| `Username`, `Password` | 0 |
| `Sign In` and `Back` buttons | 0 |

Against the baseline this is a clean removal. `baseline.md` recorded three windows across two launch passes —
the splash (`WinUI Desktop`, 760x460), the shell (`MTM_Waitlist`, 3456x1408 maximized) and, on a second pass with
the remembered session removed, the sign-in form (`Sign in`, 820x760). After the removal there is one window, it
is not any of those three, and it is the placeholder.

### Closing it

The Close button was invoked through `InvokePattern`. The process ended **0.2 s** later, and
`Get-Process MTM_Waitlist` afterwards reported **0** instances, so no orphan is left holding a store connection.
The local store's own session list confirms the same thing: the session the app held disappeared with it.

### The caveat this run found — read this before trusting "no store read"

The unit's claim is that no launch path reaches a store read. **That claim holds for the internal operational
stores and for the shell and sign-in path, and it does not hold in the strictest reading.** The exception is real,
it is named here rather than omitted, and it was not fixed in this unit.

`App()`'s constructor still starts `IVisualReachabilityProbeHost` (`App.xaml.cs`). That host's loop runs on a
background thread and, on its first iteration, does two things:

1. **A connectivity probe against Infor Visual** — `VisualConnectivityProbe` opens a `SqlConnection` and
   immediately closes it, issuing no statement at all, with its own 2-second ceiling and retries disabled. It
   reads nothing.
2. **A read of the external-read cache.** `ReadStatusProvider.RefreshAsync` calls
   `sp_visual_read_shape_freshness_get` through the MySQL helper server targeted at the **`mtm_mock`** mirror, to
   report how old the cached data is.

Item 2 is a store read, and it was observed rather than inferred. While the app was running,
`Get-NetTCPConnection -OwningProcess <pid>` reported exactly one established connection, to `::1:3306` from local
port `61183`, and the local store's `information_schema.processlist` reported the matching session
`localhost:61183` with `DB = mtm_mock`. When the app closed, that session disappeared from the list.

Two facts about it matter to the judgement:

- **It reaches the local server because of the existing host fallback, not because of anything this feature did.**
  The application ships configured for the shared plant host `172.16.1.104:3306`, which does not answer from this
  machine. `MySqlHostFallback` substitutes `localhost` when the configured host is unreachable and the local one
  answers, so the mirror read landed on the local MAMP MySQL, where `mtm_mock` exists.
- **Nothing about it produces a window, delays the placeholder, or touches an internal store.** It runs on a
  background thread, the placeholder appears regardless of its outcome, and its target is the cache mirror for
  external reads — never `mtm_waitlist`, `mtm_wip_application_winforms` or `mtm_receiving_application`.

**Judgement.** No launch path reads an internal store, and no launch path reaches the shell or the sign-in form;
that part of the claim is proved. The external-read machinery, however, does issue one probe and one mirror read
per probe cycle from the constructor. If the claim is read as "the launch path performs no store operation of any
kind", it is **false**, and the correct fix belongs to the launch-pipeline rebuild rather than to this unit.

### The startup log is not evidence for this run, and here is why

The newest entry in the redirected log file
(`C:\Users\johnk\OneDrive\Documents\MTM Waitlist\Logs\startup_forwarded_2026_09_26.jsonl`) is timestamped
14:43 local — before this run and before the removal commit — and nothing was appended by this run. The reason is
that the hosted log service's registration was removed with the rest of the old startup surface (T008), so
`StartupDebugLog.Configure(...)` receives no implementation. The UI Automation reading above is therefore the
evidence for this run; the log is not.

---

## T034 — the removal gate

### Build and suite against the baseline

| Gate | Baseline (before the removal) | After the removal (this run) | Difference |
| --- | --- | --- | --- |
| Build | `0 Warning(s)`, `0 Error(s)`, exit 0 | `0 Warning(s)`, `0 Error(s)`, exit 0 | unchanged |
| Suite | `Failed: 0, Passed: 1507, Skipped: 55, Total: 1562`, exit 0 | `Failed: 0, Passed: 1425, Skipped: 46, Total: 1471`, exit 0 | −91 total, −82 passed, −9 skipped |

Commands, verbatim:

```text
dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false
dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64
```

### Accounting for every deleted test

The count is of `[TestMethod]` declarations, taken from `5a6e3fa^` for each file and from the working tree for
each file that still exists. Nine test files were deleted outright and three surviving ones lost cases.

**Deleted test files — 83 declarations removed:**

| Declarations | File |
| --- | --- |
| 4 | `MTM_Waitlist.Tests/Module_Startup/Services/SignOutServiceTests.cs` |
| 6 | `MTM_Waitlist.Tests/Module_Startup/Services/StartupArchiveCleanupTests.cs` |
| 9 | `MTM_Waitlist.Tests/Module_Startup/Services/TemporaryCredentialLimitTests.cs` |
| 5 | `MTM_Waitlist.Tests/Services/ComputerGateServiceTests.cs` |
| 13 | `MTM_Waitlist.Tests/Services/ComputerRegistryServiceTests.cs` |
| 20 | `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` |
| 2 | `MTM_Waitlist.Tests/Services/StartupRecoveryServiceTests.cs` |
| 22 | `MTM_Waitlist.Tests/ViewModels/LoginViewModelTests.cs` |
| 2 | `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs` |

**Surviving test files that lost cases — 12 declarations removed:**

| Before → After | File | Why |
| --- | --- | --- |
| 13 → 9 | `MTM_Waitlist.Tests/Core/Models/CoreModelsTests.cs` | cases for the deleted startup models |
| 10 → 4 | `MTM_Waitlist.Tests/Core/Models/StartupOptionsModelsTests.cs` | cases for the deleted startup options |
| 4 → 2 | `MTM_Waitlist.Tests/Models/StartupModelsTests.cs` | cases for the deleted startup models |

**The reconciliation closes exactly.** 83 + 12 = **95** declarations removed by the removal. This unit added
**4** (the four removal checks in `RetiredSymbolAuditTests`). 1562 − 95 + 4 = **1471**, which is the total the
runner reported. Every deleted test is therefore accounted for by name; the four additions are the T001/T004/T005
guard.

### The skipped count

Skipped fell from **55 to 46** — nine tests, and the nine have one cause. `TemporaryCredentialLimitTests` holds a
guard in its `[TestInitialize]`:

```text
Assert.Inconclusive($"{ConnectionStringVariable} is not set; skipping the live attempt-limit proof.");
```

That one line made all nine of that class's tests report inconclusive, because MSTest honours it for every test
the fixture sets up. Deleting the class therefore removed exactly nine skips. **No surviving test changed its
gate**, and the 46 that remain skipped are the same class of environment-gated integration test the baseline
described: they report inconclusive rather than failing when their live connection string is absent, so the suite
stays green offline. They were counted as skipped, never as passing.

### The schema validators against the live local store

Run against the live local MySQL at `127.0.0.1:3306`, database `mtm_waitlist`, through
`C:\MAMP\bin\mysql\bin\mysql.exe`.

| Validator | Client exit | Issue rows | Result |
| --- | --- | --- | --- |
| `Database\Validation\startup_schema\validate.sql` | 0 | 0 | **pass** |
| `Database\Validation\settings_schema\validate.sql` | 0 | 0 | pass |
| `Database\Validation\user_management_schema\validate.sql` | 0 | 0 | pass |
| `Database\Validation\part_pictures_schema\validate.sql` | 0 | 3 | three pre-existing issues, see below |

**The convention, stated because it was mis-read once.** Every validator selects **one row per problem**, so an
empty result set is a pass. `mysql --batch` still prints the column header, so a clean validator produces one
header line and no data rows. The two harness scripts left in the temp folder treated that header as a row and
exited `1` on a clean validator; this run read the first non-empty line as the header and counted only the lines
after it, which is why the three clean validators above report `0` issue rows.

**The three issues in `part_pictures_schema` are not a regression.** They are `stale_routine_body` findings
against `sp_config_images_locations_insert`, `sp_config_images_locations_paths_move` and
`sp_config_images_locations_update`. They are staleness that predates this feature, they concern picture-location
routines that this removal never touched, and they are reported here as pre-existing rather than presented as
damage the removal caused.

### The baseline's own caveat, resolved

`baseline.md` stated that T033 and T034 could not be completed from this machine without the shared store, because
the store the application is configured for (`172.16.1.104:3306`) does not accept a connection from here. That is
still true of the *shared* store, and it is why the mirror read above fell back to localhost. It is not a barrier
to either task: T033's automation needs no store at all, and T034's validator run needs a live schema, which the
local MAMP instance supplies.

---

## What this proof does not cover

- **The database half of the removal is still outstanding.** T020, T021, T023 and T024 have not run, so the nine
  startup-only procedures are still deployed and their `create.sql` comments still name the retired
  `StartupSessionRepository`, and the startup validator is still the pre-removal one. Two retired-symbol patterns
  are narrowed in a documented way because of that: the session-repository pattern excludes a backtick-quoted
  comment mention, and the sign-in-session-keys pattern excludes a `SignInSessionKeys.cs` file-path entry in
  `ShellPage.xaml` that T019 owns. Both still bite on a real reintroduction.
- **T026 has not run**, so `capabilities/startup/` and `capabilities/startup-diagnostics/` are still in the tree.
- **Nothing here is a statement about the shared deployment.** The shared plant store is unreachable from this
  workstation, so the validator results are the local schema's, not the shared one's.
- **The reachability probe is untouched**, as the unit required. The caveat above is a finding to carry into the
  launch-pipeline rebuild, not something this proof repairs.

## T056 — the dead-weight enumeration

Subphase 5.6 makes both of its deletions depend on this enumeration, and it had not been run — which is why two
attempts at T060 and T061 were stopped by consumers the plan had not accounted for. This is that step.

### How it was produced

Re-runnable, not asserted. One script walks the four artifact directories, reads each `create.sql` for the object it
declares, then counts that object's name across six corpora: the application's own `.cs`/`.xaml`/`.resw`, the test
project's, `Database/Seeds`, `Database/Validation`, `Database/Bootstrap`, and the `All*.sql` aggregates. The script
was written for this run and is not committed.

One limitation, found and corrected while running it: the first pass left **procedure bodies out of the corpora**, so
an object used only by another procedure read as unreferenced. Two objects did exactly that — `vw_setup_work_centers_active`
is used by `sp_setup_work_centers_catalog_get` and `sp_setup_work_centers_get_all`, and
`fn_setup_work_center_name_normalized` by `sp_setup_work_centers_upsert`. A second search over the SQL artifacts put
both back. **A name-absence result is only as good as the corpora it searched.**

### The counts

| Kind | Folders | Deployed (have `create.sql`) | Rollback-only |
|---|---|---|---|
| table | 35 | 25 | 10 |
| procedure | 131 | 90 | 41 |
| function | 3 | 3 | 0 |
| view | 2 | 2 | 0 |
| **total** | **171** | **120** | **51** |

120 deployed objects is the figure the repository's own duplicate-object check reports, derived here independently —
two methods agreeing on the number.

### The consumerless set

Eight deployed objects have no mention in application code, test code, seeds, validation, the bootstrap file or an
aggregate. They are not eight pieces of dead weight, and the difference matters.

**Five were created by this feature and have no consumer yet because their consumers are unbuilt.** None of the five
exists on the owner's branch or on master, which is the proof: `sp_ops_startup_logs_filter`,
`sp_ops_startup_logs_fingerprint_groups_get` and `sp_ops_startup_logs_purge` — the log panel's readers, whose callers
arrive with the reporting panel — and `sp_config_images_locations_computer_sources_get` / `_set`, the machine
configuration's picture sources. **These must not be read as deletions; deleting them would destroy this feature's own
work.**

**One is pre-existing and genuinely unreferenced:** `sp_waitlist_request_get`, which appears only in its own folder and
in `AllSPs.sql`. No task names it and nothing calls it.

**Two were initially reported consumerless and are not**, for the corpus reason above.

### The four objects the plan names, and who consumes them

| Object | Consumers found |
|---|---|
| `auth_sessions_tokens` | `MTM_Waitlist.Tests/Module_Settings/Services/UserManagementLiveIntegrationTests.cs` lines 552, 570, 603 — an environment-gated live integration test inserts, cleans up and reads this table back |
| `core_buildings_catalog` | `Database/Seeds/seed_dev_masked_baseline/create.sql` 280, 283, `.../rollback.sql` 27, `Database/Seeds/AllSeeds.sql` 357, 360, `Database/Validation/settings_schema/validate.sql` 33, 39, and the bootstrap descriptions file |
| `core_buildings_history` | `Database/Validation/settings_schema/validate.sql` 113, 119, and the bootstrap descriptions file |
| `sp_core_buildings_upsert` | `Database/Validation/settings_schema/validate.sql` 156, 162 |

The seed consumer is the sharp one: the schema validator applies every seed on its success path, so while a surviving
seed writes `core_buildings_catalog`, the live validator cannot pass. "Delete the table" and "the validator passes" are
in direct tension until the seed stops writing it.

### What the approval covers

The owner approved the destructive database work on **2026-09-26**, on the basis that git can restore the schema
artifacts and the local store can be rebuilt from the install scripts. That is the confirmation the Security & Secrets
constraint requires at this second point. It was granted before this enumeration existed, and is recorded here so the
basis of the deletions is auditable rather than assumed.

### What this implies for T060, T061 and T062 as written

- **T060 is executable, and its one consumer is already owned.** The test file belongs to T188, which exists to
  "re-point or retire" those cases; T188 is unticked and last in the plan. Deleting the table while T188 is pending
  leaves that test's SQL naming a table no artifact creates, which fails only when the test runs against a live store.
- **T061 is not executable as written.** Two of its consumers — the seed and `settings_schema/validate.sql` — are named
  by no task in the plan. Removing the three buildings objects requires removing the seed's writes to them and the
  validation assertions, and neither edit is authorized anywhere.
- **T062's file list is incomplete** for the same reason: it names the aggregates and the descriptions file, but not the
  seed or the settings validation script.

### Uncertainties, recorded rather than resolved

- `sp_waitlist_request_get` is unreferenced by name, but a procedure could reach it through a name built at runtime.
  Nothing in the six corpora suggests one; settling it needs the live store's routine dependency view, not text.
- Consumers in environment-gated live tests are counted here by text, because those tests only execute when their
  connection-string variable is set. A skipped count is not a consumer count.
- This section was written after T021–T024 and most of T020 had run, so the "still outstanding" bullet above is stale
  as of this section: six of the nine startup-only procedures are retired and the startup validator has been reworked.
