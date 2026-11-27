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
Each command's instructions live in an agent file (`.github/agents/speckit.<cmd>.agent.md`), invoked by the
matching skill (`.github/skills/speckit-<cmd>/SKILL.md`, explicit-invocation only). The former
`.github/prompts/*.prompt.md` files were removed because agent-host harnesses do not load prompt files.
Skills that still hold only frontmatter are unconverted skeletons; their agent file is the source of truth.

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
  resolve-template,setup-tasks,update-agent-context}.ps1`; Copilot integration: `.specify/integrations/copilot.manifest.json`
  lists the files it owns, and `.github/hooks/speckit.json` with `.specify/events.py` are what it installs.

## Rules
- **Replies follow Principle VII.** Every `/speckit.*` step's chat reply is plain English and
  end-user facing: what changed and what is still open. Never paste raw tool output, JSON, diffs, file
  dumps or whole artifacts, and never print a code block to show a change you made with a tool — see
  `.github/instructions/response-format.instructions.md`. Anything the person must do after the session is
  the **last** section of the reply, under `## What you need to do`; if nothing, end with
  `Nothing needed from you.`
- **Run the spec-kit scripts with a real interpreter.** Every `.specify/scripts/powershell/*.ps1`
  invocation needs `-ExecutionPolicy Bypass`, and the Companion extension's `write-context.py` / `doctor.py`
  calls should use `python` or `py`. `python3` used to resolve to a 0-byte Microsoft Store
  alias on this workstation that answered "Python was not found" and silently dropped the run journal;
  it now resolves to a real interpreter (3.13.15, verified 2026-11-26), but that is a workstation fact.
  Re-check `python3 --version` before relying on it rather than assuming either state.
- **Specs are technology-agnostic** (WHAT/WHY, user value, acceptance scenarios, success criteria). No
  implementation details in `spec.md`; put the technical design in `plan.md` and the work items in `tasks.md`.
- **One feature per `/speckit.specify`** invocation.
- Use the **PowerShell** scripts (this repo has no bash); prefix invocations with `pwsh`/`powershell` when run
  from a shell, and pass **`-ExecutionPolicy Bypass`**: this machine reports `Undefined` at every execution-policy
  scope, so the effective policy is Windows' `Restricted` default and the scripts otherwise fail with
  "running scripts is disabled on this system" before any logic runs.
- `tasks.md` is an **executable checklist**: mark `- [x]` only when the work is implemented and verified —
  mirror the `.github/skills/checklist-execution` rule (never tick unverified tasks).
- The project constitution (`.specify/memory/constitution.md`) is **ratified at v2.0.0 (last amended 2026-10-17)**
  and governs this repo. Amend it only via `/speckit.constitution`, which must propagate to dependent
  artifacts and emit a Sync Impact Report.
- Do **not** duplicate existing repo checklists: `.github/skills/checklist-creation` / `checklist-execution`
  remain authoritative for their scopes. Spec Kit adds the spec-first layer for new features and can
  reference those checklists.
- **First feature seed:** `specs/001-module-mock-visual-fallback/` — its `spec.md`, `plan.md` and `tasks.md` —
  is the first `/speckit.specify` input this repo actually used, and remains the worked example to copy.

## Extension hooks
`.specify/extensions.yml` may declare `before_*`/`after_*` hooks per command. Honor them: skip entries with
`enabled: false`, treat missing `enabled` as enabled, and leave hooks that declare a non-empty `condition` for
the HookExecutor rather than evaluating it yourself.

**Wireframe sign-off.** The `wireframe` extension is wired in three places, and no two of them ask twice:

- `.specify/extensions.yml` — the `after_specify` / `before_plan` / `after_implement` offers for bare
  `/speckit.*` runs.
- `.specify/companion.yml` — node hooks at `specify.finalize`, `plan.plan-doc`, `implement.complete` for
  `/speckit.companion.*` runs and for the Companion's Workflow Builder board.
- `.specify/workflows/{speckit,speckit-companion}/workflow.yml` — optional `prompt` steps
  (`wireframes`, `wireframe-check`) with `continue_on_error: true`, so a declined mockup never fails a run.

Every one of them is state-aware: it reads `.specify/feature.json`, the feature's `wireframes/` directory and
the `## UI Mockup` block in `spec.md` before it offers anything, and says so in one line when there is nothing
to do. Keep that property when editing them — it is what makes the three layers idempotent.
