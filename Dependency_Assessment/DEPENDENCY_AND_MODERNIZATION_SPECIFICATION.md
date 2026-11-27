# Dependency and Modernization Specification — AI Development Environment

**Workspace:** MTM_Waitlist (`JDKoll1982/MTM_Waitlist`)
**Audit area:** `ai-tooling` (Area C — AI development environment) only
**Research date:** 2026-11-26
**Depth:** deep
**Audit output:** `Dependency_Assessment/`
**Companion report:** `Dependency_Update_and_Modernization_Report.pdf`

This document is the input to `/speckit.specify`. It records what was measured, what was approved,
and what an implementation agent must change. Nothing in this document has been implemented. No
dependency was updated, no package installed, and no source or configuration file was edited by the
audit.

The audit ran the `ai-tooling` area only. Dependency upgrades, application modernization, security
CVE remediation, performance work, UX, architecture, tests, database and localization were **not**
assessed and must not appear as work items here.

---

## 1. Overview

### Objectives

1. Make the AI development environment describe the repository as it actually is on 2026-11-26.
2. Remove AI-facing configuration that cannot work — either because the server has nothing to attach
   to, or because the operation it exists for fails.
3. Reduce the context every request pays for, without deleting a rule the agent needs.

### Scope

In scope: `.github/copilot-instructions.md`, `AGENTS.md`, `.github/instructions/**`,
`.github/agents/**`, `.github/skills/**`, `.github/prompts/**`, `.github/hooks/*.json`,
`.github/memories/repo/**`, `.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json`,
`.vscode/extensions.json`, and the `.specify/` registry metadata that describes which commands and
skills are actually installed.

Out of scope by command line: every area other than `ai-tooling`. Out of scope by flag: user-level
MCP servers, user-level extensions and user-level settings were not enumerated, because
`--user-config` was not passed. User-level material is referenced only where a repository file
duplicates it or depends on it, and each such reference says so.

### Outcome sought

An agent that opens this repository receives correct facts, a tool list it can actually use, and
roughly 3,300 fewer tokens of preamble per request, with no coding rule removed.

---

## 2. Workspace Context

### Repository and projects

| Item | Value |
| --- | --- |
| Repository | `JDKoll1982/MTM_Waitlist` |
| Solution | `MTM_Waitlist.sln` |
| Projects | 17 (`MTM_Waitlist` app + 16 libraries/tools/test) |
| Distinct NuGet packages | 21 (80 `PackageReference` declarations) |
| Target framework | `net10.0-windows10.0.19041.0` |
| UI stack | WinUI 3, Windows App SDK 2.4.0 |
| Package management | Per-project versions; no `Directory.Packages.props`, no `global.json` |
| CI | None (`.github/workflows` absent), as documented |
| Lint config | None (`.editorconfig` absent), as documented |

Project list: `MTM_Waitlist`, `MTM_Waitlist.Core`, `MTM_Waitlist.Shared`, `MTM_Waitlist.Logging`,
`MTM_Waitlist.Settings`, `MTM_Waitlist.Startup`, `MTM_Waitlist.Setup`, `MTM_Waitlist.Mock`,
`MTM_Waitlist.Mock.Service`, `MTM_Waitlist.GenericRequest`, `MTM_Waitlist.GenericRequest.Data`,
`MTM_Waitlist.Waitlist.Controls`, `MTM_Waitlist.Waitlist.View`,
`MTM_Waitlist.Waitlist.NewRequest`, `MTM_Database_Deployment`,
`MTM_Database_Deployment.Core`, `MTM_Waitlist.Tests`.

### AI development environment as found

| Surface | Measured |
| --- | --- |
| Project instruction files | `.github/copilot-instructions.md` (30,536 B) **and** `AGENTS.md` (4,735 B) — both load |
| Repository instruction files | 14 files, 109,192 B total |
| Always-applied repository instruction files | 4 files, 22,066 B (`mcp-doc-research`, `response-format`, `spec-kit`, `document-authoring`) |
| Always-applied user instruction files | 6 files, 33,550 B (`ai-agent-mode`, `ai-chat`, `ai-cli`, `ai-observability`, `ai-settings`, `ask-user-tool`) |
| **Always-on AI context, before any file is read** | **90,887 B ≈ 25,000 tokens** (at ~3.6 B/token) |
| Additional context on any XAML/C# edit | 51,716 B ≈ 14,400 tokens (6 matching instruction files) |
| Custom agent files | 109 — 92 with a YAML header, 17 without |
| Skill folders | 43 — 24 are `speckit-companion-*` |
| Prompt files | 61 — 60 are frontmatter-only shims to an agent |
| Hook files | 1 (`.github/hooks/speckit.json`) |
| Repository MCP servers | 2 in `.mcp.json` (`csv-mcp-server`, `xamlmcp`) + 1 duplicate `csv-mcp-server` in `.vscode/mcp.json` |
| Spec Kit metadata | `.specify/` — 251 files, 2,261,702 B; 9 extensions registered |

### Environment facts confirmed during the audit

| Check | Command | Result |
| --- | --- | --- |
| VS Code | `code --version` | 1.148.0 |
| XamlMcp CLI | `xamlmcp check --json` | runtime pass (.NET 10.0.15), server 1.0.0-preview.3, **0 instrumented apps running** |
| XamlMcp in-repo wiring | text search across all `.cs`/`.csproj`/`.xaml` outside `bin`/`obj` | **0 matches** |
| CSV MCP health | `health_check` | healthy, 1.0.0 |
| CSV MCP load | `load_csv` on `docs/database/tables/database-inforvisual-tables-account-balance.csv` | **fails** — `ProgressNotificationParams.total … input_value='Validating file...'` |
| CSV package releases | PyPI JSON API | 1.0.0 only, published 2025-08-13 |
| `python3` | `python3 --version` | Python 3.13.15 (resolves via `…\Python313\python3.cmd`) |

