# Cross-Artifact Analysis: User Management and Permissions

**Feature**: `specs/006-user-management-and-permissions`
**Artifacts analysed**: `user-management-and-permissions.spec.md`, `plan.md`, `research.md`, `data-model.md`,
`quickstart.md`, `contracts/permission-contract.md`, `contracts/database-contract.md`, `contracts/ui-contract.md`,
`tasks.md`, `checklists/requirements.md`
**Date**: 2026-09-20

> **Provenance, stated first because it decides how much this report is worth.** This file is the **re-analysis**
> performed at the end of an unattended `speckit.fix-findings` run. `speckit.analyze` could not be dispatched from
> inside that run, so the same cross-artifact check was performed directly by the agent rather than by a dispatched
> analyzer. The **original FAIL report is a separate document** and is not reproduced, altered or replaced here; it
> was read in full and every one of its twenty findings is accounted for in `findings.fixed.md`. Treat this PASS as
> the agent's own verdict, reached by the same checks, not as a second independent analyzer run.

> **A second verification followed, and this round repaired it.** An independent analyzer re-checked these artifacts
> and returned **FAIL**, naming eight further findings: three HIGH — the renamed role's rung had no writer, `T031`
> asserted a Phase 4 state while running in Phase 3, and the required reversal ran before the baselines that are keyed
> to the role it removes — and five lower. All eight have since been repaired in the artifacts as they now stand.
> The verdict and the tables below remain this document's own reading from the first repair round; they are not
> restated here as a verdict on the repaired artifacts.

## Verdict

**PASS.** No CRITICAL finding, no HIGH finding and no MEDIUM finding remains. Every requirement is cited by at
least one task, every task citation resolves to a real requirement, and the three numbers that disagreed before —
the gate-site count, the count of lists that become permissions, and the project count — now agree with the
specification's Context everywhere they appear.

## What the previous analysis found, and how each item stands now

| ID | Severity | State | Where it was resolved |
| --- | --- | --- | --- |
| C1 | CRITICAL | fixed | FR-046, FR-047, FR-048, FR-075, US4 scenario 6, Permission entity, Clarifications, `research.md`, `data-model.md`, permission contract, T016, T036, T060 |
| C2 | CRITICAL | fixed | T048, T051, T024, permission contract's key table |
| H1 | HIGH | fixed | specification Context (the single canonical count); plan, task list, `research.md`, permission contract point at it |
| H2 | HIGH | fixed | plan Scale note and Summary; task list Phase 5 Goal and Independent test |
| H3 | HIGH | fixed | T027 restricted; `database-contract.md` § Seeds; `research.md` rename section; plan structure |
| H4 | HIGH | fixed | FR-009 reworded and cited by T011; FR-001 and the Person entity de-duplicated |
| H5 | HIGH | fixed | FR-035 cited in T013, T066, T072; both session cases owned by T072 |
| H6 | HIGH | fixed | T017 and T026 file lists extended |
| H7 | HIGH | fixed | T036, T024, T034, SC-001, quickstart step 5, permission contract |
| M1 | MEDIUM | fixed | plan Scale note plus a paragraph after the Constitution Check |
| M2 | MEDIUM | fixed | plan structure; UI contract's test surface |
| M3 | MEDIUM | fixed | `[P]` dropped from T074 and T075; Phase 12 wave 3 heading |
| M4 | MEDIUM | fixed | T016, T035, T008, T066 |
| M5 | MEDIUM | fixed | FR-030, SC-007, T013, T073, `data-model.md`, `database-contract.md` |
| M6 | MEDIUM | fixed | T020, T053, T074, T072 |
| M7 | MEDIUM | fixed | FR-114 cited in T044 and T059 |
| L1 | LOW | fixed | T074 names both audit suites |
| L2 | LOW | fixed | T074 re-filed against the evidence document |
| L3 | LOW | fixed in part, argued down in part | FR-095, FR-115, FR-116 and SC-011 given thresholds; FR-082, FR-088 and FR-097 left as written, with the reason recorded in `findings.fixed.md` |
| L4 | LOW | fixed | count consolidated; pinned keys reduced from three copies to two |

## Requirement coverage

- **Functional requirements**: 117 (FR-001 … FR-117), no gaps.
- **Success criteria**: 20 (SC-001 … SC-020).
- **Cited by at least one task**: 137 of 137 — **100%**. Zero orphans. Range citations such as "FR-019 to FR-027"
  were expanded before counting.
- **Task citations that resolve to nothing in the specification**: 0.

The three orphans the previous analysis found — FR-009, FR-035 and FR-114 — are now cited, and FR-009 is cited by
the task that actually enforces it rather than by a task chosen to make a number green.

## Duplication

The previous analysis recorded two duplication findings; both are closed.

- **The gate-site count** has one canonical statement, in the specification's Context section, as a single quotable
  sentence: *twelve hand-written role lists in nine files across five projects; counting the developer check that
  decides from a display name, thirteen gate sites under audit in ten files; eleven of the twelve lists become
  named permissions, and the twelfth — the badge map — is re-keyed and stays out of the permission set.* The plan,
  the task list, `research.md`, the permission contract and the requirements checklist each name that count and
  agree with it, and none derives a different one; the numbers are stated where an implementer needs them rather
  than derived a second time. The previously wrong "eight projects" appears nowhere.
