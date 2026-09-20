# Specification Quality Checklist: Shared Fuzzy Match Picker and Work Order Search

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

- **Zero `[NEEDS CLARIFICATION]` markers.** Three questions were put to the owner (how far the shared capability
  should reach; prefix versus contains matching; when the modal is raised) and the owner was unavailable, so each is
  **decided and recorded** in Assumptions rather than left open. The decision most worth confirming is the match
  semantics, because the owner's own message supports both readings.
- **The feature is now two things, and the name only says one.** The owner's request widened mid-flight from "a fuzzy
  search modal on the work order page" to "a shared feature that can read any given table the caller needs". This
  specification therefore fixes a **generic contract** and ships the **work-order caller** as its first user. The
  directory is still named for the narrower original request; that is recorded in Assumptions rather than papered
  over.
- **A repository rule materially shapes the design, and is stated as a requirement.** "Every data operation goes
  through a stored procedure, with no inline or run-time-assembled statement text" (`FR-018`) means a capability that
  accepts a raw table name and builds a query is **not available**, however natural that reading of the request is.
  The requirement is therefore written as *the caller describes a registered source*, which preserves the owner's
  intent — any list, without changing the capability — while staying inside the rule. This is the single most likely
  place for the owner to disagree, so it is called out here rather than buried.
- **A behavioural distinction the specification had to invent, and it is load-bearing.** The step's current
  formatting accepts five or six digits and pads to six. The owner's example (`0451`) is four digits, so it is
  **not** a malformed work order — it is a *partial*. Partials are searched **unpadded**; complete forms are padded
  as they are today. Without that distinction the example would derive `WO-000451`, which is not what was asked for.
  `FR-006` and the Edge Cases pin it.
- **Repo-convention requirements are carried deliberately.** "Stored procedure" (`FR-018`), "matching rollback plus
  master lists in sync" (`FR-020`) and "resource mechanism" (`FR-021`) read as implementation detail, but the project
  constitution makes them requirements rather than choices, and the three previous specifications carry them the same
  way.
- **Deliberately absent, and why.** The modal's control type, its size, the row layout, the match ordering and the
  exact page control are all design decisions belonging to the plan; the specification states the observable
  behaviour only. The Levenshtein helper that already exists in the repository is named in Assumptions as a
  *candidate* for reuse, deliberately without mandating it — the plan decides.
- **Bounding is explicit rather than implied.** The capability ships one caller; a second is a follow-on, and the
  specification says why (the contract is proven by a real second use, not a speculative one).
