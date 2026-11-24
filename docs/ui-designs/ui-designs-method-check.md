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
