---
name: qol-ui-design
description: Produce a rendered BEFORE and AFTER UI design for a UI/UX or quality-of-life change, with interaction states, a side-by-side comparison and an approval record before implementation. Use when a request improves a screen, navigation, layout, controls or interaction in this application, and a picture of the current and proposed interface is needed.
---

# QOL UI design

A UI-visible change is not ready to implement until the reader can see the current screen and the
proposed one. This skill drives the existing pipeline in `docs/ui-designs/`; it does not restate the
guides.

`docs/ui-designs/ui-designs-guide.md` is the method and the accuracy rules.
`docs/ui-designs/ui-designs-01…08-*.md` are the per-image prompts. `docs/ui-designs/tools/` renders and
checks. Read the guide before the first use; after that, work from it rather than from memory. When one task
runs the whole thing - map the flow, design, verify, approve, implement - start from
`docs/ui-designs/ui-designs-09-run-the-whole-workflow.md`, or invoke the installed workspace prompt
`/qol.run-whole-workflow`, which asks for the run's inputs first.

## When this applies

Required for a change to a screen, a step, navigation, layout, control placement, information
presentation, or the number of clicks to finish a step. Not required for a backend-only change with no
visible effect; if the backend change becomes visible, include its screen.

**Wording alone does not need a picture.** When the proposed change replaces text and nothing else — the
same controls, in the same places, with the same visibility, order and count — the change is recorded in
a text-only Markdown file and the image pipeline is not run. Draw the line on the XAML and the
code-behind, not on how the screen feels:

| The change | Where it goes |
| --- | --- |
| A `Resources.resw` value changes, on a control that already exists | text-only record |
| A new resource key is added for a control that already exists | text-only record |
| Wording in a view-model string property (a literal or a resource key) | text-only record |
| A control is added, removed, moved, reordered, hidden, renamed or resized | image workflow |
| A `Visibility` rule, a bind or a command changes | image workflow |
| The number of clicks or steps changes | image workflow |
| The new wording reflows at the surface's own width (wraps, clips, grows a row) | image workflow |

The test is mechanical: **if the only files to change are `Resources.resw`, string constants, or resource
key literals in a view model, and no element's presence, order, size or visibility moves, it is text
only.** A wording change that makes an existing control wrap is a layout change wearing a text change's
clothes, and it goes through the image workflow.

## Sequence

1. **Understand the flow first.** `python docs\diagrams\tools\diagram_context.py --workflow <name>` — the
   sheets name the views, view models, commands and procedures. A field or a step that is not in the
   diagrams and not in the code does not go into a design.
2. **Inspect the existing interface.** XAML, view model, commands, bindings, validation, and the styles in
   `Styles/*.xaml`. The design's `design_system.resources` names the XAML its tokens come from. A surface
   the diagram set does not map names its own sources in `flow_source`; `startup-launch` was the last such
   flow and is now Diagram 5, so check `diagram_context.py --list` before assuming a flow is unmapped. A
   fixed launch stage records its own window size as `window.kind: "stage"`.
3. **BEFORE.** Capture the running application with `scripts/ui-automation/capture_app_window.ps1` and
   record the capture, or — when the app cannot be started or driven — reconstruct the screen from its
   XAML and mark it a reconstruction. Never present a reconstruction as a screenshot.
4. **AFTER.** A mockup built from the shared classes and tokens, using only WinUI 3 controls the project
   already has. Every element is mapped in `elements[]` to a control and an existing file.
5. **States.** Declare the states the change touches and render each one; a state that renders the same
   as default is refused.
6. **Comparison and proposal.** The renderer writes the side-by-side PNG (one row per before/after
   grouping) and the landscape PDF (one page per grouping, the pair on top and the change as real text
   below). A workflow with more than one design also needs `--workflow`, which composes the workflow's own
   PNG and PDF. Then write `<refactor>-proposal.md` with sections A–E, embedding before, after and
   comparison.
7. **Check, present, approve.** `verify_designs.py` must be clean. Present the proposal and wait. Only
   then `render_mockup.py --approve --by "<name>"`. Rendering is not approval, and approval is not
   authorisation to implement.
8. **Implement against the approved AFTER.** Afterwards capture the real screen and compare it with the
   approved image; record every deviation, intentional or not.

## Text-only changes

A text-only change gets a Markdown record in the run folder, with the same name the design would have
used:

```
docs/ui-designs/<workflow>/<screen>/<refactor>/<refactor>-text-change.md
```

No `data/`, `mockups/`, `pngs/`, design record, render record, comparison PNG or comparison PDF is
produced, and `render_mockup.py` is not run. The file carries these five sections, and `verify_designs.py`
ignores it because it is not a `*-design.json`:

```
# Text change — <title>
Workflow · Screen · Refactor · date · author

## A. Current wording
  | Resource key | Current value (verbatim from Resources.resw) | Where it shows |

## B. Proposed wording
  | Resource key | New value | Where it shows |
  New keys are marked as new, with their proposed value.

## C. Why no picture is needed
  The test from the skill, answered in one line: which files change, and that no element is added,
  removed, moved, reordered, hidden or resized. Name the control each string is bound to.

## D. Expected user benefits

## E. Impact
  Keys added or replaced, the files they live in, any test that asserts the wording, and the measured
  width of the longest new value against the width the control has today.
```

Approval is the ordinary review of that file; there is no `--approve` step and no hash. If review finds
that the new value reflows the control, the change is no longer text-only: run this skill's image
workflow for it and keep the Markdown as the pre-spec behind the design.

