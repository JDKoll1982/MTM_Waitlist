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
     NON-NEGOTIABLE Context7 / Microsoft Learn rule, the mandatory Serena location-and-config self-healing
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

Bump rationale: MINOR — Principle IV materially expanded to NON-NEGOTIABLE and the mandatory Serena
self-healing rule added. No principle removed and no existing obligation weakened or redefined
incompatibly, so this is not a MAJOR change.

## 1.1.1 → 1.1.2 (2026-09-12) — PATCH

Bump rationale: PATCH — constitution optimization audit (`/speckit.optimize.run`). No principle added,
removed, redefined, or weakened; no obligation diminished.

Applied:
  • Sync Impact Report history extracted to this file; the constitution keeps a 3-line pointer.
  • Principle IV compressed; the 4-step Serena self-healing procedure and the repo-specific focus list
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
