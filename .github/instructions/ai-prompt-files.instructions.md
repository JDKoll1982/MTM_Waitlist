---
name: 'Prompt files'
description: 'Authoring and using .prompt.md reusable prompts: frontmatter fields, arguments and variables, tool scoping, user vs workspace storage. Use when creating a repeatable slash-command workflow or converting a repeated prompt into a file.'
applyTo: '**/*.prompt.md'
---
# Prompt files (`.prompt.md`)

Verified on VS Code 1.143.1.

## What they are

Standalone Markdown files invoked as slash commands in chat. Unlike custom instructions they are **not** applied automatically — you invoke them.

Use them to:

* Encode a repeated task (scaffold a component, run and fix tests, prepare a PR).
* Override a custom agent's default behavior for one workflow (for example, demand a minimal plan).
* Package a prompt you want to invoke by name rather than retype.

## Frontmatter

| Field | Required | Purpose |
| --- | --- | --- |
| `name` | No | Command name after `/`; defaults to the file name |
| `description` | No | Short description in the picker |
| `argument-hint` | No | Placeholder text guiding the expected input |
| `agent` | No | `ask`, `agent`, `plan`, or a custom agent name. Defaults to the current agent; if `tools` is set, the default becomes `agent` |
| `model` | No | Model to run the prompt with; defaults to the current picker selection |
| `tools` | No | Tool allow-list for this prompt. Supports built-ins, MCP and extension tools, and `<server>/*` for a whole MCP server |

If a listed tool is unavailable at run time it is silently ignored.

## Body

* Markdown instructions, written to be reusable — no session-specific assumptions.
* Reference files with Markdown links relative to the prompt file, or `#file:`.
* `~` / `~/` in a path resolves from your user home, e.g. `~/prompts/style-guide.md`.
* Use `${selection}` and other built-in variables plus input variables to parameterize instead of duplicating files.

## Locations

| Scope | Location |
| --- | --- |
| Workspace | `.github/prompts` |
| User (VS Code profile) | Profile user-data `prompts` folder — on Windows `%APPDATA%\Code\User\prompts` |

Create them with `Chat: New Prompt File`, or with `/prompts` in the input (Local harness) to open the Configure Prompt Files menu. Workspace prompts are available to that workspace; user prompts are available across workspaces in the same profile.

Note: `chat.promptFilesLocations` is **deprecated** (default `{ ".github/prompts": true }`). User-scope prompt files live in profile storage; for guidance that must reach other Copilot clients, prefer a skill (`SKILL.md`) instead — the docs' migration path converts prompts to skills.

## Discovery and recommendations

* `chat.promptFilesRecommendations` (default `[]`) surfaces chosen prompts as suggested actions in a new session. Accepts prompt-name → boolean or `when` clauses.
* Prompt sources are additive: built-in, profile, workspace, and extension-contributed.

## Ground rules

* One prompt = one repeatable workflow. If it needs branching decisions, it should be an agent or skill.
* Set `tools` when the workflow must not touch anything destructive; leave it unset when the workflow is genuinely open-ended.
* Test a prompt with the editor play button before relying on it in a team workflow.
* Do not put secrets or absolute machine paths in a prompt file.
