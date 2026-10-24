# Frontmatter reference

All fields are optional unless marked required. Quote any scalar containing `: `
(colon + space) or a leading special character. An unquoted colon-space makes the
YAML invalid and the file loads with no metadata — a silent failure.

## Instructions (`.instructions.md`)

| Field | Purpose |
| --- | --- |
| `name` | Display name; defaults to the file name |
| `description` | Capability + trigger; lets the agent load it on demand |
| `applyTo` | Glob, relative to workspace root, that auto-attaches the file |

```markdown
---
name: 'Python testing'
description: 'Pytest conventions for this repo. Use when adding or changing tests.'
applyTo: '**/test_*.py'
---
```

Omitting both `description` and `applyTo` means manual attachment only.

## Prompt files (`.prompt.md`)

| Field | Purpose |
| --- | --- |
| `name` | Slash-command name; defaults to the file name |
| `description` | Short picker text |
| `argument-hint` | Placeholder shown in the input box |
| `agent` | `ask`, `agent`, `plan`, or a custom agent name |
| `model` | Model to run with; defaults to the picker |
| `tools` | Tool allow-list; `<server>/*` includes a whole MCP server |

Setting `tools` silently switches the default agent to `agent`.

## Custom agents (`.agent.md`)

| Field | Purpose |
| --- | --- |
| `name` | Picker label |
| `description` | What it does and when to use it |
| `tools` | Tool allow-list |
| `agents` | Subagent allow-list: `['*']` or omit = all, `[]` = none |
| `model` | Model or preference-ordered list |
| `reasoning-effort` | `low`, `medium`, `high`, `xhigh`, `max` |
| `user-invocable` | `false` hides it from the picker (subagent-only) |
| `disable-model-invocation` | `true` blocks use as a subagent |
| `target` | `vscode` or `github-copilot` |

`infer` is deprecated. `.chatmode.md` is retired — rename to `.agent.md`.

## Skills (`SKILL.md`)

| Field | Required | Purpose |
| --- | --- | --- |
| `name` | yes | Bare identifier; must match the directory name |
| `description` | yes | Capability + trigger, max 1024 characters |
| `argument-hint` | no | Hint when invoked as a slash command |
| `user-invocable` | no | `false` hides it from `/` but keeps auto-loading |
| `disable-model-invocation` | no | `true` requires manual invocation |
| `context` | no | `fork` runs it in a subagent context |

`context: fork` requires `github.copilot.chat.skillTool.enabled`.

## Failure modes seen in practice

| Symptom | Cause |
| --- | --- |
| File listed but no description or applyTo | Unquoted `: ` in a value, or invalid YAML |
| Skill never appears in `/` | Name contains `/` or `:` |
| Skill never auto-loads | Description lacks a "use when..." trigger |
| Agent missing from picker | `user-invocable: false` |
| Agent never chosen as subagent | `disable-model-invocation: true`, or `agents` list excludes it |
| Instructions ignored | Wrong harness selected, or the file is in an unsupported scope |
