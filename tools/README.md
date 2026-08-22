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
