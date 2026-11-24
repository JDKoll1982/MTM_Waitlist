# Image guide — after, before, states and workflow diagrams

The pictures a QOL proposal is allowed to use, what each one must be made with, where it lives, and how
it is checked. One page per image type, each a complete prompt an agent can copy into a fresh task.

**Scope.** Visual material for `docs/research/` proposals, workflow documentation and UI redesign in
this repository. It documents the existing tooling under `docs/ui-designs/tools/` and
`docs/diagrams/tools/`; it does not replace either.

**Why these files live here.** They sit beside the mockup renderer and verifier they document, and follow
the repository's `docs/<area>/<area>-<slug>.md` convention (`docs/scripts/scripts-automation-index.md`,
`docs/reference/reference-inforvisual-index.md`). A parallel `docs/ai-instructions/` copy would be a second
description of a live folder and would drift from it. The files do not use the `.instructions.md` suffix,
because that name means "applied automatically to matching files" under `.github/instructions/`; these are
prompts an agent copies into a task.

**The rule that outranks the rest:** an image is either a truthful record or a clearly labelled
proposal, and it is checked before anyone treats it as fact. A picture that invents a control, a value
or a business rule is worse than no picture, because the document around it is then wrong too.

---

## 1. Choose the method before choosing a prompt

Each type is produced by a specific part of the pipeline. None of the UI types is produced by an
image-generation model.

| Image | How it is declared | Produced by |
| --- | --- | --- |
| Before UI, captured | `before.kind: "capture"` | `scripts/ui-automation/` drives the app; the screenshot becomes `<refactor>-before.png` |
| Before UI, reconstructed | `before.kind: "reconstruction"` + `before.mockup` | `docs/ui-designs/tools/render_mockup.py` |
| After UI | `after.mockup` | `render_mockup.py` |
| Interaction states (normal, empty, error, loading, success, disabled) | `states[]`, one entry per state | `render_mockup.py`, every state × every viewport |
| Before/after comparison | none — generated | `render_mockup.py` |
| Workflow diagram | JSON spec in `docs/diagrams/tools/` | `dotgen.py` → `render_sheets.py` → `verify_pdfs.py` |
| Decorative artwork only | none | the host `image_generation` tool, labelled illustrative |

A screenshot is the only acceptable evidence of current appearance. A rendered mockup is the only
acceptable way to show a proposal in enough detail to implement. A structured diagram is the only
acceptable way to show a flow that must stay correct and editable.

### What is available on this machine

| Capability | Status | Notes |
| --- | --- | --- |
| Mockup renderer | **Available and verified** | `docs/ui-designs/tools/render_mockup.py` renders with headless Microsoft Edge, which ships with Windows. `MTM_EDGE` overrides the path. |
| Design verifier | **Available** | `docs/ui-designs/tools/verify_designs.py` — completeness, workflow references, proposal sections, approval hashes. |
| Design tests | **Available** | `python -m unittest discover -s docs\ui-designs\tools -p "test_*.py"`. |
| Application capture | **Available** | `scripts/ui-automation/`: `ui-drive.ps1`, `uia-surface-walk.ps1`, `capture_app_window.ps1`, `capture_app_region.ps1`, `ocr_png.ps1`. See `docs/scripts/scripts-automation-ui-automation.md`. |
| Diagram pipeline | **Available** | `docs/diagrams/tools/`: `dotgen.py`, `render_sheets.py`, `verify_pdfs.py`, `diagram_context.py`. Needs PyMuPDF and the Segoe UI face. |
| Image-generation model | **Available, narrowly useful** | The host `image_generation` tool: one PNG per call, aspect ratios 1:1, 3:2 and 2:3. It does not guarantee exact text or control geometry. |

### Where the image-generation model may and may not be used

**May:** decorative artwork — an empty-state illustration concept, a proposal cover image. Label it
"illustrative — not a screenshot of the application".

**May not:** a before or after UI, any interaction state, a comparison, a workflow diagram, or any image
whose text a reader must read exactly. The model synthesises plausible glyphs; a UI image with
synthesised labels is a false record. If it is the only tool available, produce no image and say so.

