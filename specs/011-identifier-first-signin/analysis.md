# Analysis Report: Identifier-First Sign-In and a Faster Launch

Read-only consistency analysis across `identifier-first-signin.spec.md`, `plan.md`, `tasks.md` and
`.specify/memory/constitution.md`. Produced before implementation, as the analyze step requires.

## Summary

| Severity | Count |
|---|---|
| CRITICAL | 1 |
| MEDIUM | 3 |
| LOW | 3 |

No constitution principle is violated by the plan or the task list. One CRITICAL finding is a conflict between a
requirement and the code as it stands, and it is the reconciliation item this feature was asked to report.

## CRITICAL

### A1 - FR-042 asserts nothing but pictures is kept on a machine, and the code reads a key file

`identifier-first-signin.spec.md` FR-042 states that nothing must be kept on a computer except the local picture
copy, and FR-035 states that no shared key file is read, copied or kept. The code does read one:
`SharedKeyFileSource` reads a UNC path, it is registered as `ISharedKeySource` in
`ModuleDependencyInjectionExtensions`, `RememberedSignInService` uses it to unlock a remembered sign-in,
`ReadRememberedSignInStep` is a registered launch step, and `LaunchStepCatalog` still names "the shared key file"
as that step's target. `.github/instructions/startup-rebuild.instructions.md` claims the file is retired, so the
document is the stale side, but the spec as written asserts a state the code does not have.

**Resolution chosen**: keep the behaviour, correct the document, and record the conflict. This is what the
request asked for. The spec's FR-042 and FR-035 are therefore only true about *this feature's* additions (the
set-up path keeps nothing local); the pre-existing key-file read is out of this feature's scope and is reported
rather than silently changed. A reviewer should decide whether to withdraw the key-file read in a later feature.

## MEDIUM

### A2 - Where a new account's user name comes from is unspecified

The account-creation contract requires `p_user_name`, and FR-034 says the procedure creates profile, credential
and rank together, but no requirement says which user name a self-created account gets, who chooses it, or what
happens if it collides with an existing account.

**Suggested remediation**: add one requirement stating that the user name is derived from the confirmed first and
last name (or is the employee number when a name cannot be derived), that it must be unique in the store, and
that a collision is reported to the person. Handled at implement time as an informed default and recorded as a
concern.

### A3 - The "several lines at once" contract is not precise enough to test

FR-031 requires the surface to present several operations while every operation keeps its own line, and T054
extends `SplashFeedLine`, but neither says what an open line is, whether ended lines remain visible, or how many
lines are shown. Given US1 of feature 010 decided the surface shows the work under way with no history, this
feature must not reintroduce history.

**Suggested remediation**: state that the surface shows one line per operation that has started and not yet
ended, and that an ended operation's line leaves the surface. Handled at implement time.

### A4 - FR-026 and FR-027 name the concurrent members but the group boundary is only in the plan

The spec names `judge-session`, `check-computer-against-store` and `check-temporary-credential` together, and the
local reads alongside `store-reachability`. `contracts/launch-concurrency-contract.md` records the exact groups,
but the spec's requirement text does not, so a reader of the spec alone cannot tell what "together" covers.

**Suggested remediation**: none required; the contract is the authoritative detail and the spec's requirement is
satisfied by it. Recorded so the gap is visible.

## LOW

### A5 - "PIN stage" and "credential stage" are used interchangeably

The spec alternates between the two; the contract says "the credential (PIN) stage". They mean the same thing.
Normalise when the spec is next edited.

### A6 - The task list has no Phase 1

The list starts at Phase 2 because there is no setup work to do: no new project, no new dependency, no
configuration. Stated in the Scale Note so the gap is not read as an omission.

### A7 - `read-computer-record` and `read-machine-configuration` are not in either group

Both read the store, so neither can join the local-reads group, and neither is a session check. That is correct
rather than an oversight, but the concurrency contract does not say so explicitly.

## Coverage

Every functional requirement has at least one task. FR-042 and FR-035 are covered only by the documentation
reconciliation task T058, which is the honest limit of this feature's scope (see A1).

## Constitution

| Principle | Result |
|---|---|
| I. Spec-first, verified delivery | PASS |
| II. Live data integrity | PASS |
| III. Stored-procedure-first | PASS |
| IV. MCP-first | PASS |
| V. WinUI 3 conformance | PASS |
| VI. Evidence-based verification | PASS |
| VII. Readable delivery | PASS |

## Next Action

Implementation may proceed. A1 is recorded, not blocking: the behaviour is unchanged and the document correction
is task T058. A2 and A3 are to be settled by informed defaults at implement time and recorded as concerns.
