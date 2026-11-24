---
name: workflow-to-spec
description: Run Spec Kit on a workflow — either a diagram-mapped flow whose findings are recorded, or an image or text design proposal under docs/ui-designs. Use when asked to "run speckit on <workflow>", "specify the startup findings", "turn the diagram findings into a feature", or to create a spec from a named flow. Asks which workflow, with a list, when none is named.
---

# Workflow to spec

This skill turns what a workflow already records into a Spec Kit feature. It does not invent
requirements, and it does not change the diagram, the design or the code: the input is evidence, and the
output is `specs/NNN-<slug>/spec.md`.

A workflow reaches this skill from one of two places, and both are read the same way:

| Source | Where it lives | What it contributes |
| --- | --- | --- |
| A mapped flow's findings | `docs/diagrams/diagram-workflow-refactor-notes.md` (and the `[REFACTOR]` flags its digest prints) | defects, their files and procedures, the sheet and node that show them |
| A design proposal | `docs/ui-designs/<workflow>/<screen>/<refactor>/` — `<refactor>-proposal.md`, `<refactor>-comparison.pdf`, `<refactor>-text-change.md` | the current state, the proposed change, the decisions behind it |

## 1. Resolve the workflow — ask when it was not named

If the request names a workflow, use it and skip this step. If it does not, build the list from what is
actually present, then ask with `ask_user` — one question, the real options, and no invented ones.

```powershell
# mapped flows, and whether each has findings
python docs\diagrams\tools\diagram_context.py --list
python docs\diagrams\tools\diagram_context.py --workflow <name> --format md --strict

# design workflows that already hold a proposal or a text-change record
Get-ChildItem docs\ui-designs -Directory | Where-Object { $_.Name -ne 'tools' }
Get-ChildItem docs\ui-designs -Recurse -Include *-proposal.md,*-text-change.md
```

Offer only sources that exist **and carry something to specify**: a flow with at least one `[REFACTOR]`
finding, or a design folder with a proposal. Label each option with what the reader will get — for
example `startup-launch (diagram) — 4 findings: setup authority wording, sign-in offer read, offer scope,
set-up panel text`. Ask as a single select whose freeform answer is always available, so a flow with no
findings can still be named; mapping it is allowed, but it is a separate, larger run.

Say plainly when a source cannot be read. `--list` shows each manifest entry's spec, `.dot` and PDF
status; a flow marked `missing` has no findings to read, and the digest reports that rather than implying
it read one. This checkout currently lacks four of the five specs.

## 2. Read the source before writing anything

**Diagram findings.** Run the digest for the flow (`--format md --strict`) and read the flagged nodes. Then
open `diagram-workflow-refactor-notes.md`: §3 holds the work-order and generic hotspots, §6 holds the
startup findings (S1–S4). Each entry names the class, method or stored procedure. Confirm the finding still
exists in the code before it becomes a requirement — a finding the code has already fixed is dropped, and
said out loud.

**A design proposal.** Read `<refactor>-proposal.md` (sections A–E) and look at `<refactor>-comparison.pdf`.
Section A is the current state, B the proposed change, E the impact and the risks; the callouts are the
change list. The design record's `elements[]` names the control and the file each part maps to.

**A text-change record.** Read `<refactor>-text-change.md`; A and B give the exact values, C says why no
picture was needed, E gives the keys and their files.

## 3. Scope one feature, and say what was left out

- **One feature per run.** When a workflow holds several findings, group the ones that share a cause or a
  screen; if the grouping is not obvious, ask which to take first rather than bundling everything.
- **Every requirement cites its evidence** — a sheet and node, a proposal section, or a notes section. An
  FR with no citation is an invention.
- **State the non-goals** the source already implies: another screen's finding, a control the design does
  not move, a rule the note says is out of scope.
- **Never spec an unapproved design as agreed work.** If the record has no `approval.approved_at`, say so
  and ask whether to proceed to a draft spec or wait for review.

## 4. Run Spec Kit

Build one description from the material above: the problem in the person's terms, the evidence with its
citations, the functional requirements, the non-goals, and the observable success criteria. Then run the
specify command, following `.github/agents/speckit.specify.agent.md` with that description as
`$ARGUMENTS`.

Run it **in this context** — the findings are already read here, and handing them to a fresh agent just to
read them again loses the detail. If the harness offers a `speckit.specify` agent, delegating is
acceptable once the description is written; the repository's subagent model rule applies if you do.

The agent file owns the mechanics — it derives the `specs/` directory (sequential `NNN` per
`.specify/init-options.json`), writes `spec.md` and the requirements checklist, and repoints
`.specify/feature.json`. Do not hand-create the folder or the pointer. Two rules from this repository still
apply to anything that runs:

- Use `-ExecutionPolicy Bypass` on any spec-kit script the command calls; this machine's policy is
  `Undefined` everywhere and the default blocks scripts.
- Use `py` or `python`, never `python3`, which is a Store alias here that answers "Python was not found".

Keep the spec technology-agnostic: no class names, no XAML, no procedure names in `spec.md` — those belong
in the description you handed over and in `plan.md`. Honour `.specify/extensions.yml` hooks exactly as the
agent file says, including a hook that declares a condition (leave it to the HookExecutor).

After the spec exists, **ask** whether to continue — `speckit.plan`, then `speckit.tasks`, then
`speckit.analyze` — and run only what was chosen. Do not start the plan unasked; it is a different
decision from writing the spec.

## 5. Report

Plain English, end-user facing, per the repository's response format: what the feature is, which evidence
it came from (sheet and node, or proposal section), the files created, and what is still open. Never paste
the spec body into chat.

## Do not

- Change the diagram spec, the design record, the mockups or any `.cs`/`.xaml` file. This skill produces a
  specification and nothing else.
- Re-word a finding into something stronger than the evidence supports, or merge two findings into one
  requirement to make the list shorter.
- Cite a sheet, node or proposal section the digest could not read.
- Skip the requirements checklist the specify command writes, or tick a task before it is implemented.

## Pitfalls

- **A partial checkout has fewer diagrams than the manifest.** `--list` is the authority for what can be
  read; a workflow listed but missing its spec has no findings, and the digest says so with a warning that
  `--strict` turns into a non-zero exit.
- **`--strict` exits non-zero on any warning**, including a `.dot` that disagrees with its spec. That is a
  reason to report the disagreement, not to skip the workflow.
- **The findings are split across two sections of the notes file** — §3 for the work-order and generic
  flows, §6 for startup. A grep for `[REFACTOR]` in the spec only finds what is drawn; the notes hold the
  detail behind each flag.
- **A flag may carry more than one defect** (the startup offer flag carries three). Split them into
  separate requirements; do not make one requirement with an "and" chain.
- **A design's proposal is not hashed**, so it can drift from its approved images. When the two disagree,
  report both and spec from the proposal only after the reviewer confirms which is current.
