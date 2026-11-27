# Tasks: AI Development Environment Remediation

**Feature**: `specs/023-ai-tooling-remediation`
**Input**: [spec.md](./spec.md) · [plan.md](./plan.md) · [research.md](./research.md)
**Size**: normal

Shared files: `.github/copilot-instructions.md` and `AGENTS.md` are touched by three stories, so both
belong to Phase 2 and are finished before any story phase starts. Every other phase owns its own files.

---

## Phase 1: Setup

- [x] **T001** Capture the before-state: bytes of the always-applied instruction files, bytes of the
  instruction files matched by `**/*.{xaml,cs}`, counts of agent files, skill folders and MCP servers,
  and the four stated figures (project count, audit-test count, Windows App SDK version, package list).
  Record them in the feature context.

---

## Phase 2: Foundational — the two always-loaded project instruction files

**Files**: `.github/copilot-instructions.md`, `AGENTS.md`

**Wave 1 — independent (different files):**

- [x] **T002** [P] [US2] Correct the facts in `.github/copilot-instructions.md`: Windows App SDK
  `2.3.1` becomes `2.4.0`, the `python3` claim becomes workstation-conditional with a
  re-verify-before-repeating note, and the two memory-note pointers are repaired — the `mcp-tooling.md`
  pointer stays (the note is created in Phase 4), the `codegraphy-setup.md` path is reworded away while
  the rule survives. · `.github/copilot-instructions.md`
- [x] **T003** [P] [US2] Correct `AGENTS.md`: `Fifteen projects` becomes `17`, `15 *AuditTests` becomes
  `17`, the package list gains `CommunityToolkit.WinUI.Controls.SettingsControls` and the eight other
  referenced packages, and the `python3` claim is reworded to match T002. · `AGENTS.md`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T004** [US3] Collapse the blocks `AGENTS.md` repeats from `copilot-instructions.md` into
  one-line pointers to the owning file, and correct its "Instructions in force" list to name the
  files that actually load. · `AGENTS.md`

**⟶ Wait for Wave 2 to finish, then:**

- [x] **T005** [US1, US3] Edit `.github/copilot-instructions.md` in one pass: delete the
  `## XamlMcp UI Inspection` section but keep the Windows UI Automation paragraph and its two pointers;
  delete the "MCP-First Documentation and Code Sample Retrieval", "Output Control Invariants" and
  "Document Authoring" sections; delete the "Multi-File Agentic Execution Workflow" section; compress
  the subagent section to the operative rule with a pointer to its memory note. · `.github/copilot-instructions.md`

**Checkpoint**: both project instruction files state only facts that measure true, and neither repeats
a rule an always-applied instruction file owns.

---

## Phase 3: User Story 1 — every registered tool works (P1)

**Files**: `.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json`,
`.github/instructions/mcp-doc-research.instructions.md`

### Tests

- [x] **T006** [US1] Verify the repository and workspace MCP configuration declare no server that
  cannot serve: each declared server is either removed or has a working endpoint, verified by an
  actual probe, not by reading the file.
- [x] **T007** [US1] Verify every server name mentioned in the instruction files resolves to a server
  declared in `.mcp.json`, and that no instruction states the retired tool-count rationale.
- [x] **T008** [US1] Verify the CSV schema guidance still describes a working read-only path
  (`Import-Csv` with a tab delimiter) after the server is gone.

### Implementation

**Wave 1:**

- [x] **T009** [US1] Remove the `csv-mcp-server` block from `.mcp.json`. · `.mcp.json`

**⟶ Wait for Wave 1 to finish, then:**

- [x] **T010** [US1] Remove the `xamlmcp` block from `.mcp.json`, leaving the file with an empty
  `mcpServers` object rather than a malformed document. · `.mcp.json`

**⟶ Wait for Wave 2 to finish, then (different files, one wave):**

- [x] **T011** [P] [US1] Delete `.vscode/mcp.json`, whose only server duplicated `csv-mcp-server` behind
  a non-existent storage path. · `.vscode/mcp.json`
- [x] **T012** [P] [US1] Remove the `chat.mcp.serverSampling` block from `.vscode/settings.json`; with
  both its server and its source file gone, no key can apply. · `.vscode/settings.json`

**⟶ Wait for Wave 3 to finish, then:**

- [x] **T013** [US1] Rewrite the doc-server table in
  `.github/instructions/mcp-doc-research.instructions.md` against the actual `.mcp.json`, replace the
  tool-count paragraph with one line of current registration state, record that `xamlmcp` returns with
  the change that adds the in-process agent, and record the CSV read-only fallback. ·
  `.github/instructions/mcp-doc-research.instructions.md`

**Checkpoint**: no instruction names a server that is not registered, and no registered server fails
its primary operation.

---

## Phase 4: User Story 2 — every stated fact is true, every named file exists (P1)

**Files**: `.github/memories/repo/mcp-tooling.md`, `.github/memories/README.md`

### Tests

- [x] **T014** [US2] Resolve every file path named in `.github/copilot-instructions.md`, `AGENTS.md` and
  `.github/instructions/**` and confirm each exists.
- [x] **T015** [US2] Re-measure each stated figure and compare: solution projects, `*AuditTests` files,
  the Windows App SDK version in `MTM_Waitlist.csproj`, and every `PackageReference` in the solution
  against the package list in `AGENTS.md`.

### Implementation

- [x] **T016** [US2] Create `.github/memories/repo/mcp-tooling.md` with the verified troubleshooting
  playbook: the stale-tool-snapshot fix (a session that started before a tool was registered keeps
  reporting it disabled), the rule that a "disabled" report is not evidence the server is broken, and
  the dated markers the other notes use. · `.github/memories/repo/mcp-tooling.md`

