# Driving the app with UI Automation

Everything in this folder that talks to the running `MTM_Waitlist` window. One page, because the scripts only make
sense as a set: a walk needs a click, a click needs a window, and a window that has stopped answering is what half of
this folder is for detecting.

Written for the person (or agent) who has to drive the app next.

---

## 1. The shared primitives — `uia-common.ps1`

Not run directly. Dot-source it and the finding, waiting, clicking and naming behaviour is the same in every script
that does:

```powershell
. "$PSScriptRoot\uia-common.ps1"
```

| Function | What it does |
| --- | --- |
| `Get-MtmAppProcess` | The app's process, or nothing when it is not running. |
| `Get-MtmAppWindows [-TitlePattern]` | **Every** top-level window the app owns: splash, sign-in, machine set-up, shell. Which one is "main" changes as the app moves between them, so a helper that assumed one window per process loses the controls the moment one closes. |
| `Get-MtmAppRoot` | The window `MainWindowHandle` points at, or `$null`. |
| `Find-MtmById -Id` | One element by automation id. |
| `Wait-MtmById -Id [-What] [-TimeoutSeconds]` | The same, across every window, until it appears or the budget runs out. |
| `Wait-MtmWindow` | Any window at all. |
| `Wait-MtmShell [-ShellAutomationId]` | The shell, recognised by its own navigation pane. |
| `Wait-MtmUiSettled -Root [-StableMilliseconds]` | The tree has stopped changing, so the page is ready to be read. |
| `Wait-MtmSettledAnchor -AnchorId` | The named control exists **and** the page is still. Use this before auditing a page that loads asynchronously. |
| `Get-MtmElementCount -Root` | How many elements the window holds - the cheap signal the settle wait watches. |
| `Invoke-MtmElement -Element` | Presses a control: `Invoke` when it accepts one, `SelectionItem.Select()` when it is a nav entry or a row. |
| `Set-MtmText -Element -Text` | Types into a field through `ValuePattern`, so the field's own validation runs. |
| `Stop-MtmApp` | Ends the process and waits for it to actually be gone. |

### Timing, and why it is not a pile of `Start-Sleep`

Nothing in these scripts sleeps for a guessed interval. A fixed sleep is wrong in both directions: too short and the
walk reads the page before it has redrawn; too long and every step pays for the worst step in the run. The app
announces when it is done - the number of elements in the tree stops changing - so the waits poll for that at
`$MtmPollMilliseconds` (120 ms) and continue as soon as the page is still.

Three knobs, set after dot-sourcing them:

| Variable | Default | Meaning |
| --- | --- | --- |
| `$MtmPollMilliseconds` | 120 | How often the tree is looked at. |
| `$MtmStableMilliseconds` | 600 | How long the tree must stay unchanged before a page counts as settled. |
| `$MtmSettleTimeoutSeconds` | 6 | The ceiling on any one settle. |
| `$MtmWaitSeconds` | 60 | The default ceiling on "wait until this exists". |
| `$MtmShellAutomationId` | `ShellPage_Navigation` | What makes a window the shell. |

A sample count is deliberately **not** used for settling. A page that has emptied itself to rebuild is momentarily
still, so two quick identical readings call it settled and the walk reads a list that has not been drawn yet - which
is exactly what happened the first time this was written. Requiring quiet for a period does not have that hole,
because the rebuild's own changes push the deadline out.

Measured cost of the change, on this machine: a step that clicked and then waited cost **680 ms** including process
start-up, where the old flat `Start-Sleep -Milliseconds 1500` plus a three-pass evidence dump cost about 2 s. The
full walk - 29 steps over the whole surface, each ending in a tree audit - went from **497 s to about 45 s**.

---

## 2. `ui-drive.ps1` — one deliberate action per call

The general-purpose driver. Each command runs in its own PowerShell session and writes evidence for the step it
performed.

```powershell
pwsh -File tools/ui-drive.ps1 -Action launch
pwsh -File tools/ui-drive.ps1 -Action signin -Identifier JKOLL -Pin $pin -Label 02-signed-in
pwsh -File tools/ui-drive.ps1 -Action dump     -Label 03-waitlist
pwsh -File tools/ui-drive.ps1 -Action click-item   -Name '100-03'        -Label 04-workcenter
pwsh -File tools/ui-drive.ps1 -Action click-button -Name 'Add Waitlist Item' -Label 05-wizard
pwsh -File tools/ui-drive.ps1 -Action click-id -AutomationId 'ShellPage_FacilitySelectorDefault' -Label 06-facility
pwsh -File tools/ui-drive.ps1 -Action wait-for-id -AutomationId WaitlistViewPage_CardList -Label 07-cards
pwsh -File tools/ui-drive.ps1 -Action kill
```

