---
name: checklist-execution
description: "Use when the user asks to work through a markdown checklist by executing each task with persona adherence. Personas include Database Engineer, Backend Engineer, Full Stack Engineer, Frontend Engineer, QA Engineer, Security Engineer, Tech Lead, DevOps Engineer."
---

# Checklist Execution

Drive task-by-task completion of a markdown checklist with persona adherence.

## Workflow

1. **Scan the checklist** and find the first unchecked task (a line starting with `- [ ]`).
2. **Extract the Persona** from the task line, e.g. `| **Persona: Database Engineer**` (or the last known persona in the document).
3. **Adopt the persona's working style** — see Persona Styles below.
4. **Execute the task** against the codebase, following the repo conventions in `.github/copilot-instructions.md` and the relevant `.github/instructions/*.instructions.md` files.
5. **Verify** — compile (`dotnet build`) or run the relevant tests; only mark a task complete when it actually works.
6. **Update the checkbox** to `- [x]`, keep any `*Depends on:*` / proof notes in the document, and move to the next unchecked task.
7. Repeat until no `- [ ]` tasks remain, then summarize everything completed.

## Persona Styles

- **Database Engineer**: focus on `Database/` SQL artifacts (tables, stored procedures, views, seeds). Follow `database-schema-rules.instructions.md`; keep `create.sql`/`rollback.sql` plus aggregate files (`AllTables.sql`, `AllSPs.sql`, `AllViews.sql`, `AllSeeds.sql`) in sync.
- **Backend Engineer**: C# services, contracts, DI registration, and stored-procedure wiring.
- **Full Stack Engineer**: end-to-end across database + backend + frontend.
- **Frontend Engineer**: XAML views and view models. Follow the WinUI 3 rules in `.github/instructions/winui3-api-rules.instructions.md` (e.g., register new resources in `App.xaml`, keep layout Fluent).
- **QA Engineer**: test design and verification — unit/integration coverage, edge cases, regression checks, and clear reproduction steps. Completion requires passing evidence.
- **Security Engineer**: secrets handling, credential storage, least privilege, input validation, and safe defaults for destructive operations.
- **Tech Lead**: architecture decisions, design-decision records (DDRs), checklists, and compliance documents.
- **DevOps Engineer**: build/test pipelines under `.github/workflows/`, packaging, deployment preflight, and environment/tooling reliability.

## Rules

- One task at a time; keep each step small and verifiable.
- If a step fails, fix it before moving on. State what completed so work can resume later.
- If a task references another file (spec, DDR, or related doc), read it before implementing.
- When requirements are ambiguous, ask a small number of clarifying questions before implementing.
