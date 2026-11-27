# Implementation Plan: AI Development Environment Remediation

**Feature**: `specs/023-ai-tooling-remediation/spec.md`
**Date**: 2026-11-26
**Size**: normal

## Summary

Bring the AI-facing configuration back to the truth by editing text and JSON only: remove the two MCP
server registrations that cannot serve, delete the instruction text that documents them, correct every
stale count and version, collapse the rules that duplicate an always-applied instruction file, move the
27 KB UI-automation procedure into a skill, and clean the agent and skill catalogues. Nothing in the
application is touched. Verification is measurement: byte counts before and after, every stated figure
re-measured, every named path resolved, every agent file parsed, and the existing build and guardrail
tests as the proof that no application behaviour moved.

## Project Structure

```
.mcp.json                                     both server blocks removed
.vscode/mcp.json                              duplicate csv-mcp-server removed (file may be deleted)
.vscode/settings.json                         chat.mcp.serverSampling block removed
.github/copilot-instructions.md               ~30.5 KB -> ~25 KB; facts fixed, sections deleted
AGENTS.md                                     counts fixed, duplicated blocks become pointers
.github/instructions/mcp-doc-research.instructions.md   server table rewritten against .mcp.json
.github/instructions/winui3-ui-automation.instructions.md   27 KB procedure -> short routing note
.github/instructions/winui3-scaling-routing.instructions.md  new skill added to the routing list
.github/skills/winui3-ui-automation/SKILL.md  new: the procedure, loaded on demand
.github/skills/speckit-companion-*/          24 folders deleted (19 skills remain)
.github/agents/speckit.{changelog,ci-guard,pr-bridge}.*.agent.md   12 dead pointers deleted
.github/agents/speckit.superspec.*.agent.md   5 headerless files deleted
.github/memories/repo/mcp-tooling.md          new troubleshooting note
.github/memories/README.md                    index bullet for the new note
```

**Structure Decision**: the repository's own layout already holds every file this feature changes, so
the plan is a list of edits at existing paths plus one new skill folder and one new memory note. No
project directory, `.csproj`, or source folder under `Module_*` is involved.

**Order of work** (shared files are touched once):

1. Facts first: `AGENTS.md` counts and package list, the SDK version in `copilot-instructions.md`.
2. Server removals before the duplicate that would otherwise be re-keyed: `csv-mcp-server` out of
   `.mcp.json`, then `.vscode/mcp.json` and the sampling block, then the `xamlmcp` block.
3. Instruction deletions: the XamlMcp section, the restatements, the workflow section, the compressed
   subagent rule.
4. The UI-automation move: create the skill, shrink the instruction, add the routing row.
5. Catalogue hygiene: 17 agent files and 24 skill folders deleted.
6. The memory note and its index entry.

## Constitution Check

| Principle | Assessment |
| --- | --- |
| I. Spec-First, Verified Delivery | PASS — spec written first; tasks are ticked only after their verification runs |
| II. Live Data Integrity | PASS — no store, cache or data path is touched |
| III. Stored-Procedure-First Database Discipline | PASS — no database artifact changes |
| IV. MCP-First, Grounded Decisions | PASS — removing servers needs no library grounding; the agent and skill header rules were verified against Microsoft Learn before FR-014 was interpreted (agent `description` is required, `name` is optional and derived from the filename; a skill requires both `name` and `description`) |
| V. WinUI 3 Platform Conformance | PASS — no XAML, view, view model or navigation change |
| VI. Evidence-Based Verification Gates | PASS — solution build and test project are the final gate; configuration changes are measured directly |
| VII. Readable, End-User-Facing Delivery | PASS — the reply reports what changed and what is open, with no raw file bodies |

No violation is needed, so there is no Complexity Tracking table.

## Phase 0 Research

See [research.md](./research.md). Decisions cover what "valid header" means for this repository, which
sections of the project instructions are restatements, how the XamlMcp removal keeps the working
Windows UI Automation material, and how the two dangling note pointers are resolved.