---

## 3. Dependency Inventory (AI development environment)

No NuGet, npm or Python package in this repository is in scope for this audit; Area A was not
requested. The inventory below is the dependency surface the AI tooling relies on, with the state
verified today.

| Component | Declared | Resolved / observed | Status | Update available |
| --- | --- | --- | --- | --- |
| `xamlmcp` (dotnet tool) | `.mcp.json`, `tools: ["*"]` | 1.0.0-preview.3, 0 apps running, 0 code references | **Unusable** — nothing to attach to | n/a — the missing piece is the in-app agent, not the tool |
| `csv-mcp-server` | `.mcp.json` + duplicate in `.vscode/mcp.json` | 1.0.0 (PyPI, only release) | **Partially broken** — `load_csv` fails; 39 tools exposed | None — 1.0.0 is latest |
| Microsoft Learn MCP | user-level | reachable | Working | n/a |
| Context7 MCP | user-level | reachable | Working | n/a |
| `.github/skills/**` | 43 folders | 19 model-invocable, 24 `disable-model-invocation` | Mixed | n/a |
| `.github/agents/**` | 109 files | 92 valid, 17 invalid | Mixed | n/a |

---

## 4. Approved Dependency Updates

No package version changes were approved or proposed. This section is retained so the specification
matches the required structure and so a future run of a different area can fill it.

| Package | Current | Target | Reason |
| --- | --- | --- | --- |
| — | — | — | No dependency update is in scope for an `ai-tooling` audit |

The two MCP server changes that were approved are registration changes, not version changes, and
appear in Section 6 under AI-001 and AI-004.

---

## 5. Existing Custom Functionality

Not applicable as a modernization target: this audit did not examine application code. The
"functionality" under review is the hand-maintained AI configuration, recorded in Section 7.

---

## 6. Approved Replacements

All 14 proposals are approved. Each row states the existing solution, the replacement, and why.

### AI-001 — Remove the unwired `xamlmcp` MCP server from `.mcp.json`

| Field | Content |
| --- | --- |
| Existing solution | `.mcp.json` registers `xamlmcp` with `command: C:\Users\johnk\.dotnet\tools\xamlmcp.exe`, `args: ["--enable-driver"]`, `tools: ["*"]` |
| Replacement | Delete that server block; restore it in the same change that adds the in-process agent |
| Alternatives considered | (a) keep and ignore — rejected, it costs ~15 tool definitions per request for zero capability; (b) wire the agent now — a source change, outside a read-only audit and outside this area |
| Benefit | Removes ~15 dead tool definitions from every request; removes a misleading affordance |
| Risk | If the app gains the agent later, the registration must return; the restore step is one JSON block |

### AI-002 — Delete the "XamlMcp UI Inspection" section from `copilot-instructions.md`

| Field | Content |
| --- | --- |
| Existing solution | `.github/copilot-instructions.md` lines 41-57, 5,673 B, describing a 10-step attach workflow and its WinUI capability notes |
| Replacement | Delete the section. If the agent is restored, reintroduce it with AI-001 |
| Alternatives considered | Shorten to one line — rejected while the tool cannot attach; a one-line "unavailable" note is stale the moment it is fixed |
| Benefit | 5,673 B ≈ 1,575 tokens removed from every request; second-largest section of the largest always-on file |

### AI-003 — Remove the dead duplicate `csv-mcp-server` registration and re-key sampling

| Field | Content |
| --- | --- |
| Existing solution | `.vscode/mcp.json` defines `csv-mcp-server` with `CSV_STORAGE_PATH` = `…\Documents\Development\InforVisual\DatabaseCSVFiles` (does not exist). `.vscode/settings.json` keys `chat.mcp.serverSampling` on `MTM_Waitlist/.vscode/mcp.json: csv-mcp-server` |
| Replacement | Delete the `.vscode/mcp.json` block. Re-key `chat.mcp.serverSampling` to the `.mcp.json` server identity, or delete the sampling block if AI-004 removes the server |
| Alternatives considered | Fix the path — rejected: the repository's own instructions already declare the `.mcp.json` instance authoritative and this one dead |
| Benefit | One registration, one storage path, and a sampling allow-list that can actually apply |
| Ordering note | **If AI-004 is implemented, re-keying has no target.** Implement AI-004 first and make AI-003 the deletion of both the entry and the sampling block |

### AI-004 — Remove the CSV MCP server

| Field | Content |
| --- | --- |
| Existing solution | `csv-mcp-server` 1.0.0 in `.mcp.json`, 39 tools, `CSV_STORAGE_PATH` = `docs/database` |
| Replacement | Delete the registration and the "CSV Schema Exploration" section that documents it; keep the documented read-only PowerShell fallback |
| Evidence | `load_csv` fails on a real schema file with `ProgressNotificationParams.total: Input should be a valid number … input_value='Validating file...'`. PyPI lists 1.0.0 (2025-08-13) as the only release, so no fixed version exists |
| Alternatives considered | (a) fork and patch the progress call — a new maintained dependency, larger effort than the benefit; (b) wait for upstream — no release in 15 months; (c) keep for its working read tools — the 5,866 schema exports are read through PowerShell today anyway |
| Benefit | 39 tool definitions removed; the instruction surface stops advertising an operation that fails |
| Risk | Loses in-session DataFrame tooling for the schema exports. Mitigation: the section is replaced by a note pointing at `Import-Csv` with a tab delimiter |

