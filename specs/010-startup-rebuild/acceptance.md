# Acceptance runs for the rebuilt launch

This file is the output of **T161** and **T168**: the two runs that can only be made against a running application
rather than against a test. Everything here was observed by launching the built artifact and reading its windows
through UI Automation, with the store pointed at deliberately broken things.

The two tasks divide the work like this. **T161** proves the machine-setup gate appears on a machine the store
has no configuration for, and cannot be bypassed. **T168** proves the launch stays honest and bounded when what it
reaches for does not answer, and compares its closing behaviour against the pre-removal figures in
`baseline.md`.

## The state these were taken against

| What | Value |
| --- | --- |
| Branch | `010-startup-rebuild-removal` |
| Commit | `14c54ed` |
| Working tree | 154 entries dirty — the rebuild's source, its tests and its spec artifacts. |
| Configuration | `Debug`, `Platform=x64`, unpackaged desktop build |
| Artifact | `bin\x64\Debug\net10.0-windows10.0.19041.0\win-x64\MTM_Waitlist.exe` |
| Launch window's automation name | `MTM Waitlist` |
| Store | the local MySQL instance, `mtm_waitlist`, which carries this feature's schema |
| How the store was aimed | `MTM_WAITLIST_DB_CONNECTION_STRING` and `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` set in the launching shell |
| Credentials | never printed, never written into the repository |

**The launch window's name is now a name of this application's choosing.** It is `MTM Waitlist`, and every case
below locates the launch window by that name. The pre-removal splash could not be located this way at all — see
the comparison section, where that difference is recorded rather than smoothed over.

**A note on how the store was made to fail.** An unreachable host and an unreachable store are not the same thing
here. The application substitutes this machine's own store when the configured host does not answer, so pointing
it at a black-hole address does not produce an unreachable store — it produces a store that answers, finds this
machine unconfigured, and shows the setup screen. Case A therefore uses a port that is closed on both hosts, and
case B uses a socket that accepts a connection and then says nothing at all.

## T161 — a machine the store has no configuration for

**Setup.** `config_images_locations` carries no row for this machine and no row at all, so the store holds no
picture source for it. `core_computers_registry` holds a record for this computer, so the launch finds the
computer and then finds it unconfigured — which is the state the gate exists for.

**Result: the gate appeared, and nothing else did.**

| Observed | Value |
| --- | --- |
| Windows seen, in order | `MTM Waitlist` at about 0.4 s, then `Set up this computer` at about 0.7 s |
| Shell windows during the run | **none** |
| Local files under `%LOCALAPPDATA%\MTM_Waitlist` | 2 before, 2 after |

The setup surface states the position in its own words:

> This computer has not been set up, so it cannot be used until someone with IT Department or Developer authority
> names it and points it at its shared pictures.

Its controls, read by automation id, are `MachineSetupWindow_Root`, `_Heading`, `_Subtitle`, `_SignInName`,
`_Credential`, `_SignInButton`, `_CancelButton`. The presence of the sign-in name and credential fields is the
point: the only way past this screen is an authorised person naming the computer.

**Result: the gate could not be bypassed, and its route out is explained.**

The screen's own `Close setup` control was invoked. The launch then stated its refusal rather than closing
quietly:

> Nobody with IT Department or Developer authority signed in to set this computer up, so the application is
> closing. Open it again to set this computer up.

That statement was shown with a `Close` button, and the process ended after it was dismissed. Every route out of
this screen ends the application, which is the behaviour SC-006 asks for and which the abort route confirms here
rather than only in a test.

**Verdict: T161 passes.** Against a machine with no configuration, the launch showed the gate, opened no main
screen, wrote nothing to the machine's own state, and on refusal said why it was closing. This is SC-004.

## T168 case A — the store cannot be reached at all

**Setup.** The connection string was aimed at port 3399 on the configured host, a port that is closed on that
host *and* on this machine, so the substitution cannot rescue it.

**Result: the launch showed its feed, then stopped at its own stated maximum, and persisted nothing.**

| Observed | Value |
| --- | --- |
| Feed lines | reading this computer's settings, that the saved settings name the store, then contacting the store |
| Stop window | `The launch stopped`, at **+17.5 s** |
| The cause it stated | Contacting the store did not finish within the 15 second(s) it is allowed. |
| Actions offered | `Close` and `Try again` — nothing else |
| Local files under `%LOCALAPPDATA%\MTM_Waitlist` | 2 before, 2 after |

**The actions offered are the substance of this result.** The stopped launch knew the store had not answered, so
it offered the only two things that could help and offered no reset. A reset offered here would be an action that
cannot remove the cause, which SC-010 forbids.

**Verdict: T168's unreachable-store case passes.** The launch stated a cause rather than stopping silently
(SC-001) and held itself to the step's own 15-second maximum (SC-002).

## T168 case B — the store accepts a connection and then says nothing

**Setup.** A listener on this machine accepted TCP connections and never wrote a byte to them, so the client sits
waiting for a greeting that never comes. The connection string's own timeout was set to **60 seconds**,
deliberately four times longer than the launch's stated maximum, so that whichever limit ended the wait would be
unambiguous.

**Result: the launch's own maximum ended the wait, not the connection timeout.**

| Observed | Value |
| --- | --- |
| Feed lines | the same three lines as case A |
| Stop window | `The launch stopped`, at **+15.6 s** |
| The cause it stated | Contacting the store did not finish within the 15 second(s) it is allowed. |
| Local files under `%LOCALAPPDATA%\MTM_Waitlist` | 2 before, 2 after |

