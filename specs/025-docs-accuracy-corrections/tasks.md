# Tasks: Documentation Accuracy Corrections

**Feature**: `specs/025-docs-accuracy-corrections` · **Plan**: [plan.md](./plan.md) · **Spec**: [spec.md](./spec.md)

30 tasks across three story phases. Each correction is a separate file, so the edits inside a phase run in parallel and the verification wave joins them.

## Phase 1: Setup

- [x] **T001** Record the baseline the change will be judged against: the search hit count for each stale token in the spec's Verbatim Constraints, and the two counts as they stand now (17 and 65) · `specs/025-docs-accuracy-corrections/baseline.md`

## Phase 2: User Story 1 — always-applied guidance points at files that exist (P1)

Files: `.github/instructions/spec-kit.instructions.md`, `.github/copilot-instructions.md`, `.github/instructions/test-conventions.instructions.md`, `.github/instructions/winui3-adoption.instructions.md`, `AGENTS.md`

### Tests

- [x] **T002** [US1] Verify FR-001: a search for the two deleted module workstream references returns no hit in `.github/instructions/spec-kit.instructions.md`, and the file names `specs/001-module-mock-visual-fallback/` and both checklist skills · fail first: both references are present today
- [x] **T003** [US1] Verify FR-002: every companion helper script named in `.github/instructions/spec-kit.instructions.md` exists under `.specify/extensions/companion/scripts/` · fail first: one named script is absent
- [x] **T004** [US1] Verify FR-007: the checklist example offered in `.github/copilot-instructions.md` resolves to a file that exists · fail first: the named file has never existed
- [x] **T005** [US1] Verify FR-008: the guardrail test cited in `.github/instructions/test-conventions.instructions.md` opens from the repository root · fail first: the cited path omits its project folder
- [x] **T006** [US1] Verify FR-009: the interface file count stated in `.github/instructions/winui3-adoption.instructions.md` equals a fresh count of `**/*.xaml` outside `bin` and `obj`, and carries its measurement date · fail first: it states an older count
- [x] **T007** [US1] Verify FR-014: the guardrail test count stated in `AGENTS.md` equals a fresh count of `MTM_Waitlist.Tests/**/*AuditTests.cs` · fail first: it states 17, the tree holds 18

### Implementation

**Wave 1 — independent (different files):**

- [x] **T008** [P] [US1] Repoint both deleted module workstream references to the shipped feature folder and the two checklist skills · `.github/instructions/spec-kit.instructions.md`
- [x] **T009** [P] [US1] Replace the absent companion helper script name with the one that exists, leaving the neighbouring real script untouched · `.github/instructions/spec-kit.instructions.md`
- [x] **T010** [P] [US1] Offer a checklist example that exists in the repository · `.github/copilot-instructions.md`
- [x] **T011** [P] [US1] Give the guardrail test its full path from the repository root · `.github/instructions/test-conventions.instructions.md`
- [x] **T012** [P] [US1] State the measured interface file count with the date it was measured · `.github/instructions/winui3-adoption.instructions.md`
- [x] **T013** [P] [US1] State the measured guardrail test count · `AGENTS.md`

**⟶ Wait for Wave 1 to finish, then re-run T002 to T007.**

**Checkpoint**: the guidance a reader meets first no longer sends them anywhere empty.

## Phase 3: User Story 2 — living documents describe what is actually there (P2)

Files: `docs/living-specs/living-specs-drift-ledger.md`, `docs/diagrams/README.md`, `docs/diagrams/diagram-workflow-refactor-notes.md`, `docs/startup/startup-rebuild-design-brief.md` (moved), `specs/010-startup-rebuild/startup-rebuild-design-brief.md` (new home), `README.md`, `MTM_Waitlist.Tests/Module_Startup/Services/AcceptanceSurfaceTests.cs`

### Tests

- [x] **T014** [US2] Verify FR-003: the retained specification reference in `docs/living-specs/living-specs-drift-ledger.md` resolves, and no reference it presents as live points at a file that is gone · fail first: it cites a path the tree does not have
- [x] **T015** [US2] Verify FR-004: every link in `docs/diagrams/README.md` and `docs/diagrams/diagram-workflow-refactor-notes.md` resolves, and both say what became of the removed workflows · fail first: sixteen links point at missing documents
- [x] **T016** [US2] Verify FR-005: `docs/startup/startup-rebuild-design-brief.md` no longer exists, the brief sits under `specs/010-startup-rebuild/`, its opening no longer reads as unbuilt, and both `README.md` links plus all four brief reads in `AcceptanceSurfaceTests` open the new path · fail first: the brief is in `docs/startup/`

### Implementation

**Wave 1 — independent (different files):**

- [x] **T017** [P] [US2] Repoint the retained specification reference and stop presenting the references to files that are gone as live · `docs/living-specs/living-specs-drift-ledger.md`
- [x] **T018** [P] [US2] Reduce the diagrams index to the workflow that exists, keep its two working links and its columns, and state that the other four were removed with their flows and sheets · `docs/diagrams/README.md`
- [x] **T019** [P] [US2] Remove the eight flat links to missing documents, keep the narrative and the two working startup links, and reconcile the verification record with the two documents present · `docs/diagrams/diagram-workflow-refactor-notes.md`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — the move, which touches three files at once:**