## Where a run lives

Every run gets its own folder, named for the refactor, and keeps its two documents at its top level while
its sources, renders and records sit in three subfolders:

```
docs/ui-designs/<workflow>/<screen>/<refactor>/
  <refactor>-proposal.md          the proposal, sections A–E
  <refactor>-comparison.png       one row per before/after grouping
  <refactor>-comparison.pdf       one page per grouping: pair on top, change in text below
  mockups/*.html                  the before, after and generated comparison sheets
  pngs/*.png                      every render, including the canonical before and after
  data/<refactor>-design.json     the record — the verifier expects it here, not at the top
  data/<refactor>-render.json     what was rendered, from which sources
```

A workflow holding two or more designs also gets `<workflow>-comparison.png`, `.pdf`,
`mockups/<workflow>-compare.html` and `data/<workflow>-comparison.json` at the workflow's own level, and
nothing else — no loose records beside the screens. `tools/template/` is copied into a new run folder. A
workflow is always its own folder directly under `docs/ui-designs/` and is never nested in another; a
design whose `workflow` does not match the folder it sits in is refused.

## Commands

```powershell
python docs\ui-designs\tools\render_mockup.py --design "<run folder>\data\<refactor>-design.json"
python docs\ui-designs\tools\render_mockup.py --workflow "docs\ui-designs\<workflow>"
python docs\ui-designs\tools\verify_designs.py --design "<run folder>\data\<refactor>-design.json"
python docs\ui-designs\tools\render_mockup.py --design "<run folder>\data\<refactor>-design.json" --approve --by "<name>"
python docs\ui-designs\tools\verify_designs.py --all --require-approval
```

## Pitfalls

Each of these cost a run. They are properties of the tools, not preferences.

- **The shared stylesheet knows six state words, not every state.** `mockup.css` reveals
  `.when-no-job`, `.when-error`, `.when-loading`, `.when-success`, `.when-empty` and `.when-disabled`, and
  nothing else. A word of your own (`state-few`, `state-solo`, `state-overdue`) renders `default` unless
  the mockup carries its own rule — `body:has(#state-few:target) .when-few { display: block; }`. Put that
  rule in the mockup's `<style>`, never in `mockup.css`.
- **An inline `style="display:…"` beats the state rule.** An element a state has to hide or show must take
  its `display` from a class; an inline one wins, the state renders as the default page, and the renderer's
  duplicate check cannot catch it because the *default* is the wrong image, not the state.
- **`file://` is not a way to look at a mockup.** Playwright MCP refuses `file:` URLs, and a mockup served
  from its own folder loads no stylesheet at all — `../../../../tools/mockup/mockup.css` escapes the
  served root. Serve the repository root (`python -m http.server`) and check
  `getComputedStyle(scope).fontFamily`: `"Times New Roman"` means the stylesheet did not load.
- **A long banner steals height.** At a launch stage (780 × 640, 520 × 560) the `.provenance` line next to
  the BEFORE/AFTER banner wraps to two lines and pushes the page content down. Keep it to the minimum the
  verifier accepts — `reconstruction` plus one file — and measure long note strings before choosing them.
- **The proposal is not hashed.** Editing `<refactor>-proposal.md` alone leaves the approval valid, so a
  proposal can drift from the images it approved. Editing a mockup, a linked CSS file or the listed
  `design_system.resources` invalidates it; re-render and re-approve.
- **`window.viewports` follows the surface.** A launch stage declares one viewport named `stage` at the
  size `MainWindow.xaml.cs` gives it, and the verifier checks that number against the code. The three desk
  sizes belong to `window.kind: "shell"` designs only.
- **`verify_designs.py --all` prints file names only.** Two runs may legitimately share a refactor slug in
  different screens, and their lines are then identical; use `--json` when the run matters.
- **One comparison is not the workflow's comparison.** A run's own sheet compares its groupings; the
  workflow needs `render_mockup.py --workflow <folder>` afterwards, and the verifier fails a workflow that
  holds two or more designs and has no workflow sheet.
- **Text extracted from the PDF is not the text you typed.** Segoe UI's ToUnicode map gives `-` as
  `U+2010`, so `Sign-in` extracts as `Sign‑in`. Compare after folding the dashes and the whitespace, as
  `squash_text` in the verifier does, rather than deciding the PDF lost a word.
- **Delete nothing to make a stale sheet pass.** A workflow sheet whose recorded hashes no longer match
  the designs is re-composed with `--workflow`; removing the record hides the problem instead of fixing it.
- **A wording change is not automatically text-only.** It is text-only when the diff touches no element:
  same controls, same order, same size, same visibility. New wording that wraps, clips or grows a row is a
  layout change, so measure the longest new value against the control's current width with
  `ImageFont.truetype(<windir>\Fonts\segoeui.ttf, <pt>).getlength(text)` before choosing the
  text-only path. A text-only record that turns out to reflow is reopened as an image design.

## Non-negotiables

- No image-generation model for a UI, a state, a comparison or a diagram. Those come from the mockup
  renderer and the diagram pipeline.
- No invented labels, values, statuses or controls; UI wording comes from `Strings/en-us/Resources.resw`.
- No workflow change smuggled into a redesign. If the flow changes, update the diagram spec and rebuild
  the PDFs, per `.github/instructions/diagram-authoring.instructions.md`.
- No application `.cs` or `.xaml` change as part of producing the design.
- An unchanged approval hash is the proof that the implemented screen was built from the approved
  design; changing a mockup after approval invalidates it.
