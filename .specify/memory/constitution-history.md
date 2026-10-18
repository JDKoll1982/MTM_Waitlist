# MTM_Waitlist Constitution — Amendment History

This file is the archived Sync Impact Report history for `.specify/memory/constitution.md`.
It is **not** governance: it carries no normative rule. The constitution itself keeps only a
short Sync Impact Report pointer at the top. Consult this file for the rationale, resolved
follow-ups, and template-status record of each amendment.

## 1.1.2 → 1.2.0 (2026-09-14) — MINOR

Bump rationale: MINOR — **Principle VII (Readable, End-User-Facing Delivery) added**. No principle was
removed, redefined, or weakened, and the amendment is append-only, so every existing reference to
Principles I–VI (for example `mcp-doc-research.instructions.md`'s "constitution Principle IV") still
points at the same rule.

What prompted it: the `speckit.companion.implement` workflow produced unreadable chat — the transcript
became garbled with half-rendered code blocks and stalled. Two causes were found and both are recorded
here because they are environment facts, not opinions:

  1. **The interpreter prerequisite was unsatisfiable on this machine, and the failure was silent.**
     Every Companion script call is written `python3 …`. On Windows `python3` is a Microsoft Store
     app-execution alias, not an interpreter: `C:\Users\johnk\AppData\Local\Microsoft\WindowsApps\python3.exe`
     is **0 bytes** and answers "Python was not found; run without arguments to install from the
     Microsoft Store". Measured 2026-09-14 with a 12-second bounded probe. `python` and `py` are real
     (3.10). So the per-task journal, the materialize fold, the coverage map and the drift report were
     all silently dropped, and the agent ticked boxes by hand instead — the doctor later reported
     `174 tasks checked in tasks.md with no journal entry` on specs/001 and `51 task finishes recorded
     inside 0s — journaling was batched` on specs/002. This is the **stall**: a failing command per task,
     retried, with its failure text landing in the chat.
  2. **The instructions told the model to emit fenced sample blocks, some shown indented inside bullets.**
     GFM: an opening fence indented by four or more spaces is an *indented code block*, so the backticks
     render literally and everything after them mis-renders; nested fences need a longer backtick run
     than the content. That is the **garbling**: a reproduced sample block became literal text and the
     rest of the reply collapsed. `copilot-instructions.md` made it worse by instructing "Output code
     blocks directly. Prioritize raw information density over prose."

Applied:
  ✅ `.github/instructions/response-format.instructions.md` — new, the working detail of Principle VII:
     plain English end-user-facing replies, no raw dumps, one line per step, fence hygiene with the GFM
     rule stated, and the mandatory closing section.
  ✅ `.github/copilot-instructions.md` — Output Control Invariants reconciled: the "output code blocks
     directly / raw information density" invariant was replaced by "make the edit with a tool, do not
     print the code", plus the fence rule and the closing-section rule.
  ✅ `.github/instructions/spec-kit.instructions.md` — Rules section now carries the response contract and
     the `python`-not-`python3` note for every script invocation.
  ✅ `.specify/extensions/companion/commands/*.md` and `.github/agents/speckit.companion.*.agent.md` — a
     `communication` part (response rules) and an `interpreter` part (resolve `python3` → `python` → `py`
     once, reuse it, never retry more than once, never tick a checkbox by hand when the writer cannot run).

Templates & dependent artifacts:
  ✅ .specify/templates/*  — verified unaffected: no template enumerates the principles
  ✅ .github/instructions/mcp-doc-research.instructions.md — unaffected; its Principle IV citation is
     unchanged by an append-only addition
  ⚠ plan.md (specs/001)    — still cites v1.0.0 and evaluates gates against six principles (tracked as T137)

Known wipe risk (recorded so it is not rediscovered): `specify extension update companion` replaces
`.specify/extensions/companion/**` from upstream and the deploy regenerates the agents, so the two parts
added there MUST be re-applied after any such update. The durable copies are the constitution and the
`.github/instructions/` files, which no installer touches.

## 1.1.0 → 1.1.1 (2026-09-10) — PATCH

Bump rationale: PATCH — no principle is added, removed, redefined, or weakened. This amendment closes the
two follow-ups the 1.1.0 report recorded as pending and corrects its diagnosis of the scaffold drift.

Resolved follow-ups:
  ✅ TODO(scaffold-drift) — the 1.1.0 report blamed a stale `.specify/scripts/powershell/common.ps1` and
     pointed at a version-matched resolver under
     `%APPDATA%\uv\tools\specify-cli\Lib\site-packages\specify_cli\core_pack\scripts\powershell`.
     Both parts of that diagnosis were wrong. Observed 2026-09-10:
       • the named core-pack path does not exist on this machine, so there was nothing to re-sync from;
       • `.specify/scripts/powershell/common.ps1` defines `Resolve-Template` (which returns the template
         PATH) while `resolve-template.ps1` (L25) and `setup-tasks.ps1` (L60) both call
         `Resolve-TemplateContent` (which must return the template CONTENT).
     Fix applied: `Resolve-TemplateContent` was added to `common.ps1` as a thin wrapper over the existing
     priority stack (overrides → presets → extensions → core), returning `$null` when nothing resolves so
     both callers keep their own error message and non-zero exit.
     Verified: `resolve-template.ps1 tasks-template` exits 0 and emits the real template markdown
     (18,858 bytes), and the function returns a single `System.String`.
  ✅ `.github/instructions/mcp-doc-research.instructions.md` now mirrors Principle IV as amended: the
     NON-NEGOTIABLE Context7 / Microsoft Learn rule, the mandatory tool location-and-config self-healing
     procedure (including the `jkoll` ↔ `johnk` profile-folder case), and a Repo-Specific Focus list that
     describes the shipped cached-fallback boundary instead of the retired demo toggles.

New prerequisite recorded (environment, not a principle change):
  • Every `.specify/scripts/powershell/*.ps1` invocation on this machine must pass
    `-ExecutionPolicy Bypass`. `Get-ExecutionPolicy -List` reports `Undefined` at every scope
    (MachinePolicy, UserPolicy, Process, CurrentUser, LocalMachine), so the effective policy falls back to
    Windows' Restricted default and the scripts fail with "running scripts is disabled on this system"
    before any logic runs. No policy was changed by this amendment.

Templates & dependent artifacts:
  ✅ .specify/templates/*                — unaffected by a PATCH clarification
  ✅ .specify/scripts/powershell/common.ps1 — repaired (see above); this is the write scope the 1.1.0 report
     deferred, and it is a script defect, not a principle
  ✅ .github/instructions/mcp-doc-research.instructions.md — amendment propagated (see above)
  ⚠ plan.md                              — still cites "v1.0.0, ratified 2026-09-09" and evaluates its
     Phase 0/Phase 1 gates against six principles; align on the next `/speckit.plan` touch (tracked as T137)

Follow-up TODOs: none — every follow-up recorded by the 1.1.0 report is closed above.

## 1.0.0 → 1.1.0 — MINOR (superseded wording retained for traceability)

Bump rationale: MINOR — Principle IV materially expanded to NON-NEGOTIABLE and the mandatory tool
self-healing rule added. No principle removed and no existing obligation weakened or redefined
incompatibly, so this is not a MAJOR change.

## 1.1.1 → 1.1.2 (2026-09-12) — PATCH

Bump rationale: PATCH — constitution optimization audit (`/speckit.optimize.run`). No principle added,
removed, redefined, or weakened; no obligation diminished.

Applied:
  • Sync Impact Report history extracted to this file; the constitution keeps a 3-line pointer.
  • Principle IV compressed; the 4-step tool self-healing procedure and the repo-specific focus list
    delegate to `.github/instructions/mcp-doc-research.instructions.md` (the MUST obligations remain).
  • Duplicated build-command string in Development Workflow now references Principle VI.
  • Deterministic rewrites: spec-exception definition (I), DPAPI specification (Security & Secrets),
    local-only AI (Cache Boundaries), deviation recording (Review), compliance verification (Governance).
  • Cross-references added: II → External Integration & Cache Boundaries; VI → Known Build Quirks;
    Documentation & Extensibility → Review step.

Templates & dependent artifacts:
  ✅ .specify/templates/*                — unaffected by a PATCH expression change
  ⚠ specs/001-module-mock-visual-fallback/plan.md — still cites v1.0.0; align on the next `/speckit.plan`
     touch (tracked as T137)

Follow-up TODOs:
  • T137 — align `plan.md` with the current constitution version.

## 1.2.0 → 1.2.1 (2026-09-27) — PATCH

Bump rationale: PATCH — no principle was added, removed, redefined, or weakened, and no new obligation
was introduced. This is an expression-only compression produced by a `/speckit.optimize.run` audit: the
constitution both restated in full and explicitly deferred to the repo instruction files that already
carry the same detail, so only the restatement was removed. Every normative clause (`MUST`, `MUST NOT`)
and every `Rationale:` paragraph survived — verified by an unchanged per-section count of normative
keywords (51 before, 51 after) and by seven `Rationale:` paragraphs before and after.

Applied:
  • Principle III — the schema-rule detail now cites
    `.github/instructions/database-schema-rules.instructions.md` inline. The master-list filenames
    (`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) and `update_table_descriptions.sql` were **retained**
    in the text rather than replaced by a bare reference, because the audit's premise that those
    filenames are enumerated in `database-schema-rules.instructions.md` is not accurate: that file names
    only `Bootstrap/update_table_descriptions.sql` and the per-artifact layout. Dropping the enumeration
    would have lost the specificity the "lists stay in sync" obligation depends on.
  • Principle IV — the enumerated "applies at minimum to" list and the step-by-step tool self-healing
    narration were compressed. The NON-NEGOTIABLE Context7 / Microsoft Learn rule, the model-memory
    prohibition, the mandatory self-healing requirement with its report-the-config-path obligation, the
    deviating-workaround-recording rule, and the pointer to
    `.github/instructions/mcp-doc-research.instructions.md` all remain.
  • Principle V — the API/naming detail was folded behind pointers to
    `.github/instructions/winui3-api-rules.instructions.md` and
    `.github/instructions/csharp-xaml-naming-rules.instructions.md` (added; the principle carried none).
    Every normative rule (Microsoft.UI.Xaml namespaces, MVVM / `INavigationAware`, shell navigation,
    `RuntimeHelper.IsMSIX`, localization, `App.xaml` registration, fluid/accessible layout, no editing of
    generated artifacts) remains.
  • Principle VII — the GFM citation and the restatement of the fence rule were compressed; the column-0
    requirement and the longer-backtick-run requirement remain normative, as does the pointer to
    `.github/instructions/response-format.instructions.md`.
  • Sync Impact Report — the HTML comment at the top of the constitution now carries only this
    amendment's report. The 1.1.2 → 1.2.0 narrative it previously held is preserved above in this file
    and was not re-derived.

Propagated:
  ✅ `.github/instructions/spec-kit.instructions.md` — its constitution citation was stale at
     v1.1.1 / 2026-09-10 while the file itself was v1.2.0 / 2026-09-14; it now cites v1.2.1 / 2026-09-27.
     This is the propagation half of the amendment, required by the Governance section ("the repo
     instruction files ... MUST stay consistent with this constitution").

Templates & dependent artifacts:
  ✅ .specify/templates/*                — unaffected by a PATCH expression change (still verified to
     enumerate no principle)
  ⚠ specs/001-module-mock-visual-fallback/plan.md — still cites v1.0.0 and evaluates gates against six
     principles (tracked as T137; unchanged by this amendment)

Not done in this amendment (recorded so it is not assumed):
  • The Companion pipeline (`specify → plan → tasks → implement`) was **not** added to Principle I or to
    the Workflow section; that is a separate, substantive amendment the owner has not approved in this
    pass.

Follow-up TODOs:
  • T137 — align `plan.md` with the current constitution version.

## 1.2.1 → 2.0.0 (2026-10-17) — MAJOR

Bump rationale: MAJOR — **Principle IV was redefined**, not merely reworded. The principle previously made a
specific MCP server the mandated tool for repository exploration, and carried a mandatory self-healing
procedure for that tool. Both are removed, so an agent that complied with the old text no longer complies
with the new text, which is the backward-incompatible redefinition the Governance section reserves for a
MAJOR bump.

What prompted it: the owner retired the MCP-based code-exploration server on 2026-10-17 and asked for its
references, registrations and project memory to be removed from the repository. The retirement was grounded
in the same reasoning that retired CodeGraphy on 2026-10-11: an indexed exploration server duplicates work
the editor's built-in code intelligence and `grep`/`glob`/`view` already do, and an index that can go stale
or fail to launch is a dependency the work does not need.

Applied:
  • Principle IV — the mandated-source sentence now names Context7 and Microsoft Learn, and adds an explicit
    obligation that repository facts come from the code itself, never from recollection. The NON-NEGOTIABLE
    documentation rule, the model-memory prohibition, the deviating-workaround rule and the pointer to
    `.github/instructions/mcp-doc-research.instructions.md` are unchanged.
  • Principle IV — the mandatory self-healing procedure for the retired tool was deleted. The durable lesson
    it carried (server commands hold workstation-absolute paths that break when the user-profile folder
    changes) is preserved in `mcp-doc-research.instructions.md` as a general check-before-blame rule, so no
    operator knowledge was lost with the tool.
  • Rationale — restated around documentation grounding and current-source grounding instead of the retired
    tool's configuration.

Migrated before deletion: the retired tool's project memories were reviewed one by one. The WinUI naming
lock and both database memories are already carried in full — and in their current form — by
`.github/instructions/csharp-xaml-naming-rules.instructions.md` and
`.github/instructions/database-schema-rules.instructions.md`, so nothing needed porting. The Module_Setup
slice note was obsolete except for its build quirk (explicit `<Page>` items cause `NETSDK1022` duplicate-item
errors), which is now recorded in the "Known Build Quirks" section of `.github/copilot-instructions.md`. The
memory-maintenance note was tool-specific and was not migrated.

Propagated:
  ✅ `.github/instructions/mcp-doc-research.instructions.md` — tool section replaced by local exploration
     guidance; registration table corrected; profile-path hazard retained.
  ✅ `.github/copilot-instructions.md` — canonical-policy pointer, CSV-tools line, code-exploration section
     and the multi-file workflow's discovery/edit phases.
  ✅ `.github/agents/module-create.agent.md`, `.github/agents/module-setup.agent.md` — descriptions, tool
     lists and required research phases.
  ✅ `.github/skills/winui-agent/SKILL.md` — description and Phase 1 discovery step.
  ✅ `.github/copilot-startup-steps.yaml` — retired tool removed from the advertised tool chains.
  ✅ `.github/instructions/winui3-ui-automation.instructions.md` — cross-reference re-pointed.
  ✅ `.github/memories/repo/tool-budget.md` and `repo/github-artifact-staleness.md` — tool-group tables and
     recorded MCP scopes corrected, with the retirement recorded.
  ✅ `.github/instructions/spec-kit.instructions.md` — constitution citation now v2.0.0 / 2026-10-17.
  ✅ `.mcp.json`, `.vscode/mcp.json`, `.vscode/settings.json` — registrations removed.
  ✅ Completed spec artifacts that named the retired tool were reworded to describe the same work without
     the tool name (specs 001, 002, 003, 008, 010).

Templates & dependent artifacts:
  ✅ .specify/templates/*                — no template enumerates principles or tools; unaffected.
  ⚠ specs/001-module-mock-visual-fallback/plan.md — still cites v1.0.0 and evaluates gates against six
     principles (tracked as T137; unchanged by this amendment).

Follow-up TODOs:
  • T137 — align `plan.md` with the current constitution version.
  • Restart the editor / start a new chat session so the tool list refreshes without the retired server.
