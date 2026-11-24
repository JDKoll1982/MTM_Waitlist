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

Required for a change to a screen, a step, navigation, layout, control placement, wording shown on a
control, information presentation, or the number of clicks to finish a step. Not required for a
backend-only change with no visible effect; if the backend change becomes visible, include its screen.

## Sequence

1. **Understand the flow first.** `python docs\diagrams\tools\diagram_context.py --workflow <name>` — the
   sheets name the views, view models, commands and procedures. A field or a step that is not in the
   diagrams and not in the code does not go into a design.
2. **Inspect the existing interface.** XAML, view model, commands, bindings, validation, and the styles in
   `Styles/*.xaml`. The design's `design_system.resources` names the XAML its tokens come from. A surface
   the diagram set does not map — `startup-launch` is one — records its flow source in `flow_source`, and
   a fixed launch stage records its own window size as `window.kind: "stage"`.
3. **BEFORE.** Capture the running application with `scripts/ui-automation/capture_app_window.ps1` and
   record the capture, or — when the app cannot be started or driven — reconstruct the screen from its
   XAML and mark it a reconstruction. Never present a reconstruction as a screenshot.
4. **AFTER.** A mockup built from the shared classes and tokens, using only WinUI 3 controls the project
   already has. Every element is mapped in `elements[]` to a control and an existing file.
5. **States.** Declare the states the change touches and render each one; a state that renders the same
   as default is refused.
6. **Comparison and proposal.** The renderer writes the side-by-side PNG; write
   `<refactor>-proposal.md` with sections A–E, embedding before, after and comparison.
7. **Check, present, approve.** `verify_designs.py` must be clean. Present the proposal and wait. Only
   then `render_mockup.py --approve --by "<name>"`. Rendering is not approval, and approval is not
   authorisation to implement.
8. **Implement against the approved AFTER.** Afterwards capture the real screen and compare it with the
   approved image; record every deviation, intentional or not.

## Where a run lives

Every run gets its own folder, named for the refactor:
`docs/ui-designs/<workflow>/<screen>/<refactor>/`. The record, the mockups, the renders, the render
record and the proposal all live inside it, so a second refactor on the same screen cannot overwrite the
first. `tools/template/` is copied into that folder; the verifier rejects a record that is not in a
folder named after its refactor.

## Commands

```powershell
python docs\ui-designs\tools\render_mockup.py --design "<run folder>\<refactor>-design.json"
python docs\ui-designs\tools\verify_designs.py --design "<run folder>\<refactor>-design.json"
python docs\ui-designs\tools\render_mockup.py --design "<run folder>\<refactor>-design.json" --approve --by "<name>"
python docs\ui-designs\tools\verify_designs.py --all --require-approval
```

## Non-negotiables

- No image-generation model for a UI, a state, a comparison or a diagram. Those come from the mockup
  renderer and the diagram pipeline.
- No invented labels, values, statuses or controls; UI wording comes from `Strings/en-us/Resources.resw`.
- No workflow change smuggled into a redesign. If the flow changes, update the diagram spec and rebuild
  the PDFs, per `.github/instructions/diagram-authoring.instructions.md`.
- No application `.cs` or `.xaml` change as part of producing the design.
- An unchanged approval hash is the proof that the implemented screen was built from the approved
  design; changing a mockup after approval invalidates it.
