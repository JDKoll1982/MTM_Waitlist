---
name: mcp-doctor
description: Diagnose Model Context Protocol server configuration and connectivity, including unresolved input placeholders, bad transports, endpoint failures, and tool-level auth errors. Use when an MCP server will not connect, its tools are missing, a tool returns an error while the server looks healthy, or a config works in one client but not another.
argument-hint: '[config path or server name]'
---

# MCP doctor

An MCP server can initialize successfully and still fail every tool call. Always
test at the **tool** level, not just the handshake.

## Run the validator

```bash
python scripts/mcp_check.py --config "%APPDATA%\Code\User\mcp.json"
python scripts/mcp_check.py --config ~/.copilot/mcp-config.json --probe
```

It checks JSON validity, transport shape, destination rules, unresolved
`${input:...}` placeholders, and with `--probe` performs a real `initialize` +
`tools/list` round trip.

## The failure that looks like a working server

An `Authorization` header whose value is still `${input:Authorization}` is sent
literally when the prompt input is never resolved. The server answers `initialize`
with HTTP 200, so the server appears connected, and only the tool call fails —
typically with a message about an invalid or missing key.

```
initialize          -> 200 OK        (looks fine)
tools/call          -> isError: true (the real problem)
```

Rules:

* If a server needs no auth, do not add an `Authorization` header or an `inputs`
  entry for it. Prefer the least configuration.
* If a header came from a gallery install that assumed a key, remove both the
  header and its now-unused `inputs` entry.
* VS Code caches MCP definitions per window. After editing `mcp.json`, reload the
  window before concluding the fix failed.

## Config destinations

| Goal | Destination |
| --- | --- |
| All workspaces, Copilot | `$COPILOT_HOME/mcp-config.json`, else `~/.copilot/mcp-config.json` |
| Team, portable | workspace `.mcp.json` (or `.github/mcp.json` for the CLI) |
| Local harness with inputs or per-server sandboxing | `.vscode/mcp.json` or profile `mcp.json` |

Portable Copilot configs do **not** support VS Code input variables or environment
files, and servers requiring interactive inputs are not forwarded to Copilot
harness sessions. A config that works in `.vscode/mcp.json` can therefore fail
after being moved to `~/.copilot/mcp-config.json`.

## Transports

```json
{ "servers": { "docs": { "type": "http", "url": "https://example.com/mcp" } } }
{ "servers": { "pw": { "type": "stdio", "command": "npx", "args": ["-y", "@playwright/mcp@latest"] } } }
```

Remote uses `url`; stdio uses `command` + `args` (+ optional `env`). Mixing them —
`type: http` with a `command` — is a common copy-paste error.

## Triage order

1. Is the file valid JSON, and is it the file the client actually reads?
2. Does the endpoint answer `initialize` (HTTP 200 + a result)?
3. Does `tools/list` return the tools you expect?
4. Does a real `tools/call` succeed, or return `isError: true`?
5. Did you reload the window after editing?

Step 4 is the one people skip.

## Guardrails

* Never print, commit, or paste a credential to test connectivity. Redact values
  and report only their shape.
* Local stdio servers execute code on your machine; review the command before
  approving. Prefer a pinned version over `@latest` for anything shared.
* Server-provided initialization instructions can influence the model. Do not
  blanket-approve them unless you trust every configured server.
