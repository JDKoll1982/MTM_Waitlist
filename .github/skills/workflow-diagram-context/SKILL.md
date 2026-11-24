---
name: workflow-diagram-context
description: Use an existing workflow diagram as implementation context when planning or implementing a UI/UX quality-of-life change. Covers the work order, generic request and application startup flows in docs/diagrams. Use when a request names one of those workflows or a screen in one, asks to improve a step, or needs the current navigation and validation explained before changing it.
---

# Workflow diagram context

`docs/diagrams/` maps five workflows, each in an engineer version and a plain-language version. The
engineer version names the views, view models, commands, services and stored procedures behind each
step. Use it to understand the current behaviour of a workflow before proposing or making a UI/UX
change to it.

A diagram records **current behaviour**. It is a reference for the change, not permission to change the
behaviour, and not a specification of what should happen next.

## 1. Designate the workflow

Never guess which diagrams apply. If the request does not name a workflow, list them and ask.

```powershell
python docs\diagrams\tools\diagram_context.py --list
```

| Workflow | Covers |
| --- | --- |
| `startup-launch` | turning the application on: the launch steps, this computer, sign-in, set-up, and the hand-over to the first screen |

`--list` is the authority, not this table: a flow added since this file was written appears there first.
A flow that is genuinely unmapped is recorded by the design's `flow_source` (see the `qol-ui-design`
skill) rather than left unmentioned.

Print the engineer version by default. Add `--variant friendly` when the audience is the person using
the screen, not a developer.

## 2. Read the digest

```powershell
python docs\diagrams\tools\diagram_context.py --workflow <name>
python docs\diagrams\tools\diagram_context.py --workflow <name> --sheet 3
python docs\diagrams\tools\diagram_context.py --workflow <name> --inspect-pdf --png
python docs\diagrams\tools\diagram_context.py --workflow <name> --format json --strict
```

The digest prints, per sheet: lane titles, every node with its id, kind and label, every edge with its
label and style, the `[REFACTOR]` findings, and the cross-sheet connectors with their pairing status.
`--inspect-pdf` adds the PDF's text per page; `--png` writes page images to `%TEMP%\mtm-diagrams\context`
for a visual look. `--strict` exits non-zero when a source is missing or a `.dot` file disagrees with
the spec.

Work through **every** sheet of the workflow, not only the first. Sheets join on connector text, not on
page order: follow the `[To Sheet N] <command>` badge to the sheet that carries the matching
`[From Sheet M] <command>`.

## 3. Inspect the visual layout when the change is about presentation

Open the PNG of the page you are changing. The digest gives structure; the page shows what a reader
actually sees — step order down a lane, where a finding sits, how much text a card holds. Report layout
problems you can see but the structure does not explain.

## 4. Correlate the diagram with the code

Each sheet names its source in the `Spec notes:` line — view models, views, services, stored
procedures. Read those files before proposing anything. Then confirm:

- the view and its page-level XAML, in `Module_*/Views/`;
- the view model and the command the node names (`[RelayCommand]` strips a trailing `Async`);
- the service and stored procedure behind any data node;
- whether an external read goes through the visual read fallback rather than the query executor.

The diagram is a map, not proof. If the code and the diagram disagree, report the discrepancy and say
which one you believe, with the file and the sheet that support it. Never silently pick one.

## 5. Scope the change

State four things before touching code:

1. **Existing behaviour** — what the cited sheets and the code do today.
2. **Documented behaviour** — what the diagram claims, if it differs from the code.
3. **Proposed behaviour** — the smallest change that delivers the QOL improvement.
4. **Unverified assumptions** — anything neither the diagram nor the code settles.

Then walk forward through the workflow: which later sheets and steps consume the thing being changed,
which shared services or procedures are involved, and whether the other variant or the other request
flow reuses it. A change to a shared step is not scoped to one workflow; say so.

Keep to the designated workflow unless shared functionality forces a wider change. Never change a
business rule because the UI would be simpler that way — raise it instead.

## 6. Present, then implement

Show the proposal before editing: the change, the sheets and nodes it touches, the code it touches, the
downstream steps it affects, and the risks. Wait for approval for anything beyond a trivial, clearly
requested edit.

If the change is visible on screen, the diagram is input, not the deliverable: produce the rendered
BEFORE and AFTER designs with the `qol-ui-design` skill first, and let the approved design be the
implementation reference.

Keep the traceability rows below in the proposal and in the summary — one row per element the change
touches.

| Workflow | Sheet (page) | Node / edge | Code | Requirement |
| --- | --- | --- | --- | --- |
| `workorder-new-request` | 1 of 5 (p1) | `v_wc` `SelectWorkCenterCommand` | `NewRequestWorkCenterViewModel` | FR-… |

## 7. Verify

- Re-read the touched step end to end: entry point → the modified interaction → the next sheet.
- Confirm every business rule the sheets name is still enforced.
- Run the build and the relevant tests for the changed code.
- If a diagram must change to match, follow `.github/instructions/diagram-authoring.instructions.md`:
  edit the JSON spec, then rebuild and run `verify_pdfs.py`. Never hand-edit a `.dot` or a PDF.

## Rules

- The JSON spec in `docs/diagrams/tools/` is the source of truth. `.dot` files are generated from it;
  PDFs are generated from both.
- Never edit a `.dot` file or a PDF. Never treat a PDF as the editable source.
- Never claim to have read a source you could not read. If only a PDF exists, say the graph structure
  is unavailable; if a PDF is missing, say there is no visual reference.
- A `[REFACTOR]` node is a recorded finding, not a finished requirement. Check
  `docs/diagrams/diagram-workflow-refactor-notes.md` for the class and method behind it. The startup
  flow's findings are in §6 of that file, because the flow was mapped after the hotspot list was written.
- The digest reads the spec and checks the `.dot` against it. It reports a missing spec, `.dot` or PDF
  rather than implying it read one; when a spec is absent from the checkout, cite the PDF and say the
  structure is unavailable.
- `dotgen.py` accepts one or more spec files; `render_sheets.py` takes a **manifest** and resolves each
  `spec` relative to that manifest's own folder. To rebuild one workflow when other specs are absent,
  write a subset manifest with absolute `spec` paths — never edit another workflow out of `manifest.json`.
- `verify_pdfs.py` enforces page size, margins and type size, not the 12–16 shape ceiling on a sheet.
  Count nodes when you add a finding; at 16, fold the new defect into the existing flag.
- No demo, sample or mock mode may be introduced, and no diagram work adds `.cs`, tests or inline SQL.
