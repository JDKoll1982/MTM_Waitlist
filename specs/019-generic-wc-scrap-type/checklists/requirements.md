# Specification Quality Checklist: Generic Work Center — Stated Scrap Type and Lugger

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-11-05
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

- Items marked incomplete require spec updates before clarify or plan
- The specification was authored on 2026-10-29 as chunk 5 of the eight-chunk split recorded in `specs/015-generic-wc-ui-shell/BREAKDOWN.md`. This pass re-checked it in place and changed four things. No existing requirement id was renumbered, and nothing was removed.
- **The route past the details step is now stated.** Spec 016 (FR-031, FR-032, added 2026-11-05) sends a scrap request from its answer step straight to the confirmation and records no description. The draft implied the confirmation by naming it, but never said the step is skipped. FR-015 and FR-016 now say so, User Story 1 gained two acceptance scenarios for it, and SC-006 measures it.
- **The failing read is now named.** The edge case said "the scrap-type list cannot be read", which conflated the shared list with the types the arrangement adds to it. Only the arrangement is read, so the edge case now names that read, says why no shortened list is acceptable, and matches spec 016's rule that a failed read reports the time of the attempt and a retry.
- **The reachable/unreachable empty state is now settled.** A5 records that the shared list always ships with the plant's types, so empty choices can only mean a failed read. That keeps a "no scrap types exist" state out of both the requirements and the screen.
- **What the confirmation already does is now bounded.** A4 records that the confirmation naming the type and the skip-the-details route both come from spec 016, so this chunk adds the answer that feeds them and no new screen or route.
- `Verbatim Constraints` is absent because the request pinned no literal identifier or string. The owner's requirement, "If picking up scrap the requester is responsible for stating the scrap type during the request process", is quoted in the Input line and is prose, not an identifier.
- The Design note defers screen design (what the step shows, its unavailable state, and the exact wording) to implement time. That is a design question rather than a spec ambiguity, so it stays a note and no `[NEEDS CLARIFICATION]` marker was raised.
