# Specification Quality Checklist: One set of rules for every screen, window and dialog

**Purpose**: Validate Companion specification completeness before planning
**Created**: 2026-10-25
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

- The only named technologies sit where they belong: `DevWinUI.WindowedContentDialog`, `PageContentMaxWidth` and
  the three rule files appear under **Verbatim Constraints** because the request pinned them. Requirements speak
  about behaviour ("its own window, owned by the main window"), not about controls.
- **FR-016 and FR-017 name a rule file instead of restating its rules.** That keeps a moving rule list in one place,
  but it means the spec cannot be graded without reading `winui3-api-rules.instructions`. Accepted, and the
  checklist item above is graded on that reading.
- **No markers remain.** All three questions are answered, and the answers are requirements, not notes:
  1. Message-only dialogs (FR-006, FR-008 to FR-012): they open in their own window and come from one standard set
     of six kinds.
  2. The service console (FR-023): in scope, window and both pages.
  3. Developer and diagnostic surfaces (FR-021, FR-026, FR-031): no exemptions. Every XAML file is treated as a
     production page, developed, tested and automated the same way, and any layout that cannot comply is redesigned
     with its behaviour preserved.
- **The api-rules file was read, not assumed.** Four of its rules become checkable requirements here because this
  work will touch them: the shared tooltip behaviour (FR-028), registering a new converter or dictionary in the same
  change (FR-029), awaiting the dialog show call directly (FR-030), and keeping a diagnostic surface raised over a
  dialog as a window (FR-031). Its line asking for `ContentDialog` for confirmations still holds: the standard
  message set is dialog content shown in its own window, not a replacement for it.
- **The inventory work is smaller than the request assumed.** All 77 XAML files already declare exactly one type, so
  "create a new file for each embedded type" has nothing to split. Recorded as an assumption with the measurement
  behind it, rather than as a task.
- **The standard message set is grounded in what the application asks today.** Measured across the code: fourteen
  screens build their own dialog, only two shared prompts exist, and no dialog anywhere uses three actions. That is
  why the set has six kinds and at most two actions each.