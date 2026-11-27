# Feature Specification: Documentation Accuracy Corrections

**Source**: Dependency_Assessment/2026-11-27-docs-deep

**Feature Branch**: `025-docs-accuracy-corrections`

**Created**: 2026-11-27

**Status**: Draft

**Input**: Documentation accuracy corrections approved by the workspace modernization audit. Bring the repository's documentation back in line with the code it describes, using the fourteen approved proposals DEV-001 to DEV-014 as the scope.

## User Scenarios & Testing

### User Story 1 - Always-applied guidance points at files that exist (Priority: P1)

A developer, or an agent working on their behalf, opens the repository and follows the guidance that is always in force. Every path it names opens. Today several of them do not: one names a folder that was deleted, one names a helper script beside two real ones, one offers a checklist file that never existed here, one gives a shortened test path, and two state counts that no longer match the tree. The reader either lands on nothing or trusts a number that is wrong, and in both cases the guidance has taught them to stop checking it.

**Why this priority**: These files are applied to every request, so a wrong path in them is repeated on every task and is the most expensive kind of documentation error. Nothing else in this feature matters if the guidance a person reads first is wrong.

**Independent Test**: Search the repository for each stale token listed in Verbatim Constraints. Every search returns no hit in the guidance files, and the two counts match the tree. This can be checked without touching any other story.

**Acceptance Scenarios**:

1. **Given** the spec-kit instruction file, **When** a reader follows its two references to the deleted module workstream folder, **Then** both name the shipped feature folder and the two checklist skills instead.
2. **Given** the spec-kit instruction file, **When** a reader looks for the helper script it names beside the two real ones, **Then** the name it gives is a script that exists.
3. **Given** the always-applied repository instructions, **When** a reader opens the checklist example they offer, **Then** the example is a checklist that exists in the repository.
4. **Given** the test-conventions instruction file, **When** a reader opens the guardrail test it cites, **Then** the cited path opens from the repository root.
5. **Given** the design-system instruction file, **When** a reader reads its count of interface files, **Then** the count matches the tree and carries the date it was measured.
6. **Given** the repository orientation file, **When** a reader reads its count of guardrail tests, **Then** the count matches the number of guardrail test files.

### User Story 2 - Living documents describe what is actually there (Priority: P2)

A maintainer reads a document that is maintained as a current record rather than as history: a drift ledger, a diagrams index, and a design brief for a feature that shipped months ago. Two of them still describe work that was removed, and the third still reads as an unbuilt proposal sitting in a folder of proposals. The reader cannot tell which material is current, so they re-check the code, and the document has cost them time instead of saving it.

**Why this priority**: These documents are read deliberately rather than on every request, so the cost per reading is lower than Story 1, but a current record that misdescribes the tree is a correctness problem for whoever relies on it.

**Independent Test**: Follow every reference in the three documents. Each one opens, and each document's prose agrees with what the repository holds. The shipped brief is found beside its feature instead of among proposals.

**Acceptance Scenarios**:

1. **Given** the drift ledger, **When** a reader follows its references, **Then** the retained reference opens and the references to files that are gone are no longer presented as live.
2. **Given** the diagrams index, **When** a reader follows its links, **Then** every link opens, and the index states plainly that the workflows it used to list were removed with their flows rather than leaving rows that link nowhere.
3. **Given** the diagrams refactor notes, **When** a reader follows its links, **Then** no link points at a missing document and the narrative of the refactor remains readable.
4. **Given** the shipped startup feature, **When** a reader looks for its design brief, **Then** the brief sits with the feature it describes and no longer reads as unbuilt work.

### User Story 3 - Proposals, notes and script guides point at real targets (Priority: P3)

A reviewer opens a design proposal to look at the before and after screens, opens a script guide to find an artefact, or opens a working note to find a specification. The proposal's images do not render because the links assume the images sit beside the document, the script guide presents generated artefacts as committed files, and one guide links to documents on another machine's disk by absolute path. The reviewer cannot review, and the machine-specific links disclose a path that belongs to someone else's workstation.

**Why this priority**: This material is read occasionally, so the cost per reader is lowest, but the disclosure of another workstation's profile path is a hygiene problem that should not stay in a shared repository.

**Independent Test**: Render each proposal's images, open each cited script artefact path, and search for absolute user-profile links under the documentation tree. Images render, cited paths are described in a way that matches how they are produced, and no absolute profile link remains.

**Acceptance Scenarios**:

1. **Given** any of the four startup-launch design proposals, **When** a reviewer opens its before, after and comparison images, **Then** all three render.
2. **Given** the automation script index, **When** a reader looks for the three tooltip artefacts it lists, **Then** the index says they are produced by the named script rather than implying they are committed files.
3. **Given** the store-publishing script guide, **When** a reader follows its two document links, **Then** the links point at material inside this repository, not at an absolute path on another machine.
4. **Given** the two working notes that cite other folders, **When** a reader follows their references, **Then** each reference names the current folder.

### Edge Cases

- A document is a historical record on purpose: the twelve defect records and the pre-spec research note cite paths as they were when written. Those are correct as history and are left exactly as they are. The corrections apply only to documents maintained as current.
- A removed workflow may still be worth mentioning: the diagrams index must state that a flow was removed, not silently drop it, so a reader who remembers it learns what happened.
- An image may exist under a different name than the link assumes: a correction must point at the file that exists rather than renaming or regenerating an image.
- A generated artefact may later be committed: the corrected text says how the artefact is produced, which stays true either way.
- A count that is right today will drift again: the corrected counts carry the date they were measured so a future reader can tell the difference between a stale number and a deliberate measurement.
- A moved document may be linked from elsewhere: any reference to the moved brief must follow it, and none may be left pointing at the old location.

## Requirements

### Functional Requirements

- **FR-001**: The spec-kit instruction file MUST name the shipped feature folder as the first feature seed, and MUST name the two checklist skills as the authoritative checklist source, replacing both references to the deleted module workstream folder.
- **FR-002**: The spec-kit instruction file MUST name only companion helper scripts that exist in the repository, replacing the script it currently lists beside the two real ones.
- **FR-003**: The living drift ledger MUST resolve: its retained reference MUST name the current specification folder, and each reference to a file that no longer exists MUST be removed rather than left as a live link.
- **FR-004**: The diagrams index MUST describe only workflows that exist: it MUST keep the startup-launch row and its two working links, MUST stop presenting the eight links that point at the four removed workflows, and MUST say in one sentence that those workflows were removed with their flows and their sheets. The diagrams refactor notes MUST keep their narrative while presenting no link to a missing document, and their record of the verification run MUST match the documents that exist.
- **FR-005**: The shipped startup feature's design brief MUST move into that feature's specification folder, MUST stop reading as unbuilt work, and every reference to it MUST follow it to its new location: the two links in the repository README, and the three paths in the startup guard tests that read the brief from disk. The brief's body MUST otherwise stay as written, because those tests assert on its recorded content.
- **FR-006**: The twelve image links across the four startup-launch design proposals MUST resolve. Eight of them link to the before and after images as though those images sat beside the document, so each needs the image directory in its path; the four comparison links already resolve and MUST keep the path they have.
- **FR-007**: The always-applied repository instructions MUST offer a checklist example that exists in the repository.
- **FR-008**: The test-conventions instruction file MUST cite the guardrail test at a path that resolves from the repository root.
- **FR-009**: The design-system instruction file MUST state the true count of interface files, in the wording it already uses, and MUST record the date the count was measured.
- **FR-010**: The working note that allocates specification task identifiers MUST cite the current specification folder.
- **FR-011**: The working note that indexes the workflow diagrams MUST cite each diagram where it now lives, and MUST record that the other four workflows are gone.
- **FR-012**: The automation script index MUST describe the tooltip artefacts its Outputs block lists as outputs of the named script, rather than as committed files, and MUST say that the folder holding them is generated.
- **FR-013**: The store-publishing script guide MUST NOT carry an absolute path into another machine's user profile, in a link or in prose. It MUST either reference the in-repository script that produces the same material or drop the reference.
- **FR-014**: The repository orientation file MUST state the true count of guardrail tests.

### Key Entities

Not applicable. No data is created, stored or reshaped; every requirement in this feature edits prose in a documentation or instruction file.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A search of the repository for each stale token listed in Verbatim Constraints returns zero hits outside historical records. Measured by search, not by reading.
- **SC-002**: All four startup-launch design proposals render twelve images between them, twelve out of twelve. Measured by opening each link target on disk; the four comparison links already pass this test today.
- **SC-003**: Every path cited by an always-applied instruction file resolves from the repository root, one hundred percent, checked by extracting the paths from those files and testing each one.
- **SC-004**: No absolute user-profile path appears anywhere under the documentation tree; a search for the pattern returns zero hits.
- **SC-005**: The two corrected counts match the tree at the time of the change, eighteen guardrail tests and ninety-two interface files, verified by counting files.
- **SC-006**: The build and the full test suite give the same result before and after the change, since no code is touched. The expected result is a clean build and the same pass count.

