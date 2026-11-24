# Prompt 7 — Success / confirmation UI state

Copy this whole file into the task. Replace the `<<…>>` placeholders in section 2.

You are producing the **success** state of a screen in a QOL design record for the MTM Waitlist
Application: WinUI 3 on .NET 10, Windows App SDK 2.4.0, `CommunityToolkit.Mvvm` 8.4.2.

---

## 1. Purpose

Show the person that the thing they did actually happened, what it was, and what they can do next. A
confirmation is a claim about the world — "the request is in the queue", "the setup is saved" — so the
image may only make a claim the application can substantiate from the store's response.

## 2. Required inputs

- `<<WORKFLOW>>`, `<<SCREEN>>`, `<<REFACTOR>>` — the design record.
- `<<XAML>>`, `<<VIEWMODEL>>`.
- `<<OPERATION>>` — the command that ran and what it wrote.
- `<<EVIDENCE>>` — the signal the app treats as success: a returned row count, an identifier, a state the
  view model switches to. "No exception" is not evidence of a write.
- `<<NEXT_ACTIONS>>` — the actions the app really offers afterwards, and where they lead.
- `<<PRESENTATION>>` — the one the code uses: a success `InfoBar`, a `ContentDialog`, an in-page result
  panel, a toast.
- `<<ORIGIN>>` — `after` mockup, or `before` reconstruction.

Read before generating:

1. The record and the after mockup.
2. The command's completion path: what it awaits, what it does with the result, what it raises.
3. The stored procedures involved. Where a save is more than one call — the setup save runs four
   procedures with no enclosing transaction, and the generic flag is written afterwards — the confirmation
   must not promise more than that sequence guarantees.
4. `Strings/en-us/Resources.resw` for the confirmation wording and button labels.
5. The workflow digest for the submit sheet.

## 3. Analysis instructions

Record before drawing:

- **What succeeded, precisely.** Which procedure returned, which record now exists, which status the
  request moved to. "Saved" without an object is not a confirmation.
- **Where the person is taken.** A dialog over the wizard, a return to the list with the new row visible,
  or the detail page of what was created. The image shows that screen.
- **What can go wrong next.** If the row is in the store but the list has not reloaded, the image does not
  show the row in the list. If the picture share is unreachable, the new row shows the placeholder and the
  proposal says why.
- **Which actions are real.** A "Print" that does not exist is not offered as a next action.
- **Whether the app confirms at all.** If the code writes and silently reloads, the truth is a reloaded
  list, and that is what the image shows — with the proposal naming the changed row.

## 4. Generation instructions

1. Add the anchor `<span class="state" id="state-success"></span>` and put the confirmation on
   `.when-success`. The record name is `state-success` — the anchor id with its prefix — because the
   renderer loads `#state-success` and the stylesheet targets it.
2. Register it: `{"name": "state-success", "applies_to": ["after"]}`.
3. Draw the presentation the app uses:
   - an `.infobar.success` with a `.title` naming the outcome and a line naming the object;
   - a `.dialog` layer only when the person must choose the next step, with the primary action the code
     performs;
   - an in-page result panel where the app replaces the form with its result.
4. Keep the screen that raised the confirmation visible behind it; that context is the point.
5. Render:
   `python docs\ui-designs\tools\render_mockup.py --design "<absolute path>\<refactor>\<refactor>-design.json"`.
6. Confirm `<refactor>-after-state-success-wide.png` differs from `default`.

## 5. Visual standards

- Success token triple from `tokens.css`: background `#dff6dd`, stroke `#0f7b0f`, text `#0b5a0b`. Text at
  4.5:1 against its background.
- The confirmation appears where the action was taken, inside the person's eye line — not at the top of a
  page they have scrolled away from.
- One primary action. A second action is a plain `.button`, not a second accent fill.
- The rest of the screen keeps its normal tokens; success does not dim the application.
- Body 16, title 24; nothing below 12.

## 6. Accuracy requirements

- The confirmation is only drawn for an operation whose evidence is named and real.
- The wording is the application's, from `Resources.resw`; a composed message is labelled a proposal.
- The object is named: request identifier, part, press, category — whatever the write created.
- Next actions exist and are wired. No disabled placeholder dressed as an action.
- Counts and identifiers match the data on the screen behind the confirmation.
- Nothing implies a notification, an email, a print or a downstream system update the application does
  not perform.
- If the save sequence is not atomic, the confirmation does not say "everything is saved" — it says what
  the code can honestly report, and the proposal records the caveat.

## 7. Technical requirements

| Item | Requirement |
| --- | --- |
| Format | PNG, 8-bit, sRGB |
| Sizes | 1600 × 1000, 1280 × 800, 900 × 500 at scale 1 |
| Files | `<refactor>-after-state-success-<viewport>.png` |
| Record | `state-success` in `states[]` |
| Content | one completed operation, with the screen that follows it |
| Renderer | `render_mockup.py` (any `--design` path) |

## 8. File naming

```
<refactor>-after-state-success-wide.png
<refactor>-after-state-success-medium.png
<refactor>-after-state-success-minimum.png
```

## 9. Validation checklist

1. **State applied.** The success files differ from `default` and from the other states.
2. **Evidence.** Name the write that was verified and how it is read back. If it was not read back, the
   image is a speculation, not a success state.
3. **Wording.** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>`
   (Windows PowerShell 5.1) and `Resources.resw`; the object is named in the message.
4. **Object consistency.** The identifier, part or press matches the data on the screen behind it and in
   the proposal.
5. **Next actions.** Each one is a real command; in the app, clicking it reaches the place the image
   implies.
6. **No over-claim.** No atomicity, notification or downstream effect the code does not have.
7. **Verify.** `verify_designs.py --design "<absolute path>\<refactor>\<refactor>-design.json"` is clean.

## 10. Acceptance criteria

- Everything the confirmation claims is verifiable in the store, and the proposal says how it was checked.
- The person knows what happened, to what, and what they can do next.
- No capability, notification or guarantee appears that the application does not have.
- The screen behind the confirmation is truthful — a new row that is in the list, a form reset the way the
  app resets it.
- The state renders at all three window sizes and the verifier is clean.
