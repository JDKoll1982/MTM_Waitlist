---
name: 'Copilot CLI'
description: 'GitHub Copilot CLI: subcommands, run modes, permission patterns, interactive slash commands, config keys, and discovery commands. Use whenever working in a terminal with the copilot executable or scripting it non-interactively.'
---
# GitHub Copilot CLI

Verified against GitHub Copilot CLI 1.0.98.

## Shape of the tool

`copilot` starts an interactive TUI. Every capability is also reachable non-interactively, which makes it scriptable.

```bash
copilot                 # interactive session
copilot -i "prompt"     # interactive, auto-run this prompt first
copilot -p "prompt"     # non-interactive, exit when done
copilot -p "prompt" -s  # response text only, for scripting
```

Non-interactive mode requires permissions up front (see below); without them it cannot proceed.

## Subcommands

`app`, `login`, `help`, `init`, `config`, `update`, `version`, `workflow`, `sessions`, `memories`, `plugin`, `mcp`, `skill`, `instruction`, `lsp`, `sandbox`, `completion`.

Discovery commands worth knowing:

```bash
copilot instruction list   # every instruction source a session would load
copilot skill list         # personal / builtin / plugin skills
copilot mcp list           # configured + builtin MCP servers
```

`copilot help <topic>` covers `billing`, `commands`, `config`, `environment`, `limits`, `logging`, `monitoring`, `permissions`.

## Modes

| Flag | Effect |
| --- | --- |
| `--mode interactive\|plan\|autopilot` | Initial agent mode |
| `--plan` | Start in plan mode (combine with `--mode autopilot` to auto-approve and implement) |
| `--autopilot` | Start in autopilot |
| `--max-autopilot-continues <n>` | Continuation budget, default `5` |
| `--fleet` | Parallel subagent orchestration (with `-i`, `-p`, or piped stdin) |

## Model and context

`--model <name|auto>`, `--reasoning-effort none|minimal|low|medium|high|xhigh|max`, `--context default|long_context`, `--auto-tier <profile>`, `--agent <agent>`.

## Sessions

`-r/--resume [id|prefix|name]`, `--continue`, `-n/--name`, `--session-id <uuid>`, `--connect [id]`, `copilot sessions`, and in-session `/fork`, `/rewind`, `/compact`, `/resume`, `/rename`, `/worktree`, `/move`.

## Permissions

Pattern form is `kind(argument)`:

| Kind | Matches |
| --- | --- |
| `shell(command:*)` | Shell commands; `shell(git:*)` matches `git push` but not `gitea` |
| `write(path?)` | File writes outside the shell tool; relative paths match trailing components |
| `<server-name>(tool?)` | Tools from one MCP server |
| `url(domain-or-url?)` | URL access by the shell and fetch tools; protocol-aware |

Precedence rules that bite:

* Denial always beats allow, **even** `--allow-all-tools`.
* `--available-tools` / `--excluded-tools` decide what the model can *see*; `--allow-tool` / `--deny-tool` decide what needs *approval*. They are independent.
* Path access defaults to the working directory, its subdirectories, and the temp dir. `--allow-all-paths` disables that check; `--disallow-temp-dir` removes the temp exemption. URLs are protocol-aware: allowing `https://` does not allow `http://`.
* `--allow-all` / `--yolo` = `--allow-all-tools --allow-all-paths --allow-all-urls`.

Safer middle ground: `--assisted-approval` routes tool requests through a safety judge (probabilistic, not a security boundary; requires `--experimental` or `enabledFeatureFlags.AUTO_APPROVAL`).

## Interactive commands (selection)

| Group | Commands |
| --- | --- |
| Agent environment | `/init`, `/agent`, `/skills`, `/mcp`, `/plugin` |
| Agents | `/model`, `/delegate`, `/fleet`, `/autopilot`, `/tasks`, `/subagents` |
| Code | `/ide`, `/diff`, `/pr`, `/review`, `/security-review`, `/rubber-duck`, `/lsp`, `/terminal-setup` |
| Permissions | `/permissions`, `/allow-all`, `/sandbox`, `/add-dir`, `/list-dirs`, `/cwd`, `/reset-allowed-tools` |
| Session | `/resume`, `/rename`, `/fork`, `/worktree`, `/move`, `/context`, `/usage`, `/session`, `/compact`, `/share`, `/remote`, `/copy`, `/rewind` |
| Meta | `/help`, `/changelog`, `/statusline`, `/memory`, `/instructions`, `/env`, `/diagnose`, `/feedback`, `/clear`, `/new`, `/plan`, `/research`, `/refine`, `/settings`, `/limits`, `/login`, `/logout`, `/update`, `/experimental`, `/voice`, `/vim` |

`/env` prints what is actually loaded — instructions, MCP servers, skills, agents, hooks, plugins, LSPs, extensions. Use it before guessing why something is not active.

## Configuration

```bash
copilot config --list              # key=value
copilot config --list --json
copilot config theme               # read (unset key exits 1)
copilot config footer.showQuota off
copilot config allowedUrls github.com   # append to a list
copilot config --rm allowedUrls github.com
copilot config --repo --list       # .github/copilot/settings.json
copilot config --local --list      # .github/copilot/settings.local.json
```

User settings live in `~/.copilot/settings.json` (or `$COPILOT_HOME/settings.json`). Repository files accept only the settings a repo may override. Settings include `allowedUrls`, `deniedUrls`, `autoUpdate`, `banner`, `bannerStyle`, `bashEnv`, `beep`, `beepOnSchedule`, `notifications`, `terminalNotifications`, `powershellFlags` (default `["-NoProfile","-NoLogo"]`), `compactPaste`, `copyOnSelect`, `commandHistoryMaxSize`, `customAgents.defaultLocalOnly`, `proxyKerberosServicePrincipal`.

## VS Code ↔ CLI integration

VS Code ships toggles for the CLI experience. Most are now deprecated in favour of the CLI's own
features — only `github.copilot.chat.cli.terminalLinks.enabled` (default `true`) remains current.
Deprecated and not to be set: `cli.planCommand`, `cli.planExitMode`, `cli.forkSessions`,
`cli.isolationOption`, `cli.lazyLoadSessionItem`, `cli.autoModel`, `cli.mcp`, `cli.remote`,
`cli.sandbox`, `cli.branchSupport`, `cli.aiGenerateBranchNames`, `cli.autoCommit`,
`cli.sessionController`, `cli.sessionControllerForSessionsApp`.

`chat.agentSessions.migrateLegacyCopilotCli` (default `false`) and
`chat.agentSessions.showExternal` (default `"last7Days"`) control whether older CLI sessions appear
in the editor. If sessions seem missing, check the second setting before assuming data loss.

## Scripting patterns

```bash
copilot -p "summarize today's changes" --allow-tool 'shell(git:*)' -s
copilot -p "review src/" --output-format json --usage-output-file usage.json
copilot -p "explain this" --deny-tool 'shell' --available-tools 'read,search'
COPILOT_ALLOW_ALL=true copilot -p "..."      # narrowest grant: exact "true"
```

## Guardrails

* Never widen permissions to make a task succeed. Report the denial instead.
* `--secret-env-vars KEY` strips values from shell/MCP environments and redacts output — use it rather than trusting logs.
* `--no-custom-instructions` isolates a run from `AGENTS.md` and related files when you need reproducibility.
* `--no-ask-user` makes the agent work without asking; only use it where guessing is acceptable.
* If the standalone binary cannot self-extract (read-only `LOCALAPPDATA`), point `LOCALAPPDATA` at a writable directory for that invocation.