**If a capability above disappears,** update §1 and the method check in the same change rather than
leaving a prompt that names a tool this machine no longer has.

---

## 2. The pipeline in one page

A design is one JSON record plus one HTML mockup per variant, in its own folder:

```
docs/ui-designs/<workflow>/<screen>/<refactor>/
  <refactor>-design.json          the record: variants, states, mappings, approval
  mockups/<refactor>-before.html  the before mockup, when the before is a reconstruction
  mockups/<refactor>-after.html   the after mockup
  <refactor>-before.png           canonical before, rendered (or the capture)
  <refactor>-after.png            canonical after
  <refactor>-comparison.png       before and after side by side
  <refactor>-<variant>-<state>-<viewport>.png
  <refactor>-render.json          what was rendered, from which sources, with hashes
  <refactor>-proposal.md          sections A–E, embedding the three canonical images
```

Each run of the skill gets its own folder under the screen, named for the refactor, so two refactors on
one screen never share a file. For a work-centre selection refactor that is
`docs/ui-designs/workorder-new-request/work-center-selection/fewer-clicks/`, holding
`fewer-clicks-design.json`, `mockups/fewer-clicks-before.html` and `mockups/fewer-clicks-after.html`.
The copy shipped with the repository is `tools/template/`; copy it to that folder and rename the files.
The verifier refuses a design record that does not sit in a folder named after its refactor.

```powershell
# Render every state at every declared window size.
# Relative or absolute: the renderer resolves every image path before Edge sees it.
python docs\ui-designs\tools\render_mockup.py --design "C:\Users\<you>\source\repos\MTM_Waitlist\docs\ui-designs\<workflow>\<screen>\<refactor>\<refactor>-design.json"

# Check the record, the renders and the proposal.
python docs\ui-designs\tools\verify_designs.py --design "C:\...\<refactor>-design.json"

# Record approval once the design is accepted; this is a separate, human decision.
python docs\ui-designs\tools\render_mockup.py --design "C:\...\<refactor>-design.json" --approve --by "<name>"

# Check every design in the repository.
python docs\ui-designs\tools\verify_designs.py --all --require-approval
```

**The record** (`<refactor>-design.json`) carries: `workflow`, `workflow_variant`, `screen`, `refactor`,
`title`, `audience`, `window.viewports`, `before`, `after`, `states`, `callouts`, `elements`,
`workflow_refs`, `design_system.resources`, `approval`. The verifier requires `workflow`, `screen`,
`refactor`, `window`, `before`, `after`, `states`, `elements`, `workflow_refs`, `design_system` and
`approval`; `title`, `audience`, `callouts`, `workflow_variant`, `flow_source` and `window.kind` /
`window.stage` are optional but used when present.

**Window sizes depend on the surface.**

- `"kind": "shell"` (the default) is a surface that fills the maximised window: `wide` 1600 × 1000,
  `medium` 1280 × 800, `minimum` 900 × 500, at a scale of 1 unless a record says otherwise. A design
  missing one of them fails verification.
- `"kind": "stage"` is a launch stage, which the host gives a fixed size of its own. The record declares
  that size as viewport `stage` (for example 780 × 640 for sign-in) and the verifier checks the figure
  against the `new SizeInt32(...)` values in `MainWindow.xaml.cs`. Rendering a fixed stage at the desk
  sizes would not be the window a person sees.

**A workflow outside the diagram set** names its own source. If `workflow` is not a manifest workflow,
the record carries `flow_source` — existing files that describe the flow — and `workflow_refs` must be
empty; the verifier reports a note that the flow is not mapped rather than pretending it cited a sheet.
Once a flow is mapped, the same record cites its sheets and nodes instead. `startup-launch` was such an
unmapped flow until Diagram 5 was added; the mechanism stays for a flow the set has not reached.

**States** are rendered without script: the mockup contains a hidden anchor whose id is the state name,
and the shared stylesheet reveals `.when-<word>` when that anchor is the URL target. The renderer loads the
page at `#<name>`, so the `states[].name` in the record must be **the anchor id itself** — `state-empty`,
`state-error`, `state-loading`, `state-success`, `state-no-job`, `state-disabled` — because the renderer
rejects a name that is not an anchor id. `default` is the page with no anchor targeted and is required for
every variant.

