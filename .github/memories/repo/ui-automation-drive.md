# Driving MTM_Waitlist with Windows UI Automation — what breaks, and the fixes

The playbook for the scripts in `scripts/ui-automation/`. Instructions live in
`.github/instructions/winui3-ui-automation.instructions.md`; the per-script documentation lives in
`docs/scripts/scripts-automation-ui-automation.md`. This note is the *experience* layer: the roadblocks that
actually stopped driven runs, what the symptom looked like, and what was changed. Read it before debugging a driven
run, because every entry below reads like the app misbehaving when it is the driver.

Recorded 2026-10-19 while verifying feature 012 (`specs/012-waitlist-card-layout`) on the home workstation
(`%USERNAME% = johnk`).

## The five-minute recipe that works here

```powershell
$d = 'scripts\ui-automation\ui-drive.ps1'
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Action launch
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Action signin -Identifier $env:USERNAME -Pin <pin>
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Action click-button -Name 'Show only my requests'
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Action dump -Label 01-waitlist
powershell -NoProfile -ExecutionPolicy Bypass -File $d -Action kill
```

- **`powershell -NoProfile -File …` works, and `-ExecutionPolicy Bypass` is optional here.** Measured on
  2026-10-19: every scope reads `Undefined` except `LocalMachine = RemoteSigned`, so a local, unsigned script in this
  repo runs without the switch. The repo's other notes say the effective policy is `Restricted`; that is wrong for
  this machine. The switch is kept in commands because it is harmless and it is what the rest of the repo's guidance
  says to use.
- **Both hosts work.** Every run on 2026-10-19 used `powershell -NoProfile -File …` (Windows PowerShell 5.1), and
  `pwsh -NoProfile -File …` (7.x) was exercised on the same day for launch and sign-in and behaved identically. Use
  whichever the command block you are copying from shows.
- The app is **single-instance**: `-Action launch` prints "already running" rather than starting a second window,
  so anything that needs a cold start is `-Action kill` first.
- The **waitlist filter matters**. The shell opens with "Show only my requests" ON, and on this store that reads
  `No Requests Right Now` for an account with no open work. Toggle it before concluding the list is empty.
- **Close the app when finished** so no orphan process holds store connections.

## Roadblocks

### 1. `-Action launch` could not find the executable at all

**Symptom:** `Executable not found: C:\..\..\bin\x64\Debug\...\MTM_Waitlist.exe` — or, in an emptied form,
`Test-Path : The path 'C:\..\..\bin\...' referred to an item that was outside the base 'C:'`.

**Cause:** `-Exe` defaulted to `"$PSScriptRoot\..\..\bin\..."` **inside the param block**. `$PSScriptRoot` is not
populated when a parameter default is evaluated in this host, so the path collapsed to `C:\..\..\`.

**Fix:** `-Exe = ''` in the param block, resolved after dot-sourcing `uia-common.ps1` from `$PSScriptRoot`. Watch
the level count: `$PSScriptRoot` is `<repo>\scripts\ui-automation`, so **two** parents reach the repo. Using
`$PSCommandPath` instead needs **three**, because the first parent is the folder.

`ui-settings-drive.ps1` and `uia-surface-walk.ps1` had already learned this and carried a comment saying so; the
driver had not.

### 2. A missing connection string aborted the launch

**Symptom:** `No connection string. Pass -ConnectionString or set MTM_WAITLIST_DB_CONNECTION_STRING.`

**Cause:** the driver threw whenever neither was present, on the assumption the machine carried the variable at user
scope. It does not here — `HKCU:\Environment` holds no `MTM*` entry at all, and `%APPDATA%`'s profile change between
work (`jkoll`) and home (`johnk`) is why the note that said otherwise was wrong.

**Fix:** the override is optional. When neither `-ConnectionString` nor the environment variable is set, the app
resolves its own store from `appsettings.json` and the driver says so. The app reaching the store is visible in the
result: it gets past the store-backed sign-in to the shell.

### 3. The walk signed in as an account the store does not hold

**Symptom:** `The shell ('ShellPage_Navigation' never appeared) did not come up within 120 s.` after a normal
`launching` → `signing in` sequence, which reads exactly like the app failing to start.

**Cause:** `-User` defaulted to a literal `JKOLL` (`uia-surface-walk.ps1`) and `jkoll` (`ui-settings-drive.ps1`).
The store on this workstation holds `JOHNK`. The walk typed an identifier the store refuses and then waited two
minutes for a shell that could never appear.

**Fix:** `-User` defaults to `$env:MTM_WAITLIST_DEV_USER`, then to `$env:USERNAME`; `-User` still overrides both.
A sign-in that does not reach the shell now throws a message naming the account it tried and both ways to change it.

### 4. `Invoke-MtmElement` refused WinUI's own toggle buttons

**Symptom:** `The button 'Show only my requests' ('Show only my requests', ControlType.Button) accepts neither
Invoke nor SelectionItem.`

**Cause:** the toggle arm was missing. WinUI's header switches (`ShellPage_MyRequestsToggleDefault`,
`ShellPage_SortButtonDefault`) advertise **`TogglePattern`** and nothing else, so a control that is plainly
operable was reported as unreachable.

**Fix:** `Invoke-MtmElement` tries `Invoke`, then `SelectionItem.Select()`, then `Toggle()`, and its message lists
all three.

### 5. A page's controls were reported missing while the page was still drawing

**Symptom:** `No ListItem matched [Pickup]. Available: Waitlist | Work Center Setup | Settings` immediately after
clicking a work centre — on a category page whose `Pickup` card appeared a fifth of a second later.

**Cause:** `Find-Element` looked once. A wizard step draws its cards after the page that hosts them.

**Fix:** `click-item` and `click-button` now go through `Wait-Element`, polling the same find at
`$MtmPollMilliseconds` for up to `-FindSeconds` (default 10) and only then reporting what *is* available. The
failure message now says "within 10 s", so a genuine absence is distinguishable from a slow draw.

### 6. A typed number was ignored by the control that needed it

**Symptom:** the skid-count field was written through `ValuePattern` and the request was submitted carrying `1`
— the default — with nothing anywhere reporting a problem.

**Cause:** `Set-MtmText` wrote through `ValuePattern`. `# of Skids` is a **`NumberBox`**, which parses its own text
on commit. Setting the value model produced no keystroke, and an `Invoke` on the next button does not move focus,
so nothing ever committed — the page submitted the number it had parsed, which was still the default `1`.