### AI-005 — Repair the broken `/memories/repo/mcp-tooling.md` pointer

| Field | Content |
| --- | --- |
| Existing solution | `.github/copilot-instructions.md` tells the agent to "consult `/memories/repo/mcp-tooling.md` first" for MCP troubleshooting, and names `/memories/repo/codegraphy-setup.md` as an existing note. Neither file exists; `.github/memories/repo/` holds 12 notes |
| Replacement | Either create `mcp-tooling.md` from the verified playbook already in the instruction body, or delete both pointers |
| Alternatives considered | Leave it — rejected: it is the first instruction an agent follows when a tool misbehaves, and it leads nowhere |
| Benefit | Removes a dead end from the highest-traffic troubleshooting path |

### AI-006 — Correct stale facts in the project instruction files

| Field | Content |
| --- | --- |
| Existing solution | `AGENTS.md` says "Fifteen projects" and "15 `*AuditTests` guardrails"; `.github/copilot-instructions.md` says "This machine's WindowsAppSDK 2.3.1"; `AGENTS.md` package list omits `CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219`; both files say `python3` is a broken 0-byte Store alias |
| Measured truth (2026-11-26) | 17 projects; 17 `*AuditTests`; Windows App SDK 2.4.0 in `MTM_Waitlist.csproj`; SettingsControls 8.2.251219 referenced; `python3` answers "Python 3.13.15" via `C:\Users\johnk\AppData\Local\Programs\Python\Python313\python3.cmd` |
| Replacement | Correct each figure; keep the `python` guidance but restate the `python3` claim as workstation-conditional and re-verify before repeating it |
| Alternatives considered | Leave the numbers — rejected: a wrong project count sends an agent looking for a project that is not there, and a wrong SDK version sends it to the wrong API documentation |
| Benefit | Removes five wrong facts from always-loaded context |

### AI-007 — Rewrite the stale MCP server table in `mcp-doc-research.instructions.md`

| Field | Content |
| --- | --- |
| Existing solution | Lines 30-40 state that `csv-mcp-server` lives in `.vscode/mcp.json`, that `xamlmcp` "is no longer registered" and that its 24 tools were removed "to keep the chat tool count under the 128-tool cap applied to BYOK models" |
| Measured truth | The authoritative CSV server is in `.mcp.json`; `xamlmcp` **is** registered in `.mcp.json`; `.vscode/settings.json` records the cap decision as obsolete ("deepseek-v4-for-copilot removed in 0.9.4") |
| Replacement | Rewrite the table against `.mcp.json`, replace the cap paragraph with a one-line statement of the current registration state |
| Alternatives considered | Delete the section — rejected: the "where the doc servers live" table is still useful, and only its facts are wrong |
| Benefit | Removes a three-way contradiction between two always-applied files and the actual configuration |

### AI-008 — Delete the sections of `copilot-instructions.md` that restate an always-applied instruction file

| Field | Content |
| --- | --- |
| Existing solution | "MCP-First Documentation and Code Sample Retrieval" (372 B) restates `mcp-doc-research.instructions.md`; "Output Control Invariants" (1,255 B) restates `response-format.instructions.md`; "Document Authoring" (384 B) restates `document-authoring.instructions.md` |
| Replacement | Delete all three; the instruction files already load on every request |
| Alternatives considered | Delete the instruction files instead — rejected: `applyTo` scoping lets those files be tuned without touching the project instructions, and `spec-kit.instructions.md` depends on `response-format` |
| Benefit | 2,011 B ≈ 560 tokens per request, with no rule lost |

### AI-009 — Retire the generic workflow section and compress the subagent-model table

| Field | Content |
| --- | --- |
| Existing solution | "Multi-File Agentic Execution Workflow" (1,330 B) describes a discover/edit/build loop with a "self-heal twice" rule; "Subagent Model" (1,767 B) carries a four-row model-id comparison plus provenance narrative |
| Replacement | Delete the workflow section; reduce the subagent section to the operative rule (use `deepseek/deepseek-flash`; it is a different model from `deepseek-v4-flash`) at roughly 600 B |
| Alternatives considered | Keep both — rejected for the workflow section, which duplicates harness behaviour; the provenance narrative is preserved in `.github/memories/repo/subagent-models.md`, which already exists |
| Benefit | ~2,500 B ≈ 700 tokens per request |
| Behavioural risk | The "self-heal twice" limit and the explicit three-phase ordering are instructions the agent may have been following. If the first post-change run shows wasted iterations, restore the workflow section and keep the table compression |

### AI-010 — Load the UI-automation instructions on demand

| Field | Content |
| --- | --- |
| Existing solution | `.github/instructions/winui3-ui-automation.instructions.md`, 27,256 B, `applyTo: **/*.{xaml,cs,ps1,resw}` |
| Replacement | Move the procedure into a skill under `.github/skills/`, leave a short `.instructions.md` that keeps the `applyTo` glob and routes to the skill, and list the new skill in `.github/instructions/winui3-scaling-routing.instructions.md` |
| Why a skill | VS Code documents agent skills for reusable multi-step workflows and instruction files for conventions; this file is a 351-line procedure |
| Alternatives considered | (a) narrow the glob — rejected: UI automation work legitimately touches `.ps1` and `.cs`; (b) leave it — rejected: it is the single largest per-edit cost in the repository |
| Benefit | ~27,256 B ≈ 7,570 tokens removed from every XAML/C# edit, loaded only when the task needs it |
| Risk | The skill must be discoverable from the task wording. Mitigation: keep the routing instruction and name the skill explicitly in it |

