# Workflow diagrams — how the set is built and what it is for (2026-11-24)

## What exists

`docs/diagrams/` holds four end-to-end workflow maps, each in an engineer version and a plain-language
version, plus one refactor note:

| Document | Sheets | Subject |
| --- | --- | --- |
| `diagram-workorder-new-request.pdf` | 5 | Work order request, entry through submit and result |
| `diagram-workorder-workcenter-setup.pdf` | 5 | Work-center setup, selection through save |
| `diagram-generic-new-request.pdf` | 4 | Generic request, work center through result |
| `diagram-generic-workcenter-setup.pdf` | 2 | The configuration paths a generic request depends on |
| `diagram-*-friendly.pdf` (4) | 2 each | The same four flows in plain language, role-free wording |
| `diagram-workflow-refactor-notes.md` | — | Comparison matrix, duplication hotspots, Spec Kit inputs |

Both variants come from one spec each, so the friendly sheet is a *translation*, not a second diagram.
Findings carry a `[REFACTOR]` tag in the engineer version and a "Worth fixing:" callout in the friendly
one. There are 18 engineer flags and 8 plain-language flags — several are the same finding drawn on more
than one sheet — consolidated into the 21 hotspots (H1–H7, M1–M10, L1–L4) in the refactor notes. Every
flag maps to an entry there, but not every entry has a flag: H4, H6, M1 and L2 came from reading the code
and are not visible on any single screen.

## Why a custom renderer instead of Graphviz alone

The print contract — exact 792 × 612 pt, 0.5 in clear margin, a header and legend on every page,
12–16 shapes, and a hard 9.5 pt type floor — is not expressible as Graphviz constraints. Both layouts
that were tried through `dot` failed on this material:

- **Lane-column layout** (`rank=same` per lane, invisible chain down each lane) fits the page, but
  `mincross` reorders rows by the barycentre of cross-lane edges, so the vertical reading order the spec
  author wrote is scrambled. Adding `weight=100` to the invisible chains did not change it.
- **Row-grid layout** (one rank per row) is deterministic but inflates both axes: column widths follow the
  longest label in each lane, and the sheet overflows the live area at a readable type size.

So `tools/render_sheets.py` draws the swimlane grid itself: lanes are columns, array index is the row.
Labels are wrapped by measured width (`fitz.Font.text_length` against the embedded Segoe UI face), the
type size is chosen per sheet from 11 pt down to 9.5 pt and never below it, and arrows are elbows routed
through the column gutter. The `.dot` files are still emitted — they are the canonical graph description
and are useful for inspecting one sheet — but they are not what produces the PDF.

`dot` needs `-Gmargin=0`, otherwise it reserves its own 0.5 in page margin on top of the content.

## Page geometry

```
page   792 x 612 pt        margin 36 pt (0.5 in)
header rule y = 49         title 9.8 pt bold at (36, 46.5), "Sheet X of Y" 9.5 pt right-aligned to 756
live   Rect(36, 51, 756, 557)
legend rule y = 555.5      9 slots of 80 pt; glyph 11 x 10 at slot + 3, label 9.5 pt at slot + 17
lanes  title 9.6 pt bold at y0 - 10, dashed frame (0.9 pt, [3 3]) inset 6 pt around the band
rows   gap starts at max(14, size * 1.7) and is stretched by up to 60 pt to fill the live area
```

Body type is 11 pt with `line_h = size * 1.26`, box padding `7 x 4`, node border 1.5 pt, edge stroke
1.2 pt (2.0 pt for the red write path), arrow head arms 6.5 pt at ±0.42 rad.

## Two bugs worth not repeating

1. **A manifest that duplicated sheet content went stale.** The first build kept full sheet data in
   `manifest.json` alongside the specs, so edits to a spec silently rendered from the old copy — a
   regenerated page still showed a node that had been deleted 40 minutes earlier. `manifest.json` now
   maps only `output → spec`; `Sheet X of Y` is computed at build time from spec order. There is exactly
   one place a sheet is described.
2. **Edge labels landed on top of node text.** Label collision avoidance only checked other labels, so a
   short annotation beside a tall node was drawn across its third line. `_label` now tests the node rects
   too (`self.boxes`) and pushes the label out through a wider nudge sequence (0, ±11, ±22, ±33, ±44).

## Verifying fidelity, not just size

`verify_pdfs.py` asserts the contract, but it cannot tell whether the drawing still says what it said.
When the generator was reconstructed, the check was span-level: extract every text span's position from
the old and new PDF and match by text. 1,325 spans, zero unmatched, worst positional delta 0.4 pt. Use
the same method after any change to `render_sheets.py` — a silent 20 pt shift moves boxes under arrows
without changing a single label.

## Dependencies

PyMuPDF (`import pymupdf`; the `fitz` alias is deprecated and warns), Segoe UI at
`C:\Windows\Fonts\segoeui.ttf` / `segoeuib.ttf` — required, because Helvetica has no `·` `→` `「」`
glyphs and renders tofu — and Graphviz at `%ProgramFiles%\Graphviz\bin\dot.exe` only for inspecting a
single `.dot`. `mmdc` is not installed.

## What the diagrams were built to answer

The refactor note is the payload; the diagrams are how it was established. The findings that matter most:

- `NewRequestSummaryViewModel.SubmitAsync` calls
  `NewRequestFlowRules.ValidateCurrentJobState(WorkCenter, WorkCenter)` — the guard compares a value with
  itself and can never fail.
- `WaitlistRequestService` detects duplicates against its in-memory `_requests` snapshot, never the store.
- `PartMatchSearchService` reaches `IVisualQueryExecutor` directly, so part search answers "unavailable"
  while the rest of the flow continues on cached data.
- `SetupPersistenceService.SaveAsync` runs four stored procedures with no transaction, and
  `ApplyGenericDesignationAsync` writes the generic flag as a separate call after the save returns.
- Work order and generic flows carry two flow-rule engines (`NewRequestFlowRules`,
  `GenericRequestFlowRules`) and two request writers.
