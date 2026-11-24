# UI designs

Rendered BEFORE and AFTER UI designs for QOL proposals: the editable mockup sources, the PNGs a
proposal is read from, and the approval record that ties a decision to the images it approved.

- **[ui-designs-guide.md](./ui-designs-guide.md)** — the method, the visual standard and the accuracy
  rules. Read this first.
- **[ui-designs-01 … 08](./ui-designs-01-before-ui.md)** — one prompt per image type: before, after, the
  interaction states, and a workflow diagram.
- **[ui-designs-09](./ui-designs-09-run-the-whole-workflow.md)** — the end-to-end prompt: map the flow if
  needed, design before and after, verify, present for approval, then implement and compare. Also
  installed as the workspace prompt `/qol.run-whole-workflow`.
- **[tools/](./tools/)** — `render_mockup.py` (headless Edge → PNG, comparison, approval),
  `verify_designs.py`, the shared `mockup/` stylesheet and the `template/` a new design starts from.
- **[ui-designs-method-check.md](./ui-designs-method-check.md)** — what was run end to end, and the two
  constraints that run exposed.

A design lives in its own folder, `docs/ui-designs/<workflow>/<screen>/<refactor>/`, holding its proposal
and its two comparison documents at the top and everything else in `mockups/`, `pngs/` and `data/`. A
workflow is its own folder directly under `docs/ui-designs/` and is never nested in another; when it holds
more than one design it also carries `<workflow>-comparison.png` and `.pdf`. A wording-only change with no
control change is recorded as `<refactor>-text-change.md` in the same folder instead of a design: no
mockup, no render, no approval hash. Workflow structure is not
duplicated here: the diagram set in [`../diagrams/`](../diagrams/README.md) remains the source for the
flow, and a design cites its sheets and node ids.

## Generating a BEFORE mockup from the XAML

The honest BEFORE is the screen as it is, so it can be built from the view rather than re-typed:

```powershell
pwsh scripts\docs\generate-docs.ps1 -Mode Designs -Screen SignInPage
```

That parses the XAML and writes `<refactor>-design.json` plus `mockups/<refactor>-before.html` (and an
`-after.html` that is a marked copy), then renders every declared viewport with headless Edge. The
output lands under `%TEMP%\mtm-diagrams\facts\designs`; pass `-WriteInPlace` to write into
`docs\ui-designs`.

What it preserves: layout containers (Grid rows and columns, StackPanel direction, ScrollViewer),
padding and margins, `Visibility="Collapsed"`, accent buttons, fields, and the mockup stylesheet's
classes. What it cannot know: the state behind a `{x:Bind}` binding, so a bound label shows its
property name and a bound `Visibility` is recorded as `data-visible-when`. Controls with no mapping are
outlined in red and named, so the gap is visible rather than guessed. The AFTER stays authored.
