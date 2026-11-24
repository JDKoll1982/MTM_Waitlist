# UI designs — how the mockup pipeline works (2026-11-24)

## What exists

`docs/ui-designs/` is the second half of the visual documentation set. `docs/diagrams/` says what a
workflow does; `docs/ui-designs/` shows what a screen looks like now and what it should look like after
a QOL change. It holds:

| Path | What it is |
| --- | --- |
| `ui-designs-guide.md` | The method, the visual standard, the accuracy rules |
| `ui-designs-01…08-*.md` | One prompt per image: before, after, six states, workflow diagram |
| `ui-designs-method-check.md` | What was run end to end, and the two constraints it exposed |
| `tools/render_mockup.py` | Headless Edge → PNG, canonical images, comparison, approval |
| `tools/verify_designs.py` | Completeness, workflow references, proposal sections, approval hashes |
| `tools/mockup/` | `tokens.css` (values named to their XAML resource) and `mockup.css` |
| `tools/template/` | `design.json`, `before.html`, `after.html`, `proposal.md` |

## The two constraints that cost a debugging session

1. **Edge resolves `--screenshot` against its own working directory.** A relative design path used to
   make every render fail with "Edge produced no image". `render_page` now resolves the output path
   before Edge sees it; verified by rendering from a relative `--design` path.
2. **A state that is not the mockup's anchor id renders `default` silently.** The page is loaded at
   `#<name>`, and the stylesheet keys on `#state-<word>`. The renderer now rejects a state name that
   does not begin `state-`, and `build()` fails when a state's PNG is byte-identical to `default` at the
   same viewport. That second check is the one that would have caught the first.

## Other traps worth knowing

- **Edge never exits.** It writes the image and keeps the browser process alive, so `subprocess.run`
  hangs. The renderer starts it, waits for the file size to settle, then kills the process.
- **A `.dot` file is build output.** The design record's `design_system.resources` names the XAML the
  tokens come from; the renderer hashes those files, and a change to `Styles/*.xaml` after approval
  invalidates the approval instead of silently leaving a stale palette.
- **OCR needs Windows PowerShell 5.1 and an absolute path.** `scripts/ui-automation/ocr_png.ps1` fails
  under `pwsh` 7 (WinRT types) and fails on a relative path. Expect `1` → `I`, callout digits → `O`/`0`.
- **The comparison is generated HTML.** `mockups/<refactor>-compare.html` is rewritten on every render
  from the design record's callouts; it is output, not a source, and is not part of the approval hash.

## Verification

```powershell
python -m unittest discover -s docs\ui-designs\tools -p "test_*.py"
python docs\ui-designs\tools\verify_designs.py --all --require-approval
```

The suite covers the verifier's failure paths against fixtures, and the renderer against headless Edge
(when Edge is present). A design is only "approved" when `approval.sources_sha256` still matches the
mockups and shared assets; editing a mockup after approval makes the verifier fail by design.

## Boundary

`docs/diagrams/` remains the only source for workflow structure. A design cites `workflow_refs`
(sheet + node ids) and the verifier resolves them through `diagram_context`; it never restates the flow.
No application `.cs` or `.xaml` file is changed by producing or checking a design.