### AI-011 — Clean up the 17 headerless agent files

| Field | Content |
| --- | --- |
| Existing solution | 12 files of ~100 B containing only a relative path, each pointing at `changelog`, `ci-guard` or `pr-bridge` — none of which is installed (all 12 targets resolve BROKEN). 5 `speckit.superspec.*` files with no YAML header |
| Replacement | Delete the 12 dead pointers; add valid headers to the 5 superspec agents, or remove them if the extension is not wanted |
| Why | VS Code requires `description` in the Copilot schema; a file with no header cannot be a valid agent |
| Alternatives considered | Keep them as placeholders — rejected: 12 broken pointers are indistinguishable from working agents in a directory listing |
| Benefit | An agents folder where every file is loadable |

### AI-012 — De-duplicate `AGENTS.md` against `copilot-instructions.md`

| Field | Content |
| --- | --- |
| Existing solution | Both are project instruction files, so both load. `PRI175`, `WMC9999`, `[RelayCommand]`, the stored-procedure rule, the no-mock rule and the view-model convention each appear in both |
| Replacement | `AGENTS.md` stays the short orientation (commands, stack, layout, conventions index); duplicated blocks collapse to one-line pointers to the file that owns the detail |
| Alternatives considered | Delete `AGENTS.md` — rejected: it is the cross-agent format and the portable orientation file; (b) delete `copilot-instructions.md` — rejected: it carries environment detail `AGENTS.md` should not hold |
| Benefit | ≈1,700 B ≈ 470 tokens per request, and one owner per rule |

### AI-013 — Consolidate the duplicated WinUI 3 scaling-routing instruction

| Field | Content |
| --- | --- |
| Existing solution | `.github/instructions/winui3-scaling-routing.instructions.md` (3,082 B) and `~/.copilot/instructions/winui3-scaling-routing.instructions.md` (2,170 B), same title, same `applyTo: '**/*.{xaml,cs}'` |
| Replacement | Keep the repository copy canonical; delete the user-level copy on this workstation |
| Alternatives considered | Delete the repository copy — rejected: the repository copy is the portable one and this change must not depend on one machine's user folder |
| Benefit | ~2,170 B removed from every XAML/C# edit **on this workstation only**; the repository change itself is nil |

### AI-014 — Retire the 24 `speckit-companion-*` skill folders

| Field | Content |
| --- | --- |
| Existing solution | 24 skill folders under `.github/skills/`, all `disable-model-invocation: true`, duplicating the `speckit.companion.*` agents |
| Evidence | `.specify/extensions/.registry` reports `registered_skills: []` for all 9 extensions; the companion commands are registered as agents |
| Replacement | Delete the 24 skill folders; keep the agents |
| Alternatives considered | Keep as a second copy — rejected: three copies (extension source, agent, skill) drift apart, and the catalogue entry is paid on every request |
| Benefit | 24 fewer entries in the skill catalogue; 19 skills remain |
| Risk | A host that resolves `/speckit-companion-*` from skills instead of agents would lose the command. Verify one companion command after the change |

---

## 7. File-Level Change Requirements

| Path | Existing implementation | Required modification | Owning proposal | Reason | Mandatory or optional | Expected outcome | Approval ID |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `.mcp.json` | Registers `xamlmcp` and `csv-mcp-server` | Remove both server blocks | AI-001, AI-004 | Neither can deliver its service | Mandatory | No repository MCP server that cannot serve | AI-001, AI-004 |
| `.vscode/mcp.json` | Duplicate `csv-mcp-server` with a non-existent storage path | Delete the `servers` entry (the file may be removed entirely) | AI-003 | Dead duplicate registration | Mandatory | One registration per server | AI-003 |
| `.vscode/settings.json` | `chat.mcp.serverSampling` keyed to the dead `.vscode/mcp.json` server | Re-key to the `.mcp.json` identity, or delete the block if AI-004 removes the server | AI-003 | Allow-list cannot apply to a server that never starts | Mandatory | Sampling applies, or no stale key remains | AI-003 |
| `.github/copilot-instructions.md` | 30,536 B, 26 sections | Delete the XamlMcp section (L41-57); delete "MCP-First", "Output Control Invariants" and "Document Authoring" (L1-24); delete "Multi-File Agentic Execution Workflow" (L127-145); compress "Subagent Model" (L160-183); fix the SDK version at L232; fix the `mcp-tooling.md` and `codegraphy-setup.md` pointers at L61-66 | AI-002, AI-005, AI-006, AI-008, AI-009 | Five of the six findings live in the largest always-on file | Mandatory | ≈25,000 B file, every remaining section true | AI-002, AI-005, AI-006, AI-008, AI-009 |
| `.github/instructions/mcp-doc-research.instructions.md` | Stale server table, L30-40 | Rewrite against `.mcp.json`; delete the 128-tool-cap paragraph | AI-007 | Contradicts the configuration and another instruction file | Mandatory | One true statement of MCP state | AI-007 |
| `.github/instructions/winui3-ui-automation.instructions.md` | 27,256 B procedure matched by `**/*.{xaml,cs,ps1,resw}` | Reduce to a routing instruction; move the procedure to a skill | AI-010 | Largest per-edit context cost | Mandatory | Instruction at or below ~2 KB; procedure loads on demand | AI-010 |
| `.github/skills/<new-ui-automation-skill>/SKILL.md` | Does not exist | Create with `name`, `description`, no `disable-model-invocation` | AI-010 | Home for the moved procedure | Mandatory | Skill discovered by description | AI-010 |
| `.github/instructions/winui3-scaling-routing.instructions.md` | Routes to the WinUI skills | Add the new skill to the routing list | AI-010 | Discoverability | Mandatory | Routing list complete | AI-010 |
| `AGENTS.md` | 4,735 B; duplicates `copilot-instructions.md`; wrong project and audit-test counts; `python3` claim | Correct 17 projects / 17 `*AuditTests`; add SettingsControls; collapse duplicated blocks to pointers | AI-006, AI-012 | Wrong facts and double-paid rules | Mandatory | Short orientation file, one owner per rule | AI-006, AI-012 |
| `.github/agents/speckit.changelog.*`, `speckit.ci-guard.*`, `speckit.pr-bridge.*` | 12 one-line files pointing at uninstalled extensions | Delete | AI-011 | All 12 targets resolve BROKEN | Mandatory | No broken agent files | AI-011 |
| `.github/agents/speckit.superspec.*` | 5 files with no YAML header | Add `name` and `description`, or delete | AI-011 | Invalid for the Copilot schema | Mandatory | Every agent file loadable | AI-011 |
| `.github/skills/speckit-companion-*/` (24 folders) | Third copy of companion instructions | Delete | AI-014 | Not registered by any extension; duplicate of the agents | Mandatory | 19 skills remain | AI-014 |
| `.github/memories/repo/mcp-tooling.md` | Does not exist | Create from the verified playbook, or remove the pointers that name it | AI-005 | Pointers lead nowhere | Mandatory (one of the two) | Every named note exists | AI-005 |
| `~/.copilot/instructions/winui3-scaling-routing.instructions.md` | Duplicate of the repository file | Delete on this workstation only | AI-013 | Double load per XAML/C# edit | Optional — machine action, not a repository change | One copy loads | AI-013 |

