# Prompt 3 — Normal / populated UI state

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **normal, populated** image of a screen in a QOL design record for the MTM
Waitlist Application: WinUI 3 on .NET 10, Windows App SDK 2.4.0, `CommunityToolkit.Mvvm` 8.4.2.

---

## 1. Purpose

Show what the screen looks like when it is working and full: which fields carry values, what the values
look like, how the controls behave with data in them. This is the image the empty, error and loading
states are compared against, and the one the implementation will be judged by.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — the design record this state belongs to.
- `<<XAML>>` / `<<CONTROL>>`, `<<VIEWMODEL>>` — the surface and what fills it.
- `<<DATA_STATE>>` — which rows and conditions, for example `one waiting row, one claimed row, one
  overdue row`.
- `<<ORIGIN>>` — `after` mockup, or `before` reconstruction when the record shows the same state on both
  sides of the comparison.

Read before generating:

1. The record `<refactor>-design.json` and the mockup `mockups/<refactor>-after.html`.
2. The XAML templates and the model types the rows bind to.
3. The view model's load path and the read procedures behind each field.
4. `Strings/en-us/Resources.resw` for every label and status word.
5. `docs/ui-designs/tools/mockup/tokens.css` and `mockup.css` — the tokens and classes you must use.
6. The workflow digest: `python docs\diagrams\tools\diagram_context.py --workflow <<WORKFLOW>> --sheet <<SHEET>>`.

## 3. Analysis instructions

Record before drawing:

- **Which fields a populated screen really shows**, and which stay empty even with data. A field the plant
  has no value for stays blank; do not fill it to look complete.
- **The relationships between values.** A part number implies its family (`MMC…` coil, `MMF…` flatstock,
  `FGT…` die, otherwise categorised); a category implies which answer steps exist; a claimed row has a
  handler and a wait state; an overdue row shows a negative time.
- **Status precedence.** On a waitlist card a handler-set status wins over `Overdue`, which wins over the
  lifecycle word. Draw the precedence the code implements.
- **What the controls do with data.** A populated `AutoSuggestBox` suggests; a populated list scrolls; a
  selected card is selected. Show the settled state, not mid-gesture.
- **What is representative and why.** If the screen cannot be produced on this machine, the values are
  representative and the proposal says so.

## 4. Generation instructions

1. `default` is the state name for this image and it must exist for both variants. It is the page with
   **no** anchor targeted, so it is the markup as it stands outside every `.when-*` block.
2. Populate the markup with the data in section 6 — real labels, real shapes, the classes the stylesheet
   provides (`.card`, `.tile`, `.badge`, `.field .input`, `.action-bar`).
3. If the same state also appears in the before reconstruction, put the matching content in
   `mockups/<refactor>-before.html` so the comparison isolates the design change rather than the data.
4. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>-design.json"`.
   Any path works: the renderer resolves the output path before Edge sees it.
5. The renderer writes `<refactor>-after.png` from the `default` state at `wide`, and every
   `default`/viewport combination.
6. Inspect `<refactor>-after.png` and `<refactor>-comparison.png` for a misaligned column, a clipped value
   or a wrapped label the control would not wrap.

Do not annotate, do not add a callout for data, and do not show a spinner, focus ring or hover state that
nobody triggered.

## 5. Visual standards

- Tokens from `mockup/tokens.css`: text `#1b1b1b` / `#5d5d5d`, cards `#ffffff` / `#f7f7f7` on `#f3f3f3`,
  strokes `#e3e3e3`, dividers `#e8e8e8`, accent `#005fb8`.
- Type 24 / 16 / 12; nothing below 12. Space from 2, 4, 8, 10, 12, 16, 20, 24, 32, 36; card radius 12.
- Values align on their columns; labels align with labels. A populated screen is where a one-pixel
  misalignment is most visible — look for it.
- Long values truncate where the app truncates and wrap where it wraps. Do not re-wrap text the control
  does not wrap.
- One clock: the ring's time and the row's text agree.

## 6. Accuracy requirements

- Every label is from `Resources.resw`; every field exists in the XAML; every row shape exists in the
  template.
- Data is internally consistent and from this domain:

| Element | Valid examples |
| --- | --- |
| Work centre | `100-03`, `100-11`, `200-04` |
| Part | `MMC0001000` (coil), `MMF500` (flatstock), `FGT00023` (die), `ABC-1234` (categorised) |
| Category | `Pickup`, `Deliver` |
| Hopper size | `Small`, `Medium`, `Large` |
| Request status | `Waiting`, `In Progress`, `Done`, `Cancelled` |
| Handler status | a set status such as `Product Buried` |
| Time on a live row | `01:02`, and `-00:15` when overdue |
| Person | a full name, never an employee number on a card or detail row |

- No lorem ipsum, no "John Doe", no invented item codes outside those formats.
- Only states the code produces. If there is no "Paused" status, no row is paused.
- If a picture shows the shared no-image artwork, the proposal says whether that is "no picture" or "the
  share was unreachable".

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Sizes | every declared viewport: 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 |
| Canonical | `<refactor>-after.png` = `default` at `wide` |
| Renderer | `render_mockup.py` (any `--design` path) |
| Record | `default` listed in `states[]` with `applies_to` including `after` |
| Content | one screen, one state, no montage |

## 8. File naming

The renderer writes these names in the design folder; do not rename them.

```
<refactor>-after.png                    default at wide, the canonical image
<refactor>-after-default-wide.png
<refactor>-after-default-medium.png
<refactor>-after-default-minimum.png
<refactor>-comparison.png
```

## 9. Validation checklist

1. **Bytes differ.** `default` must differ from every other state's file. If it matches `state-empty`
   byte for byte, the state anchors are wrong.
2. **Verify.** `python docs\ui-designs\tools\verify_designs.py --design "<absolute path>\<refactor>-design.json"`.
3. **Text.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   over `<refactor>-after.png`, compared with `Resources.resw`.
4. **Field set.** Against the XAML: no field added, none missing, none reordered.
5. **Data coherence.** Part ↔ family, category ↔ steps, claimed ↔ handler, overdue ↔ negative time, ring
   ↔ status word.
6. **Frame.** Pane state, page padding and card geometry identical to the other state images.

## 10. Acceptance criteria

- A reader who has never seen the screen understands what it shows when it is full.
- Nothing contradicts the XAML, the view model or the read procedures.
- The data could plausibly have come out of this plant's store.
- Every other state image for this design can be laid beside it without a shift in frame or typography.
- The verifier is clean for this state at all three window sizes.
