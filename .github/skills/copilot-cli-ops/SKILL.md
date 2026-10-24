---
name: copilot-cli-ops
description: Run GitHub Copilot CLI correctly for both interactive work and scripting, and diagnose why an instruction, skill, agent, MCP server, or plugin is not active. Use when invoking copilot from a terminal, writing a non-interactive pipeline, defining permission patterns, or debugging CLI configuration and discovery.
argument-hint: '[goal or failing command]'
---

# GitHub Copilot CLI operations

Verified against GitHub Copilot CLI 1.0.98.

## Pick the mode

```bash
copilot                          # interactive TUI
copilot -i "prompt"              # interactive, auto-run this prompt
copilot -p "prompt"              # non-interactive, exits when done
copilot -p "prompt" -s           # response text only, for piping
```

Non-interactive mode cannot ask for permissions, so grant them up front or it stops.

## Diagnose before guessing

```bash
copilot --version
copilot instruction list         # every instruction source this process would load
copilot skill list               # personal / builtin / plugin
copilot mcp list                 # configured + builtin servers
copilot lsp                      # language server configuration
copilot config --list            # current settings
```

Inside an interactive session, `/env` prints what is actually loaded — instructions, MCP servers, skills, agents, hooks, plugins, LSPs, and extensions. Reach for it before assuming a file is missing.

## What discovery actually means

`copilot instruction list` reports sources, not compliance, and it does **not** filter by `applyTo`. Treat personal instruction files as always-on: keep them short and let bulky reference material live in a skill, which loads progressively.

If a file is listed but its description is missing, suspect invalid YAML. An unquoted `: ` (colon + space) inside a value silently discards the whole frontmatter block.

## Permission patterns

Form is `kind(argument)`:

| Pattern | Meaning |
| --- | --- |
| `shell(git:*)` | Any `git` subcommand; matches the command stem, so not `gitea` |
| `shell(npm test)` | Exact command |
| `write(.env)` | File writes matching trailing path components |
| `write(C:\repo\.env)` | Absolute path, single location |
| `server(tool)` | One MCP tool; `server` alone means all its tools |
| `url(https://api.github.com)` | One origin; protocol-aware |

Rules that surprise people:

* Deny always beats allow, **even** `--allow-all-tools`.
* `--available-tools` / `--excluded-tools` decide what the model can *see*; `--allow-tool` / `--deny-tool` decide what needs *approval*. They are independent — a filtered-out tool cannot be allowed back.
* URLs are protocol-aware: allowing `https://` does not allow `http://`.
* Path access defaults to the working directory, its subdirectories, and the temp dir. `--allow-all-paths` removes that check; `--disallow-temp-dir` removes the temp exemption.
* `--allow-all` / `--yolo` = `--allow-all-tools --allow-all-paths --allow-all-urls`.

## Patterns that work in scripts

```bash
copilot -p "summarize today's commits" --allow-tool 'shell(git:*)' -s
copilot -p "review src/" --output-format json --usage-output-file usage.json
copilot -p "explain this" --deny-tool 'shell' --available-tools 'read,search'
COPILOT_ALLOW_ALL=true copilot -p "..."          # must be exactly "true"
```

For reproducibility in pipelines:

* `--no-custom-instructions` ignores `AGENTS.md` and related files.
* `--no-ask-user` stops the agent asking questions; only where guessing is safe.
* `--secret-env-vars KEY` strips values from shell and MCP environments and redacts output.
* `--max-ai-credits N` and `--limits` cap spend.

## Configuration

```bash
copilot config --list
copilot config footer.showQuota off
copilot config allowedUrls github.com          # appends to a list
copilot config --rm allowedUrls github.com
copilot config --repo --list                   # .github/copilot/settings.json
copilot config --local --list                  # .github/copilot/settings.local.json
```

User settings live in `~/.copilot/settings.json`, or `$COPILOT_HOME/settings.json` when set. Repository files accept only the subset a repo may override.

## Failure modes

| Symptom | Likely cause |
| --- | --- |
| Command fails to start, `EPERM` / access denied under `%LOCALAPPDATA%` | Standalone binary cannot self-extract; point `LOCALAPPDATA` at a writable dir for that run |
| `--allow-tool` ignored | The tool was filtered out by `--available-tools`, or a deny rule wins |
| Skill missing from `/` | Name has `/` or `:`; or `user-invocable: false` |
| Instruction listed, no description | Invalid YAML frontmatter |
| `no such agent: X` | Useful — the error lists available agents |
| MCP tool missing | `--disable-mcp-server`, a `--tools` filter, or server not enabled |
