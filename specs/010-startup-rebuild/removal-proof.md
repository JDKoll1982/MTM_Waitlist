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
