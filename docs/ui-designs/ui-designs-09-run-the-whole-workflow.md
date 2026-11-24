# Run the whole QOL and diagram workflow

Use this when a workflow needs a quality-of-life change and both halves of the documentation are in
scope: the flow map in `docs/diagrams/` and the rendered BEFORE/AFTER design in `docs/ui-designs/`.

It is one run from start to a reviewed proposal, and then, only on approval, to an implemented and
verified change. Nothing here authorises an implementation by itself.

## Inputs

| Input | Meaning |
| --- | --- |
| `<<WORKFLOW>>` | A workflow id from `docs/diagrams/tools/manifest.json`, or the name of a flow that is not mapped yet |
| `<<SCREEN>>` | The screen or step being improved, for example `work-center-selection` or `sign-in` |
| `<<REFACTOR>>` | A short slug for this run, for example `fewer-clicks`; it becomes the run folder's name |
| `<<IMPROVEMENT>>` | One sentence: what gets better for the person using the screen |
| `<<CAPTURE>>` | `yes` when the application may be started and signed in for the BEFORE capture, `no` otherwise |
| `<<AUDIENCE>>` | `developer` or `stakeholder`; it decides whether the engineer or the friendly map leads the explanation |

## Ask before starting, only where the answer is not already in the repository

1. **Which workflow, and which screen in it?** If `<<WORKFLOW>>` is missing, list the mapped workflows
   and ask. Never guess between two flows.
2. **What should improve, in the person's words?** "Make it easier" is not scoped enough to prove the
   result later.
3. **May the application be started and signed in for the BEFORE capture?** A capture is the only
   truthful record of the current screen; without it the BEFORE is a reconstruction and must say so.
4. **Who approves the design?** The design is presented for review before anything is implemented, and
   approval is recorded by name.

Do not ask anything the code, the diagram set or the design record already answers. Do not ask for
permission the workflow already grants.

Ask the way the person asking for the change would ask: plain English, one short sentence for what is
missing and one for what it is needed for, at most three choices when a choice is real, no file paths, no
tool output, no JSON, no jargon. When the answer is not available, stop and wait; never invent a value, a
screenshot, an approval or a decision to keep the run moving.

## Phase 0 - designate the flow, and map it if it is missing

```powershell
python docs\diagrams\tools\diagram_context.py --list
```

- If `<<WORKFLOW>>` is listed, go to Phase 1.
- If it is not, **mapping it is part of this run**. Follow
  [prompt 08](./ui-designs-08-workflow-process-diagram.md): write
  `docs/diagrams/tools/spec-diagram-<<WORKFLOW>>.json` with a top-level `"workflow"` key, and a
  friendly spec beside it when the change will be explained to the people who use the screen. Add both
  entries to `manifest.json`, then:

```powershell
python docs\diagrams\tools\dotgen.py docs\diagrams\tools\spec-diagram-<<WORKFLOW>>.json
python docs\diagrams\tools\render_sheets.py docs\diagrams\tools\manifest.json
python docs\diagrams\tools\verify_pdfs.py
```

Update `docs/diagrams/README.md`, the memory note and the diagram tests in the same change. A map that
is added but not listed in the README and the tests is incomplete.

## Phase 1 - read the flow before touching the screen

```powershell
python docs\diagrams\tools\diagram_context.py --workflow <<WORKFLOW>> --variant engineer
python docs\diagrams\tools\diagram_context.py --workflow <<WORKFLOW>> --variant friendly
```

Read **every** sheet, not the first. Follow `[To Sheet N]` to the sheet that carries the matching
`[From Sheet M]` badge. Note the node ids for the views, view models, commands, services, stored
procedures and `[REFACTOR]` findings on the screen being changed; those ids go into the design record's
`workflow_refs` and into the explanation.

If a `[REFACTOR]` node already records the problem you were asked to fix, say so and build on it rather
than re-describing it.

## Phase 2 - inspect the implementation

Read, in this order: the XAML page, its view model, the commands and bindings it binds, the services
those commands call, the stored procedures those services call, and the wording in
`Strings/en-us/Resources.resw`. Read the styles in `Styles/*.xaml`, because the design tokens come from
there.

Write down, before designing anything:

1. what the screen does today, with file and line references;
2. what the diagram says it does, and where the two disagree;
3. the smallest change that delivers `<<IMPROVEMENT>>`;
4. what you could not verify.

A disagreement between the diagram and the code is reported, never quietly resolved. A change that would
alter a business rule is raised as a decision, not folded into a UI change.

## Phase 3 - the BEFORE image

- With `<<CAPTURE>> = yes`: drive the application with `scripts/ui-automation/ui-drive.ps1`, capture with
  `capture_app_window.ps1`, and record `captured_at` and `capture_command`. Never work around sign-in.