- [x] **T020** [US2] Move the brief to `specs/010-startup-rebuild/`, rewrite its opening statement so it reads as shipped, add one line recording its former location, and leave every S-numbered heading and the rest of its content untouched · `specs/010-startup-rebuild/startup-rebuild-design-brief.md`, `docs/startup/startup-rebuild-design-brief.md` (deleted)
- [x] **T021** [US2] Point both links at the new location · `README.md`
- [x] **T022** [US2] Point the four brief reads at the new location, changing no assertion · `MTM_Waitlist.Tests/Module_Startup/Services/AcceptanceSurfaceTests.cs`

**⟶ Wait for Wave 2 to finish, then re-run T014 to T016.**

**Checkpoint**: the living records describe the repository as it is, and the shipped feature's brief sits with the feature.

## Phase 4: User Story 3 — proposals, notes and script guides point at real targets (P3)

Files: `docs/ui-designs/startup-launch/machine-setup/name-the-authority/name-the-authority-proposal.md`, `docs/ui-designs/startup-launch/sign-in/name-the-authority/name-the-authority-proposal.md`, `docs/ui-designs/startup-launch/sign-in/no-offer-explained/no-offer-explained-proposal.md`, `docs/ui-designs/startup-launch/sign-in/terminal-aware-offer/terminal-aware-offer-proposal.md`, `docs/scripts/scripts-automation-index.md`, `docs/scripts/scripts-store-publish-app-payload.md`, `.github/memories/repo/tasks-md-id-allocation.md`, `.github/memories/repo/workflow-diagrams.md`

### Tests

- [x] **T023** [US3] Verify FR-006: all twelve image links across the four proposals resolve, the eight before and after links pointing into their `pngs/` directory and the four comparison links left as they are · fail first: eight resolve nowhere
- [x] **T024** [US3] Verify FR-010: the heading of `.github/memories/repo/tasks-md-id-allocation.md` cites the specification folder that exists · fail first: it cites an abbreviated form
- [x] **T025** [US3] Verify FR-011: every diagram PDF path named in `.github/memories/repo/workflow-diagrams.md` resolves, and the note records that the four other workflows are gone · fail first: four names assume a flat layout
- [x] **T026** [US3] Verify FR-012: the Outputs block of `docs/scripts/scripts-automation-index.md` presents each artefact it lists as produced by the named script, and says the folder is generated · fail first: they read as committed files
- [x] **T027** [US3] Verify FR-013: no absolute path into another machine's user profile remains in `docs/scripts/scripts-store-publish-app-payload.md`, in a link or in prose, and the guide names the in-repository script · fail first: three such strings are present

### Implementation

**Wave 1 — independent (different files):**

- [x] **T028** [P] [US3] Prefix the before and after image links with the image directory, leaving the four comparison links untouched · the four `…-proposal.md` files above
- [x] **T029** [P] [US3] Cite the current specification folder in the heading · `.github/memories/repo/tasks-md-id-allocation.md`
- [x] **T030** [P] [US3] Cite each diagram where it now lives and record that the other four workflows are gone · `.github/memories/repo/workflow-diagrams.md`

**⟶ Wait for Wave 1 to finish, then:**

**Wave 2 — independent (different files):**

- [x] **T031** [P] [US3] Present the Outputs block's artefacts as outputs of the named script and say the folder is generated · `docs/scripts/scripts-automation-index.md`
- [x] **T032** [P] [US3] Remove the absolute another-machine path from the two links and the prose, and name the in-repository script that produces the same material · `docs/scripts/scripts-store-publish-app-payload.md`

**⟶ Wait for Wave 2 to finish, then re-run T023 to T027.**

**Checkpoint**: proposals render their own images and no guide sends a reader to another machine.

## Phase 5: Polish

- [x] **T033** Validate against Success Criteria: re-run every search in the spec's Verbatim Constraints, the path extraction over the guidance files, the image link test, the two counts and the absolute-profile search, and compare to the baseline recorded in T001 · `specs/025-docs-accuracy-corrections/baseline.md`
- [x] **T034** Run the repository's build and full test suite and confirm the result matches the pre-change result, proving the three test path literals are the only code-adjacent change · `MTM_Waitlist.sln`
- [x] **T035** Record what the change deliberately left alone, so the next reader knows the diagrams tool manifest and the drift ledger's method prose were seen and not taken on · `specs/025-docs-accuracy-corrections/concerns.md`

## Dependencies & Execution Order

Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5. Stories 1 to 3 touch disjoint files, so they may run in any order once the baseline exists; each phase's implementation wave finishes before its verification wave re-runs.

- **Phase 1**: T001 alone, blocks every verification task.
- **Phase 2**: wave 1 (T008–T013) is six independent files; the join re-runs T002–T007. T008 and T009 share a file and therefore sit in one sequence, not in parallel with each other.
- **Phase 3**: wave 1 (T017–T019) is three independent files; wave 2 (T020–T022) is the move and must follow, because T021 and T022 point at the file T020 creates; the join re-runs T014–T016.
- **Phase 4**: wave 1 (T028–T030) and wave 2 (T031–T032) are independent files; the join re-runs T023–T027.
- **Phase 5**: T033 and T034 both read the finished tree and can run together; T035 records the residue.

## Phase 6: Convergence

- [x] T036 Retire the `WeekendProject/PromptFiles` checklist pointer in the disposition memory note per SC-001 (partial)