---

## 8. Cross-Project Changes

No project, `.csproj`, package reference or shared library changes. Every approved change is to
AI-facing configuration at the repository root or under `.github/`. `MTM_Waitlist.sln` and all 17
projects are untouched.

Shared artefacts that must move together because two proposals touch them:

| Shared file | Proposals | Sequencing |
| --- | --- | --- |
| `.github/copilot-instructions.md` | AI-002, AI-005, AI-006, AI-008, AI-009, AI-012 | Apply in that order: facts first, then deletions, then compression, then cross-file dedupe |
| `AGENTS.md` | AI-006, AI-012 | Facts before dedupe |
| MCP registration (`.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json`) | AI-001, AI-003, AI-004 | AI-004 before AI-003 |
| `.github/instructions/winui3-*` and `.github/skills/` | AI-010, AI-013, AI-014 | AI-010 creates a skill folder; AI-014 deletes skill folders — confirm the new folder is not caught by the AI-014 deletion |

---

## 9. AI Development Environment

### Installed today (measured)

| Element | Count | State |
| --- | --- | --- |
| Project instruction files | 2 | Both load; content duplicated |
| Repository instruction files | 14 | 4 always-applied, 10 path-scoped |
| User instruction files | 13 | 6 always-applied, 7 path-scoped |
| Agents | 109 files | 92 valid, 17 invalid |
| Skills | 43 | 19 model-invocable |
| Prompt shims | 61 | 60 delegate to an agent |
| Hooks | 1 file, 2 events | `userPromptSubmitted`, `agentStop` |
| Repository MCP servers | 2 | Both unusable in their current form |
| Spec Kit extensions | 9 | All enabled, no skills registered |

### Proposed (approved)

| Change | Effect on the environment |
| --- | --- |
| AI-001, AI-002 | One fewer MCP server and no instructions for a capability the app does not have |
| AI-003, AI-004 | One registration per server; instructions no longer advertise a failing operation |
| AI-005, AI-006, AI-007 | Every named file exists; every stated count and version is true |
| AI-008, AI-009, AI-012 | ≈6,200 B less always-on text, with one owner per rule |
| AI-010 | 27,256 B loads on demand instead of on every code edit |
| AI-011, AI-014 | An agents folder where every file loads, and a skill catalogue without 24 duplicates |
| AI-013 | One scaling-routing instruction per machine |

### Permission and access requirements

None of the approved changes widens any permission. No new MCP server is introduced, so no new
filesystem, terminal or network grant is requested. AI-001 and AI-004 **reduce** granted surface.
The CSV section's replacement relies on PowerShell, which the agent already has.

### Related skills that must be run manually

Three repository skills carry `disable-model-invocation: true`, so no agent can load them and this
audit did not run them. They are named here so the decision to run them is explicit:

| Skill | Relevance to this audit |
| --- | --- |
| `/agent-performance-audit` | Overlaps this area. It audits the agent environment and exports a reusable optimizer prompt. If a deeper analysis of agent behaviour (as opposed to configuration) is wanted, run it directly; its findings are not claimed here |
| `/winui-agent` | Manual only. Relevant if the UI-automation skill from AI-010 is later implemented or exercised |
| `/workflow-edgecase-inspection` | Manual only. Relevant only if a stability stress pass is wanted; out of scope for `ai-tooling` |

### Duplicated material observed but not proposed

