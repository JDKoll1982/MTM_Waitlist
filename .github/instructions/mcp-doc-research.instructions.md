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
