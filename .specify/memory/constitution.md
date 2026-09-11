<!--
SYNC IMPACT REPORT
==================
Version change: 1.1.0 → 1.1.1
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

Historical entry (superseded wording retained for traceability):
  Version change: 1.0.0 → 1.1.0
  Bump rationale: MINOR — Principle IV materially expanded to NON-NEGOTIABLE and the mandatory Serena
  self-healing rule added. No principle removed and no existing obligation weakened or redefined
  incompatibly, so this is not a MAJOR change.

Follow-up TODOs: none — every follow-up recorded by the 1.1.0 report is closed above.
-->

# MTM_Waitlist Constitution

## Core Principles

### I. Spec-First, Verified Delivery (NON-NEGOTIABLE)

Non-trivial features and refactors MUST begin with a written specification before code: the Spec Kit
sequence (`/speckit.constitution` → `/speckit.specify` → `/speckit.plan` → `/speckit.tasks`) precedes
implementation, and a checklist (the repo `checklist-execution` skill) drives the work. Tasks and
checklist items MUST be marked complete only when the work is implemented, builds clean, and is
verified — never speculatively. Exceptions: trivial, single-file, low-risk edits.

Rationale: specifications and executable checklists make intent reviewable and completion auditable;
unverified "done" ticks destroy the checklist's value as a record.

### II. Live Data Integrity — Internal Stores Are Never Mocked

The application's own store (`mtm_waitlist`), the WIP store (`mtm_wip_application_winforms`), and the
Receiving store (`mtm_receiving_application`) MUST always be read and written live. No user-facing or
hidden setting, flag, or helper may short-circuit reads or writes to these stores. The only external
source that MAY be mocked is Infor Visual (read-only to us), and only through the automatic cached
fallback when it is unreachable — there MUST be no manual demo/mock mode. Persistence paths (e.g.
work-station setup saves, waitlist request lifecycle) MUST always reach the real store.

Rationale: a system that silently discards a user's save — or substitutes sample rows for real data —
is not trusted and cannot be operated on a production floor.

### III. Stored-Procedure-First Database Discipline

All database artifacts MUST follow the repo schema rules: canonical naming conventions, one artifact
per folder with paired `create.sql` / `rollback.sql`, and the hand-maintained master lists
(`AllTables.sql`, `AllSPs.sql`, `AllSeeds.sql`) plus `update_table_descriptions.sql` kept in sync.
Every data operation MUST go through a stored procedure; inline or hard-coded SQL statement text MUST
NOT remain in application code. Schema, seed, and stored-procedure changes MUST be authored per the
rules and validated against a live database before being declared complete.

Rationale: centralized, reviewable, versioned data access prevents drift across callers and keeps the
schema discoverable; ad-hoc inline SQL bypasses both.

### IV. MCP-First, Grounded Decisions (NON-NEGOTIABLE)

Before writing or changing code, decisions MUST be grounded in authoritative sources using the
available MCP servers: Serena for indexed repository exploration, Context7 for library/framework
documentation, and Microsoft Learn for Windows / WinUI / Windows App SDK guidance. For APIs that may
have changed, documentation MUST be preferred over model memory.

Whenever the agent is unsure about anything code-related, or does not know how to implement
something, consulting Context7 and Microsoft Learn is NON-NEGOTIABLE. Model memory alone MUST NOT be
treated as sufficient grounds for a code decision or an implementation approach in that situation.
This applies at minimum to: API names, signatures, and syntax; configuration and setup; version
migration and breaking-change behavior; library-specific debugging; CLI and tool usage; and official
platform guidance. Substituting recall, or deferring the question to the user, MUST NOT replace a
documentation lookup.

Serena self-healing is mandatory. When Serena fails such that its location or installation cannot be
discerned — for example the tool cannot be found, or resolves to a path that does not exist — the
agent MUST NOT abandon the lookup. It MUST: (1) inspect Serena's configuration file
(`~/.serena/serena_config.yml`, plus project-level `.serena/project.yml` or `.serena/project.local.yml`
when present); (2) account for the user-profile folder changing between the work and home
environments (`jkoll` at work, `johnk` at home), which invalidates profile-rooted paths such as
`C:\Users\<profile>\...` entries in the config and in the MCP server launch command; (3) update the
configuration accordingly; and (4) retry the operation. If the retry still fails, the agent MUST
report the config path inspected, the change attempted, and the observed error rather than silently
falling back to ungrounded guesswork.

Workarounds that deviate from documented guidance MUST be recorded as such.

Rationale: platform and library surfaces evolve, and this repo already carries profile-rooted Serena
config paths that break when the machine's user profile changes; grounding decisions in retrieved
documentation — and repairing the retrieval tool instead of bypassing it — prevents regressions that
stem from stale assumptions.

### V. WinUI 3 Platform Conformance

