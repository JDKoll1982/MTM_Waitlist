# Tools

## read_wakatime.py / wakatime_api.py

Read coding-activity data from the local WakaTime extension install and the WakaTime API.

### What they do

- `read_wakatime.py` inspects the local WakaTime folder (`%USERPROFILE%\.wakatime`) — the
  heartbeat store format/header, available Python BDB modules, extension storage, and the
  `wakatime.log` format. Useful to verify what local data is actually parseable.
- `wakatime_api.py` reads the API key from `%USERPROFILE%\.wakatime.cfg` (never printed) and
  pulls the WakaTime dashboard summaries for `last_7_days` and `last_30_days` (daily totals,
  top projects, top languages). Requires network access to `wakatime.com`.

### Usage

```powershell
python tools/read_wakatime.py
python tools/wakatime_api.py
```

Note: the local heartbeat store is not plain SQLite/Berkeley DB, so the API is the reliable
source for aggregated stats. Keep any stats you cite rounded (the dashboard reports decimal
hours and rounded per-language minutes).

## Generate-TooltipInventory.ps1

Rerunnable, low-token tooltip inventory for actionable WinUI controls.

### What it does

1. Scans `*.xaml` for actionable controls (`Button`, `ComboBox`, `TextBox`, etc.)
2. Diffs against:
   - `Strings/en-us/TooltipResources.resw`
   - `Strings/en-us/TooltipResources.developer.resw`
3. Writes inventory artifacts under `tools/tooltip-inventory/`
4. Optionally appends **missing** resx keys
5. Optionally wires `shared:TooltipBehavior` on named controls that are not wired yet  
   (`-WireXaml` is interactive: one control at a time)

Safe defaults:

- Existing resx values are never overwritten
- Inventory mode (no switches) never modifies source
- `-WireXaml` never mass-edits; each control requires `Y`

### Usage

```powershell
# Inventory only (safe)
pwsh -File tools/Generate-TooltipInventory.ps1

# Append missing tooltip resx keys
pwsh -File tools/Generate-TooltipInventory.ps1 -ApplyMissing

# Append missing keys + wire named XAML controls interactively
pwsh -File tools/Generate-TooltipInventory.ps1 -ApplyMissing -WireXaml
```

### WireXaml prompts

For each unwired candidate the script shows file/control/key/fallback, then asks:

- `Y` / `Yes` — apply TooltipBehavior to this control only
- `N` / `No` / Enter — skip this control
- `Q` / `Quit` — stop wiring; leave remaining controls unchanged

Unnamed controls are auto-skipped (too ambiguous for safe regex edits).

### Outputs

- `tools/tooltip-inventory/tooltip-inventory.json`
- `tools/tooltip-inventory/tooltip-missing.json`
- `tools/tooltip-inventory/tooltip-inventory.csv`
- `tools/tooltip-inventory/summary.txt`

### When to run

- After adding/changing UI pages or controls
- Before PR review for tooltip coverage
- Anytime; the scan is idempotent for existing keys

## Split-InforVisualGuide.ps1

Splits the single ~10,000-line `Documents/Development/InforVisual/InforVisualGuide.md`
export into per-chapter Markdown files.

### What it does

1. Reads the guide (UTF-8) and locates every `# Chapter N: Title` heading
2. Writes one file per chapter into `Documents/Development/InforVisual/Infor Visual Guide/`
3. Splits the trailing `# Index` section into `Index.md`
4. Optionally keeps the front matter (cover image, copyright, Contents TOC) as
   `00-Front-Matter.md` when `-IncludeFrontMatter` is passed

The split is line-based and lossless: chapter bodies are copied verbatim and
headings are not renumbered. Filenames use a zero-padded chapter number and a
hyphenated title (e.g. `02-Application-Global-Maintenance.md`).

### Usage

```powershell
# Chapters + Index only (front matter skipped)
pwsh -File tools/Split-InforVisualGuide.ps1

# Also keep the cover/copyright/Contents TOC as 00-Front-Matter.md
pwsh -File tools/Split-InforVisualGuide.ps1 -IncludeFrontMatter
```

## measure-reliability-window.ps1

