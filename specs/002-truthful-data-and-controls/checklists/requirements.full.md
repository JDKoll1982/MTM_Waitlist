# Specification Quality Checklist: Truthful Data and Truthful Controls

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-12
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
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
- [x] No implementation details leak into specification

## Notes

- **0 open markers.** All 5 open decisions in the feature seed (`WeekendProject/SpecTemplates/01-truthful-data-and-controls.md` §7) were resolved from the source documentation rather than by asking:
  - **Missing values (was FR-002 clarification)** → *omit the row, display no value*; grounded in
    `defects/Critical-Waitlist-CardAndDetailShowFabricatedMaterialData.md` §6 step 5 ("omit the row rather than
    print a placeholder value — a missing row is truthful, a fake value is not"). The existing hard-coded
    `"Not available"` defaults are listed as fabricated content in §3 of that defect, so they are removed too.
  - **Privacy Policy** → *remove the entry*; grounded in
    `defects/Low-Settings-PrivacyPolicyLinkPointsAtPlaceholderUrl.md` §5 branch 3 ("if no statement exists and
    none is planned, delete the HyperlinkButton and both resource entries rather than shipping a broken link").
    No published statement address or in-app policy asset exists anywhere in the repository.
  - **Hide or disable the card buttons** → *hide*; grounded in
    `defects/High-Waitlist-CardCancelAndAcceptButtonsAreInert.md` §5 step 3 ("Hide the buttons rather than
    showing disabled ones").
  - **`SampleOrder` rename** → *not required*; the source defect marks it "optional" and the retired-symbol
    audit deliberately does not match the bare word "sample".
  - **Retired-sources note location** → *at the backlog index that already lists them*, cross-referenced from
    `WeekendProject/OPEN-WORK-NEXT-SPEC.md` §10.2, which names the retired sources but does not itself hold the list.
- Two requirements were strengthened by the same documentation pass: notification activation while running now
  routes to the named request as the cold-start path already does (defect §5 step 1), and a shipped-resource
  placeholder-pattern audit is required (privacy defect §6 step 2).
- Validation iterations: 2 (first iteration stopped at the single `[NEEDS CLARIFICATION]` marker; second
  iteration passes on every item).

