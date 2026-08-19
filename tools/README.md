# Tools

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
