# Method check — the mockup pipeline and the text check

**Date:** 2026-11-24
**Why:** the eight prompts describe commands that must actually run. The mockup pipeline was exercised
end to end once, on the repository's own template, so the guide is not documenting something untested.
**What was not produced:** no proposal image for any real screen. The design rendered here was a copy of
`docs/ui-designs/tools/template/`, kept out of `verify_designs.py` discovery, and deleted afterwards.

---

## What was run

The template design and both mockups were copied into a scratch folder under `docs/ui-designs/tools/`
(inside `tools`, so `verify_designs.py --all` skips it), the `refactor` name was set to `check`, and the
renderer was run from the repository root.

| Attempt | Command | Result |
| --- | --- | --- |
| 1 | `python docs\ui-designs\tools\render_mockup.py --design docs\ui-designs\tools\check\check-design.json` | **Failed**: `Edge produced no image for check-after.html (default, 1600x1000) within 60 s` |
| 2 | same, with the design path **absolute** | **Succeeded**: `rendered 15 image(s) for check at 3 window size(s)` |
| 3 | states named `default`, `no-job`, `error` | Rendered, but every state PNG was **byte-identical** to `default` — the states did nothing |
| 4 | states renamed `default`, `state-error` | `check-after.png` and `check-after-state-error-wide.png` have **different hashes**; the error `InfoBar` is visible |

## The two constraints this exposed — both fixed

**1. A relative `--design` path failed.** Edge is given `--screenshot=<path>`; when the design path was
relative, the output path was relative too, and Edge resolved it against its own working directory. Its log
said `Failed to write file docs\ui-designs\tools\check\check-after-default-wide.png: The system cannot
find the path specified. (0x3)`. The same command with an absolute path wrote the image immediately.

**Fixed:** `render_mockup.render_page` resolves every output path before Edge sees it, so a relative
`--design` path now renders. Verified 2026-11-24 by rendering from
`docs\ui-designs\workorder-new-request\work-center-selection\fewer-clicks-design.json`.

**2. A state name that was not the mockup anchor rendered `default` silently.** The renderer loads the page
at `#<name>`; the shared stylesheet reveals `.when-<word>` on `:has(#state-<word>:target)`. A state declared
as `error` produced `#error`, which matched no anchor, so the page rendered its default and the state's PNG
was a duplicate of `default`.

**Fixed, and now enforced twice.** `states[].name` must begin `state-` (the anchor id), rejected up front
with a message naming the rule; and `render_mockup.build` compares every state's PNG with `default` at the
same viewport and stops the run when they are byte-identical. The rendering test asserts the two differ. The
shipped `template/design.json` and both template mockups use `state-no-job` and `state-error`.

## What the successful run produced

- 12–15 PNGs per run depending on the state count: every declared state at 1600 × 1000, 1280 × 800 and
  900 × 500, plus `<refactor>-before.png`, `<refactor>-after.png`, `<refactor>-comparison.png` and
  `<refactor>-render.json`.
- Canonical size confirmed: `check-after.png` is exactly **1600 × 1000**.
- The comparison image is wider than two canonical images side by side, as the verifier requires.

