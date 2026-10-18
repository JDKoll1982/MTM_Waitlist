# Scripts

Everything runnable that is not part of the build lives under `scripts/`, one folder per kind of work. Nothing here
ships with the application — the app's own behaviour never depends on these files.

| Folder | What it holds |
|---|---|
| [`activity/`](../../scripts/activity/) | `read_wakatime.py`, `wakatime_api.py` — read coding-activity data. |
| [`assets/`](../../scripts/assets/) | `Generate-TooltipInventory.ps1`, `New-AppIcons.ps1` — generate committed asset and string artifacts. |
| [`build/`](../../scripts/build/) | `run-tests.ps1`, `investigate-tests.ps1`, `kill-tests.ps1`, `bisect-tests.ps1`, `check-build.ps1`, `show-app-version.ps1` — build and test the solution. |
| [`database/`](../../scripts/database/) | `Move-PartPictureLayout.ps1` — the one-time picture-share move. |
| [`diagnostics/`](../../scripts/diagnostics/) | `capture-debug-output.ps1`, `dump-wer.ps1` / `dump-wer.cmd`, `show-blame.ps1` — capture and read what the app and the test host report. |
| [`service/`](../../scripts/service/) | `measure-reliability-window.ps1` — measure the Mock service reliability criteria. |
| [`spec-kit/`](../../scripts/spec-kit/) | `companion-clear-concerns.py`, `companion-open-tasks.py`, `run-ralph-deepseek.ps1` — work with the Spec Kit Companion and the Ralph loop. |
| [`ui-automation/`](../../scripts/ui-automation/) | Driving the running app through UI Automation; documented on its own page. |

Every script resolves the repository from its own location, so all of them run from any working directory.

## Retired scripts

These did their work and were removed rather than left to rot beside the live ones. Each is recoverable from git
history if its job ever comes back.

| Script | Why it is gone |
|---|---|
| `gen_mock_sp.ps1` | Generated the `sp_mock_*` procedures. Those mock master tables are retired, and `sp_mock_` is now a build-failing retired symbol. |
| `migrate-logging-callsites.ps1` | Performed the `StartupDebugLog` → `AppLog` call-site migration. The old type is a retired symbol with zero call sites. |
| `scan_workstation_rename.ps1` | Drove the Workstation → Computer / Work Center rename. No `workstation` occurrence remains in code or markup. |
| `probe-dunnage-folders.ps1` | One-off measurement taken for a closed defect; its numbers are recorded in that defect's file. |
| `finish-ralph-run.ps1` | Finished one specific Ralph run whose three remaining tasks (T165, T184, T188) are all closed. |
| `Split-InforVisualGuide.ps1` | Split the Infor VISUAL user guide. The split is committed under `docs/reference/` and the source export is not in the repository. |
| `labelview_lbl_inspect.py` | Inspected a `.lbl` sample that is no longer in the repository; the label printing integration is not implemented. |


## UI automation

Driving the running app with UI Automation — auditing which controls a person cannot identify, clicking, reading the
automation tree, capturing the window, and measuring whether it has actually frozen — has its own page:
[scripts-automation-ui-automation.md](./scripts-automation-ui-automation.md).

Start there for `uia-common.ps1`, `ui-drive.ps1`, `uia-surface-audit.ps1`, `uia-surface-walk.ps1`,
`uia-invoke.ps1`, `ui-settings-drive.ps1`, `capture_app_window.ps1`, `capture_app_region.ps1`, `ocr_png.ps1` and
`measure-ui-responsiveness.ps1`, all in `scripts/ui-automation/`.

## Build and test — `scripts/build/`

| Script | What it does |
|---|---|
| `run-tests.ps1` | The standard suite runner. `-All` for the whole suite, `-Modules a,b` to filter, `-LogPath` to keep the output. Uses the same stale-process guard as the editor tasks. |
| `investigate-tests.ps1` | Reports how far a run got, from the log `run-tests.ps1` writes. Use it when a run stops early or a module hangs. |
| `kill-tests.ps1` | Stops `testhost`, `vstest.console` and test-hosting `dotnet` processes and reports what it killed. |
| `bisect-tests.ps1` | Runs the suite module by module to find which module carries a failure or a hang. |
| `check-build.ps1` | One build with the same switches the editor task uses, reduced to the lines that matter. |
| `show-app-version.ps1` | Prints the built app's version and file version, read from the assembly. |

```powershell
pwsh -File scripts/build/run-tests.ps1 -All -LogPath .specify\suite.txt
pwsh -File scripts/build/investigate-tests.ps1 -LogPath .specify\suite.txt
```

## Diagnostics — `scripts/diagnostics/`