> The renderer refuses two states that would render identically: a state whose PNG matches `default`
> byte for byte stops the run with the state's name, so a fragment that no anchor matches cannot ship as a
> duplicate of the default page. The shipped `template/design.json` and both template mockups use the
> prefixed names.

**Nothing is approved by rendering.** `--approve` writes a hash of the mockups, the shared assets and
the design-system XAML into the record; the verifier then fails if any of them changes without a
re-render and re-approval. Present the proposal first.

---

## 3. The common visual standard

Every mockup inherits `docs/ui-designs/tools/mockup/mockup.css`, which resolves the tokens in
`mockup/tokens.css`. Those tokens name the application resources they come from, and the XAML files
listed in a design's `design_system.resources` are hashed at render time — so a token that drifts from
its XAML is reported rather than silently copied.

| Role | Value in `tokens.css` | Application resource |
| --- | --- | --- |
| Text primary | `#1b1b1b` | `TextFillColorPrimary` |
| Text secondary | `#5d5d5d` | `TextFillColorSecondary` |
| Text disabled | `#9a9a9a` | `TextFillColorDisabled` |
| Card surface | `#ffffff` | `CardBackgroundFillColorDefault` |
| Secondary surface | `#f7f7f7` | `CardBackgroundFillColorSecondary` |
| Card and input stroke | `#e3e3e3` | `CardStrokeColorDefault` |
| Divider | `#e8e8e8` | `DividerStrokeColorDefault` |
| Page layer | `#f3f3f3` | `LayerFillColorDefault` |
| Accent | `#005fb8` | `AccentFillColorDefault` |
| Text on accent | `#ffffff` | `TextOnAccentFillColorPrimary` |
| Caution | bg `#fff4ce`, stroke `#e9c46a`, text `#6a5300` | `SystemFillColorCaution*` |
| Error | bg `#fde7e9`, stroke `#c42b1c`, text `#8e1b10` | `SystemFillColorCritical*` |
| Success | bg `#dff6dd`, stroke `#0f7b0f`, text `#0b5a0b` | `SystemFillColorSuccess*` |

- Type: page title `--font-large` 24, card heading and control text `--font-medium` 16, hints and
  badges `--font-small` 12. Segoe UI. These are the sizes the application uses today.
- Space: `2, 4, 8, 10, 12, 16, 20, 24, 32, 36`. Card radius 12, stroke 1. Page padding
  `32, 24, 32, 20`; row spacing 20; header gap 10; action gap 12.
- Shell: title bar 32, navigation pane 320 (48 compact), window minimum 900 × 500, hit target 44.
- Dialogs: minimum 480, default 720, maximum 880.
- Mockups are light by default; `html[data-theme="dark"]` in `tokens.css` carries the dark values for a
  design that has to be checked in both.

**Do not mix in the "Lab" adoption values** from `.github/instructions/winui3-adoption.instructions.md`
(28/36 titles, radius 8, `#0F5C94`, a 4–48 spacing scale). That file describes a *proposed* conversion,
not the application as it is today. A mockup of today's screens uses today's tokens; a proposal that
adopts the new ramp says so explicitly.

---

## 4. Accuracy rules for every image

1. Every label, value and control traces to a file, a resource string or a stored procedure. If it
   cannot be cited, it is left out.
2. `Strings/en-us/Resources.resw` is the only source of UI wording. Do not retype a label from memory.
3. Nothing the application cannot do appears in an image. No invented commands, fields, statuses,
   roles, permissions or validation.
4. A proposal never appears as existing behaviour, and a defect is never quietly cleaned up in a before.
5. Data is domain data: work centres like `100-03`, parts like `MMC0001000`, `MMF500`, `FGT00023`,
   categories `Pickup` / `Deliver`, hopper sizes `Small` / `Medium` / `Large`, statuses `Waiting` /
   `In Progress` / `Done` / `Cancelled`. No lorem ipsum, no "John Doe", no `example.com`.