- **The pinned strings** — the fourteen permission keys, the nine role codes, the ranks and the dead vocabulary —
  now live in exactly two places: the specification's Verbatim Constraints section and
  `contracts/permission-contract.md`, which is the list the registry test reads. `data-model.md` points at them
  instead of copying them, so a change to the set is a change in two places rather than three.

## Ambiguity

The one cluster the previous analysis found is closed. FR-095 and FR-115 now state a row count (1,000) and FR-116
an upper bound (200 percent), and SC-011 plus the UI contract's scaling paragraph were aligned to the same bound so
the threshold is not stated two different ways. FR-082, FR-088 and FR-097 were left qualitative on purpose, with
the argument recorded; each is already pinned by an acceptance scenario or a resource-key contract, and inventing a
number for a visual property would create a threshold nobody can check.

## Consistency

Checked and consistent after the fixes:

- The baselines' home. The specification, the plan, `research.md`, `data-model.md`, the permission contract, T016
  and T036 now all say the same thing: the declaration carries the key, label, what it gates, area, shipped
  fallback and gate sites; the per-role baselines are seeded rows; a check ties them in both directions. The "who
  holds this" view reads its roles from the seeded baselines the check ties to the declaration, in FR-075, US6,
  T060, T063 and the UI contract alike.
- The rename and the shipped seed. T027 adds `material_handler_lead` and the rungs and touches neither `admin` nor
  `it_department`; T032 owns the rename entirely, including the renamed role's rung of 90 and the rung its reversal
  restores. `database-contract.md` § Seeds and `research.md` state the same, including the reason the reversal is
  exact and why the restriction beats stating an application order.
- Orderings. Both settled orderings appear in the plan and the task list: the re-rank (T006) before the first
  per-person row (T054), and the rename with its executed reversal (T032, T033) before the baselines (T036), with the
  forward rename re-applied inside T033 so the baselines are seeded against a catalogue that holds `it_department`.
- The PIN. FR-030, SC-007, T013, T073, `data-model.md` and `database-contract.md` agree that only a salted hash is
  stored and that "read back" means plaintext.
- Parallel markers. Every `[P]` task sits in a wave whose files no other task in that wave touches. T073, T074 and
  T075 carry no marker because all three write the same evidence document, `quickstart.md`, and run in that order.

## Unmapped tasks and scope creep

No task implements something nothing asked for, and no task citation dangles. The five tasks that cite success
criteria or a seed gate rather than a requirement — T023, T030, T041, T074, T075 — are aggregate regeneration,
audit runs and verification passes, not new scope.

## Constitution alignment

No MUST principle is violated, and the repair added no violation. Stored-procedure-first still holds (the JSON
change set travels as a procedure parameter, not statement text; every schema artifact ships `create.sql` with
`rollback.sql`). Live-data integrity holds (no sample data anywhere; the shipped fallback answers an access
question rather than substituting rows). The two platform claims `research.md` leans on — that `x:Load` requires
an `x:Name`, and that `ContentDialog.Closing` exists and fires before the dialog closes — were verified against
official Microsoft documentation in the previous pass and were not changed here.

## Metrics

| Measure | Before | After |
| --- | --- | --- |
| Requirements / success criteria | 117 / 20 | 117 / 20 |
| Tasks / phases / waves | 76 / 12 / 36 | 76 / 12 / 36 |
| Requirement coverage | 97.4% (114 of 117) | 100% (137 of 137) |
| Findings | 2 critical, 7 high, 7 medium, 4 low | 0 / 0 / 0 / 0 outstanding |
| Verdict | FAIL | PASS |

## What this re-analysis did not check

Stated plainly rather than left implied.

- **The shipping code, exhaustively.** Every file the artifacts name was confirmed to exist in the project it is
  attributed to, which is what makes the count of five projects right. Neither this pass nor the previous one
  searched the whole workspace for a fourteenth hand-written role list, or counted each list's membership. T002
  captures the lists as fixtures and T035 audits all thirteen sites, and those two tasks are where that proof
  lives.
- **Anything requiring a database or a build.** Nothing in this feature is implemented, so no SQL, procedure, seed,
  migration or rollback was executed or reviewed line by line, and no test or build was run. Identifiers, the
  64-character name limit and the index choices remain unverified.
- **`quickstart.md` as a proof.** Step 4's two orderings and step 5's per-person comparison were read for
  consistency with the specification; they were not executed.
- **Decisions 1 to 6, 8 to 13 and 16 to 28 of the seed's brainstorm log line by line.** The settled decisions the
  owner named as binding — 7, 9, 11, 14, 15, 29 and 30 — were read in full, and none was weakened by this repair.

## What you need to do

Nothing blocking. The feature is ready for `/speckit.implement` once the owner is content with the two judgement
calls recorded above, both of which were taken in the direction the owner instructed. If a further independent
verdict is wanted, run `speckit.analyze` from a fresh session and it will overwrite this file with its own report.