**⟶ Wait for T016, then:**

- [x] **T017** [US2] Add the new note to the `## Index` list in `.github/memories/README.md`. ·
  `.github/memories/README.md`

**Checkpoint**: every named path resolves and every stated figure matches a measurement.

---

## Phase 5: User Story 3 — smaller context, no rule lost (P2)

**Files**: none. The deletions are owned by Phase 2 because the file is shared; this phase proves them.

### Tests

- [x] **T018** [US3] Measure the always-applied instruction files after Phase 2 and confirm the
  reduction is at least 6,000 bytes against the T001 baseline.
- [x] **T019** [US3] For each statement deleted from the project instructions, open the instruction file
  that owns it and find the statement.

### Implementation

- [x] **T020** [US3] Record the owner map for every deleted statement in the feature context, so FR-018
  is answerable without reading a diff.

**Checkpoint**: the context shrank and every removed rule has a named owner.

---

## Phase 6: User Story 4 — the big procedure loads on demand (P2)

**Files**: `.github/skills/winui3-ui-automation/SKILL.md` (+ `references/`),
`.github/instructions/winui3-ui-automation.instructions.md`,
`.github/instructions/winui3-scaling-routing.instructions.md`

### Tests

- [x] **T021** [US4] Measure the instruction files matched by `**/*.{xaml,cs}` after the move and
  confirm the reduction is at least 25,000 bytes against the T001 baseline.
- [x] **T022** [US4] Confirm the new skill's `name` matches its folder and its `description` carries a
  "use when" trigger that names UI automation, then confirm the routing note names it.

### Implementation

- [x] **T023** [US4] Create `.github/skills/winui3-ui-automation/SKILL.md` plus `references/` carrying
  the procedure and its recipes moved from the instruction file, keeping the pointer to
  `.github/instructions/records/ui-automation-verification.md` live. · `.github/skills/winui3-ui-automation/`

**⟶ Wait for T023, then:**

- [x] **T024** [US4] Reduce `.github/instructions/winui3-ui-automation.instructions.md` to the routing
  shape: keep the frontmatter `applyTo: "**/*.{xaml,cs,ps1,resw}"`, the rules that always apply, and a
  situation table pointing at the skill. · `.github/instructions/winui3-ui-automation.instructions.md`

**⟶ Wait for T024, then:**

- [x] **T025** [US4] Add the new skill to the routing table and the "where the skills live" list in
  `.github/instructions/winui3-scaling-routing.instructions.md`. ·
  `.github/instructions/winui3-scaling-routing.instructions.md`

**Checkpoint**: a XAML or C# edit loads a short routing note, and the procedure is one skill load away.

---

## Phase 7: User Story 5 — only loadable catalogue entries (P3)

**Files**: `.github/agents/speckit.changelog.*`, `.github/agents/speckit.ci-guard.*`,
`.github/agents/speckit.pr-bridge.*`, `.github/agents/speckit.superspec.*`,
`.github/skills/speckit-companion-*/`

### Tests

- [x] **T026** [US5] Parse every file under `.github/agents/` and confirm each opens with frontmatter
  carrying a `description`, and that no file points at an extension absent from
  `.specify/extensions/.registry`.
- [x] **T027** [US5] Count the skill folders (19 expected) and confirm none duplicates a
  `speckit.companion.*` agent, then run one companion command to confirm the host still resolves it.

### Implementation

**Wave 1 — independent (different files):**

- [x] **T028** [P] [US5] Delete the 12 one-line agent stubs for `changelog`, `ci-guard` and `pr-bridge`. ·
  `.github/agents/speckit.changelog.*.agent.md`, `.github/agents/speckit.ci-guard.*.agent.md`,
  `.github/agents/speckit.pr-bridge.*.agent.md`
- [x] **T029** [P] [US5] Delete the 5 headerless `speckit.superspec.*` agent files. ·
  `.github/agents/speckit.superspec.*.agent.md`
- [x] **T030** [P] [US5] Delete the 24 `speckit-companion-*` skill folders, leaving the 19 repository
  skills. · `.github/skills/speckit-companion-*/`

**Checkpoint**: every agent file loads and the skill catalogue holds no duplicate of a companion command.

---

## Phase 8: Polish

- [x] **T031** Validate against Success Criteria: build the solution, run the test project, re-run the
  SC-001 to SC-008 measurements, and confirm the diff contains no change outside AI-facing
  configuration and documentation paths. · `MTM_Waitlist.sln`, `MTM_Waitlist.Tests/`

---

## Dependencies & Execution Order

- **Setup → Foundational**: T001 blocks nothing but supplies the baselines T018 and T021 compare against.
- **Foundational → stories**: T005 finishes the shared instruction file, so Phases 3 to 6 can proceed
  without touching it. T002 and T003 run in parallel; T004 waits on T003; T005 waits on T002.
- **Phase 3**: T009 → T010 (same file) → T011 and T012 in parallel → T013. T006, T007 and T008 run after
  T013.
- **Phase 4**: T016 → T017. T014 and T015 run after Phase 3 and Phase 2 are complete.
- **Phase 5**: T018 and T019 run after Phase 2. T020 records their result.
- **Phase 6**: T023 → T024 → T025. T021 and T022 run after T025.
- **Phase 7**: T028, T029 and T030 are parallel. T026 and T027 run after them.
- **Polish**: T031 runs last and is the only task that runs the suites.
