---
name: checklist-creation
description: "Use when the user asks to create, author, expand, or validate a markdown checklist (e.g., build a new checklist.md, convert a spec or DDR into an implementation checklist, add tasks or subphases, or verify an existing checklist is execution-ready). Produces checklists that the checklist-execution skill can consume accurately."
---

# Checklist Creation

Author or repair markdown checklists that the `checklist-execution` skill can consume accurately. A checklist is "accurate" when every task line is unambiguous, verifiable, and tagged with a canonical persona the execution skill can extract.

## Workflow

1. **Identify the source of truth.** Read the spec, DDR, feature request, or design doc the checklist must reflect. Never invent scope. Cite the source sections with `(Ref: Section X.X)` on each task.
2. **Decompose into Phases → Subphases → Tasks.**
   - `## Phase N: <Name>` — a coarse milestone with a verifiable end state.
   - `### Subphase N.M: <Name>` — a logical group of tasks owned by one or two personas.
   - Tasks are single bullet lines starting with `- [ ]` (unchecked) or `- [x]` (checked).
3. **Assign each task exactly one canonical persona.** Use only these names (they match the extraction in `checklist-execution` and the repo's Persona Behavior Matrix):
   `Database Engineer`, `Backend Engineer`, `Full Stack Engineer`, `Frontend Engineer`, `QA Engineer`, `Security Engineer`, `Tech Lead`, `DevOps Engineer`.
4. **Write atomic, verifiable tasks.** One deliverable per line, imperative form: `<Category>: <action> <artifact>`. If a task cannot be verified (build / test / read), split it or reword it.
5. **Mark already-complete work accurately.** Use `- [x]` only for tasks verified against the codebase. Append a short proof note after the persona: `— <what/where/when>`.
6. **Add gates and dependencies.**
   - Between phases: `**GATE: <condition> before proceeding to <next phase>. <why>.**`
   - On task lines: `*Depends on: <task>*` / `*Blocks: <task>*`.
   - Under a subphase when needed: `**PREREQUISITE: <condition>**`.
7. **Add a "Next task" pointer** after the last unchecked line of a group: `Next task: **<first unchecked task>** | **Persona: <X>**` so execution resumes cleanly.
8. **Validate before handing off.** Run the Accuracy Checks below and fix failures.

## Task Line Format (exact)

```
- [ ] **<Category>: <action> <artifact>.** (Ref: Section X.X) *Depends on: <task>* | **Persona: <Name>**
- [x] **<Category>: <action> <artifact>.** (Ref: Section X.X) | **Persona: <Name>** — verified 2026-08-22: <file/proof>
```

Rules:
- The persona tag is the LAST element: `| **Persona: <Name>**`. Nothing follows it on the line except a ` — proof` note.
- Checkbox is always `- [ ]` or `- [x]` followed by a space.
- Category prefixes seen in this repo: `Database Table`, `Database Migration`, `JSON Schema`, `Data Model`, `Service Layer`, `Configuration`, `Auth Logic`, `Settings Page`, `Settings Card`, `Dialog Row`, `Dialog Footer`, `Dialog Behavior`, `Request Type Card`, `Work Center Card`, `Workflow`, `Security Review`, `Testing`, `CI/CD`. Use or extend them consistently.
- Refs must map to real headings/sections of the source doc.
- Descriptions must be specific enough that a reviewer can tell done from not-done (name the file, table, stored procedure, or control).

## Accuracy Checks (run before finishing)

1. **Persona validity** — every `- [ ]` / `- [x]` line ends with `| **Persona: <one of the 8 canonical names>**`.
2. **No untagged tasks** — no task line is missing the `| **Persona:` suffix.
3. **Checkbox syntax** — every task starts with exactly `- [ ] ` or `- [x] `.
4. **Refs resolve** — every `(Ref: Section N.N)` maps to a real heading in the source doc.
5. **No duplicates** — no two task lines describe the same deliverable.
6. **Dependencies exist** — every `*Depends on:*` / `*Blocks:*` references a real task (by name or clear description).
7. **Next task matches** — each `Next task:` pointer names an actual `- [ ]` task that still exists.
8. **Gates placed** — a `GATE` line separates each phase boundary; gates are binary/verifiable.
9. **Atomicity** — no task bundles multiple unrelated deliverables (e.g., "create table and update JSON" is two tasks).
10. **Checked = verified** — every `- [x]` corresponds to work actually present in the codebase; add a proof note.

## Authoring vs. Repairing
- **Authoring new**: follow the workflow top-down.
- **Repairing existing** (broken persona tags, missing refs, stale `- [x]`): scan with the Accuracy Checks first, fix violations, then hand off. Do not silently mark tasks complete — verify against the codebase first.

## Hand-off
When the checklist is accurate, tell the user it is ready for `checklist-execution` and (optionally) state the first `- [ ]` task with its persona.
