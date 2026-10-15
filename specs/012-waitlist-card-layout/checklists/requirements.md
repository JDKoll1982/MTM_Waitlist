# Specification Quality Checklist: Waitlist Line Card Layout

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-10-10
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

- Two `[NEEDS CLARIFICATION]` markers, both carrying a defect the pre-spec found rather than a decision it made:
  FR-090 records that the finished-goods rule behind the item-type derivation counts stock at any location instead
  of at a finished-goods location, and FR-095 records that the dunnage `Description` field is declared in
  configuration but can never hold a value. Neither is encoded as decided; `clarify` owns both.
- Acceptance coverage is story-level, matching this repository's convention rather than one scenario per
  requirement. User Story 1 covers FR-001 to FR-010 and FR-061 to FR-069, User Story 2 covers FR-011 to FR-023,
  User Story 3 covers FR-024 to FR-060, User Story 4 covers FR-070 to FR-076, User Story 5 covers FR-088 to FR-094,
  and User Story 6 covers FR-077 to FR-087. FR-096 to FR-102 are the reversed contracts and FR-103 to FR-105 the
  surviving invariants; each is exercised by the story it belongs to plus the measurable outcomes.
- Every one of the pre-spec's 24 Item configurations appears as its own requirement (FR-030 to FR-053), so no Item
  and no cell was dropped, and the deliberate gaps at D8, D9, D10 and D17 are preserved rather than renumbered.
- Section 6 of the pre-spec is carried as FR-096 to FR-102, because each row states a behaviour that contradicts a
  ratified contract elsewhere in the repository.
- Three items the pre-spec names are deliberately absent from the requirements because they are design rather than
  behaviour: where the per-card detail sections live, that configuration label corrections ship as a seed and a
  migration, and the Print placeholder's code comment. Each belongs to the plan.
- No implementation detail leaks: the plant system is named as the plant system, and no file path, API name or
  framework appears in a requirement.