| Script | What it does |
|---|---|
| `capture-debug-output.ps1` | Listens on the platform debug channel (`DBWIN_BUFFER` — what `OutputDebugString` writes to) and records every line with the writing process id. Start the capture **before** the app. `-Emit` proves the channel is live; `-Summarise` reads a capture back. |
| `dump-wer.ps1` / `dump-wer.cmd` | Collects Windows Error Reporting records for the app into `.specify\wer.txt`. The `.cmd` is the thin wrapper the editor task calls. |
| `show-blame.ps1` | Prints the blame-hang artifacts a timed-out test run left behind. |

## Activity — `scripts/activity/`

Read coding-activity data from the local WakaTime extension install and the WakaTime API.

- `read_wakatime.py` inspects the local WakaTime folder (`%USERPROFILE%\.wakatime`) — the
  heartbeat store format/header, available Python BDB modules, extension storage, and the
  `wakatime.log` format. Useful to verify what local data is actually parseable.
- `wakatime_api.py` reads the API key from `%USERPROFILE%\.wakatime.cfg` (never printed) and
  pulls the WakaTime dashboard summaries for `last_7_days` and `last_30_days` (daily totals,
  top projects, top languages). Requires network access to `wakatime.com`.

```powershell
python scripts/activity/read_wakatime.py
python scripts/activity/wakatime_api.py
```

The local heartbeat store is not plain SQLite/Berkeley DB, so the API is the reliable source for aggregated stats.
Keep any stats you cite rounded (the dashboard reports decimal hours and rounded per-language minutes).

## Assets — `scripts/assets/`

### Generate-TooltipInventory.ps1

Rerunnable, low-token tooltip inventory for actionable WinUI controls.

#### What it does

1. Scans `*.xaml` for actionable controls (`Button`, `ComboBox`, `TextBox`, etc.)
2. Diffs against:
   - `Strings/en-us/TooltipResources.resw`
   - `Strings/en-us/TooltipResources.developer.resw`
3. Writes inventory artifacts under `scripts/assets/tooltip-inventory/`
4. Optionally appends **missing** resx keys
5. Optionally wires `shared:TooltipBehavior` on named controls that are not wired yet
   (`-WireXaml` is interactive: one control at a time)

Safe defaults:

- Existing resx values are never overwritten
- Inventory mode (no switches) never modifies source
- `-WireXaml` never mass-edits; each control requires `Y`

#### Usage

```powershell
# Inventory only (safe)
pwsh -File scripts/assets/Generate-TooltipInventory.ps1

# Append missing tooltip resx keys
pwsh -File scripts/assets/Generate-TooltipInventory.ps1 -ApplyMissing

# Append missing keys + wire named XAML controls interactively
pwsh -File scripts/assets/Generate-TooltipInventory.ps1 -ApplyMissing -WireXaml
```

#### WireXaml prompts

For each unwired candidate the script shows file/control/key/fallback, then asks:

- `Y` / `Yes` — apply TooltipBehavior to this control only
- `N` / `No` / Enter — skip this control
- `Q` / `Quit` — stop wiring; leave remaining controls unchanged

Unnamed controls are auto-skipped (too ambiguous for safe regex edits).

#### Outputs

- `scripts/assets/tooltip-inventory/tooltip-inventory.json`
- `scripts/assets/tooltip-inventory/tooltip-missing.json`
- `scripts/assets/tooltip-inventory/tooltip-inventory.csv`
- `scripts/assets/tooltip-inventory/summary.txt`

The folder is gitignored: it is generated, and `RetiredSymbolAuditTests` excludes it from the retired-symbol scan for
that reason.

#### When to run

- After adding/changing UI pages or controls
- Before PR review for tooltip coverage
- Anytime; the scan is idempotent for existing keys

### New-AppIcons.ps1

Builds a multi-resolution `.ico` from a square source PNG, writing PNG-payload frames for every size (Vista+). Windows
Explorer, the taskbar and WinUIEx all read PNG-payload frames, so this is how the app icon is produced.

```powershell
pwsh -File scripts/assets/New-AppIcons.ps1 -Source <square.png> -Destination Assets/MTM_Waitlist.ico
```

## Database — `scripts/database/`

### Move-PartPictureLayout.ps1

The **file half** of the picture-layout change (feature 008, FR-036). It moves the pictures already stored on the
picture share into the application's own collection, one folder per kind:

```text
<root>\work_center\...        ->  <root>\Waitlist\work_center\...
<root>\request_item\...       ->  <root>\Waitlist\request_item\...
<root>\request_category\...   ->  <root>\Waitlist\request_category\...
```

The row half is `sp_config_images_locations_paths_move`, which rewrites each recorded path. Either order is safe,
because the reader resolves both layouts. Run this by hand, read the report, then run the row half — **never** from
application startup and never from the build. A second run finds nothing to move.

## Mock service — `scripts/service/`

### measure-reliability-window.ps1

Measures the `specs/001` reliability criteria — **SC-007** (at least 95 % of scheduled refresh cycles succeeding over
a 30-day window, every skipped cycle attributable to a logged external-source outage) and **SC-008** (100 % of
scheduled backup windows producing a restorable artifact for every enabled store) — from a run history.

