# Specification Quality Checklist: Waitlist Handler Fulfilment and Urgency Ordering

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-09-12
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

- The card's fixed layout is carried as verbatim, user-pinned constraints rather than restated from
  scratch; those exact values are the one sanctioned place exact layout numbers appear in the spec.
- Real material identifiers (coil/part number, quantity, stock location) are deliberately **not** required
  by FR-014. Resolving them is the separate unified-card and item-resolution workstream; requiring them
  here would force the card to show a substitute value, which is the open critical defect.
- No `[NEEDS CLARIFICATION]` markers remain: every open decision recorded in the seed was resolved with an
  informed default and captured under Assumptions.
- **The requirement set grew after implementation, deliberately and in place.** FR-024 … FR-031 were added once
  the build was in use, each prompted by something the running app did or failed to do; `spec.md` carries their
  provenance so a reader can tell what was designed up front from what was learned. This checklist was written
  against the pre-implementation spec and its judgements still hold for FR-001 … FR-023. The later requirements
  are documented through `plan.md`'s revision section, `data-model.md` §1/§2/§5, the contracts' C1/C7/C8, and
  gates G11/G12 — not by re-scoring this list.