## Non-goals

- Not rewriting history: the twelve defect records and the pre-spec research note keep their original wording and paths.
- Not adding guidance: no instruction file gains a new rule, a new section or a new example beyond the corrections listed.
- Not restructuring documentation: no folder is reorganised beyond the single move in FR-005, and no document is split, merged or renamed.
- Not changing production code, data, packaging, dependencies or configuration; no production source file, project file, stored procedure or database artefact is modified. The one exception follows from FR-005: three path literals in the startup guard tests move with the brief, and no assertion changes.
- Not repairing the diagrams tooling. The tool manifest still declares ten documents and names eight spec files that are gone, and the ledger's method section names three helper scripts that are not in this tree. Both are real drift and both are outside the approved scope; this feature records them and leaves the tooling and the ledger's method prose alone.

## Change-level rules

- Only prose, links and counts change. A change that would alter what a reader is told to do, rather than whether the reference they follow opens, is out of scope.
- Instruction files keep their frontmatter and their voice. No sentence gains a new instruction.
- Where a document must say that something was removed, it says so in one plain sentence; it does not invite the reader to restore it.
- Nothing in this feature may weaken a repository guard: no demonstration or mock mode, no inline data statements, no page declarations added to any project.
- Corrected counts carry the date they were measured, so a later reader can tell a stale number from a measurement.

## Verification plan

1. Search the repository for each stale token in Verbatim Constraints and confirm no hit outside the historical records named in Non-goals.
2. Extract every repository path cited by the always-applied instruction files and test each one for existence.
3. Open each of the twelve image links in the four startup-launch proposals.
4. Count guardrail test files and interface files, and compare to the two corrected counts.
5. Search the documentation tree for absolute user-profile links.
6. Build the solution and run the full test suite, then compare the result to the result recorded before the change.

## Assumptions

- The audit's approved scope is authoritative: fourteen corrections, and the two proposals left as keep-current are not touched.
- The four startup-launch proposals are the complete set carrying sibling image links. The audit counted five; a fresh check finds four documents holding twelve links, and the corrected text follows what the repository actually holds.
- The two counts are measured at the time of the change rather than derived from a script, which is why the date travels with them.
- The diagrams tool manifest and the ledger's method prose are left untouched even though both name things that no longer exist, because the approved scope covers the three reader-facing documents rather than the machinery behind them.
- The automation index's Outputs block lists four artefacts, not three; the corrected text follows the block as it stands.
- The store-publishing guide repeats a workstation path in prose as well as in its two links, so removing only the links would leave the disclosure in place.
- Moving the design brief is a move, not a rewrite: its content is a record of the design rationale and stays as written apart from its opening statement about being unbuilt.
- Repository references to the moved brief are not confined to the documentation tree: the in-repo README links it twice at the repository root, and several specification folders cite it in their own planning records. The README links follow the move; the planning records under `specs/` are history and keep their wording, so the moved brief carries one line recording where it came from.
- The move touches one non-documentation file. Three guard tests read the brief from disk to assert on what it records, so their path literal must follow it; that keeps the test suite honest rather than making it skip. Moving the brief without them would fail those tests, which is why FR-005 names them.

## Verbatim Constraints

Stale tokens that must return zero hits outside historical records:

- `WeekendProject/PromptFiles`
- `WeekendProject/Module_Mock`
- `WAITLIST_REQUEST_WORKFLOW_TASKS.md`
- `Module_Mock/RetiredSymbolAuditTests.cs` when cited without the `MTM_Waitlist.Tests/` prefix
- `specs/001/tasks.md`
- `file:///C:/Users/jkoll/`

Exact values a corrected sentence must match:

- Guardrail test count: `18`
- Interface file count: `92`
- Shipped feature seed folder: `specs/001-module-mock-visual-fallback/`
- Checklist source: `.github/skills/checklist-creation` and `.github/skills/checklist-execution`
- Companion helper script to name: `doctor.py`
- Guardrail test path: `MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`
- Specification folder for identifier allocation: `specs/001-module-mock-visual-fallback/tasks.md`
- Diagram PDF location: `docs/diagrams/pdf/startup-launch/`
- Moved design brief: `specs/010-startup-rebuild/startup-rebuild-design-brief.md`
- Guard tests whose brief path follows the move: `MTM_Waitlist.Tests/Module_Startup/Services/AcceptanceSurfaceTests.cs`
- Repository README links that follow the move: `README.md`
- Image link prefix in each startup-launch proposal: `pngs/`