The listener's own log corroborates the wait from the other side: it recorded accepting the connection at
14:32:14, the same second the launch started, and then holding it open without answering until the launch gave up.

**Verdict: T168's slow-store case passes.** A store that answered nothing held the launch for 15.6 seconds, not
for the 60 seconds the client was willing to wait. The launch's stated maximum is what ends a stalled wait.

## T168 case C — a stalled share: not run, and why

**This case has not been run.** It is recorded here as outstanding rather than as passed.

The step that reaches the picture share is `picture-cache`, and it is the fifteenth of the catalogue's seventeen
entries — it runs after the session steps. The launch stops for a sign-in before it resolves who is signed in,
and it stops again before opening the shell, so no run reaches `picture-cache` without a person actually signed
in.

**A person cannot be signed in on this machine's store.** Every account in `core_users_profiles` carries the
temporary-credential marker, whose credential is a four-digit value shown once and never stored, so there is
nothing in the store to sign in with. Producing one would mean writing a credential into the store, which would
make the run evidence about a fixture this work invented rather than about the application. That was not done.

**What is already proven about this bound, and what is not.** The mechanism is covered by the suite: the catalogue
refuses a best-effort step bounded at the ceiling, exactly two steps are best-effort and both sit inside it, the
picture-share step is best-effort with the share named as its target, and a best-effort step that ignores its own
bound is proven not to hold the launch. What is **not** covered is the end-to-end observation: a real launch,
with a real signed-in person, meeting a share that answers nothing and carrying on regardless.

**The bound this case would test is the tighter one.** Both cases above stop the launch, because the store is on
the launch path. The share is not: `picture-cache` is best effort and bounded at 10 seconds, so the expected
result is not a stop but a launch that carries on and records the step as skipped. That is a different outcome
from either case above and is exactly why it cannot be inferred from them.

## Comparison against the pre-removal baseline

`baseline.md` holds the figures for the launch surface as it was before this feature. Every difference is
accounted for below.

| What | Before | After | Why it differs |
| --- | --- | --- | --- |
| Build | 0 warnings, 0 errors | 0 warnings, 0 errors | Unchanged. |
| Suite, passed | 1507 | 1744 | 228 tests are new in this feature, and 9 more ran than before because a store was attached. |
| Suite, skipped | 55 | 46 | The skipped class is the database-backed tests that report inconclusive when no store is configured. Nine of them ran this time because this run had one attached, so the drop is a live store rather than a better suite. |
| Suite, total | 1562 | 1790 | The 228 new tests. |
| Suite, failed | 0 | 0 | Unchanged. |
| Launch window's automation name | `WinUI Desktop` | `MTM Waitlist` | The old splash kept the framework's default name, which `baseline.md` records as "not a stable handle and must not be used to locate the window". The rebuilt launch window names itself, so it can be located. |
| Time to a usable surface | about 3.6 s to the shell | about 0.7 s to the setup screen on an unconfigured machine | Not comparable as a straight improvement. The before figure was taken on a machine whose store did not answer and whose local settings carried a live session; the after figure is a machine the store has no configuration for, so the launch stops earlier by design. |
| What a stopped launch shows | no equivalent surface | `The launch stopped`, stating a cause and two actions | New. There was no stopped-launch surface before, so SC-001 had nothing to measure against. |
| Bounds on waits | no stated maximum | 10, 15 and 30 seconds, per step | New. Cases A and B measure the 15-second bound holding at 17.5 s and 15.6 s from process start. |
| A stalled store | held until the client gave up | stopped at the launch's own maximum | This is case B, measured with a client timeout set four times longer than the bound. |

**The suite figures reconcile exactly**: 1744 pass and 46 skip is 1790, which is 1562 plus the 228 tests this
feature added. Read the skipped count as "how many database-backed tests found a store", never as a quality
figure — a run reporting 55 skips is a run with no store attached, which is how the baseline was taken.

**One difference is not a regression and one is not an improvement.** The time-to-surface row changes in both
directions at once, so it is recorded as what it is — a different machine state — rather than claimed as a
speed-up.

## The four defects these runs found

These runs are the first time the rebuilt launch was actually started as an application, and they found four
defects that no test had caught. Each is recorded here because a later change could reintroduce any of them, and
the fix for each is now guarded.

| Defect | What it looked like | What caused it |
| --- | --- | --- |
| Nineteen registered types could not be built | the launch died before its first step, reported as an unhandled exception naming a step's constructor | a container resolves a type through its public constructors only, and these had internal ones |
| The log writer logged its own writes | 21,663 of 21,678 rows in 55 seconds were the writer describing its own call | nothing excluded the writer's own work from the logging seam it writes through |
| The hand-over never reached the screen | the log said the launch had ended and no window appeared; the process exited reporting success | the launch ran without returning to its caller's context, so it finished on a background thread where opening a window cannot work |
| The process became windowless and exited | the same silent exit, after the first fix | the launch window was closed before the next surface was shown, leaving a moment with no window at all |

The last two are the reason the failure was silent: a process with no window and a success exit code looks like a
clean shutdown. The guard added for the first defect asks every registered startup type whether a container could
build it, which is a question the suite now asks on every run.

## What these runs do not cover

- **The shared store.** Everything here was run against this machine's local copy of the schema. The shared host
  does not accept a connection from this machine, which `baseline.md` and `concerns-log.md` both record.
- **A signed-in launch.** No case here involves a person signed in, so the steps after the sign-in gate — the
  picture refresh and the external-system priming — were not exercised by a run.
- **The stalled-share case**, for the reason given above.
