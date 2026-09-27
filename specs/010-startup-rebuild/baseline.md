# Baseline before the startup removal

This file is the output of Phase 1 of `tasks.md`: **T002** (the pre-removal build and suite result, with the
skipped count) and **T003** (the current splash and sign-in window geometry and their automation names).

Both are recorded before any part of the old startup surface is removed, because neither can be reconstructed
afterwards. The build and suite figures are the comparison point for the removal gate; the window figures are
the comparison point for the post-removal proof that no launch path still reaches the removed windows.

## The state this was taken against

| What | Value |
| --- | --- |
| Branch | `010-startup-rebuild` |
| Commit | `ec4ea89` |
| Working tree | 56 entries dirty (the feature's spec, plan, tasks and contract artifacts, plus unrelated XAML work). No startup-removal work is present in it. |
| Configuration | `Debug`, `Platform=x64` |

## T002 — build and suite

**Build.** `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:33.17
```

Exit code `0`.

**Suite.** `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64`

```text
Passed!  - Failed:     0, Passed:  1507, Skipped:    55, Total:  1562, Duration: 32 s - MTM_Waitlist.Tests.dll (net10.0)
```

Exit code `0`.

**The skipped count is 55.** The reason matters more than the number: the database-backed integration tests
report inconclusive rather than failing when `MTM_WAITLIST_TEST_DB_CONNECTION_STRING` is not set, which is
stated in their own documentation as a deliberate choice "so the suite stays green offline". All 55 skipped
tests in this run are that class of test. The removal gate's later run (T034) compares against these figures,
so a run that suddenly reports 0 skipped is a run with a live store attached, not a run that got better.

## T003 — splash and sign-in window geometry

The app was launched from the built artifact
(`bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe`) and its top-level windows were sampled
through UI Automation. Three windows were observed across two launch passes.

| Window | Automation name | Size | State | First seen |
| --- | --- | --- | --- | --- |
| Splash | `WinUI Desktop` | 760x460 | normal (`showCmd` 1) | about 0.6 s after launch |
| Shell (main) | `MTM_Waitlist` | 3456x1408, restore 760x500 | maximized (`showCmd` 3) | about 3.6 s after launch |

**The splash window's automation name is the framework default, `WinUI Desktop`, not a name of this
application's choosing.** It is not a stable handle and must not be used to locate the window. The same caveat
is already recorded in `.github/instructions/winui3-ui-automation.instructions.md`.

**The sign-in form does not appear by itself on this machine, and the reason is local state rather than the
store.** Two ordinary launch configurations were tried first, and both reached the main screens without
presenting a form: one against the configured store, which is unreachable from here, and one against the local
MySQL instance at `127.0.0.1:3306`. The cause is that
`%LOCALAPPDATA%\MTM_Waitlist\ApplicationData\LocalSettings.json` carries both `Startup.Session.Token` and
`Startup.Session.ExpiresUtc`, so the launch finds a live remembered session and routes straight past the form.

**It was captured on a second pass.** That file was backed up, those two keys were removed, and the app was
launched again. The form appeared at once.

| Window | Automation name | Size | State |
| --- | --- | --- | --- |
| Sign-in | `Sign in` | 820x760 | normal (`showCmd` 1) |

Its visible text is `Sign In`, `Use your assigned credentials to continue.`, `Username`, `Password` and
`Sign in to continue.`; its buttons are `Sign In` and `Back`.

The local settings file was then restored from the backup and verified byte-identical by SHA-256
(`7EB322B193BB78C43BBD69DE98C3F3FE5BE47C0E21118BB690E8EFBA41B6A4D0`), and the backup was deleted. The machine's
application state is exactly as it was before the capture.

**Consequence.** The post-removal comparison now has a pre-removal figure for all three launch windows. The
sign-in figure was taken with a reachable store and no remembered session; the splash and main-window figures
were taken against the configured store.

## What this baseline does and does not cover

It covers the build, the suite and all three launch windows, on this machine, at this commit.

It does not cover anything that needs the shared store. The store the
application is configured for, `172.16.1.104:3306` (`V-MTMFG-5.mantoolmfg.com`, the shared MySQL and Infor
Visual host), does not accept a connection from this machine: a TCP probe to port 3306 times out after five
seconds. A separate MySQL listens on `127.0.0.1:3306` here.

That gap is the single most important fact for the rest of this feature. Four later tasks cannot be completed
or verified from this machine without the shared store:

- **T034** runs the full suite and the schema validator against a live store.
- **T063** runs the schema validator against a live store after the deletions.
- **T024** confirms the protected shared artifacts were not touched, which is a read against the live schema.
- **T033** proves by automation that no launch path reaches the shell, the sign-in form or a store read.

Everything else in Phases 1 and 2 that touches only source can still be built and verified here.
