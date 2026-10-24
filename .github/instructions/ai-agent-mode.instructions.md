---
name: 'Agent mode and approvals'
description: 'Agent, Plan, and Ask modes; tool approvals; autopilot; terminal auto-approve rules; sandboxing; subagents and handoffs. Use whenever running tools, executing terminal commands, or deciding whether an action needs confirmation.'
applyTo: '**'
---
# Agent mode, tools, and approvals

Verified on VS Code 1.143.1.

## Modes

| Mode | Behavior | Reach for it when |
| --- | --- | --- |
| Ask | Answers, does not edit files | Explaining, reviewing, planning |
| Plan | Produces a plan, no edits | Scoping before implementation |
| Agent | Edits files, runs commands, calls tools | You want the outcome implemented |
| Autopilot | Agent with reduced prompting until done | Well-specified, low-risk, reversible work |

Plan mode output is the contract for the implementation. If a plan changes scope, say so instead of silently expanding it.

## Two security layers

1. **Approvals** — decide whether an action runs automatically or needs confirmation.
2. **Sandboxing** — restricts the file system and network reachable by agent terminal commands, even after approval (`chat.agent.sandbox.enabled`).

Approval is not a security boundary on its own. In high-risk environments, sandbox or use a container.

## Terminal auto-approve

The agent uses one tool to run any terminal command, so VS Code evaluates each command separately. Configure `chat.tools.terminal.autoApprove`:

* `true` — auto-approve.
* `false` — require approval. This does **not** block the command.
* Wrap a pattern in `/` for a regular expression.

```json
{
  "mkdir": true,
  "/^git (status|show\\b.*)$/": true,
  "del": false,
  "/dangerous/": false
}
```

Rules match each **subcommand** by default: a compound command is auto-approved only if every subcommand matches a `true` rule and none matches a `false` rule. `false` always wins. To match the whole command line, use object syntax with `matchCommandLine: true`.

Related:

| Setting | Default | Effect |
| --- | --- | --- |
| `chat.tools.terminal.enableAutoApprove` | `true` | Set `false` to require approval for every command |
| `chat.tools.terminal.blockDetectedFileWrites` | `"outsideWorkspace"` | `never` \| `outsideWorkspace` \| `all` (experimental) |
| `chat.tools.terminal.ignoreDefaultAutoApproveRules` | `false` | (Experimental) Ignore built-in rules and use only yours |
| `chat.tools.terminal.autoReplyToPrompts` | `false` | Auto-answer interactive terminal prompts |
| `chat.tools.terminal.preventShellHistory` | `true` | Keep agent commands out of shell history |
| `chat.tools.terminal.idlePollInterval` | `1000` | Poll interval used to detect a finished command |
| `chat.tools.terminal.detachBackgroundProcesses` | `false` | (Experimental) Let the agent detach long runs |
| `chat.tools.terminal.enforceTimeoutFromModel` | `true` | (Experimental) Honor agent-specified command timeouts |
| `chat.tools.edits.autoApprove` | `{}` | Per-path edit approval rules |
| `chat.tools.urls.autoApprove` | `{}` | Per-domain URL approval rules |
| `chat.tools.global.autoApprove` | `false` | (Experimental) One switch for every tool — do not enable casually |
| `chat.tools.riskAssessment.enabled` | `true` | (Experimental) Risk badge on command confirmations |
| `chat.tools.compressOutput.enabled` | `false` | Compress large terminal output before sending |
| `chat.tools.todos.showWidget` | `true` | Todo widget for agent task tracking |

Deprecated, do not set: `chat.tools.terminal.shellIntegrationTimeout` (use `terminal.integrated.shellIntegration.timeout`) and `chat.tools.terminal.backgroundNotifications`.

Known limitation: PowerShell/bash tree-sitter grammars miss some subcommands, `zsh`/`fish` are parsed as bash, and file-write detection is best effort. Obfuscated commands can evade matching.

## Guardrails

| Setting | Default | Effect |
| --- | --- | --- |
| `chat.agent.enabled` | `true` | Enable agent mode |
| `chat.agent.maxRequests` | `50` | Cap requests per agent turn; resolves through entitlement policy, and the Agents window uses `1000` |
| `github.copilot.chat.agent.autoFix` | `true` | Diagnose and fix issues in generated changes |
| `chat.agent.collapseCompletedResponses` | `true` | Collapse finished responses |
| `chat.agent.thinkingStyle` | `"fixedScrolling"` | `collapsed` \| `collapsedPreview` \| `fixedScrolling` |
| `chat.subagents.allowInvocationsFromSubagents` | `false` | Let a subagent spawn further subagents |
| `chat.subagents.showCreditUsage` | `false` | Show credit usage per subagent |
| `chat.tools.terminal.outputLocation` | `"chat"` | Show command output inline vs in the terminal |
| `chat.autopilot.advanced.enabled` | `false` | (Experimental) Advanced autopilot behaviour |

## Delegation

* A **subagent** runs in its own context and returns a focused result; use it to keep intermediate work out of the main thread.
* A **handoff** switches the active agent mid-conversation. Use it to move from planning to implementation; use a subagent when you do not want to switch.
* Custom agents can restrict `tools` and be made subagent-only with `user-invocable: false`.

## Ground rules

* Never widen permissions to make a task pass. If a command is denied, report it and continue with what is permitted.
* Never claim a command was tested if the shell never ran it.
* Do not add auto-approve entries for destructive commands; the default deny rules exist to be kept.
* State clearly when an action was auto-approved versus explicitly confirmed.