UI code MUST use `Microsoft.UI.Xaml` namespaces (never legacy `Windows.UI.Xaml`), follow the existing
MVVM patterns (CommunityToolkit.Mvvm source generators, `INavigationAware`), and route navigation
through the existing shell/navigation services rather than parallel stacks. MSIX-only APIs MUST be
guarded with `RuntimeHelper.IsMSIX`. User-facing strings MUST be localized (`.resw` + `GetLocalized`
with a fallback), and any new converter/resource MUST be registered in `App.xaml` in the same change.
Layouts MUST be fluid and accessible: no hardcoded pixel boundaries for layout-critical containers
(use `Auto`/star sizing with `Min*`/`Max*` bounds), ThemeResource-backed text styles, `MaxWidth` +
`TextTrimming` for headers, scrolling/wrapping for overflow, and `AdaptiveTrigger` states for
interactive overlays paired with matching content padding. Generated artifacts (`obj/`, `*.g.cs`,
`*.g.i.cs`) MUST NOT be edited.

Rationale: Windows App SDK requires modern namespaces; hardcoded sizing and unregistered resources are
the dominant sources of XAML build failures and scaling/accessibility defects in this codebase.

### VI. Evidence-Based Verification Gates

Every change MUST pass the documented gates before it is considered done: a clean solution build
(`dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → 0 warnings,
0 errors) and a green unit suite (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj
-c Debug -p:Platform=x64`). `WMC9999` MUST be treated as a real (masked) XAML error and surfaced,
not ignored. Where a change touches database paths, the opt-in live DB-integration tests MUST pass or
be explicitly recorded as environment-gated. Known build quirks (parallel dual-build locks; stale PRI
artifacts) MUST be resolved by the documented technique, not by disabling the check.

Rationale: the repo's XAML toolchain masks real errors and its solution build can race; explicit,
reproducible gates are the only reliable signal that a change is sound.

## Additional Constraints

### Security & Secrets

Credentials, connection strings, API keys, and shared tokens MUST NOT be hardcoded, committed, or
logged. Configuration MUST come from `appsettings.json`/environment variables, and service credentials
MUST be stored securely (e.g. DPAPI-protected) and never displayed in full. Destructive operations
(e.g. database restore) MUST require explicit confirmation.

### External Integration & Cache Boundaries

External read-only systems (Infor Visual) MUST never be written to. Any cached copy of external data
MUST live in its own dedicated store, MUST be treated as a fallback only (never authoritative for
internal data), and MUST be updated as complete snapshots (atomic swap) so readers never observe
partial data. Local AI features MUST remain privacy-first and local by default.

### Documentation & Extensibility

Changes that add or retire modules, data stores, or integration patterns MUST update the affected
documentation (e.g. `WeekendProject/*`, `README.md`, `CHANGELOG.md`) in the same change, including
removing stale references. New extensibility patterns MUST ship with a documented, ordered playbook so
the next contributor can extend the system without redesigning it.

## Development Workflow & Quality Gates

- **Spec Kit core sequence:** `constitution` → `specify` → `clarify` → `plan` → `tasks` → `analyze` →
  `implement`. Features live under `specs/NNN-<short-name>/`; `.specify/feature.json` is the active
  feature pointer; specs are technology-agnostic (design goes in `plan.md`).
- **Checklist execution:** tasks are worked persona-by-persona (Database/Backend/Frontend/Full Stack/
  QA/Security/DevOps/Tech Lead Engineer), and a task is ticked only with proof (build/test/verification).
- **Build & test commands:** use the single-threaded build above; run the test project (not the app)
  for `dotnet test`; never create throwaway projects under the solution while sln tooling is active.
- **Review:** every PR/review MUST verify compliance with these principles; any added complexity MUST
  be justified (see Complexity Tracking in `plan.md`); deviations require written approval recorded in
  the relevant plan or review note.
- **Structure:** the WinUI app is the composition root (Views/XAML/DI/navigation); testable non-view
  code lives in per-module class libraries (`MTM_Waitlist.*`) that the test project references directly.

## Governance

This constitution supersedes other conflicting practices; where it is silent, the repo instruction
files under `.github/instructions/` and `.github/copilot-instructions.md` govern. Amendment is made
only via `/speckit.constitution`, which MUST propagate changes to dependent templates and artifacts and
emit a Sync Impact Report. Versioning is semantic: **MAJOR** for backward-incompatible principle
removals or redefinitions, **MINOR** for a new principle or materially expanded guidance, **PATCH** for
clarifications and non-semantic refinements. Dates are recorded in ISO `YYYY-MM-DD`. Compliance is
reviewed on every change and at each feature phase gate; unjustified violations block completion.
Runtime development guidance remains in the repo instruction files, which MUST stay consistent with
this constitution.

**Version**: 1.1.1 | **Ratified**: 2026-09-09 | **Last Amended**: 2026-09-10