| Observation | Why no proposal |
| --- | --- |
| `.github/prompts/` holds 60 frontmatter-only shims | Each is 40 B and is the `/speckit.*` slash-command surface; the cost is negligible |
| `.specify/` holds 2.26 MB across 251 files | Generated by the extension installer; hand-editing would be undone by the next install |
| Companion skills and agents both hard-code `python3` | Verified working on this workstation; changing it would break Linux/macOS runs where `python` may be absent |
| `.vscode/settings.json` configures `csharpXamlGraph.*` while the instructions say code graphs were retired | The owning extension is user-level and was not enumerated (`--user-config` not passed), so the configuration cannot be shown to be stale |
| A database MCP server is absent although database work is common here | `mysql.exe`, `Database/DeployDatabase.cmd` and `Database/Mock/Validation/read_shape_metadata/validate.sql` already cover it; a new server would add credential exposure for no new capability |

---

## 10. Security Requirements

No CVE, advisory or license issue is in scope: Area A and Area `security` were not requested, and no
package version moves.

Security-relevant effects of the approved changes:

* **AI-001** removes a local executable from the MCP inventory. Net reduction in granted surface.
* **AI-004** removes a server that receives a storage path and can write files. Net reduction.
* **No secret is read, printed or moved.** `.github/copilot-instructions.md` carries a reference to a
  workstation credential note; this specification reproduces no value from it, and the approved
  changes do not alter that note.
* **AI-005** must not be satisfied by copying credential material into a new memory note. Create
  `mcp-tooling.md` from the troubleshooting playbook only.

---

## 11. Performance Requirements

Every claimed gain is a measured byte count, converted at ~3.6 bytes per token. No runtime benchmark
is claimed, because none was run.

| Effect | Measurement method | Expected result |
| --- | --- | --- |
| Always-on context | Sum of the always-applied files before and after | 90,887 B → ≈79,000 B (≈3,300 tokens saved) |
| Per-edit context on XAML/C# | Sum of instruction files matching `**/*.{xaml,cs}` plus `**/*.{xaml,cs,ps1,resw}` | 51,716 B → ≈24,460 B |
| Tool definitions | Count of `mcp_*` tools in a fresh session | −15 (AI-001), −39 (AI-004) |
| Agent files | Count of `.agent.md` files without a YAML header | 17 → 0 |
| Skill catalogue entries | Count of skill folders | 43 → 19 |

Verification is a re-measurement of the same sums after the change, not an estimate.

---

## 12. UX and Maintainability Requirements

The "user" of this work is the developer and the coding agent working in this repository.

* After the change, a single instruction file owns each rule (AI-012). A rule change is one edit.
* Every file named by an instruction exists (AI-005).
* Every number in an instruction is the number the build reports (AI-006).
* Every registered server can serve (AI-001, AI-004).
* Every file in `.github/agents` can load (AI-011).
* The UI-automation procedure stays available, but is read when needed (AI-010).

---

## 13. Architecture and Compatibility Requirements

* **Harness compatibility.** The repository targets the Copilot harness. `.github/agents` is a
  supported location for `.agent.md` files, and `description` is required by the Copilot schema.
  AI-011 follows that rule rather than inventing an alternative.
* **Portability.** AI-013 keeps the repository copy and deletes the machine-local copy, so the
  change does not depend on one workstation.
* **No new dependency.** No approved change adds a package, tool or server.
* **Reversibility.** Every change is a text edit to a tracked file. AI-001 and AI-002 are restorable
  from git in one commit. None requires a database or application change.
* **Untouched invariants.** The approved work does not add inline SQL, does not add a `<Page>` item,
  does not reintroduce a mock or demo mode, and does not move XAML views out of the app project.

---

## 14. Implementation Plan

### Phase 1 — Correct the instructions (no behavioural risk)

| Objective | Restore truthfulness of every always-loaded statement |
| --- | --- |
| Proposals | AI-005, AI-006, AI-007, AI-011 |
| Files | `.github/copilot-instructions.md`, `AGENTS.md`, `.github/instructions/mcp-doc-research.instructions.md`, `.github/agents/speckit.{changelog,ci-guard,pr-bridge,superspec}.*` |
| Changes | Create or remove the dead note pointer; correct five facts; rewrite the MCP table; delete 12 broken pointer agents; header or delete 5 superspec agents |
| Prerequisites | None |
| Phase dependencies | Must precede Phases 3 and 4, which edit the same files |
| Completion criteria | No `/memories/repo/*.md` pointer names a missing file; the counts and versions match the build; no `.agent.md` lacks a YAML header |

### Phase 2 — Remove configuration that cannot work

| Objective | One registration per server; no instructions for an absent capability |
| --- | --- |
| Proposals | AI-004, AI-001, AI-003, AI-014, AI-013 |
| Files | `.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json`, `.github/copilot-instructions.md` (CSV section), `.github/skills/speckit-companion-*/`, `~/.copilot/instructions/winui3-scaling-routing.instructions.md` |
| Changes | Remove both MCP servers; delete the duplicate registration and its sampling key; delete 24 companion skill folders; delete the user-level duplicate |
| Prerequisites | Phase 1 complete for the same files |
| Phase dependencies | AI-004 before AI-003 |
| Completion criteria | A fresh session lists no `xamlmcp` or CSV tools; a companion command still runs; one scaling-routing instruction is discovered |

### Phase 3 — De-duplicate always-loaded rules

