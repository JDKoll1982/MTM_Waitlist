# Phase 0 Research: Identifier-First Sign-In and a Faster Launch

## D1 - How concurrency is expressed

**Decision**: The pipeline keeps a single ordered walk, but a catalogue entry may declare that it belongs to a
parallel group. Steps in the same group are started together with `Task.WhenAll`, each still run through the
existing runner under its own stated maximum. The group is over when every member has ended.

**Rationale**: The runner already guarantees "one step, one stated maximum, one terminal line". Running the same
runner calls concurrently over one append-only feed needs no change to the runner or the feed, and a step that
overruns is still reported individually. Rewriting the runner to own concurrency would put the bound logic and
the scheduling logic in the same place and make the bound harder to verify.

**Alternatives considered**: A general task graph (rejected: the sequence is short and fixed, and a graph is
machinery for a problem this change does not have). Making every step concurrent (rejected: identity resolution
must precede the credential and session checks, and the shell must follow everything).

## D2 - Where the sixth read shape's module comes from

**Decision**: Add a value to `VisualReadShapeModule` for the launch area, whose folder name is `Module_Startup`.
The shape's source script lives under `Database/InforVisual/Queues/Module_Startup/Queries/`.

**Rationale**: The shape belongs to the launch surface, not to setup or the waitlist. Filing it under an
existing module would make the catalog lie about who owns the read, and the module value already exists to name
exactly this.

**Alternatives considered**: Reusing `Module_Waitlist` (rejected: dishonest ownership, and the populations and
queries folders would mislead the next reader).

## D3 - How set-up bypasses the temporary credential

**Decision**: Account creation writes the chosen PIN as the stored credential through one new procedure that
inserts the profile, the credential and the rank assignment together. The existing temporary-credential check
and the new-PIN write are untouched, and the account is created with no temporary flag, so the launch never sees
a temporary credential for it.

**Rationale**: The spec makes "the existing password-reset logic must not be disturbed" a hard requirement.
Reusing the temporary-credential path would change that logic's inputs; writing the final credential directly
leaves it alone.

**Alternatives considered**: Creating the account on the default PIN `4330` and immediately driving the
new-PIN path (rejected: it puts the person through the temporary-credential state, which the spec forbids).

## D4 - Where identifier classification lives

**Decision**: A new `IdentifierSignInService` classifies the identifier, resolves it store-first and plant-system
second, and reports one of three outcomes: resolved in the store, known to the plant system only, or unknown.
The view model drives the stages and the set-up panel; it does not classify anything itself.

**Rationale**: The classification and the two lookups are the part that must be tested exhaustively, including
the shared-number and inactive-account cases. None of that needs a window.

**Alternatives considered**: Doing the classification in the view model (rejected: it would be testable only
through the UI).

## D5 - How four windows become one

**Decision**: One `LaunchWindow` hosts a `ContentPresenter` whose content is swapped between stage views. The
sign-in, machine-setup and blocked surfaces stop being windows and become stage views. `App.ShowLaunchSurface`
chooses the stage instead of opening a window. `MainWindow` is created once and is the window the launch window
becomes.

**Rationale**: The spec asks for stages of the main window, and the shell must be the same window that hosted the
launch. Reusing `WindowEx` keeps the existing title-bar and theme hooks.

**Alternatives considered**: Keeping four windows and hiding three (rejected: hidden windows still exist, still
need closing, and the shell would still be a fifth window).

## D6 - Presenting several operations at once

**Decision**: Keep the existing append-only `LaunchActivityFeed`. The launch surface subscribes to
`EntryAppended`, keeps one line per step id that has started and not ended, and renders all open lines. No new
feed type is introduced.

**Rationale**: The feed is already append-only, thread-safe and immutable per entry, and every line already
carries its step id. "Several lines at once" is a presentation concern over the existing record, not a new data
structure.

**Alternatives considered**: A second "current steps" collection (rejected: it would duplicate state the feed
already holds and could disagree with it).

## D7 - The key-file contradiction

**Decision**: The behaviour stays exactly as it is, and the contradiction is reported rather than fixed by
changing code.

**Finding**: `.github/instructions/startup-rebuild.instructions.md` states the shared key file is retired and
that nothing reads a key. In the code, `SharedKeyFileSource` (a `ISharedKeySource` reading a UNC path) is
registered in `ModuleDependencyInjectionExtensions`, `RememberedSignInService` uses it to unlock a remembered
sign-in, `ReadRememberedSignInStep` is registered as a launch step, and the catalogue still names
"the shared key file" as that step's target. So the code does read a key file, and the instruction document is
the stale one. The reconciliation is to correct the document to match the code, in a change that does not alter
behaviour.

**Rationale**: The request is explicit that behaviour must not change silently and that which of the two is
stale must be determined and reported.

**Alternatives considered**: Deleting the key-file reader to match the document (rejected: it would withdraw a
working feature and change behaviour the spec does not ask to change).

## D8 - Reporting a launch that passes the budget

**Decision**: The budget is a linked cancellation that, when it fires, ends the walk and hands over
`LaunchOutcome.Blocked` with a diagnosis naming the budget. A step that is still running at that moment has
already been abandoned at its own stated maximum if it passed it, so the budget is only ever reached by an
accumulation of bounded steps.

**Rationale**: The launch path may not end on a wait with no stated cause. Naming the budget as the cause, and
fronting only remedies that could remove it (retry), keeps FR-004 and FR-037 true.

**Alternatives considered**: Treating a budget overrun as a step failure (rejected: no one step caused it, so the
diagnosis would blame the wrong thing).

## D9 - Moving the picture cache

**Decision**: Reorder the catalogue so `picture-cache` is the first entry. It is best-effort and bounded at ten
seconds, so starting it first cannot hold the launch, and it no longer adds waiting time at the end.

**Rationale**: The spec asks for exactly this, and the best-effort marker is what makes the move safe.

**Alternatives considered**: Running it concurrently with everything (rejected: an unbounded set of concurrent
readers against the picture share is not what the spec asks for, and starting it first already removes the wait).

## D10 - The external-system priming stays put

**Decision**: `visual-priming` keeps its position as the last step before `shell`.

**Rationale**: The owner confirmed it must not move or be removed, because the cached/live verdict has to be
settled before the shell opens.

**Alternatives considered**: None accepted. Recorded so a later reader does not "tidy" it.
