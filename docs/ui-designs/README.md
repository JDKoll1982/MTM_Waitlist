# UI designs

Rendered BEFORE and AFTER UI designs for QOL proposals: the editable mockup sources, the PNGs a
proposal is read from, and the approval record that ties a decision to the images it approved.

- **[ui-designs-guide.md](./ui-designs-guide.md)** — the method, the visual standard and the accuracy
  rules. Read this first.
- **[ui-designs-01 … 08](./ui-designs-01-before-ui.md)** — one prompt per image type: before, after, the
  interaction states, and a workflow diagram.
- **[tools/](./tools/)** — `render_mockup.py` (headless Edge → PNG, comparison, approval),
  `verify_designs.py`, the shared `mockup/` stylesheet and the `template/` a new design starts from.
- **[ui-designs-method-check.md](./ui-designs-method-check.md)** — what was run end to end, and the two
  constraints that run exposed.

A design lives at `docs/ui-designs/<workflow>/<screen>/<refactor>-*`. Workflow structure is not
duplicated here: the diagram set in [`../diagrams/`](../diagrams/README.md) remains the source for the
flow, and a design cites its sheets and node ids.
