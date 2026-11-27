# Feature Specification: AI Development Environment Remediation

**Source**: Dependency_Assessment/DEPENDENCY_AND_MODERNIZATION_SPECIFICATION.md

**Feature Branch**: `023-ai-tooling-remediation`
**Created**: 2026-11-26
**Status**: Draft

## Overview

The repository's AI-facing configuration has drifted away from what the repository actually is. An
agent that opens this workspace is given tools it cannot use, files that do not exist, and counts and
versions that are wrong, and it pays roughly 25,000 tokens of preamble for the privilege on every
request. This feature brings the AI development environment back to the truth: only tools that can
serve are registered, every fact stated to the agent is true, and the context an agent reads before
touching code is smaller without losing a single coding rule.

Nothing in the application changes. No project, package reference or library is touched.

## User Scenarios & Testing

### User Story 1 - Every registered tool works (Priority: P1)

An agent starts a session and asks for a tool. The tool it is offered exists, can attach to something,
and performs the operation it claims. No request pays for tool definitions that cannot ever succeed,
and no instruction tells the agent to use one that is gone.

**Why this priority**: A dead tool is worse than an absent one. It costs context on every request and
invites a workflow that cannot complete.

**Independent Test**: Open the repository's MCP configuration and confirm the only servers present are
ones with a working endpoint; confirm again that no instruction file names a server that is not
registered.

**Acceptance Scenarios**:

1. **Given** the repository MCP configuration, **When** it is read, **Then** it declares no server that
   has nothing to attach to and no server whose primary operation fails.
2. **Given** the editor's workspace MCP configuration, **When** it is read, **Then** it either declares
   nothing or declares a server that also works, and never a duplicate pointing at a path that does not
   exist.
3. **Given** the workspace settings, **When** the sampling allow-list is read, **Then** it names a server
   that actually starts, or it is not present.
4. **Given** any instruction file, **When** it is searched for a server name, **Then** every server it
   names is a server that is registered.

### User Story 2 - Every fact stated in the always-loaded instructions is true (Priority: P1)

An agent reads the project instruction files and orients itself from them. The project count, the
guardrail-test count, the SDK version, the package list and the tooling claims all match what a
measurement of the repository returns today. The files those instructions point at exist.

**Why this priority**: A wrong count sends the agent looking for something that is not there; a wrong
SDK version sends it to the wrong API documentation. These are the highest-cost errors because they are
silent.

**Independent Test**: For each stated figure, run the measurement it implies and compare. For each
named file, check that the path resolves.

**Acceptance Scenarios**:

1. **Given** the project orientation file, **When** the stated project count and guardrail-test count
   are compared with the solution and test project, **Then** both figures match.
2. **Given** the project instructions, **When** the stated Windows App SDK version is compared with the
   application project file, **Then** the version matches.
3. **Given** the package list in the orientation file, **When** it is compared with every package
   reference in the solution, **Then** every package the solution uses appears in the list.
4. **Given** any instruction that names a note under the repository memories folder, **When** the path
   is resolved, **Then** the file exists.

### User Story 3 - The context an agent reads is smaller with no rule lost (Priority: P2)

An agent opening the repository is given the rules it needs and nothing that restates them. Sections
that duplicate an always-applied instruction file are removed from the project instruction files, so
each rule has exactly one owner.

**Why this priority**: The saving is real but it is a cost improvement, not a correctness fix. It must
never be bought by deleting a rule.

**Independent Test**: Compare the bytes of always-loaded instruction text before and after, and check
that every removed statement is still present in the instruction file that owns it.

**Acceptance Scenarios**:

1. **Given** the always-loaded project instruction files, **When** their combined size is measured,
   **Then** it is materially smaller than before this feature.
2. **Given** a rule that was removed from the project instructions because it duplicated an
   instruction file, **When** that instruction file is read, **Then** the rule is still stated there.
3. **Given** the two project instruction files, **When** both are read, **Then** no operational rule
   appears in both.

### User Story 4 - A large procedure loads only when it is needed (Priority: P2)

An agent editing a XAML or C# file is not handed a 27 KB end-to-end UI-automation procedure. It is
handed a short routing note that names a skill, and the skill loads when the task actually needs it.

**Why this priority**: This is the single largest per-edit context cost in the repository, and it is
paid on every code edit whether or not the task is about UI automation.

**Independent Test**: Edit a XAML file, measure the instruction text that loads, and confirm the skill
is discoverable by its description and reachable from the routing note.

**Acceptance Scenarios**:

1. **Given** a task that edits XAML or C#, **When** the applicable instructions load, **Then** the
   matched instruction files total a small routing note rather than the full procedure.
