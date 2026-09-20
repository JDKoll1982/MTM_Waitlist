# .github Artifact Staleness Audit — Decisions #2 corrected

The full 2026-09-09 audit of `.github/{prompts,instructions,skills,scripts,memories,agents,workflows}`
lives in the machine-local Copilot memory store (see *Where the original lives*, below). This file is the
**committed** record, and it carries one correction to that audit that a later reader would otherwise
believe.

## Correction (verified 2026-09-20) — the two "inert" files were **not** deleted

Decisions #2 of the original audit records, on the owner's instruction *"if its not used by anything
anymore remove it"*, that both of the following were **DELETED**:

- `.github/copilot-startup-steps.yaml`
- `.github/scripts/restructure-database-layout.ps1`

**Both files are present in the tree.** They were not deleted; they were **retained and annotated**. The
correction matters because the original wording invites a future reader — or agent — to "re-delete" a file
that is already gone in their reading, or to conclude the tree has drifted when it has not.

| File | Present? | What it now says of itself |
| --- | --- | --- |
| `.github/copilot-startup-steps.yaml` | **yes** | Header marks it **"REFERENCE ONLY — NOT consumed by VS Code or GitHub Copilot (verified 2026-09-09)"**. It is a readable summary of the environment's setup steps; nothing reads it, and nothing should. |
| `.github/scripts/restructure-database-layout.ps1` | **yes** | Header marks it **"ARCHIVED ONE-SHOT MIGRATION — DO NOT RE-RUN (verified spent 2026-09-09)"**. It reads the now-absent `Database/Migrations` + `Database/Rollbacks`, so a re-run would throw and would recreate unprefixed folders. |

**The substantive finding in Decisions #2 still stands** and is the reason the annotation was chosen over
deletion: neither file is *consumed* by anything. `copilot-startup-steps.yaml` declares an `on_task_start:`
trigger and an `instruction_files:` step key that no VS Code Copilot mechanism reads, and the script is a
spent migration. **Inert is not the same as absent**, and this audit's job is to tell the reader which is
which.

**Do not delete either file.** Deleting them would remove the only record of *why* the layout is what it is,
and the annotations already neutralise the risk the deletion was meant to remove.

## What the original audit got right (unchanged, still worth knowing)

- **Reference facts:** .NET SDK `10.0.401`; TFM `net10.0-windows10.0.19041.0`;
  `Microsoft.WindowsAppSDK` **2.3.1**; canonical build
  `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false`.
- **MCP tool scopes.** Workspace `.vscode/mcp.json`: `csv-mcp-server`, `oraios/serena`, `xamlmcp`. User
  `%APPDATA%\Code\User\mcp.json`: `io.github.upstash/context7`, `microsoftdocs/mcp`,
  `microsoft/playwright-mcp`, `oraios/serena`. **`mcp_context7/*`, `mcp_microsoft_lea/*`, `context7/*` and
  `microsoft-learn/*` match no registered server** — they are not valid scopes.
- **`XamlMcp` is not wired**: no `.cs`/`.xaml` reference, no `PackageReference`. The `xamlmcp` MCP server is
  registered but has nothing to attach to.
- **No AI integration exists.** `Foundry` / `phi-4-mini` / `localhost:5272` appear only in docs;
  `Azure.AI.OpenAI` is not a `PackageReference`.
- **DB layout is file-per-artifact**: `Database/<Artifact>/<NN_name>/{create,rollback}.sql`,
  `Database/Validation/<name>/validate.sql`, aggregates `AllTables/AllSPs/AllViews/AllFunct/AllSeeds.sql`.
  `Database/Migrations` and `Database/Rollbacks` do not exist.
- **Decisions #1 (SQL banned words)** — `validate-sql-naming.ps1` bans `@('class','delete','order')` with a
  `work_order` exemption; the remaining violations are real SQL (boolean columns that should be
  `is_*`/`has_*`/`*_bool`, and `mock_parts` / `mock_locations` / `mock_requesters` table names needing a
  third underscore segment). **Fix in SQL, not in the validator.**
- **Decisions #3 — outside `.github` scope, still open:** `Directory.Build.props:30` said "the **2.3.0** in
  use" (actual 2.3.1 — corrected 2026-09-20 by
  `specs/004-unified-card-item-picker` T210); and
  `Documents/Development/CompletedImplementations/Complete-Waitlist-Request-Workflow-Checklist.md:570` uses
  the stale `-p:WinUISDKReferences=false` build command, which is **still** stale.

## Where the original lives

`%APPDATA%\Code\User\workspaceStorage\<ws>\GitHub.copilot-chat\memory-tool\memories\repo\github-artifact-staleness.md`

That store is **machine-local and is not committed on push** — which is exactly why this committed
corrected record exists. Per `.github/memories/README.md`, canonical repo facts belong in committed files
under `.github/memories/` and the ephemeral store should point here rather than the other way round.

Related: `capabilities/DRIFT.md` records the documentation-drift half of the same problem.
