# Prompt 1 — Before UI (the interface as it exists today)

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **before** half of a QOL design record for the MTM Waitlist Application: a WinUI 3
desktop app on .NET 10 (`net10.0-windows10.0.19041.0`), Windows App SDK 2.4.0,
`CommunityToolkit.Mvvm` 8.4.2, `WinUIEx` 2.9.2. The image records the interface **as it is today**,
including what is wrong with it. It proposes nothing.

---

## 1. Purpose

Give the reviewer a truthful record to compare the proposal against. The image exists to make the
problem visible — the crowding, the missing affordance, the clipped label, the button in the wrong place.
It must never show an improvement, a cleanup, or a design that has not shipped.

## 2. Required inputs

- `<<WORKFLOW>>` — a manifest id: `workorder-new-request`, `workorder-workcenter-setup`,
  `generic-new-request`, `generic-workcenter-setup`.
- `<<SCREEN>>` — the screen's folder name, for example `work-center-selection`.
- `<<REFACTOR>>` — the short name of the proposal, for example `fewer-clicks`.
- `<<XAML>>` — the view file, for example `Module_Waitlist/Views/WaitlistViewPage.xaml`.
- `<<CONTROL>>` — the control file when the subject is a control.
- `<<VIEWMODEL>>` — the view model that loads the screen.
- `<<SHEET>>` — the diagram sheet that shows the screen.
- `<<STATE>>` — what the screen is showing: populated, claimed, overdue, a cached-data notice.
- `<<SCALING>>` — the display scale of the capture machine.

Read before producing anything:

1. `docs/ui-designs/tools/render_mockup.py` and `verify_designs.py` docstrings, and the templates in
   `docs/ui-designs/tools/template/` — the record shape you are filling in.
2. The XAML of the surface, every data template it uses, and `Styles/*.xaml`.
3. The view model and its commands; the service and stored procedure behind each load.
4. `Strings/en-us/Resources.resw` — every label on the screen.
5. The workflow digest, read-only:
   `python docs\diagrams\tools\diagram_context.py --workflow <<WORKFLOW>> --sheet <<SHEET>>`.

## 3. Analysis instructions

Establish and write down:

- **The exact screen.** Which page, which step, which pane state. A shell page with the pane expanded and
  a step inside a wizard are different subjects.
- **The control inventory, in visual order.** Type, label resource key, enabled state, position — read
  from the XAML, not from the diagram.
- **The real data state.** Which fields carry a value, which are empty, and why. An empty picture on a
  card usually means the plant picture share was unreachable, not that the part has no picture; that
  belongs in the caption, not hidden.
- **The current defects.** The proposal exists because something is wrong; name each one and where it
  shows.
- **What a static image cannot show.** Tooltips, focus rings and flyouts are not in a screenshot; say so
  rather than drawing them.

## 4. Generation instructions

### Path A — capture the running application (`before.kind: "capture"`)

1. Start and drive the app, one action per call:
   `pwsh -File scripts\ui-automation\ui-drive.ps1 -Action launch`, then `-Action signin`, then the
   clicks that reach the surface.
2. Wait for the page to settle with `-Action wait-for-id` against a stable anchor, not a fixed sleep.
3. Capture: `pwsh -File scripts\ui-automation\capture_app_window.ps1 -Maximize`. It polls for a non-zero
   main window handle, because the shell appears only after startup validation against the store, which
   has measured longer than nine seconds.
4. Save the capture as `docs/ui-designs/<<WORKFLOW>>/<<SCREEN>>/<<REFACTOR>>-before.png`. It must be at
   least 900 × 500; do not upscale it to 1600 × 1000 to match the mockups — the verifier accepts the
   real capture size.
5. In the design record set `before.kind` to `"capture"`, `captured_at` to the ISO timestamp from the
   capture, `capture_command` to the exact command you ran, and **leave `before.mockup` out**. A capture
   has no mockup.
6. No state may apply to `before` when the before is a capture — the verifier rejects it.

### Path B — reconstruct from the XAML (`before.kind: "reconstruction"`)

Use only when the application cannot run. It is a reconstruction and says so.

1. Copy the template shape: `before.mockup` is
   `mockups/<<REFACTOR>>-before.html`, and that file lives beside the record.
2. Build the mockup from the XAML using the shared stylesheet
   (`<link rel="stylesheet" href="../../../tools/mockup/mockup.css">`) and the classes it already
   provides — `.titlebar`, `.shell`, `.nav`, `.page`, `.page-header`, `.page-title`, `.card`, `.field`,
   `.input`, `.hint`, `.action-bar`, `.button`, `.infobar`, `.tile`, `.stamp`.
