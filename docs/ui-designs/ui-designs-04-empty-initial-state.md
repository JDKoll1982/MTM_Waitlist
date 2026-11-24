# Prompt 4 — Empty / initial UI state

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **empty or initial** state of a screen in a QOL design record for the MTM Waitlist
Application: WinUI 3 on .NET 10, Windows App SDK 2.4.0, `CommunityToolkit.Mvvm` 8.4.2.

---

## 1. Purpose

Show the starting frame honestly: what the person is asked for, what is not yet available, and what the
screen says when there is nothing to show. Empty space is easy to draw and hard to live in — the image must
show the real starting state, including controls that are disabled until the person supplies something.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — the design record.
- `<<XAML>>` / `<<CONTROL>>`, `<<VIEWMODEL>>`.
- `<<ENTRY_POINT>>` — how the person arrives: a navigation item, a card action, a dialog's first step.
- `<<EMPTY_REASON>>` — **initial** (nothing filled in yet), **no selection** (a list exists, nothing
  chosen), **no data** (the read returned nothing), or **unavailable** (a read failed). They are not
  interchangeable.
- `<<ORIGIN>>` — `after` mockup, or `before` reconstruction when the same state is shown on both sides.

Read before generating:

1. The record and `mockups/<refactor>-after.html`.
2. The XAML: `PlaceholderText`, `Header`, default `SelectedIndex`, and each control's initial enabled
   state.
3. The view model: what it sets before `OnNavigatedTo` finishes, and which commands start disabled
   (`CanExecute` / `[RelayCommand(CanExecute = …)]`).
4. Where required-ness is enforced — view model, service, or stored procedure.
5. `Strings/en-us/Resources.resw` for every placeholder, empty message and hint.
6. `Assets/Placeholders/waitlist-empty-state.png` if the surface uses it.
7. The workflow digest for the step.

## 3. Analysis instructions

Decide and record, before drawing:

- **Which kind of empty this is.** An initial form, a list with nothing selected, a genuinely empty list
  and an unavailable read look different and say different things. An unavailable read drawn as "no
  requests" is the one mistake this state exists to prevent.
- **The required-versus-optional story.** If the app marks required fields, use its marking. If it marks
  nothing until submit, invent no asterisks.
- **What is disabled and why.** A `Save` needing a selected press, a `Continue` needing an answer, an
  action with no row: whatever `CanExecute` says, the image shows.
- **The initial value of each control** — no selection, a default number, an off switch, a filter pre-set
  by the shell.
- **What the screen says about itself.** Only the empty message the app really shows. If there is none,
  the image shows the bare screen and the proposal records that as a finding.

## 4. Generation instructions

1. Add the anchor to the after mockup: `<span class="state" id="state-empty"></span>`, and hang the empty
   content on elements carrying `.when-empty`. The name in the record is `state-empty` — the anchor id
   with its prefix — because the renderer loads the page at `#state-empty` and the stylesheet targets
   `#state-empty:target`. `empty` alone renders the default page and does nothing.
2. Register it in `states[]` as `{"name": "state-empty", "applies_to": ["after"]}`, unless the before
   reconstruction shows the same state.
3. Use the stylesheet's own empty affordances: `.infobar` for a message the app shows, `.field .input`
   with its placeholder text, `.button.disabled` for a guarded action. Keep the page's
   `.page-header`, padding and card geometry — an empty state is not a blank window.
4. If the app draws `Assets/Placeholders/waitlist-empty-state.png`, place it at its real size, centred,
   with the message beneath.
5. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>\<refactor>-design.json"`.
6. Confirm `<refactor>-after-state-empty-wide.png` differs from `<refactor>-after.png`.

Do not annotate. Do not pre-fill a field "for context". Do not show an error.

## 5. Visual standards

- Tokens from `mockup/tokens.css`; placeholder text in `--text-secondary` `#5d5d5d`, never so faint it
  fails 4.5:1 against the input's fill.
- Type 24 / 16 / 12; nothing below 12. Space from the token scale; card radius 12.
- Disabled controls use the platform look the tokens already carry — reduced-contrast fill and text, not
  a hand-made grey.
- Empty space is layout: the same column width, section rhythm and card geometry as the populated state.
- A centred empty-state artwork is not stretched and not recoloured.

## 6. Accuracy requirements

- Every placeholder string, header and hint comes from `Resources.resw`. Placeholder text is secondary and
  never looks like a value.
- No validation message, no red field, no error `InfoBar`. Nothing has been submitted, so nothing is
  wrong yet.
- No disabled control for a guard the code does not implement; no enabled control the code blocks.
- An empty list draws the app's own message or artwork. If the app has none, the image shows none and the
  proposal records the gap.
- An **unavailable** read is drawn as unavailable — the store-unavailable wording with last attempt and
  retry — never as "no requests".
- The proposal names which of the four empty kinds this image shows.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Sizes | 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 |
| Files | `<refactor>-after-state-empty-<viewport>.png` |
| Record | `state-empty` in `states[]`, `applies_to` naming the variant(s) |
| Renderer | `render_mockup.py` (any `--design` path) |

## 8. File naming

```
<refactor>-after-state-empty-wide.png
<refactor>-after-state-empty-medium.png
<refactor>-after-state-empty-minimum.png
```

If the before reconstruction shows the same state, the renderer also writes
`<refactor>-before-state-empty-*.png`. Do not rename any of them.

## 9. Validation checklist

1. **State applied.** The empty files differ from `default`; if they are byte-identical, the anchor name
   is wrong.
2. **No premature validation.** No error styling, no red text, no error `InfoBar` anywhere.
3. **Initial values.** Every control matches the XAML and the view model's constructor.
4. **Enabled states.** Every button matches its `CanExecute` guard.
5. **Placeholders.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   (Windows PowerShell 5.1) and compare with `Resources.resw`; placeholder colour is secondary.
6. **Empty kind.** The proposal's claim matches the screen: initial, no selection, no data or unavailable.
7. **Verify.** `verify_designs.py --design "<absolute path>\<refactor>\<refactor>-design.json"` is clean.

## 10. Acceptance criteria

- A person dropped into this screen would see exactly this.
- Nothing implies a problem before the person has done anything.
- Every control's enabled state and initial value is what the code produces.
- "Nothing here yet" cannot be confused with "the read failed" — the image and the proposal agree on
  which one it is.
- The state renders at all three window sizes and the verifier is clean.