#### Why it exists

The service cannot be measured as built: `RefreshRunRecordStore` and `BackupArtifactStore` keep only the **latest**
run per shape and per store, and the durable JSONL log is retained for a hard-coded **30** days — the exact period
being measured. So this script is the measurement, expressed over an append-only record shape.

#### Usage

```powershell
# Seed a 30-day history and measure it
pwsh -ExecutionPolicy Bypass -File scripts/service/measure-reliability-window.ps1 -Scenario healthy -Generate

# Measure whatever history is already in the fixture root
pwsh -ExecutionPolicy Bypass -File scripts/service/measure-reliability-window.ps1
```

#### The scenarios, each of which must fail for the stated reason

```text
healthy       29/30 = 97 %, all failures outage-attributed  -> SC-007 PASS, SC-008 PASS
marginal      28/30 = 93 %                                 -> SC-007 FAIL (rate threshold)
unattributed  97 %, one skip not outage-attributed          -> SC-007 FAIL (attribution rule)
broken        100 % refresh, one store's artifact missing   -> SC-008 FAIL
```

A scheduled interval where nothing ran leaves no record, so the window's completeness is checked as well: measure
the 30-day history left by `healthy` with `-Days 45` and it fails on **15 missing intervals** despite a 100 % success
rate.

#### History format

Defaults to `%TEMP%\mtm-reliability-fixture\run-history.jsonl`; override with `-Root`. One JSON object per line,
`record` being `refresh` (per shape, per cycle, with `outcome` and `reason`) or `backup` (per store, per window,
with `outcome`, `artifactPath` and `artifactBytes`).

#### What a green run does and does not mean

A seeded run proves the **metric** and that its thresholds bite. It is synthetic data and proves **nothing**
about the service's real-world reliability. Never report a seeded result as the measured outcome of SC-007/SC-008;
the real measurement needs the durable history in `specs/004-unified-card-item-picker/tasks.md` T230 and then 30
days on the host.

## Spec Kit — `scripts/spec-kit/`

### companion-clear-concerns.py / companion-open-tasks.py

Two workarounds for gaps in the Companion writer:

- `companion-clear-concerns.py` moves a dealt-with concern out of `.spec-context.json` into the neighbouring
  `concerns-log.md`, recording the concern in its own words plus the reason it was cleared. `--list` shows the current
  list; `--entry "<number>|<reason>"` clears one or more.
- `companion-open-tasks.py` adds or refreshes one `task_summaries` entry per **open** task read from `tasks.md`, so the
  Tasks card stops reporting a finished-only count. Idempotent; rerun whenever `tasks.md` changes.

```powershell
python scripts/spec-kit/companion-clear-concerns.py --list
python scripts/spec-kit/companion-open-tasks.py
```

Both read and write through the Companion's own store module, so `.spec-context.json` is written exactly as the
sanctioned writer writes it.

### run-ralph-deepseek.ps1

Runs the Spec Kit Ralph loop against **DeepSeek** by switching the Copilot CLI into BYOK (bring-your-own-model)
mode.

#### Why the model cannot simply be named

DeepSeek V4.1 Flash reaches VS Code Copilot Chat through the `vizards.deepseek-v4-for-copilot` extension, which is
a **chat** model provider only — it registers nothing with the Copilot CLI, and the CLI validates `--model` against
its own server-side catalog, which has no DeepSeek entry. Verified 2026-09-20: four spellings
(`deepseek-v4-flash`, `deepseek-v4.1-flash`, `copilot/deepseek-v4-flash`, `DeepSeek V4.1 Flash`) were all rejected
with *"is not available"*. The CLI *does* accept a custom OpenAI-compatible provider through environment variables;
verified with an invalid key, it accepted `deepseek-v4-flash` and reached `https://api.deepseek.com`, failing only
with HTTP 401.

#### Usage

```powershell
# Supply the key first, one of these ways (this script never prints or stores it)
$env:DEEPSEEK_API_KEY = 'sk-...'
#   or write it to a single line in %USERPROFILE%\.deepseek\api-key.txt

pwsh -ExecutionPolicy Bypass -File scripts/spec-kit/run-ralph-deepseek.ps1 -MaxIterations 70
```

The script probes the provider with one throwaway turn before starting, so a bad key or a rejected model costs one
request instead of seventy failed iterations. Exit codes: `2` no key found, `3` provider rejected the key,
`4` model name refused, `5` empty probe.

#### Caveats

BYOK bypasses GitHub authentication and bills DeepSeek for every iteration. `deepseek-v4-flash` is not a
well-known id to the CLI, so token limits do not come from a built-in catalog — pass `-MaxPromptTokens` /
`-MaxOutputTokens` to set them. The probe shows one turn succeeds; it does not prove seventy iterations of tool
calling will behave as well as on a catalog model.