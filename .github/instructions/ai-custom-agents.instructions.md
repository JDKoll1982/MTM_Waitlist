---
name: 'Custom agents'
description: 'Authoring .agent.md custom agents: frontmatter fields, tool scoping, model and reasoning effort, subagent-only agents, handoffs, and user-level storage. Use when creating a specialized role, restricting tools, or delegating to a subagent.'
applyTo: '**/*.agent.md'
---
# Custom agents (`.agent.md`)

Verified on VS Code 1.143.1. User-level discovery confirmed: an agent at `~/.copilot/agents/_probe.agent.md` resolved as `probe-agent` via `copilot --agent`.

## What they are

A reusable configuration of instructions + tools + optional model for a role (planner, reviewer, researcher). Selecting one applies it until you switch. In the Copilot harness the agent's instructions are added to your custom instructions and win on conflict.

## Frontmatter (all optional)

| Field | Type | Purpose |
| --- | --- | --- |
| `name` | string | Display name; defaults to file name |
| `description` | string | What it does and when to use it |
| `argument-hint` | string | Expected input hint (Local chat input) |
| `tools` | string / array | Tool allow-list. Names and aliases are harness-specific |
| `agents` | array | Custom agents usable as subagents: `['*']` or omitted = all, `[]` = none. Include the `agent` tool set in `tools` |
| `model` | string / array | Model, or preference-ordered list (first available wins). Omitted = current model |
| `reasoning-effort` | string | `low`, `medium`, `high`, `xhigh`, `max`; depends on model support |
| `user-invocable` | bool | `false` hides it from the agent picker (subagent-only). Default `true` |
| `disable-model-invocation` | bool | `true` prevents use as a subagent. Default `false` |
| `target` | string | `vscode` or `github-copilot` — the file's target environment, not the harness |
| `mcp-servers` | object | For Copilot cloud agent only; ignored by VS Code agents |

`infer` is deprecated — use `user-invocable` and `disable-model-invocation`.

## Locations

| Scope | Path | Formats |
| --- | --- | --- |
| Workspace (Copilot) | `.github/agents` | `.agent.md` or `.md` |
| Workspace (Claude) | `.claude/agents` | Claude agent format |
| User (Copilot) | `~/.copilot/agents` | `.agent.md` or `.md` |
| User (Claude) | `~/.claude/agents` | Claude agent format |

`chat.agentFilesLocations` defaults to `{ ".github/agents": true, ".claude/agents": true, "~/.copilot/agents": true }` — supported but not recommended for Copilot workflows, because other clients do not inherit editor settings.

`.chatmode.md` is retired: rename to `.agent.md` and move to a supported location. `chat.modeFilesLocations` is deprecated.

## Minimal example

```markdown
---
name: Planner
description: Produce an implementation plan without changing code
tools: ['read', 'search']
---
Investigate the request and return a step-by-step plan.
Name the files to change and the validation command per step. Do not edit files.
```

## Subagents and handoffs

* Subagent-only helper: `user-invocable: false`. It stays callable by the main agent but disappears from the picker.
* Prevent delegation into an agent: `disable-model-invocation: true`.
* A **subagent** works in its own context and returns a summary. A **handoff** switches the conversation to another agent, which changes who is driving.

Choose a subagent to keep intermediate exploration out of the main context; choose a handoff when the next role should take over the conversation.

## Ground rules

* Restrict `tools` to the minimum the role needs — a reviewer should not get edit tools.
* Write `description` so auto-selection works; a vague description means the agent is never picked.
* Do not set a `model` unless the role genuinely needs it; pinning models rots as the catalog changes.
* User-level agents are for *your* workflow. Team-wide roles belong in the repository.
