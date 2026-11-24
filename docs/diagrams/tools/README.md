# Diagram tooling

Generator and build scripts behind the workflow diagrams in [docs/diagrams](../). Edit a JSON
spec, not a `.dot` or a PDF: both are build output.

## Files

| File | What it is |
| --- | --- |
| `spec-*.json` | The source of truth. One file per document; one entry per sheet: title, source-of-truth note, lane order, lane titles, nodes (`[id, label, kind]`) and edges (`[from, to, label, style]`). |
| `manifest.json` | Which spec builds which PDF, in what order, and which workflow and variant each document belongs to. The workflow also names the `dot/` and `pdf/` subfolder a run writes to. | Sheet numbering (`Sheet X of Y`) is computed from spec order, so it cannot go stale. |
| `diagram_context.py` | **Read-only.** Prints a workflow's sheets, nodes, edges, findings and connectors as a digest for an agent; checks the `.dot` files against the spec and reports missing sources. |
| `test_diagram_context.py` | Tests for the digest: the real diagram set, plus fixtures for a missing spec, `.dot` or PDF, an out-of-range sheet reference and a duplicate manifest entry. |
| `dotgen.py` | Spec → `.dot` in `docs/diagrams/dot/<workflow>/`. Valid Graphviz, one sheet per file, for editing or a quick single-page look. |
| `render_sheets.py` | Spec → the print-ready PDFs in `docs/diagrams/pdf/<workflow>/`. Draws each sheet on an explicit swimlane grid rather than handing layout to Graphviz, because the print contract cannot be met by automatic layout. |
| `verify_pdfs.py` | Asserts the print contract on every page and writes 300 DPI PNGs for a visual check. |
| `dotgen.py` node check | Warns when a spec draws a node no edge touches, so a box cannot be left standing alone by accident. |

## Node kinds

`ui`, `vm`, `svc`, `db` (procedure), `erp` (Infor Visual), `mirror` (cache), `dec` (decision),
`note` (plain callout), `refactor` (dashed red finding), `conn` (off-page connector).

## Labels

Write explicit line breaks as `\n` in the JSON (`"Reads the press\nlist"`). The renderer wraps by
measurement, so a long stored-procedure name still fits, but a deliberate break keeps a box short.
Keep lines under about 30 characters.

## Build

```powershell
python docs\diagrams\tools\dotgen.py docs\diagrams\tools\spec-diagram-workorder-new-request.json
python docs\diagrams\tools\render_sheets.py docs\diagrams\tools\manifest.json
python docs\diagrams\tools\verify_pdfs.py
```

The build has three traps worth knowing before the first run:

- **The manifest path is the only argument `render_sheets.py` takes.** It has no argument parser, so
  `--help` is read as a file name. Read this page instead.
- **Each `spec` is resolved relative to the manifest's own folder.** A subset manifest written somewhere
  else must carry absolute `spec` paths, or `render_sheets.py` looks for the spec under its own folder and
  fails. That is the supported way to rebuild one workflow while other specs are absent from the checkout.
- **`verify_pdfs.py` checks the page, not the sheet's shape count.** Page size, margin and type size are
  asserted; 12–16 shapes per sheet is not. Count nodes in the spec after adding a finding, and fold related
  findings into one flag when a sheet is at 16.

## Consume

```powershell
python docs\diagrams\tools\diagram_context.py --list
python docs\diagrams\tools\diagram_context.py --workflow workorder-new-request --sheet 3
python docs\diagrams\tools\diagram_context.py --workflow workorder-new-request --inspect-pdf --png
```

Read-only: it reads the spec, checks the `.dot` against it, and reports a missing source rather than
implying it was read. `--format json` for structured output, `--strict` to exit non-zero on a warning,
and `--spec/--dot/--pdf-file` for a diagram set outside this folder.

## Check a spec against the code

`scripts\docs\generate-docs.ps1` parses the C# with the Roslyn parser PowerShell already carries
(no packages, no compilation) and reports where a spec and the code disagree:

```powershell
pwsh scripts\docs\generate-docs.ps1 -Mode Gaps -Workflow startup-launch
```

It writes `code-facts.json` (types, constructor/field/creation/call/DI-resolution edges, `sp_*`
references, with file and line) and `diagram-gaps.md`, which names the JSON location to edit for each
orphan node, label the code does not hold, and edge the code proves but the spec omits. It never edits
a spec: the wording of a label, a note or a lane choice stays an authoring decision.

## Test

```powershell
python -m unittest discover -s docs\diagrams\tools -p "test_*.py"
```

`dotgen.py` accepts any number of spec files. `verify_pdfs.py` writes PNGs to
`%TEMP%\mtm-diagrams\png`; pass `--no-png` to skip that.

Build output is split by type: `dotgen.py` writes `docs/diagrams/dot/<workflow>/*.dot` and `render_sheets.py`
writes `docs/diagrams/pdf/<workflow>/*.pdf`, where the workflow is the one declared at the top of the spec. Neither folder is edited by hand; recreate it by running the
scripts.

Requires PyMuPDF (`pip install pymupdf`), the Segoe UI face (Windows), and — only if you want to
look at a single `.dot` — Graphviz at `%ProgramFiles%\Graphviz\bin\dot.exe`. Note that
`dot -Tpdf` on its own produces a single-page automatic layout, not the paged print output; it is
for inspection, not for the deliverable.

## Print contract enforced by `verify_pdfs.py`

- 792 × 612 pt (11 in × 8.5 in landscape), 0.5 in clear margin on all four sides.
- No text below 9.5 pt; node labels render at 11 pt whenever a sheet allows it.
- Title header, `Sheet X of Y` marker and the shape legend on every page.
- 12–16 shapes per sheet. A flow too large for one sheet is split at an architectural seam
  (UI and validation, then services and stores) and joined with `conn` off-page badges.

The first three are asserted by the script. The fourth is an authoring rule it does not check: count the
nodes in the spec, and fold a second finding into an existing `refactor` node when the sheet is already at
16.
