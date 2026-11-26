---
name: workspace-modernization-audit
description: 'Run a read-only workspace-wide audit of dependency health, technology modernization, security, performance and AI development tooling, get one explicit approval decision per proposal, then emit a PDF report plus a Spec Kit input specification. Use when asked to audit outdated packages or CVEs, plan a framework or SDK upgrade, find custom code a library or framework-native API should replace, review MCP servers, extensions or Copilot instructions, or produce a modernization roadmap with per-item sign-off.'
argument-hint: '[full | dependencies | modernization | ai-tooling | security | performance | ux | architecture | tests | database | docs | localization]... [--batch | --strict] [--deep] [--since <ref>] [--focus <glob>] [--project <name>] [--min-priority <level>] [--top <n>] [--out <folder>] [--markdown-only] [--json] [--resume] [--baseline <folder>] [--user-config] [--offline]'
---

# Workspace modernization audit

A read-only assessment that ends in an approval register and a document set. It is **not** a
package-version checker: it evaluates dependency health, opportunities to replace hand-written code
with framework or library capability, security, performance, and the AI development environment.

The audit **never implements**. Approval means "may appear in the specification", not "go change it".

## Arguments

Invoke as `/workspace-modernization-audit <areas> [flags]`. Parse these in
[Phase 0](#phase-0--arguments-and-tool-discovery), apply the defaults, and state the resolved settings
once before any research starts.

### Areas — which research runs (positional, default `full`)

| Argument | Runs |
| --- | --- |
| `full` | Every area in this table |
| `dependencies` | Area A — inventory, current releases, vulnerabilities, migration cost |
| `modernization` | Area B — custom code versus framework-native, package or SDK alternatives |
| `ai-tooling` | Area C — instructions, agents, skills, MCP servers, extensions, settings, context cost |
| `security` | CVEs, supply-chain posture, unsafe APIs, credential exposure |
| `performance` | Hot paths, allocation, query and I/O cost, build and test time |
| `ux` | Usability, accessibility, responsiveness, validation, feedback, consistency |
| `architecture` | Layering, DI registration completeness, project graph, dead code, duplicated infrastructure, module boundaries |
| `tests` | Guardrail and audit tests, coverage of risky paths, slow or flaky tests, missing tests for touched code |
| `database` | Stored-procedure coverage, schema-artifact drift, index and query shape, mirror freshness, rollback coverage |
| `docs` | Drift between `docs/` and code, stale instructions, duplicated guides, orphaned references |
| `localization` | `.resw` hygiene, hardcoded strings, unused keys, culture handling |

Areas combine: `dependencies security` runs both. Anything not named is out of scope and must not
appear in the register, the report or the specification.

### Flags

| Flag | Default | Effect |
| --- | --- | --- |
| `--batch` | on | Group approval questions by category, several per call — still one question per proposal |
| `--strict` | — | One question per call, waiting for the answer before the next |
| `--deep` | — | Exhaustive research: transitive graph, every viable candidate compared, a migration plan per proposal. Costs substantially more time and tokens |
| `--since <ref\|date>` | — | Incremental run. Audit only code changed since a git ref or date; unchanged code is still mapped, never re-audited |
| `--focus <glob>` | — | Restrict findings to paths matching the glob |
| `--project <name>` | — | Restrict code findings to one project; dependency findings still cover the solution |
| `--min-priority <level>` | `low` | Ask only about proposals at or above `critical`, `high`, `medium` or `low`. Lower ones stay in the register, unasked |
| `--top <n>` | — | Stop researching after the `n` highest-value proposals; the remainder are listed as not assessed, never dropped |
| `--out <folder>` | `Dependency_Assessment` | Where the deliverables are written |
| `--markdown-only` | — | Skip the PDF. A short `REPORT_SUMMARY.md` carries the owner-facing findings instead |
| `--json` | — | Also write `register.json`, the register as machine-readable data |
| `--resume` | — | Continue from the register in `<folder>` instead of starting over; unanswered rows stay `PENDING_USER_DECISION` |
| `--baseline <folder>` | — | Compare against a completed earlier run and report drift: approved but not implemented, new since then, resolved |
| `--user-config` | — | Extend the AI scope to user-level MCP servers, extensions and settings, not only the repository |
| `--offline` | — | No network research. Version, API and platform claims are marked unverified rather than guessed |

### Argument rules

* A bare word is an area. An unrecognised word or flag is an error — list the valid arguments and stop
  rather than guessing a scope.
* `--out`, `--project`, `--focus`, `--since`, `--baseline`, `--min-priority` and `--top` require a
  value; a missing value is the same error.
* `--top` and `--min-priority` narrow what is **researched and asked**, never what is **reported**: a
  finding that was cut stays in the register with the reason.
* **There is deliberately no blanket-approval flag and no implementation flag.** A run that skipped
  per-proposal approval, or applied its own proposals, would no longer be this skill.

## Deliverables

Files land in `--out` (default `Dependency_Assessment/`): the PDF report for the application owner, the
specification for `/speckit.specify`, `REPORT_SUMMARY.md` under `--markdown-only`, and `register.json`
under `--json`. Never overwrite an existing deliverable — version it. Section order and print contract:
[deliverables.md](./references/deliverables.md).

## Non-negotiables

1. **Read-only except the two deliverables.** No installs, no manifest edits, no source edits, no
   extension/MCP/instruction changes, no migrations.
2. **One approval question per proposal.** Never one global approval question, never infer approval
   from a related approval, never treat silence as approval.
3. **No recommendation without evidence.** Every claim cites a file path and line, or an official URL
   with the date checked. Label anything unverified as a hypothesis or drop it.
4. **Never print secrets.** This repo's `appsettings.json` carries live connection strings. Note that
   credentials exist and flag any that are committed; never echo a value, an environment variable, or
   a `.env` body.
5. **No pre-release defaults.** Preview, alpha, beta, nightly and RC versions are never recommended as
   production targets.

## Phases

### Phase 0 — arguments and tool discovery

Resolve the [arguments](#arguments) first:

* Confirm every area word is known, and that `--project` names a project that exists.
* Set the cadence (`--batch` unless `--strict`), the output folder, the depth, and whether a PDF and
  `register.json` are required.
* Keep `--since`, `--focus`, `--project`, `--min-priority` and `--top` as research limits, `--offline`
  as a research constraint, and `--baseline` as a comparison against an earlier completed run.
* On `--resume`, read the existing register and treat its rows as settled unless the user reopens one.
* Print one line naming the resolved areas, cadence, depth, output path and deliverable set, then
  continue without asking for confirmation.

Then list the tools actually available and record their **real** names. Two matter:

* The interactive question tool (VS Code `vscode/askQuestions`, an `ask_user` tool, or equivalent).
* The subagent tool, if any.

Never invent a tool name. If no question tool exists, run the [fallback](#if-no-question-tool-exists)
and mark every undecided proposal `PENDING_USER_DECISION`.

Also record: date, VS Code version, workspace folder count, and whether the workspace is multi-root.

### Phase 1 — workspace discovery

Map the workspace before judging it. Sources and what to extract:
[audit-scopes.md](./references/audit-scopes.md#1-discovery-checklist).

Always map the whole workspace — dependency and relationship findings are only trustworthy when the
whole solution is known. `--project` narrows the **code** findings reported in Area B, not the map.

Minimum: every solution/project, target frameworks, `Directory.Build.props` / `.Packages.props` /
`global.json`, lockfiles, declared **vs resolved** versions, project references, duplicate or
conflicting versions, and the existing AI instruction and MCP configuration. State plainly what was
inaccessible instead of guessing.

Read the existing instructions first (`AGENTS.md`, `.github/copilot-instructions.md`,
`.github/instructions/`, `.github/skills/`, `.vscode/mcp.json`, `.mcp.json`, `.specify/`). They change
what counts as a valid recommendation — for example this repo forbids inline SQL, demo/mock modes, and
explicit `<Page>` items, so a proposal that reintroduces any of them is wrong regardless of its merits.

### Phase 2 — research the selected areas

Run only the areas chosen in Phase 0, at the depth chosen in Phase 0. With `--deep`, add the transitive
dependency graph, compare every viable candidate rather than the obvious one, and give each proposal a
migration plan. Without it, stay with the direct dependencies and the strongest alternative per finding.

Run the areas in parallel subagents when a subagent tool exists; otherwise sequentially, summarising
each. Return at most ~400 words per area in the shape
`ID | finding | evidence | confidence | expected gain | cost/risk | effort`.

* **A — Dependency health.** Inventory, current release research, security, migration cost.
* **B — Code modernization.** Custom implementations that a framework-native API, an installed
  package, or a first-party SDK would replace.
* **C — AI development environment.** Instructions, agents, skills, MCP servers, extensions, settings,
  context and token efficiency.

Category detail: [audit-scopes.md](./references/audit-scopes.md).

Verify the top claims yourself before they reach the user. A subagent finding is not evidence.

### Phase 3 — proposal register

Every candidate change gets an ID and a full record. Formats, fields, classification vocabulary and
the candidate comparison table: [proposal-register.md](./references/proposal-register.md).

Prefixes: `DEP-` dependency, `MOD-` modernization, `SEC-` security, `PERF-` performance, `UX-` user
experience, `AI-` AI tooling, `DEV-` development workflow, `DB-` database schema or procedure.

Split anything that cannot be reversed independently into separate proposals. Several file edits that
are genuinely inseparable parts of one change may travel together — name every affected file.

### Phase 4 — approval interview

Cadence follows Phase 0: `--batch` (default) groups questions by category with several in one call;
`--strict` sends one question and waits. Either way there is **one question per proposal**, each with
its own recorded answer. Each question carries: current state, recommended change, why, affected
projects and files, risks and compatibility, effort, and the source.

Options, plus free text for an alternative: `APPROVE` · `REJECT` · `DEFER` · `MORE RESEARCH` ·
`KEEP CURRENT`.

Statuses recorded in the register: `APPROVED`, `REJECTED`, `DEFERRED`,
`MORE_RESEARCH_REQUIRED`, `KEEP_CURRENT`, `PENDING_USER_DECISION`.

`MORE RESEARCH` loops: investigate, update the proposal, ask again, record the new answer. When a
decision changes, keep both the old and the new entry.

### Phase 5 — roadmap and validation

Group only approved work into phases: critical security and compatibility, shared dependencies,
framework and SDK, replacements, performance, UX, AI environment, then testing. Identify shared files,
packages that must move together, ordering constraints and conflicts.

For every approved change state how it will be validated — restore, clean build, tests, scanning,
benchmarks, rollback — with measurable acceptance criteria where possible.

### Phase 6 — write the deliverables

Follow [deliverables.md](./references/deliverables.md) exactly: PDF section order and print contract;
specification section order and Spec Kit readiness rules.

Generate the PDF with tooling already present (CSS/HTML renderer, a Markdown→PDF tool, or Word
export). Do **not** install a PDF dependency without explicit approval. Render it, then open the pages
and confirm pagination, margins, clipping, tables and links — a successful generator run is not
evidence of a correct document.

Under `--markdown-only`, skip the PDF entirely and write `REPORT_SUMMARY.md` carrying the report's
executive summary, approval summary and final recommendations. Do not generate a half-built PDF. Under
`--json`, also write `register.json`.

### Phase 7 — validate, then report

Run the deterministic check, adding `-SkipPdf` when `--markdown-only` was used:

```powershell
pwsh -ExecutionPolicy Bypass -File .github/skills/workspace-modernization-audit/scripts/check-deliverables.ps1 -Path <out-folder> [-SkipPdf]
```

It verifies the expected files exist and are substantial, that the specification's required sections
are present, that the approval register uses only known decision tokens, and that no proposal ID
appears in the document without a register row. Fix every failure before reporting.

Then report counts by decision, what was scanned, what was inaccessible, what was excluded, and the
exact deliverable paths. Confirm no change was implemented.

## Hooks to use

### Spec Kit gates (`.specify/extensions.yml`)

The specification is the input to `/speckit.specify`, so these declared gates fire and must be honored:

| Gate | Hook | Status | What it does |
| --- | --- | --- | --- |
| `before_specify` | `speckit.companion.before-step` | required | Records the specify start |
| `after_specify` | `speckit.companion.after-specify` | required | Records the specify finish |
| `after_specify` | `speckit.token-budget.compact` | optional | Compacts `spec.md` before `/speckit.plan` |
| `after_specify` | `speckit.harness.init` | optional | Sets up the state-externalized research harness |
| `after_specify` | `speckit.wireframe.generate` | optional | Generates wireframes when the feature has UI |

Hand the specification over with `/speckit.specify`, or route it through
[workflow-to-spec](../workflow-to-spec/SKILL.md) when the audit input is a named workflow. Do not
re-implement these hooks; leave entries with `enabled: false` alone and non-empty `condition` entries
to the HookExecutor.

### Agent lifecycle hooks (`.github/hooks/speckit.json`)

`userPromptSubmitted` → `.specify/events.py speckit.companion.on-prompt`, and
`agentStop` → `.specify/events.py speckit.companion.on-stop`. These fire automatically for every
prompt in this workspace, so the audit must **not** duplicate that journaling and must not hand-run
`/speckit.companion.before-step` as if it were missing. The hooks call `python`, never `python3` —
`python3` on this machine is a 0-byte Store alias that silently drops the journal entry.

### Peer skills

| Skill | Use it when | Note |
| --- | --- | --- |
| [agent-performance-audit](../agent-performance-audit/SKILL.md) | Area C overlaps it; it audits the agent environment and exports a reusable optimizer prompt | Manual invocation only — ask the user to run `/agent-performance-audit`; never claim its results |
| [mcp-doctor](../mcp-doctor/SKILL.md) | Any MCP proposal or diagnosis; validates config and probes tools | `scripts/mcp_check.py --probe` proves a server actually answers |
| [vscode-ai-customize](../vscode-ai-customize/SKILL.md) | A proposal adds or restructures instructions, an agent, a prompt or a skill | Frontmatter rules and `scripts/scaffold.py` |
| [vscode-ai-settings](../vscode-ai-settings/SKILL.md) | A proposal touches a VS Code or Copilot setting | Confirms the name, default and type before it is cited |
| [copilot-cli-ops](../copilot-cli-ops/SKILL.md) | Proving a skill, MCP server or hook is actually loaded | `copilot skill list`, `copilot mcp list`, `/env` |
| [checklist-creation](../checklist-creation/SKILL.md) | The approved roadmap should become an executable checklist | Keeps the roadmap reviewable after the PDF ages |
| [checklist-execution](../checklist-execution/SKILL.md) | Only after a separate implementation request | Never invoked by the audit itself |
| [workflow-to-spec](../workflow-to-spec/SKILL.md) | The audit input is a named workflow or a design under `docs/ui-designs` | Also the `/speckit.specify` handoff |
| [winui3-scaling-audit](../winui3-scaling-audit/SKILL.md) | XAML layout, DPI or responsiveness is in scope | `scripts/audit-scaling.ps1` produces the evidence |
| [winui3-dpi-checklist](../winui3-dpi-checklist/SKILL.md) | A display-scaling finding needs confirmation | Prints the display scale and the manual checklist |
| [qol-ui-design](../qol-ui-design/SKILL.md) | Any `UX-` proposal in this app | A UX proposal needs a rendered BEFORE/AFTER design |
| [winui-agent](../winui-agent/SKILL.md) | The user later asks to implement a WinUI change | Manual invocation only |
| [workflow-edgecase-inspection](../workflow-edgecase-inspection/SKILL.md) | Stability findings need a stress pass | Manual invocation only |

Skills marked `disable-model-invocation: true` cannot be auto-loaded. Name them in the report so the
user can run them; never fabricate what they would have found.

### MCP tools worth grounding against

Version and platform claims come from a tool, never from memory. The tool list, and which area each
serves: [audit-scopes.md](./references/audit-scopes.md#6-mcp-grounding).

## Project-specific guards

Findings that look reasonable elsewhere are wrong here:

* Internal stores are always read and written live — no demo or mock mode, no toggle that substitutes
  sample data.
* Every data operation goes through a stored procedure; schema artifacts ship `create.sql` with
  `rollback.sql`.
* Views in the app project are included by SDK defaults; never add explicit `<Page>` items.
* XAML views stay in the app; non-view code lives in the `MTM_Waitlist.*` libraries; services and view
  models are registered in `Services/DependencyInjection/ServiceRegistrationExtensions.cs`.
* The `MTM_Waitlist.Mock.Service` deployment script refuses to run off its own server host — do not
  propose bypassing that guard.
* Warnings are not errors and there is no CI workflow; do not propose enforcement the build lacks.

## If no question tool exists

Say so plainly in the report and in chat, keep researching, mark every undecided proposal
`PENDING_USER_DECISION`, and never mark anything approved.

## Done criteria

* Every proposal has a register row and a recorded decision.
* The expected deliverable set exists at the resolved output path and passes `check-deliverables.ps1`
  (`-SkipPdf` under `--markdown-only`).
* No research ran outside the areas named on the command line.
* The specification's implementation scope contains approved work only; rejected, deferred, pending
  and keep-current items are visible but outside scope.
* Counts, versions, file paths and IDs agree across the documents.
* Nothing was installed, edited or migrated.
