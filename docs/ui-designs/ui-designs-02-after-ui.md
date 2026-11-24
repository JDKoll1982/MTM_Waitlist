# Prompt 2 — After UI (the proposed interface)

> **Check this before starting.** If the change is wording only — a `Resources.resw` value, a new key for
> a control that already exists, or a string in a view model — and no control is added, removed, moved,
> reordered, hidden or resized, stop here. That change is recorded as
> `<refactor>-text-change.md` (sections A–E in text, no mockup, no render, no approval hash) per
> `docs/ui-designs/ui-designs-guide.md` §2, not as a design. Run this prompt only when the XAML or the
> code-behind changes an element, or when the new wording reflows at the surface's own width.

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **after** half of a QOL design record for the MTM Waitlist Application: a WinUI 3
desktop app on .NET 10 (`net10.0-windows10.0.19041.0`), Windows App SDK 2.4.0,
`CommunityToolkit.Mvvm` 8.4.2, `WinUIEx` 2.9.2, `DevWinUI` 10.8.0. The image shows an interface that
**does not exist yet** and that a developer must be able to build from the real control set.

---

## 1. Purpose

Let a reviewer decide on the proposal and let a developer implement it without guessing. The image shows
the improvement the proposal argues for, at the fidelity of the actual platform, in the states a person
will really encounter. A decision that is not visible has not been designed; something visible that no
decision authorises has been invented.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — as in prompt 01.
- `<<DECISIONS>>` — the numbered decisions the design implements, from the proposal in `docs/research/`.
- `<<BEFORE>>` — the before image and the record that produced it.
- `<<PROBLEM>>` — the usability problem, in one sentence, with where it shows.
- `<<STATES>>` — which interaction states this change touches; the rest are covered by prompts 03–07.
- `<<SHEET>>` — the diagram sheet the screen appears on.

Read before designing:

1. `docs/ui-designs/tools/render_mockup.py` and `verify_designs.py`, and
   `docs/ui-designs/tools/template/{design.json,after.html}` — the record and mockup you are filling in.
2. The current XAML of the surface and its templates, the view model and its commands.
3. `Strings/en-us/Resources.resw` — the existing wording.
4. `Styles/*.xaml` and the merged dictionaries: the page styles, `SetupSectionCardStyle`,
   `SetupItemCardStyle`, `SetupActionBarStyle`, the dialog styles — the app's real visual identity.
5. The navigation that places the surface: `ShellPage.xaml`, `PageService`, `NavigationService`.
6. The package references in `MTM_Waitlist.csproj`, so no control is proposed that the project cannot
   compile.
7. The workflow digest: `python docs\diagrams\tools\diagram_context.py --workflow <<WORKFLOW>> --sheet <<SHEET>>`.

## 3. Analysis instructions

Write these down before drawing:

- **The problem, located.** Which element fails the person. The design answers that element, not a
  general impression.
- **What must not change.** Business rules, required fields, role gates, number of steps, the stored
  procedure behind each action. A redesign that drops a rule is a behaviour change and needs its own
  decision.
- **The element inventory.** One row per region for `elements[]`: the design element, the WinUI 3 control
  that implements it (`GridView`, `SettingsExpander`, `InfoBar`, `AutoSuggestBox`, `TabView`, …), and the
  existing file it lives in or replaces. If you cannot name the control and the file, it is not
  implementable.
- **The change list.** Each visual difference from the before, mapped to a decision number, phrased as a
  callout for the comparison image.
- **The interaction.** Tab order, initial focus, what scrolls, what happens at 1280 × 800 and
  900 × 500, and whether any control becomes unreachable by keyboard.
- **The states.** Empty, populated, error, loading and success must fit the same frame. If the layout has
  no room for an error, the design is wrong.

## 4. Generation instructions

### The record

Fill in `data/<refactor>-design.json`:

- `workflow`, `workflow_variant`, `screen`, `refactor`, `title`, `audience`.
- `window.viewports` — for a screen that fills the maximised window, keep the three required sizes:
  `wide` 1600 × 1000, `medium` 1280 × 800, `minimum` 900 × 500, scale 1. For a launch stage
  (`SplashPage`, `MachineSetupPage`, `SignInPage`, `BlockedStatePage`), declare `kind: "stage"` and one
  viewport named `stage` whose size is the one `MainWindow.xaml.cs` gives that stage (780 × 640 for
  sign-in, 520 × 560 for machine setup, 480 × 420 for the blocked state); the verifier checks the number
  against the code and rejects the three desk sizes there.
