# Research: AI Development Environment Remediation

**Feature**: `specs/023-ai-tooling-remediation`
**Date**: 2026-11-26

Everything below was measured in the repository or read from official documentation. No decision
rests on recollection.

## D1 — What counts as a valid agent header in this repository

**Decision**: A `.agent.md` file is valid when it opens with YAML frontmatter carrying `description`.
`name` is not required.

**Rationale**: Microsoft Learn's agent-file reference lists `description` as required and `name` as
optional, with the display name falling back to the file name. A measurement of the folder agrees:
92 of 109 files carry frontmatter and only 3 of those carry `name`, because the generated agents rely
on the filename.

**Alternatives considered**: Requiring both fields, which is what the source specification asks for in
words. Rejected: it would fail 89 files that load correctly today, and adding a hand-written `name` to
generator-owned files is drift the next generation run would erase.

**Consequence**: FR-014 and SC-006 are read as "every agent file parses and has a description".
The 17 files without frontmatter are the ones that fail.

## D2 — Which agent files are deleted, and why the count is 17 not 5

**Decision**: Delete all 17 headerless files: 12 one-line pointers to `changelog`, `ci-guard` and
`pr-bridge`, and 5 `speckit.superspec.*` files.

**Rationale**: None of those four extensions appears in `.specify/extensions/.registry`, which lists
eight (bug, assess, companion, harness, review, optimize, spectest, wireframe). Each pointer therefore
resolves to a path that does not exist, and each superspec file lacks the required `description`.
Deleting them takes the folder from 109 to 92 files, all loadable.

**Alternatives considered**: Giving the five superspec files headers, as the source specification
allows. Rejected because the extension they invoke is not installed; a header would make an uninvokable
agent look usable.

## D3 — Why 24 skill folders go, including 7 with no agent twin

**Decision**: Delete every `speckit-companion-*` folder under `.github/skills/`, including the seven
`living-*` folders that have no agent counterpart.

**Rationale**: The companion extension's registry entry reports `registered_skills: []`, so no
extension installs these folders; they are a third copy of instructions whose source of truth is the
extension and whose runtime is the `speckit.companion.*` agents. The nineteen skills that remain are the
repository's own. One companion command is run after the deletion to prove the host still resolves it.

**Alternatives considered**: Keeping the seven living skills because nothing obvious replaces them.
Rejected: they are companion documentation, and a skill that no extension registers is exactly the
duplicate this proposal removes. If the host turns out to need them, restoring the folder is one copy.

## D4 — How the XamlMcp removal keeps the tooling that still works

**Decision**: Delete the `## XamlMcp UI Inspection` section from `.github/copilot-instructions.md` and
the `xamlmcp` block from `.mcp.json`, but keep the paragraph that records Windows UI Automation as the
approach that works today, with its pointers to `.github/memories/repo/ui-automation-drive.md` and
`docs/scripts/scripts-automation-ui-automation.md`.

**Rationale**: The section mixes a capability the repository does not have (no in-process agent, so the
server has nothing to attach to) with a capability it does have and uses. Deleting the whole section
would delete working guidance; keeping the whole section keeps ~15 dead tool definitions per request.

**Alternatives considered**: Keeping a one-line "unavailable" note. Rejected: it is stale the moment the
agent is restored, and it still advertises a tool that is gone.

## D5 — Restoring the server is a documented step, not a lost one

**Decision**: Record in the rewritten doc-server table that `xamlmcp` is unregistered because the
in-process agent is absent, and that re-registering it belongs to the change that adds the agent.

**Rationale**: The removal must not read as a decision against the tool. The restore is one JSON block
plus the instruction section, and a future reader needs to know that.

**Alternatives considered**: Recording nothing. Rejected: the repository would then hold no explanation
for a capability its own history mentions.

## D6 — Writing the missing troubleshooting note rather than deleting its pointers

**Decision**: Create `.github/memories/repo/mcp-tooling.md` and add it to the `## Index` in
`.github/memories/README.md`.

