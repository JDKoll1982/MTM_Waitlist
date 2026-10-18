# Tool budget — which tool groups are disabled and why (owner decisions, 2026-10-11)

## Why this exists

The model backing the chat session accepts a maximum of **128 tools**. The session had more than that
configured, so the client silently withheld the excess and warned:

> The model only supports 128 tools, 49 tools were not provided to the model. Configure Tools

The danger of that warning is not the count — it is that **which** tools get withheld is not something you
choose, so a tool the work depends on can disappear without any visible failure beyond a missing tool. The
owner therefore triaged the groups deliberately on 2026-10-11 and the decisions are recorded here so they are
not re-litigated (or accidentally reverted) in a later session.

## Decided: disabled

| Group | Approx. tools | Source | Why it was dropped |
| --- | --- | --- | --- |
| Playwright (`microsoft-playwright-mcp-browser_*`) | 25 | built-in `playwright` MCP server | This is a WinUI 3 desktop app. UI verification here is done with **Windows UI Automation** (`.github/instructions/winui3-ui-automation.instructions.md`), never a browser. |
| Pylance (`pylance-mcp-server-*`) | 21 | `ms-python.vscode-pylance` | No Python development happens in this repo. The only Python is the Spec Kit Companion's helper scripts (`write-context.py`, `doctor.py`, `drift.py`), and those are run through the shell — never through the Pylance MCP tools. |
| NuGet (`NuGet-*`) | 6 | `ms-dotnettools.csdevkit` | Package work here is done through the `dotnet` CLI, and package changes are infrequent. |

Total withheld: about 52 tools, which is the 49 that were being dropped plus headroom so the important tools
are not the ones that get sacrificed.

## Decided: kept — do NOT disable these

| Group | Source | Why it must stay |
| --- | --- | --- |
| Context7 (`io-github-upstash-context7-*`) | `vijaynirmal.context7-mcp-relay` | The repo constitution (Principle IV) makes MCP-first documentation lookup **non-negotiable** before writing code. |
| Microsoft Learn (`microsoftdocs-mcp-*`) | Microsoft Learn MCP | Same constitution rule, for official Microsoft/Windows/WinUI guidance. |
| GitHub (`github-mcp-server-*`, `issue_fetch`, `labels_fetch`, `notification_fetch`, `doSearch`) | built-in `github` MCP server | Used for issues, PRs and commit lookups. Note the repo prefers the `gh` CLI where it can, so if the budget ever gets tight again this group is the next place to narrow — via `github.copilot.chat.githubMcpServer.toolsets` rather than disabling it outright. |

## Retired alongside this: CodeGraphy, and the code-exploration MCP server

The owner retired **CodeGraphy** on 2026-10-11 ("lose Codegraphy"). CodeGraphy was a
VS Code extension plus the `@codegraphy-dev/core` CLI, and it contributed a **skill**, not MCP tools — so
retiring it freed no tool slots. Its section was removed from `.github/copilot-instructions.md`, which now
records the retirement and forbids agents from invoking the CLI or skill. The globally installed CLI and
skill live outside this repo and are not part of its workflow.

On **2026-10-17** the owner retired the **MCP-based code-exploration server** as well. It is gone from the
workspace `.vscode/mcp.json` and the user `mcp.json`, and its project-local config and memory folders were
deleted from the repo. No group was re-enabled to fill the freed slots: code exploration now runs on the
editor's built-in code intelligence plus `grep`/`glob`/`view`, which need no server and cannot be withheld by
the 128-tool ceiling. Do not re-add an indexed code-exploration server without the owner's say-so.

## How to apply or reverse this

- Turn a group on or off in the chat client's **Configure Tools** dialog (the link the warning itself
  offers). The enablement list is held in the client's own workspace/global storage, **not** in
  `.vscode/mcp.json` — that file is empty here on purpose, and no `settings.json` key gates these servers.
  Editing `.vscode/mcp.json` will not achieve anything.
- Disabling an extension-provided server (Pylance, NuGet, Context7) can also be done by disabling the
  contributing extension in that workspace.
- **A change only takes effect in a NEW chat session.** A session that started before the change holds a
  stale tool snapshot; this is the same class of behaviour documented for `xamlmcp` in
  `.github/copilot-instructions.md`.
- Do not re-enable a disabled group without the owner's say-so, and never "temporarily" re-enable one to
  work around a missing tool — report the gap instead.