2. **Given** a task that does need UI automation, **When** the agent looks for guidance, **Then** a
   skill exists whose description and routing note point at the procedure.

### User Story 5 - The agent and skill catalogues contain only loadable entries (Priority: P3)

An agent browsing the available agents and skills sees only entries that load and do something. No
one-line file points at an extension that is not installed, and the same behaviour is not installed
three times over.

**Why this priority**: It is a discoverability and hygiene issue. It does not change what the agent can
do, but a broken entry is indistinguishable from a working one in a listing.

**Independent Test**: Enumerate the agent files and confirm each carries a valid header; enumerate the
skill folders and confirm none duplicates a registered command.

**Acceptance Scenarios**:

1. **Given** the agent folder, **When** every file is checked for a valid header, **Then** every file
   has one.
2. **Given** an agent file that only points at an uninstalled extension, **When** the folder is
   inspected, **Then** that file is gone.
3. **Given** the skill catalogue, **When** it is counted, **Then** it contains no folder that duplicates
   a companion command installed as an agent.

### Edge Cases

- A deleted MCP server is needed again later. The instruction files must not forbid re-adding it; the
  restore is a small, documented step, and the in-app agent it requires is a separate change.
- A deleted skill folder is resolved by a host that reads skills instead of agents. One companion
  command must be verified working after the deletion.
- A rule appears to be duplicated but is worded differently in the two files. Only a genuine
  restatement is removed; a differently-scoped rule stays.
- The optional workstation-level duplicate of the scaling-routing instruction cannot be removed from
  another machine. The repository change stands on its own and the machine action is left to the owner.
- The UI-automation procedure contains facts that the project instructions also state. Moving it must
  not leave a claim with no home, and must not leave the Windows UI Automation material — which still
  works today — deleted along with the tool it replaced.

## Requirements

### Functional Requirements

- **FR-001**: The repository MCP configuration MUST declare no server that has nothing to attach to and
  no server whose documented primary operation fails on the repository's own data.
- **FR-002**: The workspace editor MCP configuration MUST NOT duplicate a server that the repository
  configuration already declares, and MUST NOT point at a storage path that does not exist.
- **FR-003**: The workspace settings MUST NOT keep a model allow-list entry for a server that will not
  start; if the server it names is removed, the entry MUST be removed with it.
- **FR-004**: Any documentation section whose subject is a removed server MUST be deleted from the
  project instructions, except where that section also records tooling that still works, which MUST be
  kept.
- **FR-005**: The CSV schema guidance MUST keep a working read-only way to inspect the schema exports
  after any server removal.
- **FR-006**: Every note path named in the project instructions MUST resolve to a file in this
  repository.
- **FR-007**: Every count, version and package claim in the project instructions MUST match a
  measurement of the repository at the time of the change.
- **FR-008**: The instruction that tells the agent how to run scripted automation in this repository
  MUST distinguish between a claim that is true everywhere and one that is true only on a particular
  workstation.
- **FR-009**: The instruction file that describes where documentation servers live MUST agree with the
  actual configuration, and MUST NOT state a retired rationale for a registration decision.
- **FR-010**: The project instructions MUST NOT restate a rule that an always-applied instruction file
  already carries.
- **FR-011**: The project instructions MUST NOT carry a generic execution-workflow section that
  duplicates harness behaviour, and the subagent model guidance MUST be reduced to the operative rule
  with its provenance left in the repository memories.
- **FR-012**: The UI-automation instruction file MUST be reduced to a routing note that keeps its
  applicability pattern, and the procedure MUST move to a skill that loads on demand.
- **FR-013**: The routing instruction for WinUI 3 skills MUST list the new UI-automation skill so it can
  be discovered from task wording.
- **FR-014**: Every agent file in the repository MUST carry a valid header with a name and description.
  Files that only point at an extension that is not installed MUST be deleted.
- **FR-015**: The project orientation file MUST stay the short orientation and MUST NOT repeat an
  operational rule owned by another instruction file; it MUST point at the owning file instead.
- **FR-016**: Skill folders that duplicate a command already installed as an agent MUST be removed, and
  the remaining catalogue MUST be counted and reported.
- **FR-017**: No change in this feature MUST introduce a new server, a new permission, or a new
  dependency.
- **FR-018**: Each removed rule MUST be traceable to the file that still owns it; a rule with no owner
  MUST NOT be deleted.

### Key Entities

Not applicable. This feature changes configuration and instructions, not data.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Zero MCP server registrations in the repository remain that cannot serve: the count of
  servers declared across the repository and workspace editor configuration that fail a connectivity or
  primary-operation check is 0.