| Objective | One owner per rule |
| --- | --- |
| Proposals | AI-008, AI-009, AI-012 |
| Files | `.github/copilot-instructions.md`, `AGENTS.md` |
| Changes | Delete three duplicated sections; delete the workflow section; compress the subagent section; collapse `AGENTS.md` duplicates to pointers |
| Prerequisites | Phases 1 and 2 (same files) |
| Phase dependencies | Depends on all earlier edits to `copilot-instructions.md` |
| Completion criteria | Measured always-on total at or below 79,500 B; no rule present in both files |

### Phase 4 — Restructure context loading

| Objective | Load the UI-automation procedure only when needed |
| --- | --- |
| Proposals | AI-010 |
| Files | `.github/instructions/winui3-ui-automation.instructions.md`, new `.github/skills/<slug>/SKILL.md`, `.github/instructions/winui3-scaling-routing.instructions.md` |
| Changes | Move the procedure to a skill; leave a routing instruction; add the skill to the routing list |
| Prerequisites | Phase 2 complete (skill folder deletions), so the new folder is not caught by cleanup |
| Phase dependencies | Last, because it changes what is discoverable |
| Completion criteria | The instruction file is at or below 2 KB; the skill is listed and loads when a UI-automation task is started |

---

## 15. Testing and Verification Strategy

| Change | Verification | Evidence required |
| --- | --- | --- |
| All | `dotnet build MTM_Waitlist.sln` | Build result unchanged (no project or package touched) |
| AI-001, AI-004 | Fresh Copilot session; inspect the MCP tool list | No `xamlmcp` or `csv-mcp-server` tools present |
| AI-003 | Read `.vscode/settings.json` and `.vscode/mcp.json` | One registration; no sampling key for a non-existent server |
| AI-005 | For every `/memories/repo/<name>.md` mentioned in instructions, `Test-Path` | Every path resolves |
| AI-006 | Diff every stated count and version against the project files and the `PackageReference` lines | 17 projects, 17 audit tests, Windows App SDK 2.4.0 |
| AI-007 | Read the rewritten table against `.mcp.json` | The table lists exactly the servers registered |
| AI-008, AI-009, AI-012 | Re-run the byte-sum script used in this audit | Always-on total at or below 79,500 B |
| AI-010 | Start a task that edits a `.cs` file without UI automation, then one that needs UI automation | Procedure absent in the first, discovered in the second |
| AI-011 | `Select-String -Pattern '^---$' -List` over `.github/agents` | No file without a header |
| AI-013 | List `.github/instructions` and the user instruction folder | One scaling-routing file in total |
| AI-014 | `copilot skill list` or the skill catalogue | 19 project skills |
| Audit artefacts | `check-deliverables.ps1 -Path Dependency_Assessment` | PASSED |

---

## 16. Acceptance Criteria

1. `.mcp.json` registers no server that cannot serve a request (AI-001, AI-004).
2. `.vscode/mcp.json` contains no duplicate server registration, and `.vscode/settings.json` contains no sampling key for a server that does not exist (AI-003).
3. Every `.github/memories/repo/<name>.md` path named by an instruction file exists (AI-005).
4. Every stated project count, audit-test count and package version in `AGENTS.md` and `.github/copilot-instructions.md` matches the build (AI-006).
5. `.github/instructions/mcp-doc-research.instructions.md` describes the servers that are actually registered (AI-007).
6. No rule appears in both `AGENTS.md` and `.github/copilot-instructions.md` (AI-012).
7. The always-applied instruction total, measured with the audit's byte-sum method, is at or below 79,500 B (AI-008, AI-009, AI-012).
8. `.github/instructions/winui3-ui-automation.instructions.md` is at or below 2,000 B and the procedure is reachable as a skill (AI-010).
9. No `.agent.md` file under `.github/agents` lacks a YAML header with `name` and `description` (AI-011).
10. Exactly one WinUI 3 scaling-routing instruction is discovered on this workstation (AI-013).
11. `.github/skills` contains 19 skill folders (AI-014).
12. `dotnet build MTM_Waitlist.sln` succeeds, and no `.csproj`, `.sln` or source file changed.
13. No rejected, deferred, pending or keep-current item is implemented. All 14 proposals are `APPROVED`, so this criterion is satisfied by completing items 1-12 and by adding nothing else.

---

## 17. Risks and Constraints

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Deleting the workflow and UI-automation text changes agent behaviour for the worse | Medium | Wasted iterations, or a forgotten DPI rule | Both are one-commit reverts. Restore the workflow section if the first post-change run regresses; keep the skill alongside the routing instruction so the procedure stays reachable |
| A companion command disappears if a host resolves it from skills rather than agents | Low | One broken slash command | Run one companion command after AI-014; restore that one skill folder if it fails |
| AI-004 removes working read tools together with the broken loader | Medium | Less convenient schema analysis | The instruction replacement names the PowerShell equivalent |
| Multiple proposals edit `copilot-instructions.md` and conflict | High if applied in parallel | Merge churn | The phase order in Section 14 is mandatory |
| `.specify/` regenerates the deleted files | Low | Work redone | The deleted files are under `.github/`, generated from installed extensions; record the extension version so a reinstall is visible |
| Instruction text is guidance, not a control | Certain | A rule can be ignored regardless | No acceptance criterion depends on model compliance; every one is a file or text check |

Constraints: read-only audit; no installs; no manifest or source edits; every change reversible with
`git revert`; no secret value reproduced anywhere.

---

## 18. Out of Scope

* Every area other than `ai-tooling`: dependency upgrades, application modernization, CVE and
  supply-chain remediation, performance profiling, UX, architecture, tests, database and
  localization.
* User-level MCP servers, extensions and settings were not enumerated, because `--user-config` was
  not passed. Where a user-level file appears (AI-013), it appears only because a repository file
  duplicates it.
