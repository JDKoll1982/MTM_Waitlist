---
applyTo: "**/*"
---

# Spec-Driven Development (GitHub Spec Kit)

This repo has Spec Kit initialized (`specify init .`) — version `0.6.2.dev0`, integration `copilot`,
scripts `powershell`, `branch_numbering: sequential` (see `.specify/init-options.json`).

## When to use
Use Spec Kit for any non-trivial new feature or refactor: produce a **spec first**, then a plan, then tasks,
then implement — in that order. Do not jump straight to code for work that warrants a specification.
(Spec Kit is the spec-first layer on top of the repo's existing checklist workflow; it does not replace it.)

## Command ↔ agent map
Each command exists as a prompt (`.github/prompts/speckit.<cmd>.prompt.md`) delegating to an agent
(`.github/agents/speckit.<cmd>.agent.md`).

- Core sequence: `constitution` → `specify` → `clarify` → `plan` → `tasks` → `analyze` → `implement`.
- Supporting: `checklist`, `taskstoissues`.
- Git helpers: `git.initialize`, `git.feature`, `git.validate`, `git.remote`, `git.commit`.

Run the core sequence in order; `specify` must precede `plan`, which must precede `tasks`.

## Directory conventions
- Features live under `specs/NNN-<short-name>/` (sequential `NNN`). `spec.md` is mandatory; later phases add
  `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`, and `checklists/`.
- Active feature pointer: `.specify/feature.json` (`feature_directory`). Downstream commands (`plan`/`tasks`/
  `implement`) resolve the feature from this file, **not** from the git branch name — keep it current.
- Project constitution: `.specify/memory/constitution.md`. Templates: `.specify/templates/*`.
- Scripts (Windows/PowerShell): `.specify/scripts/powershell/{create-new-feature,setup-plan,check-prerequisites,
  update-agent-context}.ps1`; Copilot integration: `.specify/integrations/copilot/scripts/update-context.ps1`.

## Rules
- **Specs are technology-agnostic** (WHAT/WHY, user value, acceptance scenarios, success criteria). No
  implementation details in `spec.md`; put the technical design in `plan.md` and the work items in `tasks.md`.
- **One feature per `/speckit.specify`** invocation.
- Use the **PowerShell** scripts (this repo has no bash); prefix invocations with `pwsh`/`powershell` when run
  from a shell.
- `tasks.md` is an **executable checklist**: mark `- [x]` only when the work is implemented and verified —
  mirror the `.github/skills/checklist-execution` rule (never tick unverified tasks).
- The project constitution (`.specify/memory/constitution.md`) is **ratified at v1.0.0 (2026-09-09)**
  and governs this repo. Amend it only via `/speckit.constitution`, which must propagate to dependent
  artifacts and emit a Sync Impact Report.
- Do **not** duplicate existing repo checklists: `WeekendProject/PromptFiles/*` and
  `.github/skills/checklist-creation` / `checklist-execution` remain authoritative for their scopes. Spec Kit
  adds the spec-first layer for new features and can reference those checklists.
- **First feature seed:** the Module_Mock workstream docs — `WeekendProject/Module_Mock/Spec.md`,
  `Plan.md`, `Tasks.md`, and `Discovery/01..03` — are the input for the first `/speckit.specify` run.

## Extension hooks
`.specify/extensions.yml` may declare `before_*`/`after_*` hooks per command. Honor them: skip entries with
`enabled: false`, treat missing `enabled` as enabled, and leave hooks that declare a non-empty `condition` for
the HookExecutor rather than evaluating it yourself.
