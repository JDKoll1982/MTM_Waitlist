# Implementation Plan: Documentation Accuracy Corrections

## Summary

Fourteen documentation corrections approved by the workspace audit, applied as prose, link and count edits only. Every correction either makes a cited path resolve or makes a stated count true, so a reader who follows the documentation stops landing on nothing. One file moves: the shipped startup design brief goes to `specs/010-startup-rebuild/startup-rebuild-design-brief.md`, which also means the three guard tests that read it from disk follow it.

No stack choice is involved: this feature edits Markdown and one path literal in a test file. Nothing is built, installed or migrated.

## Project Structure

```text
MTM_Waitlist/
├── AGENTS.md                                        count correction (FR-014)
├── README.md                                        two links follow the moved brief (FR-005)
├── .github/
│   ├── copilot-instructions.md                      checklist example (FR-007)
│   ├── instructions/
│   │   ├── spec-kit.instructions.md                 seed folder, checklist skills, helper script (FR-001, FR-002)
│   │   ├── test-conventions.instructions.md         guardrail test path (FR-008)
│   │   └── winui3-adoption.instructions.md          interface file count (FR-009)
│   └── memories/repo/
│       ├── tasks-md-id-allocation.md                spec folder in the heading (FR-010)
│       └── workflow-diagrams.md                     diagram locations (FR-011)
├── docs/
│   ├── diagrams/
│   │   ├── README.md                                eight dead links, five-workflow prose (FR-004)
│   │   └── diagram-workflow-refactor-notes.md       eight dead links, verification record (FR-004)
│   ├── living-specs/living-specs-drift-ledger.md    dead references (FR-003)
│   ├── scripts/
│   │   ├── scripts-automation-index.md              generated artefacts (FR-012)
│   │   └── scripts-store-publish-app-payload.md     another-machine path (FR-013)
│   └── ui-designs/startup-launch/                   four proposals, eight image links (FR-006)
├── specs/
│   ├── 010-startup-rebuild/startup-rebuild-design-brief.md   moved here (FR-005)
│   └── 025-docs-accuracy-corrections/               this feature
└── MTM_Waitlist.Tests/Module_Startup/Services/
    └── AcceptanceSurfaceTests.cs                    three brief paths follow the move (FR-005)
```

**Structure Decision**: the existing documentation layout is kept. The only structural change is the brief's move into the folder of the feature it describes, which is where the other records of that feature already live.

## Call paths

```calls follow the moved design brief
~ Service_AcceptanceSurfaceTests_TheCatalogue_StatesThatTheOriginalSweepBreakdownWasNotRecovered() @ MTM_Waitlist.Tests/Module_Startup/Services/AcceptanceSurfaceTests.cs:247
```
note: the same test reads the brief again for its second assertion, and two more tests read it at lines 263 and 313; all four reads take the moved path, and no assertion changes.

## Constitution Check

| Principle | Assessment | Note |
| --- | --- | --- |
| I. Spec-First, Verified Delivery | PASS | This specification is the contract; the verification plan is run before the work is reported done. |
| II. Live Data Integrity (no mocked stores) | PASS | No store, query or data path is touched. |
| III. Stored-Procedure-First Database Discipline | PASS | No database artefact is created, changed or referenced. |
| IV. MCP-First, Grounded Decisions | PASS | Every claim in the spec was checked against the file it describes, and the two counts were measured rather than recalled. |
| V. WinUI 3 Platform Conformance | PASS | No XAML, view, view model or resource changes. |
| VI. Evidence-Based Verification Gates | PASS | Each correction is proved by a search, a link test or a file count, and the build and tests are re-run to show the change is inert. |
| VII. Readable, End-User-Facing Delivery | PASS | The corrected documents are read by people; the edits keep their voice and add no guidance. |

No violations, so no Complexity Tracking table.

## Phase 0: research

Recorded in [research.md](./research.md). Five decisions shaped the work: how far to follow the moved brief, which references count as live rather than historical, what to do with the drift ledger's retired helpers, how many images actually need repairing, and how to word a removal so a reader learns what happened without being invited to restore it.
