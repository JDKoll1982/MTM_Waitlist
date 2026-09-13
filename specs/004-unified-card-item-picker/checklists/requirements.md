# Specification Quality Checklist: Unified Waitlist Card and Category/Item Request Picker

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-09-13
**Feature**: [spec.md](../spec.md)

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

- **Zero `[NEEDS CLARIFICATION]` markers.** Every open question from the brainstorm was answered before this
  specification was written (§13 and §16 of the seed), so there is nothing left to defer to `clarify`.
- **Two requirements were split during the self-check.** `FR-024` originally carried both "every data operation
  goes through a stored procedure" and "no artifact ships without a rollback"; the second half is now `FR-025`,
  so each statement is independently testable. `FR-026` … `FR-028` were renumbered accordingly, and the coverage
  records were updated to match rather than left pointing at the old numbering.
- **Repo-convention requirements are carried deliberately.** "Stored procedure" (`FR-024`), "matching rollback"
  (`FR-025`) and "resource mechanism" (`FR-022`) read as implementation detail, but the project constitution makes
  them requirements rather than choices, and the two previous specifications carry them the same way.
- **Deliberately absent, and why.** The 'Other' card's full-width grid span is a layout value belonging to the
  plan, not a business requirement, so the specification states only that the card is uniform (`FR-006`) and
  leaves the span to `plan.md`. Pixel dimensions, control names and library choices are likewise left out.
- **Bounding is explicit rather than implied.** `FR-028` and the Assumptions name the four Items with no value
  source as out of scope, and name the deferred material identifiers as belonging to the later item-resolution
  work — so a reader cannot mistake them for omissions.
- **Refreshed 2026-09-13, after the two seed documents were corrected.** The refresh closed four gaps and changed
  no requirement's number:
  - The workflow document's availability check sat *after* the Item step, which would have offered an unsupported
    Item and then refused it. `FR-002` now pins the check ahead of the list.
  - The Scrap rule was tightened to the codebase's own predicate `HasScrapDecision` (`FR-031`), after the
    `Scrap Type Required` placeholder was found to be persistable and would otherwise have surfaced as a scrap
    type.
  - The wrong-material Items were corrected to single Deliver requests naming the material they bring (`FR-029`),
    and the die's destination-dependent second line is now pinned in a US2 scenario and in Verbatim Constraints.
  - `FR-032` records that the lifecycle does not change with the Item.
  `FR-029` … `FR-032` were **appended**, not inserted, so nothing needed renumbering and no coverage record was
  orphaned.
- **Both seed documents are now named as documents of record** in the header — the configuration spreadsheet and
  the workflow document — with the Assumptions stating that neither is read or shipped by the application.