| Action | Notes |
| --- | --- |
| `launch` | Starts the exe with the connection string taken from `MTM_WAITLIST_DB_CONNECTION_STRING` (never from a command line), then dumps the window it finds. Does nothing if the app is already running. |
| `signin` | Both stages: identifier, Continue, PIN, Sign in, then waits for the shell. |
| `dump` | Writes the window's list items, buttons, edit fields and texts to the evidence file. |
| `click-item` / `click-button` | Match by accessible name: exact first, then prefix, then contains. Pass several comma-separated candidates to try in order. |
| `click-id` | Clicks by automation id. **Necessary for a control with no accessible name**, because a name-based find cannot see one. |
| `wait-for-id` | Waits for a control, then dumps the window that owns it. |
| `kill` | Ends the process and waits for it to be gone. |

Useful switches: `-NoEvidence` skips the dump (the walk marks each click); `-WaitSeconds` raises the budget for
launch and sign-in; `-EvidenceDir` puts the evidence somewhere else; `-Name` accepts several candidates separated by
commas.

> The PIN is passed in, never defaulted. This folder is committed, and a working PIN in source is a credential in
> source. `ui-settings-drive.ps1` reads it from `MTM_WAITLIST_DEV_PIN` for the same reason.

---

## 3. `uia-surface-audit.ps1` — what a person cannot identify

Answers "which interactive controls have no accessible name", for the surface on screen **right now**.

```powershell
pwsh -File tools/uia-surface-audit.ps1                       # findings only
pwsh -File tools/uia-surface-audit.ps1 -All                  # every interactive control
pwsh -File tools/uia-surface-audit.ps1 -AnchorId WaitlistViewPage_CardList -StableMilliseconds 900
pwsh -File tools/uia-surface-audit.ps1 -Window 'Sign'        # only windows whose title matches
pwsh -File tools/uia-surface-audit.ps1 -FailOnFindings       # exit 1 when anything is unnamed
```

- A finding is decided by **capability, not control type**: anything accepting `Invoke`, `Toggle`, `SelectionItem`,
  `Value`, `RangeValue` or `ExpandCollapse` is something a person operates, and each needs a name. A type list
  always misses whichever control the framework added last. Reported as `findings : N`, and the report's
  `interactive :` line doubles as a cheap fingerprint of the page.
- Text, images, scroll bars, panes, groups, custom containers and progress rings are not findings - they are not
  operated. `-IncludeChrome` audits them anyway.
- Three kinds are reported: `unnamed`, `generic-name` (a name that only repeats the control type, like `Edit`) and
  `name-is-the-id`.
- Each finding carries the text the control itself draws. A button whose caption is a child `TextBlock` has a name
  of `''` and a caption of `Add`, and the caption is what says which control it is.
- Each finding carries its **ancestor chain**, because a finding you cannot locate in the tree is a finding nobody
  fixes.
- `-AnchorId` is what makes the audit honest on a page that loads asynchronously: "the page is still" means nothing
  until the page that was asked for has arrived.

Writes `<label>.txt` (readable) and `<label>.json` (diffable) into the evidence directory.

---

## 4. `uia-surface-walk.ps1` — the whole surface, in one run

"Give the unnamed controls a name" is only answerable against the whole surface, and the surface is not one screen:
the sign-in window, the shell's three pages, the card list, a request's detail page, every step of the new-request
wizard, and the flyouts that hang off the shell.

```powershell
$env:MTM_WAITLIST_DEV_PIN = '<pin>'
pwsh -File tools/uia-surface-walk.ps1
pwsh -File tools/uia-surface-walk.ps1 -KeepOpen -FailOnFindings
```

- **Non-mutating by construction.** Every step navigates, expands or opens. Nothing accepts, cancels, completes,
  gives back, submits or saves, so the walk can be run twice against the same store. The one exception is deliberate
  and stated in the script: it signs in, because the shell is behind the sign-in stage. The safety margin is that
  the wizard's submit button is named `Submit request`, not `Continue`, so no step can reach it.
- **Twenty-nine steps over two journeys**: sign-in, card list, detail page, the wizard's die flow (five steps), the
  wizard's hopper flow (three steps, including one answer so the flow advances), four flyouts with the flyout closed
  between them, work centre setup, and settings. Two journeys because the answer page belongs to the *item*: the die
  flow shows the die page, the hopper flow shows the hopper page.
- Each step ends in a full audit; the per-surface report lands beside the others, and the summary names every
  finding, every step that was **not reached**, and every step whose page did not change.
