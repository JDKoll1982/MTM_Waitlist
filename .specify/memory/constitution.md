<!--
SYNC IMPACT REPORT
==================
Version change: 1.2.0 → 1.2.1 (PATCH — expression only; no principle added, removed, redefined, or
weakened, and no new obligation introduced).
Last amended: 2026-09-27.
Modified: Principles III, IV, V and VII compressed to their normative core by removing wording that
restated `.github/instructions/database-schema-rules.instructions.md`,
`.github/instructions/mcp-doc-research.instructions.md`,
`.github/instructions/winui3-api-rules.instructions.md`,
`.github/instructions/csharp-xaml-naming-rules.instructions.md` and
`.github/instructions/response-format.instructions.md`; every normative clause and every `Rationale:`
paragraph was retained, verified by an unchanged per-section count of normative keywords (51 before,
51 after).
Propagated: `.github/instructions/spec-kit.instructions.md` — its stale constitution citation
(v1.1.1 / 2026-09-10) now cites v1.2.1 / 2026-09-27.
Full amendment history and rationale: `.specify/memory/constitution-history.md`.
-->

# MTM_Waitlist Constitution

## Core Principles

### I. Spec-First, Verified Delivery (NON-NEGOTIABLE)

Features and refactors MUST begin with a written specification before code, except for the exception
below: the Spec Kit sequence (`/speckit.constitution` → `/speckit.specify` → `/speckit.plan` →
`/speckit.tasks`) precedes implementation, and a checklist (the repo `checklist-execution` skill)
drives the work. Tasks and checklist items MUST be marked complete only when the work is implemented,
builds clean, and is verified — never speculatively. Exceptions: an edit confined to a single file that
changes no public type, no navigation route, and no stored-procedure call.

Rationale: specifications and executable checklists make intent reviewable and completion auditable;
unverified "done" ticks destroy the checklist's value as a record.

### II. Live Data Integrity — Internal Stores Are Never Mocked

The application's own store (`mtm_waitlist`), the WIP store (`mtm_wip_application_winforms`), and the
Receiving store (`mtm_receiving_application`) MUST always be read and written live. No user-facing or
hidden setting, flag, or helper may short-circuit reads or writes to these stores. The only external
source that MAY be mocked is Infor Visual (read-only to us), and only through the automatic cached
fallback when it is unreachable — there MUST be no manual demo/mock mode. Persistence paths (e.g.
work-station setup saves, waitlist request lifecycle) MUST always reach the real store. The
cached-fallback boundary and the cache-mirror rules are defined in Additional Constraints
(External Integration & Cache Boundaries).

Rationale: a system that silently discards a user's save — or substitutes sample rows for real data —
is not trusted and cannot be operated on a production floor.

### III. Stored-Procedure-First Database Discipline

All database artifacts MUST follow the repo schema rules
(`.github/instructions/database-schema-rules.instructions.md`) — canonical naming, one artifact per
folder with paired `create.sql` / `rollback.sql`, and the hand-maintained master lists (`AllTables.sql`,
`AllSPs.sql`, `AllSeeds.sql`) plus `update_table_descriptions.sql` kept in sync. Every data operation
MUST go through a stored procedure; inline or hard-coded SQL statement text MUST NOT remain in
application code. Schema, seed, and stored-procedure changes MUST be authored per the rules and
validated against a live database before being declared complete.

Rationale: centralized, reviewable, versioned data access prevents drift across callers and keeps the
schema discoverable; ad-hoc inline SQL bypasses both.

### IV. MCP-First, Grounded Decisions (NON-NEGOTIABLE)

Before writing or changing code, decisions MUST be grounded in authoritative sources: Serena for
indexed repository exploration, Context7 for library/framework documentation, and Microsoft Learn for
Windows / WinUI / Windows App SDK guidance.

When the agent is unsure about anything code-related, consulting Context7 and Microsoft Learn is
NON-NEGOTIABLE: documentation MUST be preferred over model memory, model memory alone MUST NOT be
treated as grounds for a code decision, and substituting recall — or deferring the question to the
user — MUST NOT replace a documentation lookup. This applies at minimum to API names and signatures,
configuration, version migration, library-specific debugging, CLI usage, and platform guidance.

Serena self-healing is mandatory: when Serena's location or installation cannot be discerned, the agent
MUST NOT abandon the lookup — it MUST inspect and repair the configuration and retry — and if the retry
still fails the agent MUST report the config path inspected, the change attempted, and the observed
error rather than falling back to ungrounded guesswork.

Workarounds that deviate from documented guidance MUST be recorded as such.

Config-file locations, the profile-path procedure, and the repo-specific focus list are in
`.github/instructions/mcp-doc-research.instructions.md`.

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
Layouts MUST be fluid and accessible: `Auto`/star sizing with `Min*`/`Max*` bounds rather than hardcoded
pixel boundaries, ThemeResource-backed text styles, `MaxWidth` + `TextTrimming` for headers,
scrolling/wrapping for overflow, and `AdaptiveTrigger` states for interactive overlays paired with
matching content padding. Generated artifacts (`obj/`, `*.g.cs`, `*.g.i.cs`) MUST NOT be edited. The
naming and API detail lives in `.github/instructions/winui3-api-rules.instructions.md` and
`.github/instructions/csharp-xaml-naming-rules.instructions.md`.

Rationale: Windows App SDK requires modern namespaces; hardcoded sizing and unregistered resources are
the dominant sources of XAML build failures and scaling/accessibility defects in this codebase.

