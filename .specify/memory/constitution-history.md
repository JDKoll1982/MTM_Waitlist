# MTM_Waitlist Constitution — Amendment History

This file is the archived Sync Impact Report history for `.specify/memory/constitution.md`.
It is **not** governance: it carries no normative rule. The constitution itself keeps only a
short Sync Impact Report pointer at the top. Consult this file for the rationale, resolved
follow-ups, and template-status record of each amendment.

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