- With `<<CAPTURE>> = no`, or when the app cannot be started: reconstruct the screen from its XAML in a
  mockup whose banner says **reconstruction**, and list the XAML in `before.source_xaml`.

Never present a reconstruction as a screenshot, and never present a screenshot of another screen.

## Phase 4 - the AFTER image and its states

Create the run folder and copy the template into it:

```
docs/ui-designs/<<WORKFLOW>>/<<SCREEN>>/<<REFACTOR>>/
```

Use only the shared mockup classes and tokens, and only WinUI 3 controls the project already has. Give
every element of the change a numbered callout and list each one in `callouts[]`. Declare each state the
change touches as a `state-*` anchor; a state that renders the same as `default` stops the run.

Preserve the existing functionality: fields, buttons, validation, navigation and store writes. Anything
moved, merged or removed is stated in the proposal and justified.

## Phase 5 - render, compare and write the proposal

```powershell
python docs\ui-designs\tools\render_mockup.py --design "docs\ui-designs\<<WORKFLOW>>\<<SCREEN>>\<<REFACTOR>>\<<REFACTOR>>-design.json"
```

Open the canonical BEFORE, AFTER and comparison PNGs and the page images in `%TEMP%\mtm-diagrams\context`.
A label on a border, a clipped row or a callout pointing at nothing means the page is not finished.

Write `<<REFACTOR>>-proposal.md` with the five sections:

1. **A. Current State** - what the person sees now, and what it costs them; the BEFORE image.
2. **B. Proposed Improvement** - the change, why it helps, how the person uses it; the AFTER image.
3. **C. Visual Comparison** - the comparison image and numbered callouts.
4. **D. Expected User Benefits** - observable benefits only, no invented numbers.
5. **E. Implementation Impact** - XAML, view model, commands, services, resources, navigation, risk.

Include the traceability table: workflow, sheet, node id, code file, requirement.

## Phase 6 - verify, present, and stop

```powershell
python docs\ui-designs\tools\verify_designs.py --design "docs\ui-designs\<<WORKFLOW>>\<<SCREEN>>\<<REFACTOR>>\<<REFACTOR>>-design.json"
```

It must be clean apart from the expected "not approved yet" note. Check one rendered image with OCR:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path "<absolute path to the AFTER png>"
```

Show the proposal and the images. **Stop.** Rendering is not approval, and approval is not an instruction
to implement. Record approval only when the named approver has said yes:

```powershell
python docs\ui-designs\tools\render_mockup.py --design "<design.json>" --approve --by "<name>"
```

## Phase 7 - implement, only after approval

Use the approved design as the reference. Translate elements to XAML, bind them to the view model, reuse
existing resources. Do not change the flow, a stored procedure, a validation rule or a role gate as part
of a visual change; if the design cannot be built as approved, say so and propose the alternative before
changing the design.

Run the build and the tests that cover the changed code.

## Phase 8 - verify the finished screen, then close the loop

1. Capture the running application's changed screen.
2. Compare it with the approved AFTER image; record every difference as intentional or not.
3. If the flow changed at all, update the diagram spec and rebuild:
   `dotgen.py`, `render_sheets.py`, `verify_pdfs.py`.
4. If the improvement changes the wording, add the resource key and keep the old one only if something
   else still reads it.

```powershell
python -m unittest discover -s docs\diagrams\tools -p "test_*.py"
python -m unittest discover -s docs\ui-designs\tools -p "test_*.py"
python docs\ui-designs\tools\verify_designs.py --all --require-approval
python docs\diagrams\tools\verify_pdfs.py
```

## Rules that do not bend

- The JSON spec under `docs/diagrams/tools/` is the only source of a flow. `.dot` and PDF are build
  output; never hand-edit them.
- No image-generation model for a UI, a state, a comparison or a diagram.
- No invented labels, values, statuses, roles or controls; wording comes from `Resources.resw`.
- No application `.cs` or `.xaml` change before approval. No demo, sample or mock mode, ever.
- One run, one folder: `docs/ui-designs/<workflow>/<screen>/<refactor>/`.
- A flow written without an owner decision about a business rule is not a QOL change; it is a feature.

## Done means

- [ ] The flow is mapped, or explicitly recorded as unmapped with its source named.
- [ ] BEFORE and AFTER images exist at every declared state and window size, and the comparison exists.
- [ ] The proposal carries A-E, the traceability rows and the callouts.
- [ ] `verify_designs.py` is clean and the approval is recorded by name.
- [ ] After implementation, the finished screen was captured and compared with the approved image.
- [ ] Both test suites and the print-contract check pass.