- **A repeated page is reported, not counted as coverage.** Each audit is reduced to the page's control signature,
  and a step whose signature matches the one before it is marked `[same page as ...]` and excluded from the distinct
  count. Without that, clicking `Continue` on a page that will not advance until an answer is chosen looks like
  three more surfaces audited - which is exactly how this walk first over-reported itself.
- A step marked `Retry = $true` is performed once more if its page did not change, for the case where a click is
  swallowed. The two header flyouts are marked this way.
- Exit `0` clean, `1` findings under `-FailOnFindings`, `2` a surface could not be reached.

Tune it with `-WorkCenter`, `-Item`, `-SecondItem` and `-Die` when the store's data differs from the defaults.

### Known limitation, and the recipe that covers it

The shell's two header flyouts - the facility selector and the signed-in-user badge - open reliably when clicked
**directly**, and not reliably from inside this ordered walk, because their flyouts are dismissed by whatever
preceded them in the same session. The walk reports them as `[same page as ...]` rather than pretending otherwise.
Audit them on their own:

```powershell
pwsh -File tools/ui-drive.ps1 -Action click-item -Name 'Waitlist' -NoEvidence
pwsh -File tools/ui-drive.ps1 -Action click-id -AutomationId 'ShellPage_FacilitySelectorDefault' -NoEvidence
pwsh -File tools/uia-surface-audit.ps1 -Label 20-facility-flyout       # ListItem count rises by one while it is open

pwsh -File tools/ui-drive.ps1 -Action click-id -AutomationId 'ShellPage_CurrentUserButtonDefault' -NoEvidence
pwsh -File tools/uia-surface-audit.ps1 -Label 21-user-flyout           # a MenuItem appears for Sign out
```

The other three wizard answer pages (coil turn, part search, skid count) are reached through *their* items'
flows, which this walk does not visit; they were checked by reading their markup, where no interactive control is
without a declared name.

---

## 5. `uia-invoke.ps1` — reach a control that has no name

The escape hatch, and the reason the audit above exists. It finds elements by **capability** (do they accept
`Invoke`?) instead of by name, so an unnamed button is still reachable.

```powershell
pwsh -File tools/uia-invoke.ps1 -Label 10-invokables              # list them, with indexes
pwsh -File tools/uia-invoke.ps1 -Invoke -Index 7 -Label 11-submit  # invoke index 7, then dump
```

A control that only a numbered index can reach is a control that needs a name.

---

## 6. `ui-settings-drive.ps1` — does the app survive Settings?

A cold-launch walk to the Settings page, written when visiting Settings was crashing the app. Its job is the verdict
at the end.

```powershell
$env:MTM_WAITLIST_DEV_PIN = '<pin>'
powershell -NoProfile -ExecutionPolicy Bypass -File tools/ui-settings-drive.ps1
```

- Exit `0` the app was still running **and answering**; `1` it died or stopped answering; `2` the walk never reached
  Settings.
- The soak after selecting Settings is **watched rather than slept through**: the app is asked every 120 ms whether
  it is still there and still responding, so a crash or a hang is reported when it happens instead of after the
  period. Under `-SettleSeconds` (default 6).
- A window that exists but does not answer is reported as **HUNG**, not ALIVE. That distinction is the whole point:
  an app with a frozen UI thread still answers `Get-Process` and still reports its last good text.
- Do not override the connection string for this walk. `appsettings.json` names the shared store, and pointing it at
  a local database that does not exist makes the launch decide the computer is unconfigured - so the app opens the
  machine-set-up window instead of sign-in, and the walk fails for a reason that has nothing to do with Settings.

---

## 7. `uia-inspect-control.ps1` — hover a control and open the developer inspector

The control inspector is armed by a **hover**: `Ctrl+Alt+Right-Click` over a tracked control opens a window with
that control's tooltip, workflow and associated source files. A hover cannot be produced through the automation
tree's patterns (there is no `Invoke` for a hover, and the tooltip is not an element that can be pressed), so this
script drives the **real pointer** — `SetCursorPos`, a mouse-move, and a real modifier right-click.

```powershell
pwsh -File tools/uia-inspect-control.ps1 -Name 'Select the active facility'      # hover and read the tooltip
pwsh -File tools/uia-inspect-control.ps1 -AutomationId 'ShellPage_FacilitySelector' -Shortcut -KeepOpen
```