3. Keep the required banner in the mockup: `BEFORE — CURRENT IMPLEMENTATION`, with a `.provenance` line
   that literally contains the word **reconstruction** and names the XAML — for example
   `reconstruction from Module_Waitlist/Views/WaitlistViewPage.xaml`. The verifier warns when that word is
   absent, because a reader must not mistake the image for a screenshot.
4. Reproduce existing faults faithfully — a clipped label stays clipped, a misaligned row stays
   misaligned. A tidy reconstruction is a false before.
5. Fill `before.source_xaml` with the files you drew from; every path must exist.
6. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<<REFACTOR>>-design.json"`.
   The renderer resolves every output path itself, so a relative design path works; an absolute path is
   still the clearest in a script.

### Never

- Never use the image-generation model for this image.
- Never annotate the before with arrows, red boxes, circles or "NEW" badges. Explain in the proposal and
  the caption. Numbered callouts belong to the after mockup, not this one.
- Never declare states on a capture.

## 5. Visual standards

- Full window: title bar, navigation pane, page content, and the cached-data notice row when it is
  showing.
- Light theme unless the proposal is about theme behaviour.
- The mockup uses the tokens in `mockup/tokens.css` — text `#1b1b1b` / `#5d5d5d`, cards `#ffffff` on a
  `#f3f3f3` layer, strokes `#e3e3e3`, dividers `#e8e8e8`, accent `#005fb8`, card radius 12.
- Type 24 / 16 / 12 (`--font-large`, `--font-medium`, `--font-small`); nothing smaller than 12.
- Space from the token scale (2, 4, 8, 10, 12, 16, 20, 24, 32, 36); page padding 32, 24, 32, 20.
- A capture keeps its own pixels. Screenshot accent is the capturing machine's accent — record it.

## 6. Accuracy requirements

- Every visible string appears character-for-character in `Strings/en-us/Resources.resw`.
- Every visible control exists in the XAML. No additional buttons, badges, filters, columns or counts.
- Values are real data from the running app, or clearly representative reconstruction values from the
  domain: work centres `100-03`; parts `MMC0001000`, `MMF500`, `FGT00023`; categories `Pickup`,
  `Deliver`; hopper sizes `Small`, `Medium`, `Large`; statuses `Waiting`, `In Progress`, `Done`,
  `Cancelled`.
- Disabled and enabled states are captured as they are, not normalised.
- A defect stays visible. If an action is not implemented, the disabled placeholder is what the before
  shows and the proposal says so.
- The record says capture or reconstruction, and a reconstruction names its source XAML.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Capture size | its own pixels, at least 900 × 500, never upscaled |
| Reconstruction size | 1600 × 1000 (`wide`), plus 1280 × 800 and 900 × 500, produced by the renderer at scale 1 |
| Outputs | `<<REFACTOR>>-before.png`, and for a reconstruction the per-state, per-viewport files the renderer writes |
| Record | `<<REFACTOR>>-design.json` with `before`, `after`, `states`, `elements`, `workflow_refs`, `design_system` |
| Renderer | `docs/ui-designs/tools/render_mockup.py` (any `--design` path) |

## 8. File naming

Given `docs/ui-designs/<<WORKFLOW>>/<<SCREEN>>/`:

```
<<REFACTOR>>-design.json
mockups/<<REFACTOR>>-before.html        (reconstruction only)
<<REFACTOR>>-before.png                 canonical before
<<REFACTOR>>-before-default-wide.png    etc., written by the renderer
<<REFACTOR>>-render.json
```

The renderer names the files; do not rename them afterwards, and do not hand-write a PNG.

## 9. Validation checklist

1. **Record.** `python docs\ui-designs\tools\verify_designs.py --design "<absolute path>\<<REFACTOR>>-design.json"`
   reports no error about `before`.
2. **Kind.** A capture has `captured_at`, a `capture_command` and no `mockup`; a reconstruction has a
   mockup, `source_xaml` entries that exist, and the word "reconstruction" in the banner.
3. **Text.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   (Windows PowerShell 5.1) and compare with `Resources.resw`. Words that differ are failures; a `·` or a
   `1` read as `I` is not.
4. **Controls.** Compare the capture against `ui-drive.ps1 -Action dump`, or the reconstruction against
   the XAML. Different order, count or label is a failure.
5. **No proposal leakage.** Nothing from the proposal appears in the before.
6. **Size.** A capture is not upscaled; a reconstruction's files are exactly the declared viewports.
7. **Readability.** Open at 100%; nothing unreadable and nothing clipped beyond what the app clips.

## 10. Acceptance criteria

- The image is a capture of the running application, or a reconstruction that says it is one.
- Every visible string and control traces to a file in this repository.
- The current defects the proposal is about are visible, not hidden.
- The record validates, and the canonical `<<REFACTOR>>-before.png` exists at a legal size.
- A reviewer can open the XAML and the image side by side and find nothing in one that is not in the
  other, except what the caption explains.