**Fix:** `Set-MtmText` **types first** (focus → `Ctrl+A` → `{DEL}` → characters via `SendKeys`) and keeps
`ValuePattern` as the fallback, then reads the value back and warns rather than passing silently. `-Commit` adds the
`{TAB}` a person's own click would produce; use it for parse-on-commit controls. A **password field reports
`●●●●`**, so the read-back accepts a mask whose length matches what was typed — otherwise every correct PIN
produced a false "could not put it in" warning and a pointless `SetValue` on top of it.

Verified end to end: `# of Skids` = 4 typed → stored as `{"# of Skids": "4"}` → card drew `# of Skids | 4`.

### 7. The PIN is not on this machine

`MTM_WAITLIST_DEV_PIN` is unset, and it must stay out of the repo. Pass `-Pin` per run (or set the variable in the
session only). A refused credential leaves the app on the sign-in stage — with three failures the store also makes
the account set a new password — so treat "shell never appeared" as a credential problem first, and re-read the
sign-in window with `-Action dump` before trying again.

### 8. Reading the store after a driven action

The app's `appsettings.json` names the plant host, but from the home workstation `mysql.exe` **cannot reach**
`172.16.1.104` (error 2003) while a local server holds the same database names. Read the local one:

```powershell
$env:MYSQL_PWD = 'root'
& 'C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe' -h 127.0.0.1 -u root `
    -e "SELECT id, work_center, item, status, handler_status, answers_json FROM mtm_waitlist.waitlist_requests_queue ORDER BY id DESC LIMIT 5;"
```

`waitlist_requests_queue` carries `handler_status` (the condition) and `answers_json` (the answers), which is how a
driven flow is proven to have reached the store rather than only the screen. `mtm_mock` holds the cached Infor
Visual mirror the app falls back to — including `visual_disposition_input_result`, which is what decides whether a
job is classified Outside Service / Finished Goods / WIP.

### 9. Not every failure in a walk is a failure

`uia-surface-walk.ps1` reports `NOT REACHED` for steps whose assumptions no longer hold on the current data — on
2026-10-19 that was `17-wizard-pick` (`no list item matched [Die]`), `18-wizard-tiles` (`FGT0279-02`) and
`19-wizard-summary` / `24c-wizard2-next` (`no button matched [Continue]`). The walk still audited 25 of 29 surfaces
with zero unnamed controls, which is its actual job. Read the summary, not the "not reached" list alone, and treat
the items/work-centre names as examples that need refreshing rather than as defects.

### 10. XamlMcp still cannot attach — and is not even registered here

`xamlmcp` is **not in `.vscode/mcp.json`** on this workstation (only `csv-mcp-server` is), and the in-process agent
is absent from the repo (no `PackageReference`, no `XamlMcp.WinUI.Attach()` in `App.xaml.cs`). The CLI still exists at
`%USERPROFILE%\.dotnet\tools\xamlmcp.exe` and `check --json` reports `0 live app(s)`, which is expected rather than a
fault. Windows UI Automation is the working path — do not spend a session trying to make the inspector attach. Full
detail in `.github/copilot-instructions.md`.

## Verification status of the fixes

- The full surface walk was re-run after every change: **signed in, audited 25 of 29 surfaces, 0 findings**, and
  the app closed itself. That is the regression gate for these scripts.
- T058 of feature 012 was verified with them: condition chosen → `handler_status` in the store → column colour
  `#B02418` → `#C0392B` on word and ring → replacement → restored after reload and after restart, and all four
  conditions surviving a restart across four rows (SC-016).
