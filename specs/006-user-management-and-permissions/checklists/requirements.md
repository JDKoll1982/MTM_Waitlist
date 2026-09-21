# Specification Quality Checklist: User Management and Permissions

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-09-20
**Feature**: [User Management and Permissions](../user-management-and-permissions.spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed (User Scenarios, Requirements, Success Criteria)

## Requirement Completeness

- [x] Any [NEEDS CLARIFICATION] markers are genuine ambiguities (≤3) deferred to clarify — not unresolved guesses
- [x] Each Functional Requirement is a single, testable MUST/SHOULD statement
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into the specification

## Notes

**Self-check pass, 2026-09-20.** One pass, no rewrite loop. The seed's decisions 1 to 30 were treated as settled
requirements, so no clarification was required and no `[NEEDS CLARIFICATION]` marker remains: the count is 0.

The pass found and fixed one real gap, and recorded four gaps it could not fix by writing.

Fixed in place:

- Password reset had requirements but no acceptance scenario. User Story 2 was widened from "create and correct" to
  "create, reset and correct", and now carries scenarios for the reset itself, for a session surviving a reset, for
  a second reset superseding the first, for the person's page layout, for reactivation and for the display-name
  length.
- Five requirements had no acceptance criterion at all: the stale-entitlement re-check, the return to Settings, a
  saved filter naming a role that has gone, an unreadable store on the list, and the checkable rung. Each now has a
  scenario, in User Story 1, 3 or 4 as it belongs.
- Four requirements were visible only in the Edge Cases or the Success Criteria: the duplicate-key answer, the
  employee-number non-uniqueness, the theme that a control is never offered and then refused, and the audit's
  shape. The first two are covered by User Story 2 and the Edge Cases; the third gained a scenario in User Story 4;
  the fourth gained a success criterion.

Recorded rather than fixed by writing:

- **Section 4 of the seed is the explicit non-goals list** and is carried as an "Out of Scope" section. The
  Companion section list does not include one, so this is an addition, placed after the Success Criteria to keep the
  mandated sections in their required order.
- **A "Context in the application today" section** carries the two facts the specification depends on (twelve
  hand-written role lists in nine files, and every one of them comparing display names). Both were verified against
  the shipping code on 2026-09-20 rather than taken from the seed's summary.
- **The seed's API status codes do not exist.** The seed names no HTTP status code anywhere. What it does name is the
  duplicate-key answer `1062`, which is recorded in Verbatim Constraints, together with the permission keys, the role
  codes, the rank numbers, the legacy temporary value and the dead role vocabulary.
- **Two owner questions the seed defers to the screen** are answered here as assumptions, because this run is
  unattended: a plain worker in the setup role is not granted work-centre setup by default, and Material Handler Lead
  is not granted the ignored-locations feature. Both are recorded for the owner to flip on the permissions page.

Items marked incomplete require spec updates before clarify or plan. There are none.

**Remediation pass, 2026-09-20 (`speckit.fix-findings`, after a cross-artifact analysis returned FAIL).** The
findings that touched this checklist's own verdicts were the two duplication clusters it could not see from inside
the specification alone:

- The gate-site count has one canonical statement, in the specification's Context section. The plan and the task list
  restate the decomposition only where a task is about the sites it audits, each anchored back to the Context;
  `research.md` names the thirteenth site alone, and one of the three contracts names the set. What the earlier
  disagreement was — twelve, thirteen and "eight projects" all naming one count — no longer appears anywhere: no
  artifact states a different number.
- The fourteen pinned keys are now stated in two places rather than three: the specification's Verbatim Constraints
  and `contracts/permission-contract.md`. `data-model.md` points at them instead of copying them.
- The seed's section 3.8 contradiction about where the per-role baselines live is resolved in favour of seeded data
  (FR-048) and recorded as a deliberate resolution in Clarifications, `research.md` and the permission contract.

The ticked boxes above still hold: nothing in this pass added an implementation detail to the specification body,
put a pinned identifier outside Verbatim Constraints, or removed a mandatory section.
