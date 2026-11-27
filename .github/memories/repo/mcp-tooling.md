# MCP tooling — troubleshooting playbook (verified 2026-11-26, `johnk`)

Read this when an MCP tool is missing, reports itself disabled, or throws unexpectedly. It exists so the
fix is not re-derived from scratch each time.

## The one failure that looks like a broken server

A tool the server genuinely provides reports **"currently disabled by the user"** even though it is
checked and enabled in the Configure Tools dialog. Nothing is wrong with the server or its
configuration.

The cause is the **running session's tool snapshot**. A Copilot session fixes its tool list when it
starts, so a session that began before a server was registered or enabled keeps the old list for its
whole life and reports every tool from that server as disabled.

Fix, in order:

1. Start a **new chat session**. That is the whole fix in almost every case.
2. Confirm the server shows as connected in the MCP panel of the new session.
3. Only if it still fails, treat it as a real server fault.

There is no settings-file gate to look for. Neither user nor workspace `settings.json` carries an MCP
allowlist or blacklist, so hunting one wastes time.

## Prove the server, not the client

Before blaming a server, ask it to initialize. A healthy server answers this:

```powershell
$h = @{ "Accept" = "application/json, text/event-stream" }
Invoke-WebRequest -Uri "https://learn.microsoft.com/api/mcp" -Method Post -Headers $h `
  -ContentType "application/json" -TimeoutSec 25 `
  -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}'
```

The `Accept` header is required. Streamable HTTP answers `406 Not Acceptable` without it, which looks
like a server fault and is not.

Meanwhile, do the lookup anyway. "Disabled" is not licence to answer from memory; fetch the same page
another way and say which source was used.

## What this repository registers

Nothing. The repository declares no MCP server: `.mcp.json` keeps an empty `mcpServers` object, and
`.vscode/mcp.json` is gone. Microsoft Learn, Context7 and Playwright are registered at user scope in
`%APPDATA%\Code\User\mcp.json`.

Two servers were retired on 2026-11-26, for different reasons:

| Server | Why it is gone | When it comes back |
| --- | --- | --- |
| `csv-mcp-server` 1.0.0 | `load_csv` fails on the repository's own schema exports (a progress notification sends text where a number is expected), and 1.0.0 is the only release. Read the exports with `Import-Csv -Delimiter "`t"` instead. | Only if a fixed release ships, together with a working `load_csv` on a real `docs/database` file |
| `xamlmcp` 1.0.0-preview.3 | The WinUI in-process agent it attaches to is absent, so it has nothing to serve and costs ~15 tool definitions per request | In the same change that adds the `XamlMcp.WinUI` package reference and its `#if DEBUG` attach |

Do not re-register either one on its own. A registration without the capability behind it is the exact
defect this note was written to stop.

## Related

- `.github/instructions/mcp-doc-research.instructions.md` — where the doc servers live and how to check
  the tool names.
- `.github/memories/repo/tool-budget.md` — the 2026-10-11 tool-group decisions.
- `.github/memories/repo/ui-automation-drive.md` — what actually works for driving the app window today.
