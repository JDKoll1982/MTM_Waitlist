---
name: 'MCP servers'
description: 'Connecting Model Context Protocol servers: transports, per-client config destinations, VS Code mcp.json shape, trust checks, autostart, and the copilot mcp commands. Use when an agent needs a capability its built-in tools lack.'
applyTo: '**/*mcp*.json'
---
# MCP servers

Verified on VS Code 1.143.1 and Copilot CLI 1.0.98 (`copilot mcp list` reports the built-in `github-mcp-server` over HTTP).

## When to add one

Only when the agent needs a capability its existing tools do not provide — querying a database, an internal API, a docs service. If current tools suffice, an extra server is added attack surface and context cost.

## Transports

| Kind | Transport | Configure as |
| --- | --- | --- |
| Local process | stdio | `command` + `args` + optional `env` |
| Remote | HTTP / SSE | `url` + optional headers |

Local servers execute code on your machine; remote servers may require sign-in. Review publisher, command/endpoint, and requested access before installing.

## Config destinations

| Goal | Destination |
| --- | --- |
| Use across workspaces (Copilot) | Global → Copilot Global: `$COPILOT_HOME/mcp-config.json`, or `~/.copilot/mcp-config.json` when `COPILOT_HOME` is unset |
| Share with a team | Workspace-root `.mcp.json` (guided flow offers it when experimental `chat.mcp.workspaceRootConfig.enabled` is on) |
| Local harness with input variables or per-server sandboxing | Workspace `.vscode/mcp.json` or the user-profile `mcp.json` — still available, labeled deprecated in the guided flow |

Copilot CLI loads: user `~/.copilot/mcp-config.json`, workspace `.mcp.json` or `.github/mcp.json`, and servers bundled in installed plugins.

Portable (Copilot) configurations do **not** support VS Code input variables or environment files, and servers requiring interactive inputs are not forwarded to Copilot harness sessions. Use the authentication guidance for the configuration instead of inventing credential placeholders.

## VS Code `mcp.json` shape

```json
{
  "servers": {
    "playwright": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "@playwright/mcp@latest"]
    },
    "docs": {
      "type": "http",
      "url": "https://example.com/mcp"
    }
  },
  "inputs": []
}
```

`inputs` declares values prompted at start (API keys, paths) so secrets are not hardcoded.

## Settings

| Setting | Default | Effect |
| --- | --- | --- |
| `chat.mcp.access` | `true` | Which MCP servers may be used |
| `chat.mcp.discovery.enabled` | `false` | Auto-discover config from other applications |
| `chat.mcp.autostart` | `"newAndOutdated"` | (Experimental) Which servers start on submit |
| `chat.mcp.serverSampling` | `{}` | Models exposed to servers for sampling |
| `chat.mcp.apps.enabled` | `true` | MCP Apps (server-provided UI) |

## CLI operations

```
copilot mcp list
copilot mcp get <name>
copilot mcp add <name> -- <command> [args...]
copilot mcp add --transport http <name> <url>
copilot mcp add --transport http --header "Authorization: ..." <name> <url>
copilot mcp enable|disable|remove <name>
```

Per-run flags: `--additional-mcp-config <json|@file>`, `--enable-mcp-server`, `--disable-mcp-server`, `--disable-builtin-mcps`, `--allow-all-mcp-server-instructions`, `--mcp-github-auth <server=origin>` (sends the GitHub credential only to that explicit server and approved origin; HTTPS required except literal loopback).

`--tools "*" | list | ""` on `copilot mcp add` filters which tools a server exposes.

## Ground rules

* Never paste credentials into chat, commit them, or embed them in a shared config. Use `inputs`, environment variables, or the header/env flags.
* Verify with a real tool call after adding a server; a green "connected" state is not proof the tool works.
* Server-provided initialization instructions can influence the model. Leave `--allow-all-mcp-server-instructions` off unless you trust every configured server.
* When a tool is missing, check whether an `--available-tools`/`--excluded-tools` filter or a `--tools` filter excluded it before debugging the server.
* Removing a server you no longer use is part of using MCP well.