## The text check

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ui-automation\ocr_png.ps1 -Path <png>` read
back every visible line of the rendered error state, including the `InfoBar` title and message, the card
data and the action labels.

Differences to expect, and not to treat as failures:

| Image | OCR | Cause |
| --- | --- | --- |
| `MTM Waitlist` in the title bar | `MTM waitlist` | case normalisation |
| `Sheet 1 of 5` | `Sheet I of 5` | `1` read as `I` |
| callout badges `1`, `2` | `O`, `0` | the badge is a filled circle |

Under `pwsh` 7 the script fails before recognition: the WinRT types (`Windows.Media.Ocr.OcrEngine`,
`Windows.Graphics.Imaging.BitmapDecoder`, `Windows.Storage.StorageFile`) do not resolve. Windows PowerShell
5.1 is required.

## What this does not prove

- **The capture path.** The application was not running, so `ui-drive.ps1`, `capture_app_window.ps1` and
  the settle waits were not exercised. Their behaviour is documented from
  `docs/scripts/scripts-automation-ui-automation.md` and the UI-automation instruction file.
- **The diagram pipeline.** `verify_pdfs.py` was not re-run. The print contract in prompt 08 is the one
  that script already enforces and that `docs/diagrams/tools/README.md` records.
- **Approval and the verifier's error paths.** `verify_designs.py` was not run against the scratch design,
  because a template without a proposal and an approval is expected to fail both checks.

## Cleanup

`docs/ui-designs/tools/check/` and the temporary Edge profiles were deleted after the run. Nothing from
this check is committed.

---

# Second run — startup QOL designs and the diagram rebuild

**Date:** 2026-11-24, same day, after the first check.
**What was run:** three design runs under `docs/ui-designs/startup-launch/` (two reusing the sign-in
stage, one on the machine-setup stage) and a rebuild of Diagram 5 in both variants.

## The design pipeline

| What | Result |
| --- | --- |
| `render_mockup.py` on a stage design | Rendered one viewport (`stage`) instead of three, and wrote `<refactor>-before/after.png` from that viewport — the canonical image of a launch stage is the stage size |
| `verify_designs.py --design` on each run | Clean for all three; each reported the expected "not approved yet" note |
| `verify_designs.py --all` | Clean for all four records, but the two `name-the-authority` runs print identical lines because only the file name is shown. Use `--json` when the report matters |

## What this run exposed, now written into the guide and the prompts

1. **The shared stylesheet knows six state words.** `state-few` and `state-solo` rendered nothing until the
   mockup carried its own `:has(#state-…:target)` rules. The guide's state section said the stylesheet
   "reveals `.when-<word>`", which is true only for the six.
2. **An inline `display` beat the state rule.** The set-up panel's wrapper carried `style="display:flex"`,
   so the panel showed in `default` as well as in the state, and the run still passed every duplicate
   check. Fixed by moving `display` into a class. The verifier compares states to `default`; it cannot
   tell that the default itself is wrong.
3. **The stylesheet path is four levels, not three.** `../../../tools/mockup/mockup.css` loads nothing from
   `mockups/`; prompt 01 carried the three-level path. A served page proved it: `fontFamily` came back as
   `"Times New Roman"`.
4. **`file:` is not available for inspection.** Playwright MCP refuses it, and serving the mockup's own
   folder leaves `../../../../tools/…` outside the served root. Serving the repository root works.
5. **A long banner costs a row.** At 780 px and again at 520 px the provenance line wrapped and pushed the
   page down, misaligning the before from the after.
6. **Wording has to be measured at stage widths.** A 513 px note in a 456 px column wrapped; candidates
   were measured with `PIL.ImageFont.truetype(<windir>\Fonts\segoeui.ttf, 14).getlength(text)` before one
   was chosen.

## The diagram rebuild

| What | Result |
| --- | --- |
| `dotgen.py <spec> <spec>` | Rebuilt both variants; no warnings once every flag had an edge |
| `render_sheets.py docs\diagrams\tools\manifest.json` | **Failed**: four non-startup specs are absent from this checkout, so the loop stopped on the first missing one |
| `render_sheets.py <subset manifest in %TEMP%>` | **Failed second**: spec paths are resolved against the manifest's folder, so bare names were looked for under `%TEMP%` |
| `render_sheets.py <subset manifest with absolute spec paths>` | **Succeeded** — `diagram-startup-launch.pdf` 3 pages, `-friendly.pdf` 2 pages |
| `verify_pdfs.py` | Every page 792 × 612 pt, nothing in the margin band, text 9.5–11.0 pt |
| `diagram_context.py --workflow startup-launch --strict` | Clean, and names both flags |
| `python -m unittest discover -s docs\diagrams\tools` | Fails in this checkout for the same missing specs; `test_diagram_context.py` asserts against the full set |

Layout findings from this run, now written into the instruction file: a `refactor` node needs an edge; its
lane decides the layout, because a flag in `ui` takes that lane's rank and drags edges across the sheet;
and `verify_pdfs.py` does not enforce the 12–16 shape ceiling, so the count is the author's to keep —
sheet 2 reached 17 nodes with a second flag and the two were folded into one.

## What this run does not prove

- **The capture path** was still not exercised; every BEFORE in these runs is a reconstruction.
- **Approval** was not recorded: all four records are deliberately unapproved.
- **The other four workflow diagrams** were not rebuilt, because their specs are not in this checkout.

---

# Third run — comparisons as documents, and a tree that cannot be nested

**Date:** 2026-11-24, after the second run.
**Why:** the comparison was one image showing `default` only, which hid every other before/after
grouping a design had; nothing produced a printable document; and the run folders had grown a pile of
loose records and renders at their top level.

## What changed in the tooling

| Change | Where |
| --- | --- |
| One comparison row per before/after grouping, not one row for `default` | `render_mockup.comparison_pairs` |
| `<refactor>-comparison.pdf`: 792 × 612 pt landscape, one page per grouping, the pair on top and the change as real text below | `render_mockup.write_comparison_pdf` |
| `--workflow <folder>`: one PNG and one PDF holding every design in a workflow, one row and one page each | `render_mockup.build_workflow` |
| The run folder keeps only the proposal, the comparison PNG and the comparison PDF at its top; mockups, PNGs and JSON records live in `mockups/`, `pngs/` and `data/` | `render_mockup.run_dir`, `PNGS_DIR`, `DATA_DIR` |
| A workflow is its own folder directly under `docs/ui-designs/` and is never nested; a design whose `workflow` disagrees with its folder is refused, by both tools | `build_workflow`, `verify.check` |
| The verifier reads the PDF back: page count, page size, an image on every page, the callouts present as text, and nothing below 9.4 pt | `verify.check_comparison_pdf` |

## What the run exposed, now written into the guide and the skill

1. **The comparison compared one state.** A three-state design produced a single row; the other two
   groupings existed only as loose per-state PNGs. Both the per-design sheet and the PDF now carry one row
   and one page per grouping.
2. **The hyphen is not the hyphen.** Segoe UI's ToUnicode map returns `-` as `U+2010`, so `Sign-in`
   extracts as `Sign‑in` and a plain substring test said the PDF was missing a change it plainly held. The
   verifier folds the dashes before comparing.
3. **The `file://` page and the served page disagree.** Already recorded in the second run; the mockup
   paths now sit one level deeper, so the stylesheet reference is still four levels up and a served page
   is the only honest way to look at one.
4. **The unit tests depended on a checkout this machine does not have.** The fixtures named
   `workorder-new-request`, whose spec is absent, so 17 cases failed on a missing spec rather than on the
   rule they test. They now use `startup-launch`, whose spec is present, and all 29 pass.

## Verified

- `render_mockup.py --design` for all four runs: every state rendered, one comparison row per grouping,
  one PDF page per grouping.
- `render_mockup.py --workflow docs\ui-designs\startup-launch`: one PNG with four rows, one PDF with four
  pages, `data/startup-launch-comparison.json` recording the PNG hashes it was composed from.
- `verify_designs.py --all`: four designs and the workflow sheet reported OK.
- `python -m unittest docs.ui-designs.tools.test_designs`: 29 tests pass.

## What this run does not prove

- **The other four workflow diagrams** are still absent from this checkout, so
  `test_diagram_context.py` still fails on missing specs — pre-existing, and untouched by these changes.
- **Approval** is still unrecorded for all four designs.