Measures the `specs/001` reliability criteria — **SC-007** (≥ 95 % of scheduled refresh cycles succeeding over a
30-day window, every skipped cycle attributable to a logged external-source outage) and **SC-008** (100 % of
scheduled backup windows producing a restorable artifact for every enabled store) — from a run history.

### Why it exists

The service cannot be measured as built: `RefreshRunRecordStore` and `BackupArtifactStore` keep only the **latest**
run per shape and per store, and the durable JSONL log is retained for a hard-coded **30** days — the exact period
being measured. So this script is the measurement, expressed over an append-only record shape.

### Usage

```powershell
# Seed a 30-day history and measure it
pwsh -ExecutionPolicy Bypass -File tools/measure-reliability-window.ps1 -Scenario healthy -Generate

# Measure whatever history is already in the fixture root
pwsh -ExecutionPolicy Bypass -File tools/measure-reliability-window.ps1
```

### The scenarios, each of which must fail for the stated reason

```text
healthy       29/30 = 97 %, all failures outage-attributed  -> SC-007 PASS, SC-008 PASS
marginal      28/30 = 93 %                                 -> SC-007 FAIL (rate threshold)
unattributed  97 %, one skip not outage-attributed          -> SC-007 FAIL (attribution rule)
broken        100 % refresh, one store's artifact missing   -> SC-008 FAIL
```

A scheduled interval where nothing ran leaves no record, so the window's completeness is checked as well: measure
the 30-day history left by `healthy` with `-Days 45` and it fails on **15 missing intervals** despite a 100 % success
rate.

### History format

Defaults to `%TEMP%\mtm-reliability-fixture\run-history.jsonl`; override with `-Root`. One JSON object per line,
`record` being `refresh` (per shape, per cycle, with `outcome` and `reason`) or `backup` (per store, per window,
with `outcome`, `artifactPath` and `artifactBytes`).

### What a green run does and does not mean

A seeded run proves the **metric** and that its thresholds bite. It is synthetic data and proves **nothing**
about the service's real-world reliability. Never report a seeded result as the measured outcome of SC-007/SC-008;
the real measurement needs the durable history in `specs/004-unified-card-item-picker/tasks.md` T230 and then 30
days on the host.

## run-ralph-deepseek.ps1

Runs the Spec Kit Ralph loop against **DeepSeek** by switching the Copilot CLI into BYOK (bring-your-own-model)
mode.

### Why the model cannot simply be named

DeepSeek V4.1 Flash reaches VS Code Copilot Chat through the `vizards.deepseek-v4-for-copilot` extension, which is
a **chat** model provider only — it registers nothing with the Copilot CLI, and the CLI validates `--model` against
its own server-side catalog, which has no DeepSeek entry. Verified 2026-09-20: four spellings
(`deepseek-v4-flash`, `deepseek-v4.1-flash`, `copilot/deepseek-v4-flash`, `DeepSeek V4.1 Flash`) were all rejected
with *"is not available"*. The CLI *does* accept a custom OpenAI-compatible provider through environment variables;
verified with an invalid key, it accepted `deepseek-v4-flash` and reached `https://api.deepseek.com`, failing only
with HTTP 401.

### Usage

```powershell
# Supply the key first, one of these ways (this script never prints or stores it)
$env:DEEPSEEK_API_KEY = 'sk-...'
#   or write it to a single line in %USERPROFILE%\.deepseek\api-key.txt

pwsh -ExecutionPolicy Bypass -File tools/run-ralph-deepseek.ps1 -MaxIterations 70
```

The script probes the provider with one throwaway turn before starting, so a bad key or a rejected model costs one
request instead of seventy failed iterations. Exit codes: `2` no key found, `3` provider rejected the key,
`4` model name refused, `5` empty probe.

### Caveats

BYOK bypasses GitHub authentication and bills DeepSeek for every iteration. `deepseek-v4-flash` is not a
well-known id to the CLI, so token limits do not come from a built-in catalog — pass `-MaxPromptTokens` /
`-MaxOutputTokens` to set them. The probe shows one turn succeeds; it does not prove seventy iterations of tool
calling will behave as well as on a catalog model.
