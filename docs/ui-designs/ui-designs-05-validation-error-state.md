# Prompt 5 — Validation / error UI state

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **error** state of a screen in a QOL design record for the MTM Waitlist Application:
WinUI 3 on .NET 10, Windows App SDK 2.4.0, `CommunityToolkit.Mvvm` 8.4.2.

---

## 1. Purpose

Show how the application tells a person something is wrong, which field is wrong, and what to do about it.
The image is judged on one thing: could the person who sees this screen fix the problem without asking
anyone? A red banner with no name on it fails that test.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — the design record.
- `<<XAML>>`, `<<VIEWMODEL>>`.
- `<<RULE>>` — the exact rule, and where it lives: a view-model guard, a service check, or a stored
  procedure's `SIGNAL`.
- `<<MESSAGE>>` — the message string and its key in `Strings/en-us/Resources.resw`.
- `<<TRIGGER>>` — when the person meets it: on leaving a field, on pressing the primary action, or on a
  store rejection after submit.
- `<<ORIGIN>>` — `after` mockup, or `before` reconstruction.

Read before generating:

1. The record and the after mockup.
2. The validator or guard that enforces the rule — file and method, not a guess.
3. The message resource, character for character. A message that would expose a procedure, column or
   connection string is not something the app shows; do not put it on screen.
4. Which presentation the app uses: field-level text, an `InfoBar`, a `ContentDialog`, or a
   `TeachingTip`. Use the one in the code.
5. The workflow digest for the step, to see whether the failure returns the person to an earlier step.

## 3. Analysis instructions

Record before drawing:

- **One rule, one error.** Three errors are three rules, each named. No generic "something went wrong".
- **The scope the app uses.** The field itself (stroke plus message beneath), the section (an `InfoBar`
  above a group), or the page (a dialog). The smallest scope that names the problem.
- **The person's next action.** Which field to change, in what way, and what happens then. If the message
  does not say, the design is incomplete and the proposal says so.
- **The timing the app really uses.** Most flows validate on submit; the app does not paint fields red
  while the person is typing. Do not draw an error that could not have appeared yet.
- **Whether it is a validation error at all.** A failed store read is an unavailable state with a retry;
  a failed Infor Visual read is the cached-data notice at the foot of the shell. Neither is drawn as a
  validation error.

## 4. Generation instructions

1. Add the anchor `<span class="state" id="state-error"></span>` to the after mockup and hang the error
   content on `.when-error`. The record name is `state-error` (the anchor id with its prefix) — the
   renderer loads `#state-error` and the stylesheet targets it.
2. Register it: `{"name": "state-error", "applies_to": ["after"]}`.
3. Build the failed screen from the populated markup, with one rule failed:
   - field-level: add `.hint` text in the error colour beneath the field, or the stylesheet's error
     styling on the input; the control keeps its size so the layout does not jump;
   - section-level: an `.infobar.error` with a `.title` naming the problem and a line saying what to do;
   - blocking: the `.dialog` layer, with one primary action that returns to the field.
4. In the proposal, say whether the app moves focus to the first invalid control, and draw the focus only
   if it does.
5. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>-design.json"`.
6. Confirm `<refactor>-after-state-error-wide.png` differs from the default and from `state-empty`.

No modal dialog for a field-level rule, no toast for something the person must fix, no `TeachingTip` for
an error.

## 5. Visual standards

- Error token triple from `tokens.css`: background `#fde7e9`, stroke `#c42b1c`, text `#8e1b10`; on a light
  surface the message text is `#8e1b10` or `#c42b1c`, never a lighter red.
- Everything else on the page keeps its normal tokens. Only the failing element changes.
- Message text 12 or 16 (`--font-small` / `--font-medium`), 4 epx from its control, wrapping rather than
  truncating; 4.5:1 against its background.
- Focus ring, if drawn, is the 2 epx accent ring (`#005fb8`) on the control the app focuses.
- Body 16, title 24; nothing below 12.

## 6. Accuracy requirements

- The rule is real and enforced by real code; cite the file and method in the proposal.
- The message is the real resource string, unedited. If the app has no message for this rule, say so — do
  not compose one and present it as the application's wording.
- No validation the application does not perform: no email format, no password strength, no phone rule
  unless the code has one.
- The values that triggered the error stay visible, so a reader can see what failed.
- The rest of the screen stays truthful: a failed submit does not grey out the navigation, and a
  field-level error does not disable the page.
- No internal detail on screen — no procedure names, stack traces, connection strings or SQL.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Sizes | 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 |
| Files | `<refactor>-after-state-error-<viewport>.png` |
| Record | `state-error` in `states[]` |
| Content | one failed rule per state; a second rule is a second state or a second design |
| Renderer | `render_mockup.py` (any `--design` path) |

## 8. File naming

```
<refactor>-after-state-error-wide.png
<refactor>-after-state-error-medium.png
<refactor>-after-state-error-minimum.png
```

## 9. Validation checklist

1. **State applied.** The error files differ from `default` and from `state-empty`.
2. **Rule fidelity.** The rule exists in the cited file and would fire for the values shown.
3. **Message fidelity.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   (Windows PowerShell 5.1), then compare character for character with `Resources.resw`.
4. **Timing.** The error could have appeared at this point, not before.
5. **Correction path.** A reader can state what to change to clear it.
6. **Scope.** Only the failing element is marked.
7. **Category.** This is a validation error, not an unavailable store or a cached-data notice.
8. **Verify.** `verify_designs.py --design "<absolute path>\<refactor>-design.json"` is clean.

## 10. Acceptance criteria

- The reader knows which field is wrong and exactly what to do.
- Nothing invents a rule, a message or a capability.
- The presentation is the one the code uses, drawn with supported WinUI 3 controls and the shared tokens.
- The rest of the screen is unchanged from its normal state, so the error reads as an event, not a
  different page.
- The state renders at all three window sizes and the verifier is clean.
