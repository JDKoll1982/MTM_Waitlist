# Residue: what this change found and deliberately left

Two items were discovered while verifying the corrections. Both are real drift. Neither is in the approved
scope of this feature, so both are recorded here rather than fixed, and both are named in the chat summary so
the next reader can decide.

## R1. A generated PDF still embeds another workstation's path

`specs/015-generic-wc-ui-shell/BREAKDOWN.pdf` contains ninety `file:///C:/Users/jkoll/...` links, all pointing
at `specs/015-generic-work-center-request/spec.md`, a folder that was later renamed to
`specs/015-generic-wc-ui-shell/`.

- The source it was rendered from, `specs/015-generic-wc-ui-shell/BREAKDOWN.md`, contains no such path, so the
  links were written into the PDF at generation time by the docs pipeline.
- Fixing it means regenerating the PDF with `scripts/docs/generate-docs.ps1`, which is a build-output change
  rather than a documentation edit.
- No editable file under `docs/` carries an absolute profile path any more, which is the scope criterion this
  feature set itself.

## R2. A live memory note points at a retired checklist

`.github/memories/repo/infor-visual-disposition.md` line 5 gives its checklist as
`WeekendProject/PromptFiles/14-57%-Phase2-UnifiedWaitlistCard.md`. The `WeekendProject` folder was deleted, so
the pointer resolves nowhere. The line exists to tell a reader where the long-form research lives, and the
retired half of it sends them to a file that is gone.

Left as written because FR-001 covers the always-applied spec-kit instruction, and this note was not among the
approved corrections. The one-line fix is to drop the checklist half of that line, or to mark it retired in the
same way the drift ledger marks deleted artefacts.

## R3. The diagrams tool manifest still declares ten documents

`docs/diagrams/tools/manifest.json` names ten documents and eight `spec-diagram-*.json` files that no longer
exist. The three reader-facing diagram documents were corrected; the machinery behind them was not, by
decision (see the spec's Non-goals). A reader who follows the README's rebuild commands will hit missing
specs before anything else.
