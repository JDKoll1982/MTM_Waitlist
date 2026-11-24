# Diagram tooling

Generator and build scripts behind the workflow diagrams in [docs/diagrams](../). Edit a JSON
spec, not a `.dot` or a PDF: both are build output.

## Files

| File | What it is |
| --- | --- |
| `spec-*.json` | The source of truth. One file per document; one entry per sheet: title, source-of-truth note, lane order, lane titles, nodes (`[id, label, kind]`) and edges (`[from, to, label, style]`). |
| `manifest.json` | Which spec builds which PDF, in what order. Sheet numbering (`Sheet X of Y`) is computed from spec order, so it cannot go stale. |
| `dotgen.py` | Spec → `.dot` beside each PDF in `docs/diagrams/`. Valid Graphviz, one sheet per file, for editing or a quick single-page look. |
| `render_sheets.py` | Spec → the print-ready PDFs. Draws each sheet on an explicit swimlane grid rather than handing layout to Graphviz, because the print contract cannot be met by automatic layout. |
| `verify_pdfs.py` | Asserts the print contract on every page and writes 300 DPI PNGs for a visual check. |

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

`dotgen.py` takes any number of spec files. `verify_pdfs.py` writes PNGs to
`%TEMP%\mtm-diagrams\png`; pass `--no-png` to skip that.

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