### VI. Evidence-Based Verification Gates

Every change MUST pass the documented gates before it is considered done: a clean solution build
(`dotnet build MTM_Waitlist.sln -c Debug -p:Platform=x64 /m:1 /nodeReuse:false` → 0 warnings,
0 errors) and a green unit suite (`dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj
-c Debug -p:Platform=x64`). `WMC9999` MUST be treated as a real (masked) XAML error and surfaced,
not ignored. Where a change touches database paths, the opt-in live DB-integration tests MUST pass or
be explicitly recorded as environment-gated. Known build quirks (parallel dual-build locks; stale PRI
artifacts) MUST be resolved by the documented technique, not by disabling the check. The technique is
documented in `.github/copilot-instructions.md` (Known Build Quirks).

Rationale: the repo's XAML toolchain masks real errors and its solution build can race; explicit,
reproducible gates are the only reliable signal that a change is sound.

### VII. Readable, End-User-Facing Delivery

Every chat reply MUST be written in **plain English** for the person who asked for the work, never as a
compiler log: what changed, what it means for the product, and what is still open. A reply MUST NOT paste
raw material into the chat — tool output, terminal transcripts, JSON payloads, stack traces, file dumps,
diffs, or whole artifact bodies. Progress MUST be reported in short sentences, never by restating the
task list and never by narrating each command as it is run.

Code fences MUST be safe to render: a fence starts at **column 0**, a fence indented by four or more
spaces (or placed inside a list item) is an *indented code block* whose backticks render literally and
which garbles everything after it, and content that itself contains a fence MUST be wrapped in a
**longer** run of backticks than the inner one.

Anything the person must do **after** the session — approving a step, running a command, reinstalling
the database, restarting the application, supplying a credential — MUST be the **last** section of the
reply, under the heading `## What you need to do`, as a short numbered list in the order they should do
them. When there is nothing, the reply MUST end with the single line `Nothing needed from you.` An
action MUST NOT be buried in the middle of a reply.

Rationale: the chat transcript is how this repo's work is reviewed, audited and resumed. A garbled or
fence-broken reply hides the decisions and the open work, and an action buried mid-reply is an action
that does not happen. The working detail lives in
`.github/instructions/response-format.instructions.md`.

## Additional Constraints

### Security & Secrets

Credentials, connection strings, API keys, and shared tokens MUST NOT be hardcoded, committed, or
logged. Configuration MUST come from `appsettings.json`/environment variables, and service credentials
MUST be stored DPAPI-protected (`ProtectedData`, `CurrentUser` scope) and never displayed in full. A
destructive operation (database restore, bulk delete) MUST require explicit confirmation.

### External Integration & Cache Boundaries

External read-only systems (Infor Visual) MUST never be written to. Any cached copy of external data
MUST live in its own dedicated store, MUST be treated as a fallback only (never authoritative for
internal data), and MUST be updated as complete snapshots (atomic swap) so readers never observe
partial data. Local AI features MUST remain privacy-first and local-only: no external inference
endpoint MAY be called.

### Documentation & Extensibility

Changes that add or retire modules, data stores, or integration patterns MUST update the affected
documentation (`WeekendProject/*`, `README.md`, `CHANGELOG.md`) in the same change, including removing
stale references. New extensibility patterns MUST ship with a documented, ordered playbook so the next
contributor can extend the system without redesigning it. This constraint is applied at the Review step
of Development Workflow & Quality Gates.

## Development Workflow & Quality Gates

- **Spec Kit core sequence:** `constitution` → `specify` → `clarify` → `plan` → `tasks` → `analyze` →
  `implement`. Features live under `specs/NNN-<short-name>/`; `.specify/feature.json` is the active
  feature pointer; specs are technology-agnostic (design goes in `plan.md`).
- **Checklist execution:** tasks are worked persona-by-persona (Database/Backend/Frontend/Full Stack/
  QA/Security/DevOps/Tech Lead Engineer), and a task is ticked only with proof (build/test/verification).
- **Build & test commands:** as specified in Principle VI; run the test project (not the app) for
  `dotnet test`; never create throwaway projects under the solution while sln tooling is active.
- **Review:** every PR/review MUST verify compliance with these principles; any added complexity MUST
  be justified (see Complexity Tracking in `plan.md`); a deviation MUST be recorded in `plan.md` →
  Complexity Tracking with its rationale before the change is merged.
- **Structure:** the WinUI app is the composition root (Views/XAML/DI/navigation); testable non-view
  code lives in per-module class libraries (`MTM_Waitlist.*`) that the test project references directly.

## Governance

This constitution supersedes other conflicting practices; where it is silent, the repo instruction
files under `.github/instructions/` and `.github/copilot-instructions.md` govern. Amendment is made
only via `/speckit.constitution`, which MUST propagate changes to dependent templates and artifacts and
emit a Sync Impact Report. Versioning is semantic: **MAJOR** for backward-incompatible principle
removals or redefinitions, **MINOR** for a new principle or materially expanded guidance, **PATCH** for
clarifications and non-semantic refinements. Dates are recorded in ISO `YYYY-MM-DD`. Compliance is
verified by the build gate (Principle VI) and by the `checklist-execution` skill before a task is
ticked; unjustified violations block completion.
Runtime development guidance remains in the repo instruction files, which MUST stay consistent with
this constitution.

**Version**: 1.2.1 | **Ratified**: 2026-09-09 | **Last Amended**: 2026-09-27