* Any application `.cs` or `.xaml` change, any `.csproj` or `PackageReference` change, and any
  database or stored-procedure change.
* Any new MCP server or extension.
* Rejected, deferred, pending and keep-current proposals: **none**. All 14 proposals were approved.
  The observations in Section 9 ("Duplicated material observed but not proposed") were never
  proposals and are excluded.

---

## 19. User Decisions and Approval Register

| Proposal ID | Proposed Change | Category | Priority | Decision | Notes |
| --- | --- | --- | --- | --- | --- |
| AI-001 | Remove the unwired `xamlmcp` server from `.mcp.json` | AI tooling | high | APPROVED | 0 code references; `xamlmcp check` reports 0 running apps; ~15 dead tool definitions per request |
| AI-002 | Delete the 5,673 B XamlMcp instruction section | AI tooling | high | APPROVED | Describes a workflow that cannot run; restorable with AI-001 |
| AI-003 | Remove the dead duplicate `csv-mcp-server` and re-key sampling | AI tooling | high | APPROVED | Storage path does not exist; sampling key targets the dead server |
| AI-004 | Remove the CSV MCP server | AI tooling | medium | APPROVED | `load_csv` fails at the only published version; 39 tool definitions |
| AI-005 | Repair the broken `/memories/repo/mcp-tooling.md` pointer | AI tooling | high | APPROVED | Three instruction files send agents to a missing file |
| AI-006 | Correct stale facts in the project instruction files | AI tooling | high | APPROVED | 17 projects not 15; 17 audit tests not 15; SDK 2.4.0 not 2.3.1; `python3` works here |
| AI-007 | Rewrite the stale MCP table in `mcp-doc-research` | AI tooling | high | APPROVED | Wrong config location and an obsolete cap rationale |
| AI-008 | Delete sections that restate an always-applied instruction file | AI tooling | medium | APPROVED | 2,011 B paid twice per request; no rule lost |
| AI-009 | Retire the generic workflow section; compress the subagent table | AI tooling | medium | APPROVED | ~2,500 B saved; behavioural risk accepted, revert path stated |
| AI-010 | Load the UI-automation instructions on demand | AI tooling | high | APPROVED | 27,256 B matched by `**/*.{xaml,cs,ps1,resw}` |
| AI-011 | Clean up the 17 headerless agent files | AI tooling | medium | APPROVED | 12 point at uninstalled extensions; 5 have no YAML header |
| AI-012 | De-duplicate `AGENTS.md` against `copilot-instructions.md` | AI tooling | medium | APPROVED | Both load; six rules duplicated; ≈1,700 B |
| AI-013 | Consolidate the duplicated scaling-routing instruction | AI tooling | low | APPROVED | Repository copy canonical; user copy deleted on this workstation |
| AI-014 | Retire the 24 unregistered companion skills | AI tooling | low | APPROVED | `registered_skills: []`; third copy of the companion instructions |

Decision counts: APPROVED 14 · REJECTED 0 · DEFERRED 0 · MORE_RESEARCH_REQUIRED 0 ·
KEEP_CURRENT 0 · PENDING_USER_DECISION 0.

---

## 20. Decisions Required

None outstanding. All 14 proposals received a decision on 2026-11-26.

Open research questions carried forward, none of which blocks implementation:

1. Whether this workstation's user-level instruction set (13 files, 6 always-on) can be trimmed was
   not assessed, because `--user-config` was not passed. Re-run with that flag to cover it.
2. Whether `csharpXamlGraph.*` in `.vscode/settings.json` belongs to an extension that is still
   installed could not be verified without enumerating user-level extensions.
3. Whether a maintained CSV MCP server exists with a working loader was checked only against the
   package this repository already uses; no alternative was vetted, because AI-004 recommends
   removal rather than replacement.

---

## 21. Research References

| Source | Used for | Date checked |
| --- | --- | --- |
| https://code.visualstudio.com/docs/agents/reference/custom-agent-file-reference | Agent file locations; `description` required by the Copilot schema; skills versus instructions | 2026-11-26 |
| https://code.visualstudio.com/docs/agent-customization/custom-agents | What a custom agent is for; "keep shared rules in project instructions" | 2026-11-26 |
| https://code.visualstudio.com/docs/agent-customization/custom-instructions | Project instruction files (`.github/copilot-instructions.md` **or** `AGENTS.md`); `applyTo` activation; skills for multi-step workflows | 2026-11-26 |
| https://pypi.org/pypi/csv-mcp-server/json | `csv-mcp-server` 1.0.0, published 2025-08-13, is the only release | 2026-11-26 |
| Local: `xamlmcp check --json` | XamlMcp runtime pass, 0 instrumented apps | 2026-11-26 |
| Local: `csv-mcp-server` `health_check` and `load_csv` | Server healthy; `load_csv` fails with a pydantic `ProgressNotificationParams.total` error | 2026-11-26 |
| Local: `.specify/extensions/.registry` | `registered_skills: []` for all 9 extensions | 2026-11-26 |
| Local: `MTM_Waitlist.csproj`, `Directory.Build.props` | Windows App SDK 2.4.0; SettingsControls 8.2.251219; application version 1.1.1 | 2026-11-26 |
| Local: `AGENTS.md`, `.github/copilot-instructions.md`, `.github/instructions/**`, `.github/agents/**`, `.github/skills/**`, `.mcp.json`, `.vscode/*.json`, `.github/memories/repo/**` | Every measured size, count and quotation in this document | 2026-11-26 |
