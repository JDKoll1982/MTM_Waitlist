# Workflow diagrams

Four end-to-end workflows in this application, mapped from the code and rendered as print-ready
flowcharts. Each has an **engineer** version — class names, command names, service interfaces, stored
procedures, cache-fallback shapes — and a **friendly** version in plain language for the people who use
the screens. Both come from the same source, so they cannot disagree.

The point of the set is a refactor: the same job is done twice in this application, once for a work order
and once for a generic request, and the two copies have drifted. [diagram-workflow-refactor-notes.md](./diagram-workflow-refactor-notes.md)
turns that into something `/speckit.specify` can consume.

## The eight documents

| # | Workflow | Engineer | Plain language | Sheets |
| --- | --- | --- | --- | --- |
| 1 | Work Order — New Request | [PDF](./pdf/diagram-workorder-new-request.pdf) | [PDF](./pdf/diagram-workorder-new-request-friendly.pdf) | 5 / 2 |
| 2 | Work Order — Work Center Setup | [PDF](./pdf/diagram-workorder-workcenter-setup.pdf) | [PDF](./pdf/diagram-workorder-workcenter-setup-friendly.pdf) | 5 / 2 |
| 3 | Generic Request — New Request | [PDF](./pdf/diagram-generic-new-request.pdf) | [PDF](./pdf/diagram-generic-new-request-friendly.pdf) | 4 / 2 |
| 4 | Generic Request — Work Center Setup | [PDF](./pdf/diagram-generic-workcenter-setup.pdf) | [PDF](./pdf/diagram-generic-workcenter-setup-friendly.pdf) | 2 / 2 |

The finished documents are in [`pdf/`](./pdf/); the Graphviz sources they are described by are in
[`dot/`](./dot/); the specs that generate both are in [`tools/`](./tools/README.md).

## How to read a page

Every page is the same four-column swimlane, left to right:

```
1  UI (XAML views)   2  ViewModels and commands   3  Services (DI)   4  Store / ERP
```

Flow moves down a column and right across columns. Colours are fixed and repeated in the legend on
every page: blue view, purple view model, amber diamond decision, teal service, grey stored procedure,
tan Infor Visual read, pink cache mirror, red dashed finding, blue dashed off-page connector.

Two conventions matter when following a flow:

- **`[To Sheet N: Command]`** badges are where a flow continues on another sheet. Read the command name,
  then find the matching `[From Sheet N]` badge there — the pages are joined on the command, not on the
  page number.
- **`[REFACTOR]`** nodes (and "Worth fixing:" callouts on the friendly pages) are findings, not steps.
  Each one is written up in the refactor notes with the class and method it refers to.

## What the diagrams found

The full list is in [the refactor notes](./diagram-workflow-refactor-notes.md) — 21 hotspots across
seven high-severity, ten moderate and four low. The ones that change behaviour today:

- The submit guard compares the work centre with itself, so it cannot fail
  (`NewRequestSummaryViewModel.SubmitAsync`).
- Duplicate detection reads this session's in-memory list, never the store
  (`WaitlistRequestService`).
- Part search reaches the ERP executor directly, so it reports "unavailable" while the rest of the flow
  continues on cached data (`PartMatchSearchService`).
- A failed availability read is reported as "no stock" rather than as an outage.
- Setup save runs four stored procedures with no transaction, and the generic flag is written as a
  separate call afterwards (`SetupPersistenceService`, `SetupWorkCenterService`).
- No role gate: any employee on the list can raise any request at any press.

## Rebuilding

The PDFs in `pdf/` and the `.dot` files in `dot/` are build output. The source is a JSON spec per
document in [tools/](./tools/README.md).

```powershell
python docs\diagrams\tools\dotgen.py docs\diagrams\tools\spec-diagram-workorder-new-request.json
python docs\diagrams\tools\render_sheets.py docs\diagrams\tools\manifest.json
python docs\diagrams\tools\verify_pdfs.py
```

Needs PyMuPDF (`pip install pymupdf`) and the Segoe UI face that ships with Windows. Graphviz is only
needed to inspect a single `.dot` file — it does not produce these PDFs.

## Using a diagram as context for a change

The set is also an input to UI/UX work. Before proposing a change to a mapped workflow, print its
digest — sheets, lanes, nodes, edges, findings and cross-sheet connectors — instead of describing the
flow from memory:

```powershell
python docs\diagrams\tools\diagram_context.py --list
python docs\diagrams\tools\diagram_context.py --workflow workorder-new-request --sheet 3
```

Add `--inspect-pdf --png` to read the rendered pages and write them to `%TEMP%` for a look. The command
is read-only and reports a missing spec, `.dot` or PDF rather than glossing over it. The full
procedure — correlate with the code, scope the change, report a diagram/code disagreement — is the
`workflow-diagram-context` skill.

A change that alters what a person sees also needs a rendered BEFORE and AFTER design, not just the
flow: see [`../ui-designs/`](../ui-designs/README.md).

## Print contract

Every page is checked before release, and `verify_pdfs.py` enforces it:

- 792 × 612 pt (11 in × 8.5 in landscape) with a 0.5 in clear margin on all four sides;
- no type below 9.5 pt, node labels at 11 pt wherever the sheet allows;
- 12–16 shapes per sheet, with a title header, `Sheet X of Y` and the colour legend on every page;
- pure white canvas, dark ink and 1.5 pt borders, so pages read in colour and in grayscale.

A flow too large for one sheet is split at an architectural seam rather than shrunk: UI and validation
on one sheet, services and stores on the next.

## Authoring rules

[`.github/instructions/diagram-authoring.instructions.md`](../../.github/instructions/diagram-authoring.instructions.md)
carries the rules an editor must follow — spec-first, the type floor, how to split a sheet, how to word a
finding. The build history and the two mistakes worth not repeating are in
[`.github/memories/repo/workflow-diagrams.md`](../../.github/memories/repo/workflow-diagrams.md).