**Rationale**: Two pointers in `copilot-instructions.md` name it, and the lesson it was created for —
a running session holding a stale tool snapshot after a server is registered — survives every deletion
in this feature and applies to any server. Creating the note satisfies FR-006 and keeps the lesson.

**Alternatives considered**: Deleting both pointers, which AI-002 and AI-005 also allow. Rejected: the
stale-snapshot lesson is the single most useful MCP troubleshooting fact in the repository.

## D7 — The `codegraphy-setup.md` pointer is reworded, not satisfied by creating a file

**Decision**: Reword the sentence so it no longer names a path that does not exist, while keeping the
rule that the retired decision must not be revived.

**Rationale**: FR-006 asks every named note path to resolve. Creating a file whose only purpose is to
say "do not use this" would be worse than removing the path: the rule belongs where it is written, and
the path was the only thing that was wrong.

**Alternatives considered**: Creating the file. Rejected: it manufactures a note about a retired tool.

## D8 — The UI-automation procedure becomes a skill with a routing note

**Decision**: Create `.github/skills/winui3-ui-automation/SKILL.md` with the procedure and its
supporting detail under `references/`, reduce
`.github/instructions/winui3-ui-automation.instructions.md` to the routing shape used by
`.github/instructions/winui3-scaling-routing.instructions.md`, and add a row for the new skill to that
file's routing table.

**Rationale**: Microsoft Learn documents skills for reusable multi-step workflows and instruction files
for conventions; this file is a 419-line procedure. Its `applyTo` pattern also matches `.ps1` and
`.resw`, so the saving is larger than the XAML/C# measurement shows. The routing note keeps the
`applyTo` glob, so the trigger is unchanged and only the payload moves.

**Alternatives considered**: Narrowing the glob. Rejected: UI-automation work legitimately touches
PowerShell and resource files.

**Consequence**: `records/ui-automation-verification.md` has no frontmatter, so it never loads as an
instruction. It is referenced from inside the procedure and must keep resolving after the move.

## D9 — Project-instruction sections that are restatements are deleted, not rewritten

**Decision**: Delete the "MCP-First Documentation and Code Sample Retrieval", "Output Control
Invariants" and "Document Authoring" sections from `.github/copilot-instructions.md`, and delete the
"Multi-File Agentic Execution Workflow" section.

**Rationale**: Each of the first three restates a rule that an always-applied instruction file already
carries (`mcp-doc-research`, `response-format`, `document-authoring`). The fourth describes a generic
discover-edit-build loop that the harness already performs and that the constitution does not require.

**Alternatives considered**: Deleting the instruction files instead. Rejected: their `applyTo` scoping
lets them be tuned without touching project instructions, and `spec-kit.instructions.md` depends on
`response-format`.

## D10 — The subagent guidance keeps the rule and loses the provenance narrative

**Decision**: Compress the subagent-model section in `copilot-instructions.md` to the operative rule:
use `deepseek/deepseek-flash`, and it is a different model from `deepseek-v4-flash`. The provenance
narrative already lives in `.github/memories/repo/subagent-models.md`.

**Rationale**: The rule is what an agent must follow; how the model ids were verified is background a
reader can find in the note.

**Alternatives considered**: Keeping the four-row table. Rejected: it is 1.7 KB of always-on context
that restates a note the repository already maintains.

## D11 — The package list in the orientation file is corrected to the full solution set

**Decision**: Replace the partial package list in `AGENTS.md` with the packages the solution actually
references, including `CommunityToolkit.WinUI.Controls.SettingsControls`.

**Rationale**: The measurement found the listed packages are a subset: `Microsoft.Data.SqlClient`,
`MySqlConnector`, the `Microsoft.Extensions.*` set, `Microsoft.Xaml.Behaviors.WinUI.Managed`,
`CommunityToolkit.WinUI.UI.Animations`, the cryptography packages, `Microsoft.Windows.SDK.BuildTools`
and the test stack are all referenced and unlisted. A partial list invites an agent to assume a package
is absent.

**Alternatives considered**: Adding only the one package the source specification names. Rejected: it
leaves the rest of the list wrong, which is the defect FR-007 exists to fix.