- `after.mockup` — `mockups/<refactor>-after.html`.
- `states` — one entry per state you render, `applies_to: ["after"]` unless the before is a
  reconstruction that shows the same state.
- `callouts` — one sentence per numbered callout, in the order the numbers appear.
- `elements` — design element → WinUI control → existing code paths. Every path must exist.
- `workflow_refs` — `{ "sheet": N, "nodes": [...] }` with node ids that exist in that sheet's spec.
- `design_system.resources` — the XAML files the tokens come from; hashed at render time.

Write `<refactor>-proposal.md` beside the record, with the verifier's exact headings:

```
## A. Current State
## B. Proposed Improvement
## C. Visual Comparison
## D. Expected User Benefits
## E. Implementation Impact
```

and embed `<refactor>-before.png`, `<refactor>-after.png` and `<refactor>-comparison.png` by filename. The
verifier fails when a heading or an embedded image is missing.

### The mockup

1. Copy the template shape of `template/after.html`: banner `AFTER — PROPOSED QOL IMPROVEMENT` with a
   `.provenance` line saying it is a mockup, then `.titlebar`, `.shell`, `.nav`, `.page`. The mockup lives
   in the run's `mockups/` subfolder, so the shared stylesheet is `<link rel="stylesheet"
   href="../../../../tools/mockup/mockup.css">` — four levels up, not three.
2. Build the layout from the shared classes: `.page-header`, `.page-title`, `.page-subtitle`, `.row`,
   `.row.wrap`, `.card`, `.card.secondary`, `.tile`, `.tile.selected`, `.badge`, `.field`, `.field .label`,
   `.field .input`, `.field .hint`, `.infobar` (`.caution`, `.error`, `.success`), `.action-bar`,
   `.button`, `.button.primary`, `.button.disabled`, `.callout`, `.stamp`.
3. Declare every state you render as a hidden anchor, `<span class="state" id="state-<name>">`, and hang
   the state's content on a `.when-<name>` element. The `states[].name` in the record is **the anchor id
   with its prefix** — `state-empty`, `state-error`, `state-loading`, `state-success`, `state-no-job`,
   `state-disabled` — because the renderer loads the page at `#<name>` and targets `#state-<name>`. A name
   without the `state-` prefix renders the default page and the state silently does nothing.

   The shared stylesheet carries rules for those six words only. Any other state (`state-few`,
   `state-solo`, `state-overdue`) needs its own rule in the mockup's `<style>`, in the same shape:

   ```html
   <style>
     .when-few { display: none; }
     body:has(#state-few:target) .when-few { display: block; }
   </style>
   ```

   Toggle `display` from a class and never from an inline `style="display:…"`: an inline value wins over
   the state rule, so the state's content then shows in `default` too. That failure passes the renderer's
   duplicate check — every state still differs from the (wrong) default — so inspect the `default` image
   as carefully as the states.
4. Number the changes with `<span class="callout">1</span>` where the change is, and put the matching
   sentences in `callouts[]`. Those are the only annotations allowed on the design.
5. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>\data\<refactor>-design.json"`.
   The renderer resolves every output path itself, so a relative design path works here too.
6. Look at `<-refactor>-after.png` and `<-refactor>-comparison.png`. A label on a border, a clipped row, a
   collapsed column, or a callout pointing at nothing means it is not finished.

### What the image must contain

- The same surface and workflow as the before, with the improvement applied.
- Real labels and domain values, the same statuses and time formats the app uses.
- One accent-filled primary action per surface; everything else neutral.
- Enough chrome to place the screen: pane, title, page heading.
- Numbered callouts for the changes, matching `callouts[]`.

### What it must not contain

- Ad-hoc arrows, red boxes, sticky notes, "NEW!" badges, or a hand-drawn legend beyond the numbered
  callouts.
- Invented controls, fields, statuses, or a control the project cannot reference.
- A device frame, browser window, drop shadow or perspective.
- Light and dark in one image.

## 5. Visual standards

- The tokens from `mockup/tokens.css`: text `#1b1b1b` / `#5d5d5d` / disabled `#9a9a9a`; cards `#ffffff`
  and `#f7f7f7` on a `#f3f3f3` layer; strokes `#e3e3e3`; dividers `#e8e8e8`; accent `#005fb8` with white
  text on it; caution, error and success as the token triples.
