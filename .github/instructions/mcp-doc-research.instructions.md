---
applyTo: "**/*"
---
# MCP-First Documentation and Code Sample Retrieval

Use available MCP servers to ground implementation decisions before writing or changing code.

## Serena MCP (Repository Exploration)

- Use Serena MCP for indexed codebase exploration, symbol discovery, and faster cross-file navigation.
- Prefer Serena-driven lookup before fallback text search when identifying existing implementations.
- Use Serena findings to narrow and validate edits before applying code changes.

## General Libraries and Frameworks

- Always resolve a library first with `mcp_context72_resolve-library-id`.
- Then fetch focused docs with `mcp_context72_get-library-docs`.
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
- Helper-server and mock-data routing patterns that short-circuit to app-data-backed sample data when module-specific toggles are enabled (`Feature.InforVisualMockData` for Infor Visual flows, `Feature.RecvMockData` for receiving/MySQL flows)