- **SC-002**: For every server name mentioned in any instruction file, 100% resolve to a registered
  server; for every file path mentioned, 100% resolve to an existing file.
- **SC-003**: Every stated figure in the project instruction files matches its measurement: project
  count, guardrail-test count, SDK version and package list all agree, with 0 mismatches.
- **SC-004**: Always-loaded instruction text shrinks by at least 6,000 bytes with 0 rules lost — every
  deleted statement is found, in the file that owns it, by a follow-up read.
- **SC-005**: The instruction text that loads on a XAML or C# edit shrinks by at least 25,000 bytes.
- **SC-006**: 100% of agent files carry a valid header, and the count of agent files pointing at an
  uninstalled extension is 0.
- **SC-007**: The skill catalogue drops from 43 to 20 — 24 duplicate companion folders removed and one skill
  added for the moved UI-automation procedure — with at least one companion command verified working
  afterwards.
- **SC-008**: No application project, package reference or library is modified: the solution's diff
  contains 0 changes outside AI-facing configuration and documentation paths.

## Non-goals

- Upgrading, adding or removing any NuGet, npm or Python package.
- Any change to application code, XAML views, view models, services or database artifacts.
- Performance, security-CVE, UX, architecture, test-coverage or localization work.
- Changes to another workstation's user-level settings, except the one optional duplicate instruction
  file the owner may delete locally.
- Adding the in-process agent that would make the UI-inspection server usable again. Removing that
  server is not permission to build it.
- Introducing CI, linting or packaging changes.

## Change-level rules

- No new MCP server, permission, dependency or user-level requirement may be introduced to make a step
  pass.
- The repository copy of any instruction file stays canonical; no change may depend on one machine's
  user folder.
- A rule may only be deleted when the file that still owns it is named in the change.
- Deletions and edits are ordered so that a shared file is touched once: facts first, then deletions,
  then compression, then cross-file de-duplication.
- Server removals are ordered so the server that is removed entirely is handled before the duplicate
  that would otherwise be re-keyed.
- Where a section both documents a dead tool and records working tooling, only the dead-tool material
  is removed.

## Verification plan

1. Read the repository and workspace MCP configuration and settings; confirm only serving servers are
   declared and no allow-list entry names a removed server.
2. Run the measurements the instruction files claim — solution project count, guardrail-test count, the
   application project's Windows App SDK version, the package reference list — and compare each against
   the stated figure.
3. Resolve every file path named in the instruction files.
4. Sum the bytes of the always-applied instruction files before and after, and for a sample of removed
   statements, read the owning instruction file and find the statement there.
5. Sum the bytes of instruction files matched by the XAML/C# pattern, before and after; confirm the new
   skill exists and is named by the routing instruction.
6. Check every agent file for a header; count the skill folders; run one companion command to confirm it
   still resolves.
7. Confirm the solution builds and its guardrail tests pass, proving no configuration change broke the
   build.

## Assumptions

- The 14 proposals in the source specification are all approved; this feature implements them as
  written, with no proposal added or dropped.
- The five headerless agent files for an extension that is not installed in this project are deleted
  rather than given headers, because the extension does not appear in the project's extension registry.
  Re-adding them is a single step if that extension is installed later.
- The dead-notes proposal is resolved by creating the missing troubleshooting note from the verified
  content that survives the other deletions, rather than by deleting every pointer that names it, so
  the lesson about a stale tool snapshot is not lost.
- The optional workstation-level duplicate instruction file is left to the owner; the repository change
  does not depend on it.
- Removing the UI-inspection server removes its instruction section; the paragraph recording that
  Windows UI Automation is the tooling that actually works today survives that deletion.
- The build and guardrail tests are the proof that nothing in the application was disturbed.

## Verbatim Constraints

- Server names, exactly as registered: `csv-mcp-server`, `xamlmcp`.
- Configuration keys and files: `.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json`,
  `chat.mcp.serverSampling`, `mcpServers`, `servers`, `CSV_STORAGE_PATH`.
- Instruction and orientation files: `.github/copilot-instructions.md`, `AGENTS.md`,
  `.github/instructions/mcp-doc-research.instructions.md`,
  `.github/instructions/winui3-ui-automation.instructions.md`,
  `.github/instructions/winui3-scaling-routing.instructions.md`.
- Memory path: `.github/memories/repo/mcp-tooling.md`.
- Subagent model identifier: `deepseek/deepseek-flash`, distinct from `deepseek-v4-flash`.
- Framework version: `2.4.0`; target framework: `net10.0-windows10.0.19041.0`.
- Package and counts that must stay exact: `CommunityToolkit.WinUI.Controls.SettingsControls`,
  `17` projects, `17` `*AuditTests`.
