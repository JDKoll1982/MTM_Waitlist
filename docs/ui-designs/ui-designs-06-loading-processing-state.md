# Prompt 6 — Loading / processing UI state

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **loading** state of a screen in a QOL design record for the MTM Waitlist
Application: WinUI 3 on .NET 10, Windows App SDK 2.4.0, `CommunityToolkit.Mvvm` 8.4.2.

---

## 1. Purpose

Show what the person sees between pressing an action and the result arriving, and prove the screen says
work is happening rather than looking frozen. The image names a specific operation and attaches a specific
indication to it — not a generic spinner pasted on a page.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — the design record.
- `<<XAML>>`, `<<VIEWMODEL>>`.
- `<<OPERATION>>` — the command (`SubmitAsync`, a save, a reload raised by a status change), the service
  and the stored procedures behind it.
- `<<DURATION_KIND>>` — `indeterminate` unless the code computes a fraction. No write in this application
  publishes a percentage, so `determinate` needs a citation.
- `<<MESSAGE>>` — the status wording resource key, if one exists.
- `<<ORIGIN>>` — `after` mockup, or `before` reconstruction.

Read before generating:

1. The record and the after mockup.
2. The command's busy property and what the XAML binds: `ProgressRing`, `ProgressBar`, a disabled
   `Button`, an overlay.
3. Whether the rebuild runs on pool threads. In this application the UI thread keeps pumping, so a long
   rebuild shows as **content that has not changed yet**, not as an unresponsive window. That decides
   whether the image shows a ring or the old content still standing.
4. `Strings/en-us/Resources.resw` for the status text. No string means proposed wording, labelled as a
   proposal.
5. `scripts/ui-automation/measure-ui-responsiveness.ps1` if you need to prove the window is answering.

## 3. Analysis instructions

Record before drawing:

- **Which operation this is.** "Saving the request" and "re-reading the list" are different waits with
  different expected times — one may be 50 ms, the other several seconds.
- **Whether a person is blocked.** If the primary action must not be pressed twice, it is disabled. If the
  page is still readable and the list is merely stale, it is not greyed out.
- **What the indication is attached to.** A ring in the pressed button, a ring in the reloading section,
  or a bar at the top — the smallest surface that matches the wait. No modal spinner over a screen the
  person can still use.
- **What the indication does not know.** No percentage, no time remaining, no "step 2 of 5" unless the
  code tracks it.
- **The state that follows.** The image is only honest if that state exists.

## 4. Generation instructions

1. Add the anchor `<span class="state" id="state-loading"></span>` and put the busy content on
   `.when-loading`. The record name is `state-loading` — the anchor id with its prefix — because the
   renderer loads `#state-loading` and the stylesheet targets it. The stylesheet also carries
   `.hide-when-loading`, for content that must not show while busy.
2. Register it: `{"name": "state-loading", "applies_to": ["after"]}`.
3. Draw the indication the app uses:
   - a ring inside or beside the action that was pressed, with the status text next to it;
   - the primary action in `.button.disabled` with its size and position unchanged;
   - the rest of the page in its normal tokens.
4. If a cancel command exists, show it; if it does not, do not invent one.
5. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>\<refactor>-design.json"`.
6. Confirm `<refactor>-after-state-loading-wide.png` differs from `default`.

## 5. Visual standards

- Tokens from `tokens.css`: ring in the accent `#005fb8`; text `#1b1b1b` / `#5d5d5d`; disabled control as
  the platform's disabled look, not a hand-made grey.
- Ring size 20, 24 or 32 epx with a 4 epx gap to its label; the label never sits under the ring.
- The disabled action keeps its size and position: a button that shrinks when disabled makes the page
  jump, and the image would show that jump as design.
- Everything else stays visually normal — no global dimming, no grey text.
- Body 16, title 24; nothing below 12.

## 6. Accuracy requirements

- The operation is one the application performs, with a real command and service behind it.
- The indication is the control the XAML uses. If the app has no busy indication for this operation, the
  image shows that truth — the pressed button's normal state and the unchanged content — and the proposal
  records the gap.
- No percentage, countdown or step counter the code does not compute.
- Status wording is from `Resources.resw`, or clearly marked as proposed.
- The image does not suggest a frozen application. If the capture shows an unchanged screen during a long
  rebuild, the proposal says the window was still answering and cites
  `measure-ui-responsiveness.ps1` or the elapsed time.
- No "cancelling" affordance unless a cancel command exists.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Sizes | 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 |
| Files | `<refactor>-after-state-loading-<viewport>.png` |
| Record | `state-loading` in `states[]` |
| Content | one operation per image |
| Renderer | `render_mockup.py` (any `--design` path) |

## 8. File naming

```
<refactor>-after-state-loading-wide.png
<refactor>-after-state-loading-medium.png
<refactor>-after-state-loading-minimum.png
```

## 9. Validation checklist

1. **State applied.** The loading files differ from `default` and from the other states.
2. **Operation exists.** The command, service and procedure are real and reachable from this screen.
3. **Indication matches the code.** The ring, bar or disabled control is what the XAML binds.
4. **No invented progress.** No percentage or countdown unless computed.
5. **Layout stability.** The page did not move when the state changed.
6. **Wording.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   (Windows PowerShell 5.1) and `Resources.resw`; proposed wording is labelled as proposed.
7. **Verify.** `verify_designs.py --design "<absolute path>\<refactor>\<refactor>-design.json"` is clean.

## 10. Acceptance criteria

- A reader understands which operation is running and that the screen has not frozen.
- Every element of the state is one the application produces.
- Nothing promises a completion estimate the app cannot make.
- The page around the indication is unchanged and still usable to the extent the app allows.
- The state renders at all three window sizes and the verifier is clean.