6. One clock per image. Times use the app's own formats — the card ring shows `01:02` and `-00:15`.
7. Every design element maps to a WinUI control and a real code path in `elements[]`; every workflow
   claim maps to a sheet and node in `workflow_refs`.
8. A captured before is a capture, with a date and a command. A reconstructed before says so in its own
   mockup banner, and the verifier warns when the word "reconstruction" is missing.

---

## 5. Validating a finished image

| Check | How |
| --- | --- |
| Record and renders agree | `verify_designs.py --design "<path>"` — every state at every viewport, canonical images present, correct dimensions |
| The before is honest | `before.kind` matches reality; a reconstruction names its `source_xaml`, a capture names `captured_at` and `capture_command` |
| Design maps to code | `elements[]` names an existing WinUI control and an existing file; `workflow_refs` resolve to real sheets and nodes |
| Text is exactly the application's | `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>` over the canonical PNG, compared with `Resources.resw`. It needs Windows PowerShell 5.1 — under `pwsh` 7 the WinRT types do not resolve. Expect punctuation-only differences (`·`, `1` read as `I`); a word difference is a failure. |
| Nothing invented | walk every visible element back to a file, string or procedure |
| The proposal is complete | `<refactor>-proposal.md` carries A–E and embeds the three canonical PNGs |
| Approval is real | approval is recorded by a person after review, and its hashes still match |

---

## 6. How the prompts fit the documentation and QOL workflow

1. **Describe the problem** in a pre-spec under `docs/research/`, with the current behaviour and the
   numbered decisions.
2. **Read the flow, not the memory:** `python docs\diagrams\tools\diagram_context.py --workflow <workflow> --sheet <n>`.
3. **Produce the before** with prompt 01 — a capture where the app can run, a labelled reconstruction
   where it cannot.
4. **Produce the after** with prompt 02, then the states the proposal changes with prompts 03–07.
5. **Diagram any changed flow** with prompt 08, so the PDF and the code cannot disagree.
6. **Write `<refactor>-proposal.md`** in the design folder, embedding the before, after and comparison.
7. **Verify, present, approve.** `verify_designs.py` clean, then the human decision, then
   `--approve --by "<name>"`. The approved design is the implementation reference; implementation is a
   separate authorisation (Update §9).
8. **Verify after implementation** against the approved after image, and record any intentional
   deviation.

An image never replaces a decision. If a picture is doing the explaining, the decision is missing.

---

## 7. The nine prompts

| # | File | Use when |
| --- | --- | --- |
| 1 | [ui-designs-01-before-ui.md](./ui-designs-01-before-ui.md) | recording the interface as it exists today |
| 2 | [ui-designs-02-after-ui.md](./ui-designs-02-after-ui.md) | proposing the improved interface |
| 3 | [ui-designs-03-normal-populated-state.md](./ui-designs-03-normal-populated-state.md) | the screen at work, with real data |
| 4 | [ui-designs-04-empty-initial-state.md](./ui-designs-04-empty-initial-state.md) | before the person has entered anything |
| 5 | [ui-designs-05-validation-error-state.md](./ui-designs-05-validation-error-state.md) | a correctable problem, and how to fix it |
| 6 | [ui-designs-06-loading-processing-state.md](./ui-designs-06-loading-processing-state.md) | an operation in progress |
| 7 | [ui-designs-07-success-confirmation-state.md](./ui-designs-07-success-confirmation-state.md) | a completed operation |
| 8 | [ui-designs-08-workflow-process-diagram.md](./ui-designs-08-workflow-process-diagram.md) | mapping a flow, or updating a mapped flow |
| 9 | [ui-designs-09-run-the-whole-workflow.md](./ui-designs-09-run-the-whole-workflow.md) | running both workflows end to end on one change, from the flow map to the approved and verified screen |

For a single task that runs the flow map, the design and the verification in order, start with
prompt 09 and let it dispatch to 01-08 as it needs them. It is also installed as the workspace prompt
`/qol.run-whole-workflow`, which asks for the workflow, screen, refactor slug, improvement, capture
permission and audience before it starts.

The pipeline was exercised on 2026-11-24; what ran, what it produced and the two constraints it exposed
are recorded in [ui-designs-method-check.md](./ui-designs-method-check.md).
