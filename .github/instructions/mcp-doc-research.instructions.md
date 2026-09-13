---
applyTo: "**/*"
---
# MCP-First Documentation and Code Sample Retrieval

Use available MCP servers to ground implementation decisions before writing or changing code.

## Bottom line (constitution Principle IV, NON-NEGOTIABLE)

Whenever you are unsure about anything code-related, or you do not know how to implement something,
consulting Context7 and Microsoft Learn is **NON-NEGOTIABLE**. Model memory alone is not sufficient
grounds for a decision, and deferring the question to the user does not replace a documentation lookup.

This applies at minimum to: API names, signatures, and syntax; configuration and setup; version
migration and breaking-change behaviour; library-specific debugging; CLI and tool usage; and official
platform guidance.

## Serena MCP (Repository Exploration)

- Use Serena MCP for indexed codebase exploration, symbol discovery, and faster cross-file navigation.
- Prefer Serena-driven lookup before fallback text search when identifying existing implementations.
- Use Serena findings to narrow and validate edits before applying code changes.

### Where the doc servers actually live (verified 2026-09-13)

An audit of this machine found the registrations split across two files, which is why "it is not in
`.vscode/mcp.json`" is **not** evidence that a server is unavailable:

| Server | Registered in | Transport |
| --- | --- | --- |
| `microsoftdocs/mcp` (Microsoft Learn) | **user** `%APPDATA%\Code\User\mcp.json` | http → `https://learn.microsoft.com/api/mcp` |
| `io.github.upstash/context7` | **user** `%APPDATA%\Code\User\mcp.json` | stdio, `npx @upstash/context7-mcp` |
| `microsoft/playwright-mcp` | **user** `%APPDATA%\Code\User\mcp.json` | stdio, `npx @playwright/mcp` |
| `oraios/serena` | **user** `%APPDATA%\Code\User\mcp.json` **and** workspace `.vscode/mcp.json` | stdio |
| `csv-mcp-server`, `xamlmcp` | **workspace** `.vscode/mcp.json` | stdio |

Check both files before concluding a tool is missing, and check the *tool* name rather than the server name —
the live MS Learn tools are `mcp_microsoft_lea_microsoft_docs_search`, `..._microsoft_code_sample_search` and
`..._microsoft_docs_fetch`.

### When a doc MCP tool reports "currently disabled by the user"

1. **It is not the config.** There is no settings-file gate: neither user nor workspace `settings.json` carries
   any `mcp` / `chat.tools` enable/disable entry (checked 2026-09-13). Do not go looking for one.
2. **It is usually not the server either.** Prove it in one command before blaming the server — a healthy one
   answers an MCP `initialize`:

   ```powershell
   $h = @{ "Accept" = "application/json, text/event-stream" }
   Invoke-WebRequest -Uri "https://learn.microsoft.com/api/mcp" -Method Post -Headers $h `
     -ContentType "application/json" -TimeoutSec 25 `
     -Body '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"probe","version":"1.0"}}}'
   ```

   The `Accept` header is required — streamable HTTP answers `406 Not Acceptable` without it, which looks like a
   server fault and is not. Verified 2026-09-13: `200`, `serverInfo.name = Microsoft Learn MCP Server`,
   `version 1.0.0`, all three tools advertised.
3. **So it is the session.** A Copilot session that started before the server was registered or enabled holds a
   **stale tool snapshot** and keeps reporting disabled for the rest of that session. Start a **new chat
   session** and the tools appear. This is the same class of failure as the XamlMcp one recorded in
   `.github/copilot-instructions.md`.
4. **Meanwhile, do the lookup anyway.** "Disabled" is not a licence to answer from memory — the constitution's
   MCP-first rule is about grounding the answer, not about which tool fetched it. `fetch_webpage` against the
   same Microsoft Learn page returns the same content the MCP server would have served, and it worked twice on
   2026-09-13 (the `{x:Bind}` `UpdateSourceTrigger` default, and the Segoe Fluent Icons glyph list).
   State in the answer which source was used.

### Serena self-healing (mandatory)

When Serena fails such that its location or installation cannot be discerned — the tool cannot be found,
or it resolves to a path that does not exist — do **not** abandon the lookup. Instead:

1. Inspect Serena's configuration: `~/.serena/serena_config.yml`, plus project-level
   `.serena/project.yml` or `.serena/project.local.yml` when present.
2. Account for the user-profile folder changing between environments (`jkoll` at work, `johnk` at home).
   That invalidates profile-rooted paths such as `C:\Users\<profile>\...` entries in the config and in
   the MCP server launch command.
3. Update the configuration accordingly.
4. Retry the operation.

If the retry still fails, report the config path inspected, the change attempted, and the observed error
rather than falling back to ungrounded guesswork.

## General Libraries and Frameworks

- Always resolve a library first with `mcp_context7_resolve-library-id`.
- Then fetch focused docs with `mcp_context7_get-library-docs`.
- Prefer MCP documentation over memory for APIs that may have changed.

## Microsoft and Windows Topics

- Use `mcp_microsoft_lea_microsoft_docs_search` first for official Microsoft guidance.
- Use `mcp_microsoft_lea_microsoft_code_sample_search` when generating Microsoft/Azure code.
- Use `mcp_microsoft_lea_microsoft_docs_fetch` for complete page details when snippets are insufficient.

## Repo-Specific Focus

For this repository, prioritize MCP-backed validation for:

- WinUI 3 and Windows App SDK APIs
- App notifications and packaging flows
- Local AI and Foundry-related guidance
- The external-read fallback: how `MTM_Waitlist.Mock` detects Infor Visual unreachability and serves the five
  read shapes from the `mtm_mock` mirror (`sp_visual_<shape>_get`), plus how the on-host
  `MTM_Waitlist.Mock.Service` refreshes that mirror (`sp_visual_<shape>_refresh`) and exposes it over the
  token-gated API
- The retired-systems boundary: internal stores (`mtm_waitlist`, `mtm_wip_application_winforms`,
  `mtm_receiving_application`) are always read and written live, and there is no manual demo/mock mode
  (see the feature spec at `specs/001-module-mock-visual-fallback`, FR-001/FR-003/FR-014)