- Type 24 / 16 / 12. Semi-bold for headings  600; no Bold, no Italic; nothing below 12.
- Space from 2, 4, 8, 10, 12, 16, 20, 24, 32, 36. Card radius 12, stroke 1. Page padding 32, 24, 32, 20.
  Row spacing 20, header gap 10, action gap 12.
- Shell: title bar 32, pane 320 expanded / 48 compact, window minimum 900 × 500, hit target 44.
- One surface level: no card inside a card — a divider instead.
- Icons from the app's existing glyph set at 16 or 20; no emoji, no mixed icon families.
- The image reads the same at 100% zoom and printed at page width.

## 6. Accuracy requirements

- Every element maps to a WinUI 3 control the project can build and to a real code path in `elements[]`.
- Every change maps to a decision number and appears in `callouts[]`.
- Every business rule in the workflow digest survives: same gates, same permissions, same required
  inputs, same outcomes.
- Existing wording is verbatim from `Resources.resw`. A newly proposed string is named as a proposal with
  a proposed resource key, in the proposal — never slipped into the mockup as though it shipped.
- No improvement the proposal does not ask for. A redesign is not a licence to tidy unrelated parts of
  the screen.
- A reconstruction's before and this after share one stylesheet, so the comparison isolates the change.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB, written by the renderer; the comparison also as a PDF |
| Sizes | 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 — every declared state at every size |
| Canonical | `pngs/<refactor>-after.png` (the `default` state at `wide`), `<refactor>-comparison.png` and `<refactor>-comparison.pdf` |
| Comparison PDF | US Letter landscape, 792 × 612 pt, one page per before/after grouping, the pair on top and the callouts as real text below at or above 9.4 pt |
| Renderer | `docs/ui-designs/tools/render_mockup.py` (any `--design` path); `--workflow` composes the workflow's own sheet |
| Record | `elements[]`, `workflow_refs`, `design_system.resources` all populated and resolvable |
| Text | real text in the markup, never an image or an SVG path |
| Accessibility | 4.5:1 body text; visible focus; 44 epx hit targets; nothing conveyed by colour alone |

## 8. File naming

```
docs/ui-designs/<workflow>/<screen>/<refactor>/
  <refactor>-proposal.md
  <refactor>-comparison.png           one row per before/after grouping
  <refactor>-comparison.pdf           one page per grouping
  mockups/<refactor>-after.html
  mockups/<refactor>-compare.html     generated
  pngs/<refactor>-after.png
  pngs/<refactor>-after-<state>-<viewport>.png
  data/<refactor>-design.json
  data/<refactor>-render.json
```

The renderer writes the PNGs, the comparison PDF and the render record. Do not rename them; the proposal
and the approval hash name them. Nothing but the proposal and the two comparison documents sits at the
run folder's top level.

## 9. Validation checklist

1. **Verify.** `python docs\ui-designs\tools\verify_designs.py --design "<absolute path>\<refactor>\data\<refactor>-design.json"`
   — no errors about images, elements, workflow refs or the proposal.
2. **States.** Every state named in the record produced a different PNG from `default`; a state whose file
   is byte-identical to `default` never applied, which means its anchor name is wrong. That check does not
   prove the opposite fault: open `default` itself and confirm the state-only content is absent — an
   inline `display` on that content leaves it visible in `default` while every state still differs.
3. **Text.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   over `<refactor>-after.png`, compared with `Resources.resw`. Invented or misspelled labels fail.
4. **Implementability.** For every region, name the control and the file. Anything you cannot name is
   redrawn.
5. **Decision traceability.** Every callout number has a sentence in `callouts[]` and a decision in the
   proposal; every decision is visible.
6. **Resize.** Open the `minimum` render; no clipped text, no horizontal scroll that the app would not
   have.
7. **Honesty.** The banner says "proposed", the provenance says "mockup, not a screenshot", and the design
   is not approved yet.

## 10. Acceptance criteria

- A developer can implement the screen from the mockup plus `elements[]`, using only controls the project
  already references.
- Every change is traceable to a decision, and every rule the workflow carries still holds.
- Every label either exists in `Resources.resw` or is declared as a proposed key in the proposal.
- The three required window sizes render, all states differ from `default`, and the verifier is clean.
- Rendering the design is not approval of it: the proposal is presented, and approval is recorded
  separately.