- Exit `0` the sequence ran (with `-Shortcut`, the inspector's window appeared); `1` the shortcut was asked for and
  no window appeared; `2` the control was not on screen.
- The application is brought to the front first. A window that sits behind another one still reports valid
  rectangles, so every hover would land silently on whatever is on top — which looks exactly like a broken inspector.
  Only a *minimised* window is restored: restoring a maximised one resizes it, and the rectangles read a moment
  earlier would then be stale.
- The pointer is walked across the control's rectangle rather than dropped on its centre, because a control can be
  clipped or laid out past the edge of the window. **The tooltip opening is the proof the pointer arrived** — it is
  the application saying so.
- `-KeepOpen` leaves the inspector's window up, and its text is dumped to `%TEMP%\die-walk\inspect-shortcut.txt`.

### Two platform facts this script exists to record

1. **`SetCursorPos` alone does not hover.** It moves the pointer without posting a mouse message, and a WinUI control
   only learns it is being hovered from one. The pointer has to be nudged afterwards (`mouse_event` with a one-pixel
   move and back), or the pointer sits over the control while the application behaves as though it were elsewhere.
2. **A point cannot be hit-test verified.** `AutomationElement.FromPoint` answers "a pane" for WinUI content whether
   the point is over a button or over empty space, because the whole content is hosted as one island. A check that
   used it to confirm the aim would reject good points and accept nothing, so the aim is judged by the tooltip
   instead.

A synthesised hover still does **not** reliably raise a tooltip in this app (a real mouse does), so the shortcut leg
can report `did NOT open` on a machine where the inspector itself is fine. When that happens, do not read it as a
broken inspector: the failure is at the hover, one step earlier, and the script says so rather than reporting a pass.
To see the window itself without the hover, a temporary boot hook that resolves `IControlInspectorDetailPresenter`
and calls `Present(...)` with a sample detail opens it at launch — that is how the window was confirmed to render,
and the hook must be removed afterwards.

---

## 8. Looking at the window

| Script | Use |
| --- | --- |
| `capture_app_window.ps1 [-WaitSeconds] [-Maximize]` | Screenshots the shell window to `%TEMP%\mtm_list.png`. Understands `PrintWindow` with `PW_RENDERFULLCONTENT`: `BitBlt` returns blank for WinUI content. Polls for the window instead of sleeping. |
| `capture_app_region.ps1` | Captures a rectangle of the window, for the case where the whole window is too much to read. |
| `ocr_png.ps1 -Path <png>` | Reads text out of a capture with `Windows.Media.Ocr`, so a screenshot can be checked for a *value* rather than eyeballed. |

Screenshots are the slowest way to answer a question that the automation tree can answer directly; reach for
`-Action dump` or an audit first.

---

## 9. `measure-ui-responsiveness.ps1` — is the window actually frozen?

Measures **how long the UI thread is unresponsive**, which is a different question from how long an action takes.

```powershell
pwsh -File tools/measure-ui-responsiveness.ps1 -Seconds 15
pwsh -File tools/measure-ui-responsiveness.ps1 -Seconds 25 -Trigger { <drive the window> }
```

The window is asked to acknowledge a null message every `-IntervalMilliseconds` (25 ms) with a `-TimeoutMilliseconds`
(80 ms) deadline. An acknowledgement that does not come back inside it means the thread was busy and could not
process input: that is a frozen window, whatever is holding the thread.

Why this rather than a stopwatch around the click: an action that writes to the store and then rebuilds a list has
two parts and only one of them is a hang. Timing the whole action conflates a fast write with a slow rebuild, and it
cannot tell a busy thread from one merely awaiting a network call.

It has already settled one argument in this repo: a "5-10 second hang after a status change" measured **zero**
unresponsive samples across a 3718 ms rebuild, which established that the UI thread never froze and that the work
belonged in the card rebuild, not in the thread.

---

## 9. The order to reach for these

1. **`uia-surface-audit.ps1`** while looking at the screen in question - is anything here unnameable?
2. **`ui-drive.ps1 -Action dump`** to see what the automation tree actually holds for a step you are writing.
3. **`uia-invoke.ps1`** when the control you need has no name, so its index is the only handle on it.
4. **`uia-surface-walk.ps1`** to prove the whole surface at once, and to re-prove it after a change.
5. **`measure-ui-responsiveness.ps1`** when the complaint is about time rather than about identity.
6. **`ui-settings-drive.ps1`** when the complaint is that a page kills the app.

## 10. Two traps worth knowing before you write a step

- **A control found by `FindAll` goes stale the moment the page rebuilds.** Re-look it up after anything that
  changes the page; polling a stale element is how a walk reports a control that is plainly on screen.
- **`ControlType` is returned as a fresh instance on every read**, so comparing control types by instance never
  matches. Compare `ProgrammaticName`, as `uia-surface-audit.ps1` does.
